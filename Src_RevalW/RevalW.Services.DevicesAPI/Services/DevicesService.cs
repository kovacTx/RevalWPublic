using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RevalW.Services.DevicesAPI.Data;
using RevalW.Services.DevicesAPI.Model;
using RevalW.Services.DevicesAPI.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.DataTablesRequest;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils.IFace;

namespace RevalW.Services.DevicesAPI.Services;

public class DevicesService(AppDbContext db, IMapper mapper, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor, CommonData commonData, ISimpleHasher simpleHasher)
{
    private TagsService? _tagsService;
    private TagsService TagsService => _tagsService ??= serviceProvider.GetRequiredService<TagsService>();

    private OrganizationService? _organizationService;
    private OrganizationService OrganizationService => _organizationService ??= serviceProvider.GetRequiredService<OrganizationService>();

    private ItemsService? _itemsService;
    private ItemsService ItemsService => _itemsService ??= serviceProvider.GetRequiredService<ItemsService>();

    private DataTablesWorker<Device, long, DeviceHeaderDto>? _dataTablesWorker;
    private DataTablesWorker<Device, long, DeviceHeaderDto> DataTablesWorker => _dataTablesWorker ??= serviceProvider.GetRequiredService<DataTablesWorker<Device, long, DeviceHeaderDto>>();

    public async Task<(int status, ResponseDto<DeviceUpsertDto> response)> Create(long currentOrgId, bool isUber, bool isAuthorized, string? userId, DeviceUpsertDto dto, string? hash)
    {
        try
        {
            if (!isAuthorized)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("user is not authorized for this operation"));
            if (dto.Id != default)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("the id have to be 0"));
            if (!isUber || dto.OrganizationId == default) dto.OrganizationId = currentOrgId;

