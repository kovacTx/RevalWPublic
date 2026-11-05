using AutoMapper;
using RevalW.Services.AuthAPI.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;

namespace RevalW.Services.AuthAPI.Extensions;

public static class ExtensionMethods
{
    private static IMapper? _mapper;
    public static IMapper Mapper => _mapper ??= ServiceProviderAccessor.Instance.GetService<IMapper>()!;
    public static UserRegistrationRequest ToUserRegistrationRequest(this RegistrationWithinOrganizationRequestDto dto)
        => Mapper.Map<UserRegistrationRequest>(dto);
    public static OrganizationDto ToOrganizationRegistrationRequest(this NewRegistrationRequestDto dto)
        => new()
        {
            Name = dto.OrganizationName,
            Description = dto.OrganizationDescription,
            Street = dto.OrganizationStreet,
            Zip = dto.OrganizationZip,
            City = dto.OrganizationCity,
            CountryRegion = dto.OrganizationCountryRegion
        };
    public static UserDto ToUserDto(this ApplicationUser appUser)
        => Mapper.Map<UserDto>(appUser);
    public static ApplicationUser ToApplicationUser(this UserRegistrationRequest req)
        => Mapper.Map<ApplicationUser>(req);
    public static RoleDto ToRoleDto(this ApplicationRole r, HashSet<string> permissions)
    {
        return new RoleDto
        {
            Name = StripName(r.Name),
            Permissions = RightsList.All.Where(kv => permissions.Contains(kv.Item1)).ToDictionary(kv => kv.Item1, kv => kv.Item2),
        };

        static string StripName(string? rName)
        {
            if (string.IsNullOrEmpty(rName)) return "-";
            var index = rName.IndexOf('_');
            return index >= 0 ? rName[(index + 1)..] : rName;
        }
    }
    public static RoleDto ToRoleDto(this string name, List<(string, string)> permissions)
    {
        return new RoleDto
        {
            Name = name,
            Permissions = permissions.ToDictionary(kv => kv.Item1, kv => kv.Item2),
        };
    }
}
