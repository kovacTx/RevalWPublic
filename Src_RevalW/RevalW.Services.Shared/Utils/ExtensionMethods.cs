using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevalW.Services.Shared.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RevalW.Services.Shared.Utils;

public static partial class ExtensionMethods
{
    public const long DEFAULT_ORG_ID = 1;
    private static readonly Dictionary<string, EItemStatus> ItemStatusToEnum = new()
        {
            { nameof(EItemStatus.New), EItemStatus.New },
            { nameof(EItemStatus.Exported), EItemStatus.Exported },
            { nameof(EItemStatus.Imported), EItemStatus.Imported },
            { nameof(EItemStatus.Finished), EItemStatus.Finished },
            { nameof(EItemStatus.Deleted), EItemStatus.Deleted },
        };
    public static string ToStringExt(this EItemStatus status)
    {
        if (!ItemStatusToEnum.ContainsValue(status)) return string.Empty;
        return ItemStatusToEnum.FirstOrDefault(kvp => kvp.Value == status).Key;
    }
    public static bool IsLogged(this ControllerBase controller)
        => controller.User?.Identity is not null && controller.User.Identity.IsAuthenticated;
    public static string CurrentUserId(this ControllerBase controller)
        => CurrentUserId(controller.User);
    public static string CurrentUserId(this ClaimsPrincipal? user)
        => user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    public static string? CurrentToken(this ControllerBase controller)
    {
        var auth = controller.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
            return null;
        return auth["Bearer ".Length..].Trim();
    }
    public static long CurrentOrgId(this ControllerBase controller)
    {
        if (!long.TryParse(controller.User?.FindFirst(ClaimNames.OrgId)?.Value ?? string.Empty, out var ret))
            return DEFAULT_ORG_ID;
        else
            return ret;
    }
    public static bool IsUberAdmin(this ControllerBase controller)
        => controller.User.IsInRole(ERoles.UberAdmin.ToUpper());
    public static bool IsCreator(this ControllerBase controller)
        => controller.User.IsInRole(ERoles.Creator);
    public static bool IsAuthorized(this ControllerBase controller, (string, string) right)
        => IsAuthorized(controller.User, right.Item1);
    public static bool IsAuthorized(this ControllerBase controller, string right)
        => IsAuthorized(controller.User, right);
    public static bool IsAuthorized(this ClaimsPrincipal user, (string, string) right)
        => IsAuthorized(user, right.Item1);
    public static bool IsAuthorizedInAll(this ClaimsPrincipal user, IEnumerable<(string, string)> rights)
        => IsAuthorizedInAll(user, rights.Select(r => r.Item1));
    public static bool IsAuthorizedInAny(this ClaimsPrincipal user, IEnumerable<(string, string)> rights)
        => IsAuthorizedInAny(user, rights.Select(r => r.Item1));
    public static bool IsAuthorized(this ClaimsPrincipal user, string right)
        => user.HasClaim(c => c.Type == ClaimNames.Right && c.Value == right);
    public static bool IsAuthorizedInAll(this ClaimsPrincipal user, IEnumerable<string> rights)
        => rights.All(r => user.HasClaim(c => c.Type == ClaimNames.Right && c.Value == r));
    public static bool IsAuthorizedInAny(this ClaimsPrincipal user, IEnumerable<string> rights)
        => rights.Any(r => user.HasClaim(c => c.Type == ClaimNames.Right && c.Value == r));
    public static ObjectResult InternalServerError(this ControllerBase controller, object value)
        => controller.StatusCode(StatusCodes.Status500InternalServerError, value);
    public static IQueryable<T> OrderByDynamic<T>(
        this IQueryable<T> source, string propertyName, bool ascending)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return source;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.PropertyOrField(parameter, propertyName);
        var selector = Expression.Lambda(property, parameter);

        var method = ascending ? "OrderBy" : "OrderByDescending";

        var result = Expression.Call(
            typeof(Queryable),
            method,
            [source.ElementType, property.Type],
            source.Expression,
            Expression.Quote(selector));

        return source.Provider.CreateQuery<T>(result);
    }
    public static string? ToJson(this JsonDocument? doc)
        => doc?.RootElement.GetRawText();
    public static (JsonDocument?, bool) ToJsonDocument(this string? jsonString)
    {
        JsonDocument? ret = null;

        if (string.IsNullOrWhiteSpace(jsonString)) return (ret, true);

        try
        {
            ret = JsonDocument.Parse(jsonString);
            return (ret, true);
        }
        catch
        {
            return (null, false);
        }
    }
    public static void UpdateDynamicProperties<T>(this List<T> _old, long id, List<DynamicPropertyDto> _new, DbSet<T> table, IMapper mapper) where T : DynamicProperty
    {
        var existingPropertyNames = _old.Select(dp => dp.Name).ToList();
        // Update or Add properties
        foreach (var p in _new)
        {
            // Apply the filtering regex to the key Name
            p.Name = ExtensionMethods.JsonKeyNameRegex().Replace(p.Name, "");

            var ep = _old.FirstOrDefault(dp => dp.Name == p.Name);
            if (ep is not null)
            {
                ep.Label = p.Label;
                ep.Required = p.Required;
                ep.Readonly = p.Readonly;
                ep.InputType = p.InputType;
                ep.Min = p.Min;
                ep.Max = p.Max;
                ep.Step = p.Step;
                ep.ComboOptions = p.ComboOptions;
                ep.Default = p.Default;
                table.Update(ep);
                existingPropertyNames.Remove(p.Name); // Remove from the set of existing property names
            }
            else
            {
                var np = mapper.Map<T>(p);
                np.Id = 0;
                np.ConfigId = id;
                table.Add(np);
                _old.Add(np);
            }
        }

        // Remove properties that are in toUpdate but not in organization
        foreach (var propertyName in existingPropertyNames)
        {
            var propertyToRemove = _old.First(dp => dp.Name == propertyName);
            _old.Remove(propertyToRemove);
            table.Remove(propertyToRemove);
        }

    }

    [GeneratedRegex(@"[\^\-\+\*\/\\\(\)\[\]\{\}\<\>\|\&\!\?\°\'\:;\,\.\s]")]
    public static partial Regex JsonKeyNameRegex();
}