            if (dto.Tags is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("no Tags?"));
            if (dto.Tags.Count == 0)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("no Tags?"));
            dto.Tags = [.. dto.Tags.Where(n => n is not null)];

            var device = mapper.Map<Device>(dto);
            if (device is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read Device to save"));
            (device.DeviceData, var success) = dto.DeviceDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read DeviceData to save"));
            (device.ConfigData, success) = dto.ConfigDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read ConfigData to save"));
            (device.OwnerEmails, success) = dto.OwnerEmailsJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read OwnerEmails to save"));

            device.LastUpdated = DateTime.UtcNow;
            await PrepareTags(dto.Tags, device, dto.OrganizationId, isUber);

            db.Devices.Add(device);
            await db.SaveChangesAsync();
            var ret = mapper.Map<DeviceUpsertDto>(device);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err("could not read saved Device"));
            return (StatusCodes.Status200OK, ResponseDto<DeviceUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DeviceUpsertDto> response)> Update(long currentOrgId, bool isUber, bool isAuthorized, string? userId, DeviceUpsertDto dto)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            if (!isAuthorized)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("user is not authorized for this operation"));
            if (dto.Id == default)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("the id can not be 0"));
            var toUpdate = await db.Devices.Include(c => c.Tags).FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("there is no such Device to update"));
            if (isUber)
                dto.OrganizationId = toUpdate.OrganizationId;
            else
                dto.OrganizationId = currentOrgId;
            if (toUpdate.OrganizationId != dto.OrganizationId)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not change the organization"));

            if (dto.Tags is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("no Tags?"));
            if (dto.Tags.Count == 0)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("no Tags?"));
            dto.Tags = [.. dto.Tags.Where(n => n is not null)];

            var device = mapper.Map<Device>(dto);
            if (device is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("invalid Device to update"));
            (device.DeviceData, var success) = dto.DeviceDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read DeviceData to save"));
            (device.ConfigData, success) = dto.ConfigDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read ConfigData to save"));
            (device.OwnerEmails, success) = dto.OwnerEmailsJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not read OwnerEmails to save"));

            db.Devices.Entry(toUpdate).CurrentValues.SetValues(device); // update scalar fields

            toUpdate.Tags.Clear(); // clear old relationships

            await PrepareTags(dto.Tags, toUpdate, dto.OrganizationId, isUber);
            toUpdate.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var ret = mapper.Map<DeviceUpsertDto>(toUpdate);
            if (ret is null)
                return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err("could not read updated Device"));

            _ = ItemsService.DeviceChanged(authHeader, ret); // this is not good habit. better do it via servicebus
            return (StatusCodes.Status200OK, ResponseDto<DeviceUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DeviceUpsertDto> response)> GetById(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var device = await db.Devices.Include(i => i.Tags).Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (device is null) return (StatusCodes.Status404NotFound, ResponseDto<DeviceUpsertDto>.Err("Device with this id does not exists"));
            var ret = mapper.Map<DeviceUpsertDto>(device);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err("could not read Device"));
            return (StatusCodes.Status200OK, ResponseDto<DeviceUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DeviceHeaderDto> response)> GetHeaderById(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var device = await db.Devices.Include(c => c.Tags).Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (device is null) return (StatusCodes.Status404NotFound, ResponseDto<DeviceHeaderDto>.Err("Device with this id does not exists"));
            var ret = mapper.Map<DeviceHeaderDto>(device);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceHeaderDto>.Err("could not read Device"));
            return (StatusCodes.Status200OK, ResponseDto<DeviceHeaderDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceHeaderDto>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<List<DeviceHeaderDto>> response) GetByTag(long currentOrgId, bool isUber, string tag)
    {
        try
        {
            var device = db.Devices.Include(i => i.Tags).Where(i => i.Tags.Any(t => t.Name == tag) && (isUber || i.OrganizationId == currentOrgId));
            if (device is null) return (StatusCodes.Status404NotFound, ResponseDto<List<DeviceHeaderDto>>.Err("Device with this id does not exists"));
            var ret = mapper.Map<List<DeviceHeaderDto>>(device);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<DeviceHeaderDto>>.Err("could not read Device"));
            return (StatusCodes.Status200OK, ResponseDto<List<DeviceHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<DeviceHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<ResponseDto<DataTablesResponse<DeviceHeaderDto>>> GetByDataTables(long currentOrgId, bool isUber, Request request, bool? deleted)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        var (_, deviceGridColumnsResp) = await OrganizationService.GetDeviceGridColumns(authHeader);
        var queryable = db.Devices
            .Include(d => d.Tags)
            .Where(d => isUber || d.OrganizationId == currentOrgId)
            .Where(d => !deleted.HasValue || d.Deleted == deleted.Value);

        var mainGridColumns = AppendDbColumnsForDataTables(GetJsonColumnsForDataTables(deviceGridColumnsResp?.Result));
        var ret = await DataTablesWorker.DoDataTablesSelect(queryable, request, mainGridColumns);

        if (ret.data is null)
            return ResponseDto<DataTablesResponse<DeviceHeaderDto>>.Ok(ret);

        foreach (var dto in ret.data) dto.Tags = await GetTagsForDevice(dto.Id);
        return ResponseDto<DataTablesResponse<DeviceHeaderDto>>.Ok(ret);
    }
    public async Task<(int status, ResponseDto<DeviceConfigDataNTagsDto> response)> GetConfigDataNTags(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var device = await db.Devices.Include(d => d.Tags).FirstOrDefaultAsync(d => d.Id == id);
            if (device is null) return (StatusCodes.Status404NotFound, ResponseDto<DeviceConfigDataNTagsDto>.Err("Device with this id does not exists"));
            if (!isUber && device.OrganizationId != currentOrgId) device = null;
            if (device is null) return (StatusCodes.Status404NotFound, ResponseDto<DeviceConfigDataNTagsDto>.Err("Device with this id does not exists"));
            var ret = mapper.Map<DeviceUpsertDto>(device);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceConfigDataNTagsDto>.Err("could not read Device"));
            return (StatusCodes.Status200OK, ResponseDto<DeviceConfigDataNTagsDto>.Ok(new() { Tags = ret.Tags, ConfigData = ret.ConfigDataJson }));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceConfigDataNTagsDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DeviceUpsertDto> response)> Delete(long currentOrgId, bool isUber, bool isAuthorized, long id)
    {
        try
        {
            if (!isAuthorized)
                return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("user is not authorized for this operation"));
            var toMark = await db.Devices.FindAsync(id);
            if (toMark is null)
                return (StatusCodes.Status404NotFound, ResponseDto<DeviceUpsertDto>.Err("can not delete Device that does not exists"));
            if (!isUber && toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<DeviceUpsertDto>.Err("can not delete Device that does not belongs to this organization"));

            db.Devices.Remove(toMark);
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<DeviceUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DeviceUpsertDto>.Err(e.ToString()));
        }
    }

    #region private
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> AppendDbColumnsForDataTables(IEnumerable<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? jsonColumns)
    {
        List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> ret;
        if (jsonColumns is null) ret = [];
        else ret = [.. jsonColumns];
        ret.Add((nameof(Device.Label), null));
        return ret;
    }
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? GetJsonColumnsForDataTables(List<MainGridColumnDto>? mainGridColumns)
    {
        if (mainGridColumns is null) return null;
        var ret = new List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>
        {
            (nameof(Device.DeviceData), mainGridColumns.Select(mgc => mgc.Name))
        };
        return ret;
    }
    private async Task<List<string>> GetTagsForDevice(long id)
    {
        var e = await db.Devices.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (e is null || e.Tags is null) return [];
        return [.. e.Tags.Select(t => t.Name)];
    }
    private async Task PrepareTags(IEnumerable<string> names, Device item, long currentOrgId, bool isUber)
    {
        item.Tags = [];
        foreach (var n in names)
        {
            var tn = await TagsService.GetTagByName(currentOrgId, isUber, n);
            if (tn is not null)
            {
                item.Tags.Add(tn);
                continue;
            }
            var (status, response) = await TagsService.CreateAndReturnTag(currentOrgId, isUber, new() { Name = n, OrganizationId = currentOrgId });
            if (status != StatusCodes.Status200OK) continue;
            if (response?.Result is null) continue;
            item.Tags.Add(response.Result);
        }
    }

    #endregion
}
