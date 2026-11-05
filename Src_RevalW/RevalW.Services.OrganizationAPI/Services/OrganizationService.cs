using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RevalW.Services.OrganizationAPI.Data;
using RevalW.Services.OrganizationAPI.Extensions;
using RevalW.Services.OrganizationAPI.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.DataTablesRequest;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils.IFace;
using ExtensionMethods = RevalW.Services.Shared.Utils.ExtensionMethods;

namespace RevalW.Services.OrganizationAPI.Services;

// organization match: o1.Name.ToLower() == o2.Name.ToLower() && o1.CoutryRegion.ToLower() == o2.CoutryRegion.ToLower();

public partial class OrganizationService(AppDbContext db, IMapper mapper, ISimpleHasher simpleHasher, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
{
    private DataTablesWorker<Organization, long, OrganizationDto>? _dataTablesWorker;
    private DataTablesWorker<Organization, long, OrganizationDto> DataTablesWorker => _dataTablesWorker ??= serviceProvider.GetRequiredService<DataTablesWorker<Organization, long, OrganizationDto>>();

    private AuthService? _authService;
    private AuthService AuthService => _authService ??= serviceProvider.GetRequiredService<AuthService>();

    private TagsService? _tagsService;
    private TagsService TagsService => _tagsService ??= serviceProvider.GetRequiredService<TagsService>();

    public List<OrganizationDto> GetAllOrganizations()
    {
        var organizations = db.Organizations.ToList();
        return mapper.Map<List<OrganizationDto>>(organizations);
    }
    public bool DoesExist(OrganizationDto? org)
    {
        if (org is null) return false;
        return db.Organizations.Any(o => o.Name.ToLower() == org.Name.ToLower() && o.CountryRegion.ToLower() == org.CountryRegion.ToLower());
    }
    public async Task<(int status, ResponseDto<OrganizationDto> response)> Create(OrganizationDto? dto, string? hash, bool isUber)
    {
        try
        {
            if (dto is null) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("organization is null"));
            if (DoesExist(dto)) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("the same organization does already exist"));
            if (!isUber && simpleHasher.ComputeHash(dto.Name + dto.Description + "uroboros") != hash) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("cannot create this Organization"));

            var org = mapper.Map<Organization>(dto);
            await TagsService.UpdateTags(org, dto.Tags);

            db.Organizations.Add(org);
            await db.SaveChangesAsync();

            var ret = await db.Organizations.FirstOrDefaultAsync(org => org.Name.ToLower() == dto.Name.ToLower() && org.CountryRegion.ToLower() == dto.CountryRegion.ToLower());
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<OrganizationDto>.Err("cannot create organization"));
            return (StatusCodes.Status200OK, ResponseDto<OrganizationDto>.Ok(ret.ToOrganizationDto()));
        }
        catch (Exception ex)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<OrganizationDto>.Err($"error occurred: {ex.Message}"));
        }
    }
    public async Task<(int status, ResponseDtoo response)> ReallyDelete(long id, string? hash, bool isUber)
    {
        try
        {
            var toDelete = await db.Organizations.FindAsync(id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDtoo.Err("cannot delete Organization that does not exists"));
            if (!isUber && simpleHasher.ComputeHash((id + 42).ToString() + "uroboros") != hash) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("cannot delete this Organization"));

            db.Organizations.Remove(toDelete);
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception ex)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err($"error occurred: {ex.Message}"));
        }
    }
    public async Task<(int status, ResponseDto<OrganizationDto>)> GetOrganizationById(long currentOrgId, bool isCreator, bool isUberAdmin, long id)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

        if (!isUberAdmin && !isCreator) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("cannot access organization info"));
        if (!isUberAdmin) id = currentOrgId;
        var organization = await db.Organizations.Include(o => o.Tags).Include(o => o.NewItemProperties).Include(o => o.ItemGridColumns).Include(o => o.NewDeviceProperties).Include(o => o.DeviceGridColumns).Include(o => o.NewUserProperties).FirstOrDefaultAsync(o => o.Id == id);
        if (organization is null) return (StatusCodes.Status404NotFound, ResponseDto<OrganizationDto>.Err("organization not found"));
        var ret = mapper.Map<OrganizationDto>(organization);

        var roles = await AuthService.GetAssignableRoles(authHeader);
        ret.Roles = (roles?.Result is null) ? [] : [.. roles.Result];

        return (StatusCodes.Status200OK, ResponseDto<OrganizationDto>.Ok(ret));
    }
    public (int status, ResponseDto<List<DynamicPropertyDto>>) GetNewItemPropsById(long currentOrgId, bool isUberAdmin, long id)
    {
        if (isUberAdmin) currentOrgId = id;
        if (currentOrgId != id) return (StatusCodes.Status400BadRequest, ResponseDto<List<DynamicPropertyDto>>.Err("cannot read organization info"));
        var ret = db.NewItemProperties.Where(dp => dp.ConfigId == id);
        if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<List<DynamicPropertyDto>>.Err("organization not found"));

        return (StatusCodes.Status200OK, ResponseDto<List<DynamicPropertyDto>>.Ok(mapper.Map<List<DynamicPropertyDto>>(ret)));
    }
    public (int status, ResponseDto<List<DynamicPropertyDto>>) GetNewUserPropsById(long currentOrgId, bool isUberAdmin, long id)
    {
        if (isUberAdmin) currentOrgId = id;
        if (currentOrgId != id) return (StatusCodes.Status400BadRequest, ResponseDto<List<DynamicPropertyDto>>.Err("cannot read organization info"));
        var ret = db.NewUserProperties.Where(dp => dp.ConfigId == id);
        if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<List<DynamicPropertyDto>>.Err("organization not found"));

        return (StatusCodes.Status200OK, ResponseDto<List<DynamicPropertyDto>>.Ok(mapper.Map<List<DynamicPropertyDto>>(ret)));
    }
    public (int status, ResponseDto<List<DynamicPropertyDto>>) GetNewDevicePropsById(long currentOrgId, bool isUberAdmin, long id)
    {
        if (isUberAdmin) currentOrgId = id;
        if (currentOrgId != id) return (StatusCodes.Status400BadRequest, ResponseDto<List<DynamicPropertyDto>>.Err("cannot read organization info"));
        var ret = db.NewDeviceProperties.Where(dp => dp.ConfigId == id);
        if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<List<DynamicPropertyDto>>.Err("organization not found"));

        return (StatusCodes.Status200OK, ResponseDto<List<DynamicPropertyDto>>.Ok(mapper.Map<List<DynamicPropertyDto>>(ret)));
    }
    public (int status, ResponseDto<List<MainGridColumnDto>>) GetItemGridColsById(long currentOrgId, bool isUberAdmin, long id)
    {
        if (isUberAdmin) currentOrgId = id;
        if (id != 1 && currentOrgId != id) return (StatusCodes.Status400BadRequest, ResponseDto<List<MainGridColumnDto>>.Err("cannot read organization info"));
        var ret = db.ItemGridColumns.Where(mgc => mgc.OrganizationId == id);
        if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<List<MainGridColumnDto>>.Err("organization not found"));

        return (StatusCodes.Status200OK, ResponseDto<List<MainGridColumnDto>>.Ok(mapper.Map<List<MainGridColumnDto>>(ret)));
    }
    public (int status, ResponseDto<List<MainGridColumnDto>>) GetDeviceGridColsById(long currentOrgId, bool isUberAdmin, long id)
    {
        if (isUberAdmin) currentOrgId = id;
        if (id != 1 && currentOrgId != id) return (StatusCodes.Status400BadRequest, ResponseDto<List<MainGridColumnDto>>.Err("cannot read organization info"));
        var ret = db.DeviceGridColumns.Where(mgc => mgc.OrganizationId == id);
        if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<List<MainGridColumnDto>>.Err("organization not found"));

        return (StatusCodes.Status200OK, ResponseDto<List<MainGridColumnDto>>.Ok(mapper.Map<List<MainGridColumnDto>>(ret)));
    }
    public async Task<(int status, ResponseDto<OrganizationDto> response)> UpdateOrganization(long currentOrgId, bool isUberAdmin, OrganizationDto dto)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

        try
        {
            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Organizations
                .Include(o => o.Tags)
                .Include(o => o.NewItemProperties)
                .Include(o => o.NewUserProperties)
                .Include(o => o.NewDeviceProperties)
                .Include(o => o.ItemGridColumns)
                .Include(o => o.DeviceGridColumns)
                .FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("there is no such Organization to update"));
            if (isUberAdmin) currentOrgId = dto.Id;
            if (dto.Id != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("cannot change the organization that does not belong to this user"));
            if (!dto.Name.Equals(toUpdate.Name, StringComparison.CurrentCultureIgnoreCase)) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("cannot change the name of the organization"));
            if (!dto.CountryRegion.Equals(toUpdate.CountryRegion, StringComparison.CurrentCultureIgnoreCase)) return (StatusCodes.Status400BadRequest, ResponseDto<OrganizationDto>.Err("cannot change country/region of the organization"));

            UpdateEntity(dto, toUpdate);
            toUpdate.NewItemProperties.UpdateDynamicProperties(toUpdate.Id, dto.NewItemProperties, db.NewItemProperties, mapper);
            toUpdate.NewDeviceProperties.UpdateDynamicProperties(toUpdate.Id, dto.NewDeviceProperties, db.NewDeviceProperties, mapper);
            toUpdate.NewUserProperties.UpdateDynamicProperties(toUpdate.Id, dto.NewUserProperties, db.NewUserProperties, mapper);
            UpdateItemGridColumns(dto, toUpdate);
            UpdateDeviceGridColumns(dto, toUpdate);
            var roleUpdateTask = AuthService.UpdateRoles(authHeader, dto.Roles);
            var tagsUpdateTask = TagsService.UpdateTags(toUpdate, dto.Tags);
            await Task.WhenAll(roleUpdateTask, tagsUpdateTask);
            TagsService.RemoveUnused();

            await db.SaveChangesAsync();

            var ret = mapper.Map<OrganizationDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<OrganizationDto>.Err("could not read updated Organization"));
            return (StatusCodes.Status200OK, ResponseDto<OrganizationDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<OrganizationDto>.Err(e.ToString()));
        }
    }
    public async Task<ResponseDto<DataTablesResponse<OrganizationDto>>> GetByDataTables(long currentOrgId, bool isUber, Request request, bool? deleted)
    {
        var queryable = db.Organizations
            .Include(c => c.Tags);

        var mainGridColumns = AppendDbColumnsForDataTables(GetJsonColumnsForDataTables(null));
        var ret = await DataTablesWorker.DoDataTablesSelect(queryable, request, mainGridColumns);
        return ResponseDto<DataTablesResponse<OrganizationDto>>.Ok(ret);
    }

    #region private
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> AppendDbColumnsForDataTables(IEnumerable<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? jsonColumns)
    {
        List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> ret;
        if (jsonColumns is null) ret = [];
        else ret = [.. jsonColumns];
        ret.Add((nameof(Organization.Name), null));
        ret.Add((nameof(Organization.Description), null));
        return ret;
    }
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? GetJsonColumnsForDataTables(List<MainGridColumnDto>? mainGridColumns) => null;

    private static void UpdateEntity(OrganizationDto dto, Organization toUpdate)
    {
        toUpdate.Description = dto.Description;
        toUpdate.Street = dto.Street;
        toUpdate.City = dto.City;
        toUpdate.Zip = dto.Zip;
    }
    private void UpdateItemGridColumns(OrganizationDto dto, Organization toUpdate)
    {
        var existingColumnNames = toUpdate.ItemGridColumns.Select(c => c.Name).ToList();
        // Update or Add columns
        foreach (var c in dto.ItemGridColumns)
        {
            // Apply the filtering regex to the Name
            c.Name = ExtensionMethods.JsonKeyNameRegex().Replace(c.Name, "");

            var existingColumn = toUpdate.ItemGridColumns.FirstOrDefault(dp => dp.Name == c.Name && dp.Prefix == c.Prefix);
            if (existingColumn is not null)
            {
                existingColumn.Label = c.Label;
                db.ItemGridColumns.Update(existingColumn);
                existingColumnNames.Remove(c.Name); // Remove from the set of existing column names
            }
            else
            {
                var newColumn = mapper.Map<ItemGridColumn>(c);
                newColumn.Id = 0;
                newColumn.OrganizationId = toUpdate.Id;
                db.ItemGridColumns.Add(newColumn);
                toUpdate.ItemGridColumns.Add(newColumn);
            }
        }

        // Remove columns that are in toUpdate but not in organization
        foreach (var columnName in existingColumnNames)
        {
            var columnToRemove = toUpdate.ItemGridColumns.First(dp => dp.Name == columnName);
            toUpdate.ItemGridColumns.Remove(columnToRemove);
            db.ItemGridColumns.Remove(columnToRemove);
        }
    }
    private void UpdateDeviceGridColumns(OrganizationDto dto, Organization toUpdate)
    {
        var existingColumnNames = toUpdate.DeviceGridColumns.Select(c => c.Name).ToList();
        // Update or Add columns
        foreach (var c in dto.DeviceGridColumns)
        {
            // Apply the filtering regex to the Name
            c.Name = ExtensionMethods.JsonKeyNameRegex().Replace(c.Name, "");

            var existingColumn = toUpdate.DeviceGridColumns.FirstOrDefault(dp => dp.Name == c.Name);
            if (existingColumn is not null)
            {
                existingColumn.Label = c.Label;
                db.DeviceGridColumns.Update(existingColumn);
                existingColumnNames.Remove(c.Name); // Remove from the set of existing column names
            }
            else
            {
                var newColumn = mapper.Map<DeviceGridColumn>(c);
                newColumn.Id = 0;
                newColumn.OrganizationId = toUpdate.Id;
                db.DeviceGridColumns.Add(newColumn);
                toUpdate.DeviceGridColumns.Add(newColumn);
            }
        }

        // Remove columns that are in toUpdate but not in organization
        foreach (var columnName in existingColumnNames)
        {
            var columnToRemove = toUpdate.DeviceGridColumns.First(dp => dp.Name == columnName);
            toUpdate.DeviceGridColumns.Remove(columnToRemove);
            db.DeviceGridColumns.Remove(columnToRemove);
        }
    }

    #endregion
}
