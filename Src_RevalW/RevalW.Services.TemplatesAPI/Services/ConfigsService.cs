using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RevalW.Services.Shared.Utils;
using RevalW.Services.TemplatesAPI.Data;
using RevalW.Services.TemplatesAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.DataTablesRequest;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RevalW.Services.TemplatesAPI.Services;

public partial class ConfigsService(AppDbContext db, IMapper mapper, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
{
    private TagsService? _tagsService;
    private TagsService TagsService => _tagsService ??= serviceProvider.GetRequiredService<TagsService>();

    private ItemsService? _itemsService;
    private ItemsService ItemsService => _itemsService ??= serviceProvider.GetRequiredService<ItemsService>();

    private FileStorageService? _fileStorageService;
    private FileStorageService FileStorageService => _fileStorageService ??= serviceProvider.GetRequiredService<FileStorageService>();

    private DataTablesWorker<Config, long, ConfigHeaderDto>? _dataTablesWorker;
    private DataTablesWorker<Config, long, ConfigHeaderDto> DataTablesWorker => _dataTablesWorker ??= serviceProvider.GetRequiredService<DataTablesWorker<Config, long, ConfigHeaderDto>>();

    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> Create(long currentOrgId, bool isUber, ClaimsPrincipal user, ConfigUpsertDto dto, HttpContext httpContext)
    {
        try
        {
            var authorized = ThorizeConfigCreate(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (dto.Id != default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("the id have to be 0"));
            if (!isUber || dto.OrganizationId == default) dto.OrganizationId = currentOrgId;

            var duplicateAndOverlapResp = CheckDuplicatesAndOverlappingTags(dto);
            if (duplicateAndOverlapResp is not null) return duplicateAndOverlapResp.Value;

            var config = mapper.Map<Config>(dto);
            if (config is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot read Config to save"));

            await UpdateTags(dto.Tags, config, dto.OrganizationId, isUber);
            config.LastUpdated = DateTime.UtcNow;
            config.Public = false;
            config.Readonly = false;
            PrepareComboSources(config);
            config.NewItemProperties.UpdateDynamicProperties(config.Id, dto.NewItemProperties, db.NewItemProperties, mapper);
            db.Configs.Add(config);
            await db.SaveChangesAsync();

            var ret = mapper.Map<ConfigUpsertDto>(config);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err("could not read saved Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> Update(long currentOrgId, bool isUber, ClaimsPrincipal user, ConfigUpsertDto dto)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            var authorized = ThorizeConfigUpdate(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Configs
                .Include(c => c.Tags)
                .Include(o => o.NewItemProperties)
                .FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("there is no such Config to update"));
            if (isUber) dto.OrganizationId = toUpdate.OrganizationId;
            else dto.OrganizationId = currentOrgId;

            if (toUpdate.OrganizationId != dto.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot change the organization (or update the Config that does not belongs to this organization)"));
            if (toUpdate.Readonly) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot update readonly Config"));

            var duplicateAndOverlapResp = CheckDuplicatesAndOverlappingTags(dto);
            if (duplicateAndOverlapResp is not null) return duplicateAndOverlapResp.Value;

            var removedAndUpdatedResp = await CheckRemovedAndUpdatedTags(dto, toUpdate, authHeader);
            if (removedAndUpdatedResp is not null) return removedAndUpdatedResp.Value;

            var config = mapper.Map<Config>(dto);
            if (config is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("invalid Config to update"));

            // this values are updating in dllUpdate
            config.WDllLastUpdated = toUpdate.WDllLastUpdated;
            config.WDllPrivatePath = toUpdate.WDllPrivatePath;
            config.ADllLastUpdated = toUpdate.ADllLastUpdated;
            config.ADllPrivatePath = toUpdate.ADllPrivatePath;
            config.IDllLastUpdated = toUpdate.IDllLastUpdated;
            config.IDllPrivatePath = toUpdate.IDllPrivatePath;

            db.Configs.Entry(toUpdate).CurrentValues.SetValues(config); // update scalar fields
            toUpdate.Tags.Clear(); // clear old relationships

            await UpdateTags(dto.Tags, toUpdate, dto.OrganizationId, isUber);
            toUpdate.LastUpdated = DateTime.UtcNow;
            toUpdate.Public = false;
            toUpdate.Readonly = false;
            PrepareComboSources(toUpdate);
            toUpdate.NewItemProperties.UpdateDynamicProperties(config.Id, dto.NewItemProperties, db.NewItemProperties, mapper);
            await db.SaveChangesAsync();

            var messageBase = "config has been updated, but:";
            var (status, response) = await TidyUpTemplateNames(dto.OrganizationId);
            if (status != StatusCodes.Status200OK) return (status, ResponseDto<ConfigUpsertDto>.Err($"{messageBase} {response.Message}"));

            await db.SaveChangesAsync();

            var ret = mapper.Map<ConfigUpsertDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err("could not read updated Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> Autosave(long currentOrgId, bool isUber, ClaimsPrincipal user, MonacoAutosaveRequest dto)
    {
        try
        {
            var authorized = ThorizeConfigUpdate(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Configs.FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("there is no such Config to update"));
            if (isUber) currentOrgId = toUpdate.OrganizationId;

            if (toUpdate.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot change the organization (or update the Config that does not belongs to this organization)"));
            if (toUpdate.Readonly) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot update readonly Config"));

            toUpdate.Content = dto.Content;
            toUpdate.LastUpdated = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var ret = mapper.Map<ConfigUpsertDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err("could not read updated Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> UpdateImage(long currentOrgId, bool isUber, ClaimsPrincipal user, UpdateImageDto dto)
    {
        try
        {
            var authorized = ThorizeConfigUpdate(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Configs.FirstOrDefaultAsync(c => c.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("there is no such Config to update"));
            if (isUber) dto.OrganizationId = toUpdate.OrganizationId;
            else dto.OrganizationId = currentOrgId;

            if (toUpdate.OrganizationId != dto.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot change the organization (or update the Config that does not belongs to this organization)"));

            await FileStorageService.TryRemoveImage(toUpdate);
            await FileStorageService.UploadImage(dto, toUpdate);
            await db.SaveChangesAsync();

            var ret = mapper.Map<ConfigUpsertDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err("could not read updated Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> UpdateDll(long currentOrgId, bool isUber, ClaimsPrincipal user, UpdateDllDto dto)
    {
        try
        {
            var authorized = ThorizeConfigUpdate(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Configs.FirstOrDefaultAsync(c => c.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("there is no such Config to update"));
            if (isUber) dto.OrganizationId = toUpdate.OrganizationId;
            else dto.OrganizationId = currentOrgId;

            if (toUpdate.OrganizationId != dto.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot change the organization (or update the Config that does not belongs to this organization)"));

            await FileStorageService.TryRemoveDllFiles(dto, toUpdate);
            await FileStorageService.TryUploadDllFilesAndAddPaths(dto, toUpdate);
            // if i want to remove dll, the UpdateDll==true and the Dll==null
            if (dto.UpdateWDll)
            {
                if (toUpdate.WDllPrivatePath is null) toUpdate.WDllLastUpdated = null;
                else toUpdate.WDllLastUpdated = DateTime.UtcNow;
            }
            if (dto.UpdateADll)
            {
                if (toUpdate.ADllPrivatePath is null) toUpdate.ADllLastUpdated = null;
                else toUpdate.ADllLastUpdated = DateTime.UtcNow;
            }
            if (dto.UpdateIDll)
            {
                if (toUpdate.IDllPrivatePath is null) toUpdate.IDllLastUpdated = null;
                else toUpdate.IDllLastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            var ret = mapper.Map<ConfigUpsertDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err("could not read updated Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<(Stream stream, string fileName)> response)> DownloadDllForConfigId(long currentOrgId, bool isUber, long id, string platform)
    {
        try
        {
            Config? config;
            if (id == 0) id = 1;
            config = await db.Configs.FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (config is null) return (StatusCodes.Status404NotFound, ResponseDto<(Stream, string)>.Err("there is no such Config"));

            var platformChar = platform.ToLower().FirstOrDefault();
            var path = platformChar switch
            {
                'w' => config.WDllPrivatePath,
                'a' => config.ADllPrivatePath,
                'i' => config.IDllPrivatePath,
                _ => null
            };
            if (path is null) return (StatusCodes.Status404NotFound, ResponseDto<(Stream, string)>.Err("there is no DLL for this Config and platform"));
            return await FileStorageService.GetDllFileStream(path);
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<(Stream, string)>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> GetById(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var config = await db.Configs
                .Include(c => c.Tags)
                .Include(o => o.NewItemProperties)
                .FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (config is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigUpsertDto>.Err("config with this id does not exists"));
            var ret = mapper.Map<ConfigUpsertDto>(config);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err("could not read Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigHeaderDto> response)> GetHeaderById(long currentOrgId, long id)
    {
        try
        {
            var config = await db.Configs.FindAsync(id);
            if (config is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigHeaderDto>.Err("could not read Configs"));
            if (config.Id == 1) currentOrgId = config.OrganizationId;
            if (config.OrganizationId != currentOrgId) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigHeaderDto>.Err("could not read Configs"));

            var ret = mapper.Map<ConfigUpsertDto>(config);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigHeaderDto>.Err("could not read Config"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigHeaderDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigHeaderDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<List<ConfigHeaderDto>> response)> GetByTag(long currentOrgId, bool isUber, string tagName)
    {
        try
        {
            var tag = await db.Tags.Include(t => t.Configs).FirstOrDefaultAsync(t => t.Name == tagName && (isUber || t.OrganizationId == currentOrgId));
            if (tag is null) return (StatusCodes.Status404NotFound, ResponseDto<List<ConfigHeaderDto>>.Err("no Config with such Tag"));
            if (tag.Configs is null || tag.Configs.Count == 0) return (StatusCodes.Status404NotFound, ResponseDto<List<ConfigHeaderDto>>.Err("no Config with such Tag"));

            var ret = mapper.Map<List<ConfigHeaderDto>>(tag.Configs);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<ConfigHeaderDto>>.Err("could not read Configs"));
            return (StatusCodes.Status200OK, ResponseDto<List<ConfigHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<ConfigHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<ResponseDto<DataTablesResponse<ConfigHeaderDto>>> GetByDataTables(long currentOrgId, bool isUber, Request request, bool? deleted)
    {
        var queryable = db.Configs
            .Include(c => c.Tags)
            .Where(c => isUber || c.OrganizationId == currentOrgId)
            .Where(c => !deleted.HasValue || c.Deleted == deleted.Value);

        var mainGridColumns = AppendDbColumnsForDataTables(GetJsonColumnsForDataTables(null));
        var ret = await DataTablesWorker.DoDataTablesSelect(queryable, request, mainGridColumns);

        if (ret.data is null)
            return ResponseDto<DataTablesResponse<ConfigHeaderDto>>.Ok(ret);

        foreach (var dto in ret.data) dto.Tags = await GetTagsForConfig(dto.Id);
        return ResponseDto<DataTablesResponse<ConfigHeaderDto>>.Ok(ret);
    }
    public async Task<(int status, ResponseDto<List<ConfigHeaderDto>> response)> GetByTags(long currentOrgId, bool isUber, IEnumerable<string> tagNames)
    {
        try
        {
            var tags = db.Tags.Include(t => t.Configs).Where(t => tagNames.Contains(t.Name) && (isUber || t.OrganizationId == currentOrgId));
            if (tags is null) return (StatusCodes.Status404NotFound, ResponseDto<List<ConfigHeaderDto>>.Err("no Configs with such Tags"));
            if (!await tags.AnyAsync()) return (StatusCodes.Status404NotFound, ResponseDto<List<ConfigHeaderDto>>.Err("no Configs with such Tags"));
            var configs = tags.SelectMany(t => t.Configs).Distinct();
            if (configs is null || !configs.Any()) return (StatusCodes.Status404NotFound, ResponseDto<List<ConfigHeaderDto>>.Err("no Configs with such Tags"));

            var ret = mapper.Map<List<ConfigHeaderDto>>(configs);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<ConfigHeaderDto>>.Err("could not read Configs"));
            return (StatusCodes.Status200OK, ResponseDto<List<ConfigHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<ConfigHeaderDto>>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<List<ConfigHeaderDto>> response) GetByOrganizationId(bool isUber, int id)
    {
        try
        {
            if (!isUber) return (StatusCodes.Status400BadRequest, ResponseDto<List<ConfigHeaderDto>>.Err("cannot use this endpoint"));
            var configs = db.Configs.Include(c => c.Tags).Where(c => c.OrganizationId == id);
            if (configs is null) return (StatusCodes.Status404NotFound, ResponseDto<List<ConfigHeaderDto>>.Err("Configs from this organization does not exists"));
            var ret = mapper.Map<List<ConfigHeaderDto>>(configs);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<ConfigHeaderDto>>.Err("could not read Configs"));
            return (StatusCodes.Status200OK, ResponseDto<List<ConfigHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<ConfigHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<List<DynamicPropertyDto>>)> DynamicProperties4Config(long currentOrgId, bool isUberAdmin, long id)
    {
        var ret = await db.Configs.Include(c => c.NewItemProperties).FirstOrDefaultAsync(c => c.Id == id && (isUberAdmin || c.OrganizationId == currentOrgId));
        if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<List<DynamicPropertyDto>>.Err("Config not found"));

        return (StatusCodes.Status200OK, ResponseDto<List<DynamicPropertyDto>>.Ok(mapper.Map<List<DynamicPropertyDto>>(ret.NewItemProperties)));
    }
    public (int status, ResponseDto<IEnumerable<ConfigHeaderDto>> response) GetHeaders(long currentOrgId, bool isUber, int? pageNum, int? pageSize, bool? deleted)
    {
        try
        {
            var configsQ = db.Configs
                .Where(c => isUber || c.OrganizationId == currentOrgId)
                .Where(c => !deleted.HasValue || c.Deleted == deleted.Value)
                .Include(c => c.Tags)
                .OrderBy(c => c.Id);
            List<Config> retConfigs;
            if (pageNum.HasValue && pageSize.HasValue)
                retConfigs = [.. configsQ.Skip(pageNum.Value * pageSize.Value).Take(pageSize.Value)];
            else
                retConfigs = [.. configsQ];
            var ret = mapper.Map<IEnumerable<ConfigHeaderDto>>(retConfigs);
            return (StatusCodes.Status200OK, ResponseDto<IEnumerable<ConfigHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<ConfigHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> MarkDeleted(long currentOrgId, bool isUber, ClaimsPrincipal user, long id)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            var authorized = ThorizeConfigDelete(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            var toDelete = await db.Configs
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(tn => tn.Id == id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigUpsertDto>.Err("cannot delete Config that does not exists"));
            if (!isUber && toDelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot delete Config that does not belongs to this organization"));
            if (toDelete.Id == 1) return (StatusCodes.Status404NotFound, ResponseDto<ConfigUpsertDto>.Err("cannot delete Config id 1"));

            foreach (var t in toDelete.Tags.Select(t => t.Name))
            {
                var (statusTags, responseTags) = await ItemsService.GetByTag(authHeader, t);
                if (statusTags != StatusCodes.Status200OK || responseTags?.Result is null) return (statusTags, ResponseDto<ConfigUpsertDto>.Err($"failed to check the config usage: {responseTags?.Message}"));
                foreach (var i in responseTags.Result)
                {
                    if ((i.Status == EItemStatus.New) || (i.Status >= EItemStatus.Finished)) continue;
                    return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err($"cannot delete config that is in use"));
                }
            }

            toDelete.Deleted = true;
            toDelete.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var messageBase = "config has been deleted, but:";
            var (status, response) = await TidyUpTemplateNames(currentOrgId);
            if (status != StatusCodes.Status200OK) return (status, ResponseDto<ConfigUpsertDto>.Err($"{messageBase} {response.Message}"));

            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> MarkAsUndeleted(long currentOrgId, bool isUber, ClaimsPrincipal user, long id)
    {
        try
        {
            var authorized = ThorizeConfigDelete(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("the id cannot to be 0"));

            var toUndelete = await db.Configs.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
            if (toUndelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigUpsertDto>.Err("cannot undelete Config that does not exists"));
            if (!isUber && toUndelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot undelete Config that does not belongs to this organization"));

            var overlappingNames = OverlappingTags(toUndelete.Tags.Select(tn => tn.Name), currentOrgId, id);
            if (overlappingNames is not null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err($"the following Tags are already used in another Config: {string.Join(", ", overlappingNames)}"));

            toUndelete.Deleted = false;
            toUndelete.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var messageBase = "config has been deleted, but:";
            var (status, response) = await TidyUpTemplateNames(currentOrgId);
            if (status != StatusCodes.Status200OK) return (status, ResponseDto<ConfigUpsertDto>.Err($"{messageBase} {response.Message}"));

            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigUpsertDto> response)> ReallyDelete(long currentOrgId, bool isUber, ClaimsPrincipal user, long id)
    {
        try
        {
            var authorized = ThorizeConfigDelete(user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            var toDelete = await db.Configs.FindAsync(id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigUpsertDto>.Err("cannot delete Config that does not exists"));
            if (!isUber && toDelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("cannot delete Config that does not belongs to this organization"));

            await FileStorageService.TryRemoveImage(toDelete);
            db.Configs.Remove(toDelete);
            await db.SaveChangesAsync();

            var messageBase = "config has been deleted, but:";
            var (status, response) = await TidyUpTemplateNames(currentOrgId);
            if (status != StatusCodes.Status200OK) return (status, ResponseDto<ConfigUpsertDto>.Err($"{messageBase} {response.Message}"));

            return (StatusCodes.Status200OK, ResponseDto<ConfigUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigUpsertDto>.Err(e.ToString()));
        }
    }

    public async Task<(int status, ResponseDto<IEnumerable<TagDto>> response)> TidyUpTemplateNames(long currentOrgId)
    {
        var partialStrings = db.Configs
            .Where(c => c.OrganizationId == currentOrgId)
            .Where(c => !c.Deleted)
            .Include(c => c.Tags)
            .SelectMany(c => c.Tags.Select(tn => tn.Name));

        var distinct = new HashSet<string>(partialStrings);

        var (status, response) = await TagsService.GetAll(currentOrgId, false, false);
        if (status != StatusCodes.Status200OK) return (status, response);
        var notUseAnymore = response.Result!
            .Where(tn => !distinct.Contains(tn.Name))
            .ToList();
        foreach (var d in notUseAnymore)
        {
            await TagsService.MarkAsDeleted(currentOrgId, false, d.Id);
        }
        return (StatusCodes.Status200OK, ResponseDto<IEnumerable<TagDto>>.Ok());
    }

    #region private
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> AppendDbColumnsForDataTables(IEnumerable<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? jsonColumns)
    {
        List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> ret;
        if (jsonColumns is null) ret = [];
        else ret = [.. jsonColumns];
        ret.Add((nameof(Config.Designation), null));
        ret.Add((nameof(Config.Author), null));
        return ret;
    }
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? GetJsonColumnsForDataTables(List<MainGridColumnDto>? mainGridColumns) => null;
    private async Task<List<string>> GetTagsForConfig(long id)
    {
        var e = await db.Configs.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (e is null || e.Tags is null) return [];
        return [.. e.Tags.Select(t => t.Name)];
    }
    private static void PrepareComboSources(Config? config)
    {
        if (config is null) return;
        if (string.IsNullOrWhiteSpace(config.Content))
        {
            config.ComboSourcesJson = null;
            return;
        }

        config.Content = config.Content.Replace("\r\n", "\n").Replace("\r", "\n");

        List<string> comboSources = [];
        try
        {
            var xml = $"<root>{config.Content}</root>";
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            var names = doc
                .Descendants("DbContent")
                .Select(e => (string?)e.Attribute("Name"))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .ToList();
            comboSources = names;
        }
        catch
        {
            var rx = DbComboContentNameAttributeRegex();
            var matches = rx.Matches(config.Content);
            var list = matches
                .Select(m => m.Groups[1].Value)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            comboSources = list;
        }
        config.ComboSourcesJson = JsonSerializer.Serialize(comboSources);
    }
    private static HashSet<string>? CheckDistinct(IEnumerable<string> names)
    {
        HashSet<string> seen = [];
        HashSet<string> duplicates = [];

        foreach (var name in names)
            if (!seen.Add(name))
                duplicates.Add(name);
        return duplicates.Count > 0 ? duplicates : null;
    }
    private IEnumerable<string>? OverlappingTags(IEnumerable<string> names, long currentOrgId, long dtoId)
    {
        var lowerTags = names.Select(tn => tn.ToLower());
        var overlayingTagsConfigs = db.Configs
            .Include(c => c.Tags)
            .Where(c =>
                c.OrganizationId == currentOrgId &&
                !c.Deleted &&
                dtoId != c.Id &&
                c.Tags
                    .Select(tn => tn.Name.ToLower())
                    .Any(tn => lowerTags.Contains(tn)));
        if (overlayingTagsConfigs is null || !overlayingTagsConfigs.Any()) return null;

        return overlayingTagsConfigs.SelectMany(c =>
            c.Tags
            .Select(tn => tn.Name.ToLower())
            .Where(tn => lowerTags.Contains(tn)))
            .Distinct();
    }
    private (int status, ResponseDto<ConfigUpsertDto> response)? CheckDuplicatesAndOverlappingTags(ConfigUpsertDto dto)
    {
        if (dto.Tags is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("no Tags?"));
        if (dto.Tags.Count == 0) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err("no Tags?"));
        dto.Tags = [.. dto.Tags.Where(n => n is not null)];
        var duplicates = CheckDistinct(dto.Tags);
        if (duplicates is not null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err($"the following Tags are duplicates: {string.Join(", ", duplicates)}"));

        var overlappingNames = OverlappingTags(dto.Tags, dto.OrganizationId, dto.Id);
        if (overlappingNames is not null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err($"the following Tags are already used in another Config: {string.Join(", ", overlappingNames)}"));
        return null;
    }
    private async Task<(int status, ResponseDto<ConfigUpsertDto> response)?> CheckRemovedAndUpdatedTags(ConfigUpsertDto dto, Config toUpdate, StringValues authHeader)
    {
        var removed = toUpdate.Tags.Select(t => t.Name).Except(dto.Tags);
        foreach (var t in removed)
        {
            var (statusTags, responseTags) = await ItemsService.GetByTag(authHeader, t);
            if (statusTags != StatusCodes.Status200OK || responseTags?.Result is null) return (statusTags, ResponseDto<ConfigUpsertDto>.Err($"failed to check the tag usage: {responseTags?.Message}"));
            if (responseTags.Result.Count != 0) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err($"cannot remove Tag '{t}' from Config because it is used in some Items"));
        }

        foreach (var t in dto.Tags)
        {
            var (statusTags, responseTags) = await ItemsService.GetByTag(authHeader, t);
            if (statusTags != StatusCodes.Status200OK || responseTags?.Result is null) return (statusTags, ResponseDto<ConfigUpsertDto>.Err($"failed to check the config usage: {responseTags?.Message}"));
            foreach (var i in responseTags.Result)
            {
                if ((i.Status == EItemStatus.New) || (i.Status >= EItemStatus.Finished)) continue;
                //return (StatusCodes.Status400BadRequest, ResponseDto<ConfigUpsertDto>.Err($"cannot edit config that is in use"));
            }
        }
        return null;
    }
    private async Task UpdateTags(IEnumerable<string> names, Config config, long currentOrgId, bool isUber)
    {
        config.Tags = [];
        foreach (var n in names)
        {
            var tn = await TagsService.GetEntityByName(currentOrgId, isUber, n);
            if (tn is not null)
            {
                await TagsService.MarkAsUndeleted(currentOrgId, isUber, tn.Id);
                config.Tags.Add(tn);
            }
            var (status, tagResp) = await TagsService.CreateAndReturnTag(currentOrgId, isUber, new() { Name = n, OrganizationId = currentOrgId });
            if (status != StatusCodes.Status200OK) continue;
            if (tagResp?.Result is null) continue;
            config.Tags.Add(tagResp.Result);
        }
    }
    private ResponseDto<ConfigUpsertDto>? ThorizeConfigCreate(ClaimsPrincipal user)
    {
        if (!user.IsAuthorized(RightsList.Config_Create))
            return ResponseDto<ConfigUpsertDto>.Err("user is not authorized for this operation");

        return null;
    }
    private ResponseDto<ConfigUpsertDto>? ThorizeConfigUpdate(ClaimsPrincipal user)
    {
        if (!user.IsAuthorized(RightsList.Config_Edit))
            return ResponseDto<ConfigUpsertDto>.Err("user is not authorized for this operation");

        return null;
    }
    private ResponseDto<ConfigUpsertDto>? ThorizeConfigDelete(ClaimsPrincipal user)
    {
        if (!user.IsAuthorized(RightsList.Config_Erase))
            return ResponseDto<ConfigUpsertDto>.Err("user is not authorized for this operation");

        return null;
    }

    [GeneratedRegexAttribute("<DbContent\\b[^>]*\\bName\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DbComboContentNameAttributeRegex();

    #endregion
}
