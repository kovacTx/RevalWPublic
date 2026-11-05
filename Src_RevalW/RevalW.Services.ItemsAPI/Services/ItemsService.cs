using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RevalW.MessageBus;
using RevalW.Services.ItemsAPI.Data;
using RevalW.Services.ItemsAPI.Model;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.DataTablesRequest;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;
using System.Security.Claims;

namespace RevalW.Services.ItemsAPI.Services;

public class ItemsService(AppDbContext db, IMapper mapper, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor, CommonData commonData, ISimpleHasher simpleHasher)
{
    private AuthService? _authService;
    private AuthService AuthService => _authService ??= serviceProvider.GetRequiredService<AuthService>();

    private TagsService? _tagsService;
    private TagsService TagsService => _tagsService ??= serviceProvider.GetRequiredService<TagsService>();

    private CTagsService? _cTagsService;
    private CTagsService CTagsService => _cTagsService ??= serviceProvider.GetRequiredService<CTagsService>();

    private CDocumentsService? _cDocumentsService;
    private CDocumentsService CDocumentsService => _cDocumentsService ??= serviceProvider.GetRequiredService<CDocumentsService>();

    private DocumentsService? _documentsService;
    private DocumentsService DocumentsService => _documentsService ??= serviceProvider.GetRequiredService<DocumentsService>();

    private OrganizationService? _organizationService;
    private OrganizationService OrganizationService => _organizationService ??= serviceProvider.GetRequiredService<OrganizationService>();

    private DevicesService? _devicesService;
    private DevicesService DevicesService => _devicesService ??= serviceProvider.GetRequiredService<DevicesService>();

    private AzurePrivateBlobService? _azurePrivateBlobService;
    private AzurePrivateBlobService AzurePrivateBlobService => _azurePrivateBlobService ??= serviceProvider.GetRequiredService<AzurePrivateBlobService>();

    private IMessageBus? _messageBus;
    private IMessageBus MessageBus => _messageBus ??= serviceProvider.GetRequiredService<IMessageBus>();

    private DataTablesWorker<Item, long, ItemHeaderDto>? _dataTablesWorker;
    private DataTablesWorker<Item, long, ItemHeaderDto> DataTablesWorker => _dataTablesWorker ??= serviceProvider.GetRequiredService<DataTablesWorker<Item, long, ItemHeaderDto>>();

    private Thorizer? _thorizer;
    private Thorizer Thorizer => _thorizer ??= serviceProvider.GetRequiredService<Thorizer>();

    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> Create(long currentOrgId, bool isUber, bool isAuthorized, string? userId, ItemUpsertDto dto, string? hash)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            if (!isAuthorized)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("user is not authorized for this operation"));
            if (simpleHasher.ComputeHash(dto.GetMyHash() + "uroboros") != hash)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("cannot create this Item"));
            if (dto.Id != default)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the id have to be 0"));
            if (dto.DeviceId == default)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the id of the assigned device can not be 0"));
            if (dto.Status != EItemStatus.New)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the status of the Item cannot be different than New"));

            if (!isUber || dto.OrganizationId == default) dto.OrganizationId = currentOrgId;

            if (dto.Tags is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("no Tags?"));
            if (dto.Tags.Count == 0)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("no Tags?"));
            dto.Tags = [.. dto.Tags.Where(n => n is not null)];

            var item = mapper.Map<Item>(dto);
            if (item is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read Item to save"));
            (item.ItemData, var success) = dto.ItemDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read ItemData to save"));
            (item.AppData, success) = dto.AppDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read AppData to save"));
            (item.AppFinishedData, success) = dto.FinishedDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read AppFinishedData to save"));

            var setDeviceResp = await SetDevice(authHeader, item.DeviceId, item);
            if (setDeviceResp?.IsSuccess == false)
                return (StatusCodes.Status400BadRequest, setDeviceResp.ToResponseDto<ItemUpsertDto>());
            (item.DeviceData, success) = dto.DeviceDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read DeviceData to save"));
            (item.ConfigData, success) = dto.ConfigDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read ConfigData to save"));

            item.LastUpdated = DateTime.UtcNow;
            item.Public = false;
            item.UserIdCreated = userId;
            await PrepareTags(dto.Tags, item, dto.OrganizationId, isUber);

            db.Items.Add(item);
            await db.SaveChangesAsync();
            var ret = mapper.Map<ItemUpsertDto>(item);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err("could not read saved Item"));
            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> Update(long currentOrgId, bool isUber, ClaimsPrincipal user, ItemUpsertDto dto)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            if (dto.Id == default)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the id can not be 0"));
            if (dto.DeviceId == default)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the id of the assigned device can not be 0"));

            var toUpdate = await db.Items.Include(c => c.Tags).FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("there is no such Item to update"));
            if (isUber) dto.OrganizationId = toUpdate.OrganizationId;
            else dto.OrganizationId = currentOrgId;

            var authorized = Thorizer.ThorizeUpdate(toUpdate, user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized.ToResponseDto<ItemUpsertDto>());

            if (toUpdate.OrganizationId != dto.OrganizationId)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not change the organization"));
            if (toUpdate.Status != dto.Status && !CanShiftStatus(toUpdate.Status, dto.Status))
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err($"invalid status transition ({toUpdate.Status} -> {dto.Status})"));

            if (dto.Tags is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("no Tags?"));
            if (dto.Tags.Count == 0)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("no Tags?"));
            dto.Tags = [.. dto.Tags.Where(n => n is not null)];

            var item = mapper.Map<Item>(dto);
            if (item is null)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("invalid Item to update"));
            (item.ItemData, var success) = dto.ItemDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read ItemData to save"));
            (item.AppData, success) = dto.AppDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read AppData to save"));
            (item.AppFinishedData, success) = dto.FinishedDataJson.ToJsonDocument();
            if (!success)
                return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read AppFinishedData to save"));

            var setDeviceResp = await SetDevice(authHeader, item.DeviceId, item);
            if (setDeviceResp?.IsSuccess != true)
                return (StatusCodes.Status400BadRequest, setDeviceResp?.ToResponseDto<ItemUpsertDto>() ?? ResponseDto<ItemUpsertDto>.Err("can not set Device for the Item"));

            bool configDataChanged = false;
            if (!string.IsNullOrEmpty(dto.ConfigDataJson))
            {
                var configDataStr = item.ConfigData?.ToJson();
                (item.ConfigData, success) = dto.ConfigDataJson.ToJsonDocument();
                if (!success)
                    return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read ConfigData to save"));
                configDataChanged = item.ConfigData?.ToJson() != configDataStr;
            }
            bool deviceDataChanged = false;
            if (!string.IsNullOrEmpty(dto.DeviceDataJson)) // mobile app is setting DeviceDataJson. if anyone want to change DeviceDataJson, accept it. else, just ignore it
            {
                var deviceDataStr = item.DeviceData?.ToJson();
                (item.DeviceData, success) = dto.DeviceDataJson.ToJsonDocument();
                if (!success)
                    return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not read DeviceData to save"));
                deviceDataChanged = item.DeviceData?.ToJson() != deviceDataStr;
            }

            // todo: maybe set deviceData and configData back to device and config

            var originalUserIdCreated = toUpdate.UserIdCreated;
            var originalUserIdExported = toUpdate.UserIdExported;
            var originalUserIdImported = toUpdate.UserIdImported;
            var originalUserIdFinalized = toUpdate.UserIdFinalized;
            var originalUserIdDeleted = toUpdate.UserIdDeleted;
            var originalUserIdUndeleted = toUpdate.UserIdUndeleted;
            var originalUserIdReadonlied = toUpdate.UserIdReadonlied;

            db.Items.Entry(toUpdate).CurrentValues.SetValues(item); // update scalar fields

            toUpdate.UserIdCreated = originalUserIdCreated;
            toUpdate.UserIdExported = originalUserIdExported;
            toUpdate.UserIdImported = originalUserIdImported;
            toUpdate.UserIdFinalized = originalUserIdFinalized;
            toUpdate.UserIdDeleted = originalUserIdDeleted;
            toUpdate.UserIdUndeleted = originalUserIdUndeleted;
            toUpdate.UserIdReadonlied = originalUserIdReadonlied;

            toUpdate.Tags.Clear(); // clear old relationships

            await PrepareTags(dto.Tags, toUpdate, dto.OrganizationId, isUber);
            toUpdate.LastUpdated = DateTime.UtcNow;
            toUpdate.Public = false;
            await db.SaveChangesAsync();

            var ret = mapper.Map<ItemUpsertDto>(toUpdate);
            if (ret is null)
                return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err("could not read updated Item"));
            if ((configDataChanged || deviceDataChanged) && setDeviceResp.Result is not null)
            {
                setDeviceResp.Result.DeviceDataJson = dto.DeviceDataJson;
                setDeviceResp.Result.ConfigDataJson = dto.ConfigDataJson;
                _ = DevicesService.Update(authHeader, setDeviceResp.Result); // this is not good habit. better do it via servicebus
            }
            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> DeviceChanged(long currentOrgId, bool isUber, ClaimsPrincipal user, DeviceUpsertDto dto)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            if (dto.Id == default)
                return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));

            if (isUber) currentOrgId = dto.OrganizationId;
            if (currentOrgId != dto.OrganizationId)
                return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not change the device of different organization"));

            var toUpdate = db.Items.Where(i => i.DeviceId == dto.Id && i.OrganizationId == currentOrgId && i.Status == EItemStatus.New);
            if (!(toUpdate?.Count() > 0))
                return (StatusCodes.Status304NotModified, ResponseDtoo.Err("there is no such Item to update"));

            foreach (var i in toUpdate)
            {
                var authorized = Thorizer.ThorizeSetDevice(i, user);
                if (authorized is not null) continue;

                (i.DeviceData, _) = dto.DeviceDataJson.ToJsonDocument();
                (i.ConfigData, _) = dto.ConfigDataJson.ToJsonDocument();
            }
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> AppendDevice(long currentOrgId, bool isUber, ClaimsPrincipal user, long id, long deviceId)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id of Item can not be 0"));
            if (deviceId == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id of Device can not be 0"));
            var toUpdate = await db.Items.FirstOrDefaultAsync(tn => tn.Id == id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to update"));
            if (isUber) currentOrgId = toUpdate.OrganizationId;
            if (toUpdate.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not update the Item that does not belongs to this organization"));
            //if (toUpdate.DeviceId != 0) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the Item already has a Device assigned"));

            var authorized = Thorizer.ThorizeSetDevice(toUpdate, user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            var (status, message) = await DevicesService.GetHeaderById(authHeader, deviceId);
            if (message?.Result is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Device to assign"));
            if (message.Result.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not assign Device that does not belongs to this organization"));

            toUpdate.DeviceId = deviceId;

            var setDeviceResp = await SetDevice(authHeader, toUpdate.DeviceId, toUpdate);
            if (setDeviceResp?.IsSuccess != true)
                return (StatusCodes.Status400BadRequest, setDeviceResp?.ToResponseDtoo() ?? ResponseDtoo.Err("can not set Device for the Item"));

            toUpdate.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> TryMarkFinished(long currentOrgId, bool isUber, long id, string? userId, string hash1, string hash2)
    {
        try
        {
            if (string.IsNullOrEmpty(userId)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("who is marking the Item as finished?"));
            if (string.IsNullOrEmpty(hash1)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("no hash?"));
            var controlHash2 = simpleHasher.ComputeHash(hash1 + "uroboros");
            if (controlHash2 != hash2) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("invalid hash"));
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id of Item can not be 0"));
            var toMark = await db.Items.Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(tn => tn.Id == id);
            if (toMark is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to mark"));
            if (toMark.DeviceId == 0) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not mark the Item that does not have any Device inside"));
            if (isUber) currentOrgId = toMark.OrganizationId;

            if (toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not mark the Item that does not belongs to this organization"));
            if (toMark.Status != EItemStatus.Imported && toMark.Status != EItemStatus.New) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("only imported Items can be marked as finished"));

            if (toMark.Documents2Print.Any(d2p => !toMark.GeneratedDocuments.Any(gd => gd.DocumentId == d2p)))
                return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not mark as finished Item with pending document generation"));

            toMark.Status = EItemStatus.Finished;
            toMark.UserIdFinalized = userId;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> GetById(long currentOrgId, bool isUber, long id)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        try
        {
            var item = await db.Items.Include(i => i.Tags).Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(i => i.Id == id && (isUber || i.OrganizationId == currentOrgId));
            if (item is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("item with this id does not exists"));
            var ret = mapper.Map<ItemUpsertDto>(item);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err("could not read Item"));
            if (item.DeviceId != 0)
            {
                var (_, message) = await DevicesService.GetHeaderById(authHeader, item.DeviceId);
                ret.Device = message?.Result;
            }

            ret.AppDataJson = TypeDiscriminatorHelper.ReorderTypeDiscriminatorInArray(item.AppData)?.ToJsonString();
            ret.FinishedDataJson = TypeDiscriminatorHelper.ReorderTypeDiscriminatorInArray(item.AppFinishedData)?.ToJsonString();
            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemHeaderDto> response)> GetHeaderById(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var item = await db.Items.Include(i => i.Tags).Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(i => i.Id == id && (isUber || i.OrganizationId == currentOrgId));
            if (item is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemHeaderDto>.Err("item with this id does not exists"));
            var ret = mapper.Map<ItemHeaderDto>(item);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ItemHeaderDto>.Err("could not read Item"));
            return (StatusCodes.Status200OK, ResponseDto<ItemHeaderDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemHeaderDto>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<List<ItemHeaderDto>> response) GetByTag(long currentOrgId, bool isUber, string tag)
    {
        try
        {
            var item = db.Items.Include(i => i.Tags).Where(i => ((i.Status & EItemStatus.Deleted) == 0) && i.Tags.Any(t => t.Name == tag) && (isUber || i.OrganizationId == currentOrgId));
            if (item is null) return (StatusCodes.Status404NotFound, ResponseDto<List<ItemHeaderDto>>.Err("item with this id does not exists"));
            var ret = mapper.Map<List<ItemHeaderDto>>(item);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<ItemHeaderDto>>.Err("could not read Item"));
            return (StatusCodes.Status200OK, ResponseDto<List<ItemHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<ItemHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<ResponseDto<DataTablesResponse<ItemHeaderDto>>> GetByDataTables(long currentOrgId, bool isUber, Request request)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        var allStatus = request.StatusFilter?.ToLower() == "all";
        if (!Enum.TryParse<EItemStatus>(request.StatusFilter, out var status)) status = EItemStatus.New;
        var (_, itemGridColumnsResp) = await OrganizationService.GetMainGridColumns(authHeader);
        var queryable = db.Items
            .Include(i => i.Tags)
            .Where(i => isUber || i.OrganizationId == currentOrgId);
        if (!allStatus) queryable = queryable.Where(c => c.Status == status);

        var mainGridColumns = AppendDbColumnsForDataTables(GetJsonColumnsForDataTables(itemGridColumnsResp?.Result));

        var ret = await DataTablesWorker.DoDataTablesSelect(queryable, request, mainGridColumns);
        if (ret.data is null)
            return ResponseDto<DataTablesResponse<ItemHeaderDto>>.Ok(ret);
        foreach (var dto in ret.data)
        {
            dto.Tags = await GetTagsForItem(dto.Id);
            if (!dto.DeviceId.HasValue) continue;
            if (dto.DeviceId.Value == 0) continue;
            var deviceResp = await DevicesService.GetHeaderById(authHeader, dto.DeviceId.Value);
            dto.Device = deviceResp.message?.Result;
        }
        return ResponseDto<DataTablesResponse<ItemHeaderDto>>.Ok(ret);
    }
    public async Task<(int status, ResponseDto<IEnumerable<ItemAppHeaderDto>> response)> GetHeadersForApp(long currentOrgId, bool isUber, ClaimsPrincipal user, int? pageNum, int? pageSize, EItemStatus? statusFilter, List<InAppItemDto> inAppItems)
    {
        try
        {
            if (currentOrgId != 1 && !user.IsAuthorized(RightsList.Item_View))
                ResponseDto<IEnumerable<ItemAppHeaderDto>>.Err("user is not authorized for this operation");
            var itemsQ = db.Items.Include(c => c.Tags).Include(i => i.GeneratedDocuments)
                .Where(i => isUber || i.OrganizationId == currentOrgId);
            itemsQ = itemsQ.OrderBy(c => c.Id);
            var dbItems = await itemsQ.ToListAsync();

            var dbById = dbItems.ToDictionary(i => i.Id);

            var resultList = new List<ItemAppHeaderDto>();
            var usedDbIds = new HashSet<long>();

            // 1) First, add items coming from the app (inAppItems) preserving their order.
            AddInAppItems(inAppItems, usedDbIds, dbById, resultList);
            // 2) Then add remaining DB items that were not present in inAppItems
            AddRestItems(dbItems, usedDbIds, resultList);
            // 3) Filter by status
            IEnumerable<ItemAppHeaderDto> result;
            if (!statusFilter.HasValue || (statusFilter.Value == 0)) result = resultList;
            else
            {
                var userId = user.CurrentUserId();
                result = resultList.Where(i =>
                {
                    if ((i.Status & statusFilter.Value) != 0) return true;
                    if (inAppItems.FirstOrDefault(iai => iai.DbId == i.Id) is not null) return true;
                    if (statusFilter != EItemStatus.Readonly && i.Status == EItemStatus.Exported && i.UserIdExported == userId) return true;
                    return false;
                });
            }

            // 4) Apply paging on the composed list
            IEnumerable<ItemAppHeaderDto> paged;
            if (pageNum.HasValue && pageSize.HasValue)
                paged = result.Skip(pageNum.Value * pageSize.Value).Take(pageSize.Value);
            else
                paged = result;

            return (StatusCodes.Status200OK, ResponseDto<IEnumerable<ItemAppHeaderDto>>.Ok(paged));

        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<ItemAppHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> MarkAsNew(long currentOrgId, bool isUber, ClaimsPrincipal user, long id)
    {
        try
        {
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));
            var toMark = await db.Items.FirstOrDefaultAsync(tn => tn.Id == id);
            if (toMark is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to mark as new"));
            if (isUber) currentOrgId = toMark.OrganizationId;

            if (toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not mark as new the Item that does not belongs to this organization"));

            var authorized = Thorizer.ThorizeStatusReset(toMark, user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            toMark.Status = EItemStatus.New;
            toMark.Documents2Print = [];
            toMark.UserIdExported = null;
            toMark.UsedConfigId = null;
            toMark.AppFinishedData = null;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> ForwardState(long currentOrgId, bool isUber, long id, string? userId)
    {
        try
        {
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

            if (string.IsNullOrEmpty(userId)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("who is forwarding the state?"));
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));
            var toMark = await db.Items.FirstOrDefaultAsync(tn => tn.Id == id);
            if (toMark is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to forward"));
            if (isUber) currentOrgId = toMark.OrganizationId;

            if (toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not forward the Item that does not belongs to this organization"));

            if (toMark.Status == EItemStatus.New)
            {
                // todo: finish
                // go directly to finished
            }
            else if (toMark.Status == EItemStatus.Exported)
            {
                toMark.Status = EItemStatus.Imported;
                toMark.UserIdImported = userId;
                toMark.LastUpdated = DateTime.UtcNow;
                await db.SaveChangesAsync();

                var ret = await StartGenerateDocuments(authHeader, toMark, false);
                if (ret.HasValue) return ret.Value;
            }
            else if (toMark.Status == EItemStatus.Imported)
            {
                if (toMark.DeviceId == 0)
                    return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("no device?"));

                var ret = await StartGenerateDocuments(authHeader, toMark, true);
                if (ret.HasValue) return ret.Value;
            }
            else return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not forward state of item that is not in 'New' or 'Imported' state"));

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> ReadonlyState(long currentOrgId, bool isUber, ClaimsPrincipal user, long id, bool leaveDevice)
    {
        try
        {
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));
            var toMark = await db.Items.FirstOrDefaultAsync(tn => tn.Id == id);
            if (toMark is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to readonly"));
            if (isUber) currentOrgId = toMark.OrganizationId;

            if (toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not readonly the Item that does not belongs to this organization"));
            var authorized = Thorizer.ThorizeReadonly(toMark, user);
            if (authorized is not null) return (StatusCodes.Status400BadRequest, authorized);

            if (!leaveDevice)
            {
                toMark.DeviceId = 0;
                toMark.DeviceData = null;
            }
            toMark.Status = EItemStatus.Readonly;
            toMark.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DocumentCreationResult?> response)> GenerateDoc(long currentOrgId, bool isUber, long itemId, long documentId)
    {
        try
        {
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

            if (itemId == default) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("the item id can not be 0"));
            if (documentId == default) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("the document id can not be 0"));
            var item = await db.Items.Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(tn => tn.Id == itemId);
            if (item is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("there is no such Item"));
            if (isUber) currentOrgId = item.OrganizationId;

            if (item.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("can not generate document on the Item that does not belongs to this organization"));
            if (item.Status != EItemStatus.Imported) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("documents can be generated only on imported Item"));
            if (!item.Documents2Print.Contains(documentId)) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("this document is not marked for generation on this Item"));

            var existingDocument = item.GeneratedDocuments.FirstOrDefault(gd => gd.DocumentId == documentId);
            if (existingDocument is not null)
            {
                await AzurePrivateBlobService.DeleteFileByBaseNameAsync(existingDocument.FilePath);
                db.GeneratedDocuments.Remove(existingDocument);
                await db.SaveChangesAsync();
            }

            var documentGenReqResp = await GetDocumentGenerateRequest(authHeader, item, documentId);
            if (documentGenReqResp?.Result is null || !documentGenReqResp.IsSuccess)
                return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err(documentGenReqResp?.Message ?? "could not get document generate request"));

            var (docStatus, documentGenResp) = await DocumentsService.Generate(authHeader, documentGenReqResp.Result);
            return (docStatus, documentGenResp ?? ResponseDto<DocumentCreationResult?>.Err("could not generate document"));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentCreationResult?>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<string?>? response)> GenerateDocStart(long currentOrgId, bool isUber, long itemId, long documentId)
    {
        try
        {
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

            if (itemId == default) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("the item id can not be 0"));
            if (documentId == default) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("the document id can not be 0"));
            var item = await db.Items.Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(tn => tn.Id == itemId);
            if (item is null) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("there is no such Item"));
            if (isUber) currentOrgId = item.OrganizationId;

            if (item.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("can not generate document on the Item that does not belongs to this organization"));
            if (item.Status != EItemStatus.Imported) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("documents can be generated only on imported Item"));
            if (!item.Documents2Print.Contains(documentId)) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("this document is not marked for generation on this Item"));

            var existingDocument = item.GeneratedDocuments.FirstOrDefault(gd => gd.DocumentId == documentId);
            if (existingDocument is not null)
            {
                await AzurePrivateBlobService.DeleteFileByBaseNameAsync(existingDocument.FilePath);
                db.GeneratedDocuments.Remove(existingDocument);
                await db.SaveChangesAsync();
            }

            var documentGenReqResp = await GetDocumentGenerateRequest(authHeader, item, documentId);
            if (documentGenReqResp?.Result is null || !documentGenReqResp.IsSuccess)
                return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err(documentGenReqResp?.Message ?? "could not get document generate request"));

            var (docStatus, documentGenResp) = await DocumentsService.GenerateStart(authHeader, documentGenReqResp.Result);
            return (docStatus, documentGenResp);
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<string?>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DocumentCreationResult?> response)> GenerateDocProgress(string? operationId)
    {
        try
        {
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;

            if (string.IsNullOrEmpty(operationId)) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentCreationResult?>.Err("no operation Id?"));
            var (docStatus, documentGenResp) = await DocumentsService.GenerateProgress(authHeader, operationId);
            return (docStatus, documentGenResp ?? ResponseDto<DocumentCreationResult?>.Err("could not get document generation progress"));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentCreationResult?>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> MarkExported(long currentOrgId, bool isUber, long id, string? userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("who is marking the Item as exported?"));
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));
            var toMark = await db.Items.FirstOrDefaultAsync(tn => tn.Id == id);
            if (toMark is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to mark"));
            if (isUber) currentOrgId = toMark.OrganizationId;

            if (toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not mark the Item that does not belongs to this organization"));
            if (toMark.Status != EItemStatus.New) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("only new Items can be marked as exported"));

            toMark.Status = EItemStatus.Exported;
            toMark.UserIdExported = userId;
            toMark.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> Free(long currentOrgId, bool isUber, long id, string? userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("who is freeing the Item?"));
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));
            var toMark = await db.Items.FirstOrDefaultAsync(tn => tn.Id == id);
            if (toMark is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Item to free"));
            if (isUber) currentOrgId = toMark.OrganizationId;

            if (toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not free the Item that does not belongs to this organization"));
            if (toMark.UserIdExported != userId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not free Item that was not exported by this user"));
            if (toMark.Status != EItemStatus.Exported) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("only exported Items can be freed"));

            toMark.UserIdExported = null;
            toMark.Status = EItemStatus.New;
            toMark.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> MarkDeleted(long currentOrgId, bool isUber, long id, string? userId)
    {
        try
        {
            var toMark = await db.Items.FindAsync(id);
            if (toMark is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("can not delete Item that does not exists"));
            if (!isUber && toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not delete Item that does not belongs to this organization"));

            toMark.Status = EItemStatus.Deleted;
            toMark.UserIdDeleted = userId;
            toMark.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> MarkAsUndeleted(long currentOrgId, bool isUber, long id, string? userId)
    {
        try
        {
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the id can not be 0"));

            var toMark = await db.Items.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
            if (toMark is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("ca not undelete Item that does not exists"));
            if (!isUber && toMark.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("can not undelete Item that does not belongs to this organization"));


            await RemoveNonExistentTags(toMark);
            toMark.Status = EItemStatus.New;
            toMark.UserIdUndeleted = userId;
            toMark.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();
            var ret = mapper.Map<ItemUpsertDto>(toMark);
            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> DocumentGenerated(GeneratedDocumentDto dto)
    {
        try
        {
            var document = mapper.Map<GeneratedDocument>(dto);
            if (document.ItemId == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id of Item can not be 0"));

            var item = await db.Items.Include(i => i.GeneratedDocuments).FirstOrDefaultAsync(i => i.Id == document.ItemId);

            if (item is null) return (StatusCodes.Status404NotFound, ResponseDtoo.Err("there is no Item where to put Document"));

            var gd = await db.GeneratedDocuments.FirstOrDefaultAsync(d => d.ItemId == document.ItemId && d.DocumentId == document.DocumentId);

            // replace document if already exists
            if (gd is not null)
            {
                if (gd.FilePath != document.FilePath) await AzurePrivateBlobService.DeleteFileByBaseNameAsync(gd.FilePath);
                gd.FilePath = document.FilePath;
                gd.Title = document.Title;
                TryFinishIfEverythingIsGenerated(item, dto);
                await db.SaveChangesAsync();
                return (StatusCodes.Status200OK, ResponseDtoo.Ok());
            }

            db.GeneratedDocuments.Add(document);
            TryFinishIfEverythingIsGenerated(item, dto);

            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> RemoveDocument(long currentOrgId, bool isUber, long id)
    {
        try
        {
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id can not be 0"));

            var toDelete = await db.GeneratedDocuments.Include(d => d.Item).FirstOrDefaultAsync(d => d.Id == id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDtoo.Err("can not delete Document that does not exists"));
            if (!isUber && toDelete.Item.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("can not delete Document that does not belongs to this organization"));

            db.GeneratedDocuments.Remove(toDelete);
            await db.SaveChangesAsync();
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<ResponseDto<Stream>> DownloadDocument(string path)
    {
        try
        {
            if (path is null) return ResponseDto<Stream>.Err("there is no file");
            return ResponseDto<Stream>.Ok(await AzurePrivateBlobService.DownloadFileAsync(path));
        }
        catch (Exception e)
        {
            return ResponseDto<Stream>.Err(e.ToString());
        }
    }

    #region private
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> AppendDbColumnsForDataTables(IEnumerable<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? jsonColumns)
    {
        List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> ret;
        if (jsonColumns is null) ret = [];
        else ret = [.. jsonColumns];
        ret.Add((nameof(Item.Label), null));
        return ret;
    }
    private static IEnumerable<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? GetJsonColumnsForDataTables(List<MainGridColumnDto>? mainGridColumns)
    {
        if (mainGridColumns is null) return null;
        var ret = mainGridColumns.GroupBy(mgc => mgc.Prefix).Select(g =>
        {
            var jsonColName = g.Key;
            if (string.IsNullOrEmpty(jsonColName)) return (null, g.Select(mgc => mgc.Name));
            if (jsonColName.StartsWith("d", StringComparison.CurrentCultureIgnoreCase))
                return (nameof(Item.DeviceData), g.Select(mgc => mgc.Name));
            if (jsonColName.StartsWith("i", StringComparison.CurrentCultureIgnoreCase))
                return (nameof(Item.ItemData), g.Select(mgc => mgc.Name));
            return (g.Key, g.Select(mgc => mgc.Name));
        });
        return ret;
    }
    private async Task<List<string>> GetTagsForItem(long id)
    {
        var e = await db.Items.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (e is null || e.Tags is null) return [];
        return [.. e.Tags.Select(t => t.Name)];
    }
    private async Task<ResponseDto<DocumentGenerateRequest?>> GetDocumentGenerateRequest(StringValues authHeader, Item item, long dId, bool tryFinishAfter = false)
    {
        try
        {
            var documentsResourcesResp = await CDocumentsService.GetDocumentsResourcesByConfigId(authHeader, item.UsedConfigId ?? 0);
            if (!item.Documents2Print.Contains(dId)) return ResponseDto<DocumentGenerateRequest?>.Err($"Item can not generate this document");
            var documentsResources = documentsResourcesResp?.Result?.Select(dr => (dr.Name, dr.PrivatePath));

            var documentResp = await CDocumentsService.GetById(authHeader, dId);
            if (documentResp?.Result is null || !documentResp.IsSuccess) return ResponseDto<DocumentGenerateRequest?>.Err($"Can not get this document template");
            var documentDto = documentResp.Result;

            var userDataResp = await AuthService.GetUserData(authHeader, item.UserIdImported);
            if (userDataResp?.Result is null || !userDataResp.IsSuccess) return ResponseDto<DocumentGenerateRequest?>.Err($"Can not get user data");
            var userDataJson = userDataResp.Result;

            var ret = new DocumentGenerateRequest
            {
                DocumentTemplateId = dId,
                DocumentLabel = documentDto.Label,
                ItemId = item.Id,
                OrganizationId = item.OrganizationId,
                AppDataJson = TypeDiscriminatorHelper.ReorderTypeDiscriminatorInArray(item.AppFinishedData)?.ToJsonString(),
                ItemDataJson = item.ItemData.ToJson(),
                ConfigDataJson = item.ConfigData.ToJson(),
                UserDataJson = userDataJson,
                Template = documentDto.Template,
                ResourceFiles = documentsResources,
                TryFinishAfter = tryFinishAfter
            };

            return ResponseDto<DocumentGenerateRequest?>.Ok(ret);
        }
        catch (Exception e)
        {
            return ResponseDto<DocumentGenerateRequest?>.Err(e.ToString());
        }
    }
    private async Task<(int status, ResponseDtoo response)?> StartGenerateDocuments(StringValues authHeader, Item item, bool tryFinishAfter)
    {
        try
        { // todo: add idempotency check to avoid generation of document that is generating.
          // but allow regenerating of already generated document if requested.
            await RemoveAllDocuments(item.OrganizationId, item.Id);
            var errDocs = new List<long>();
            foreach (var dId in item.Documents2Print)
            {
                var documentGenReqResp = await GetDocumentGenerateRequest(authHeader, item, dId, tryFinishAfter);
                if (documentGenReqResp?.Result is null || !documentGenReqResp.IsSuccess)
                {
                    errDocs.Add(dId);
                    continue;
                }
                await MessageBus.PublishMessage(documentGenReqResp.Result, commonData.GenerateDocumentQueueName, commonData.DefaultQueueConnection);
            }
            if (errDocs.Count > 0) return (StatusCodes.Status200OK, ResponseDtoo.Err($"Failed to generate documents with ids {string.Join(", ", errDocs)}"));
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    private async Task RemoveAllDocuments(long currentOrgId, long itemId)
    {
        var rootPath = NameGenerator.GetPdfFilesPath(currentOrgId);
        await AzurePrivateBlobService.DeleteFileByBaseNameAsync(rootPath);
        db.GeneratedDocuments.RemoveRange(db.GeneratedDocuments.Where(d => d.ItemId == itemId));
        await db.SaveChangesAsync();
    }
    private static bool CanShiftStatus(EItemStatus oldStatus, EItemStatus newStatus)
        => (oldStatus, newStatus) switch
        {
            (EItemStatus.New, EItemStatus.Exported) => true,
            (EItemStatus.Exported, EItemStatus.Imported) => true,
            (EItemStatus.Imported, EItemStatus.Finished) => true,
            _ => false,
        };
    private async Task RemoveNonExistentTags(Item? item)
    {
        if (item is null) return;
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        var (status, message) = await CTagsService.GetAll(authHeader, false);
        if (status != StatusCodes.Status200OK || message is null || !message.IsSuccess || message.Result is null) return;
        var allTags = message.Result;
        var toRem = new List<Tag>();
        foreach (var t in item.Tags)
        {
            if (!allTags.Any(ct => ct.Name == t.Name)) toRem.Add(t);
        }
        item.Tags.RemoveAll(t => toRem.FirstOrDefault(tr => tr.Name == t.Name) is not null);
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
    private async Task PrepareTags(IEnumerable<string> names, Item item, long currentOrgId, bool isUber)
    {
        item.Tags = [];
        foreach (var n in names)
        {
            var tn = await TagsService.GetEntityByName(currentOrgId, isUber, n);
            if (tn is not null)
            {
                item.Tags.Add(tn);
                continue;
            }
            var (status, response) = await TagsService.CreateAndReturnEntity(currentOrgId, isUber, new() { Name = n, OrganizationId = currentOrgId });
            if (status != StatusCodes.Status200OK) continue;
            if (response?.Result is null) continue;
            item.Tags.Add(response.Result);
        }
    }
    private void AddInAppItems(List<InAppItemDto>? inAppItems, HashSet<long> usedDbIds, Dictionary<long, Item> dbById, List<ItemAppHeaderDto> result)
    {
        //    - If inAppItem has DbId and DB item exists -> map DB item and set AppId.
        //    - If no DbId (or DB item not found) -> create minimal ItemAppHeaderDto with only AppId.
        if (inAppItems is null) return;
        foreach (var appItem in inAppItems)
        {
            if (appItem.DbId > 0 && dbById.TryGetValue(appItem.DbId, out var dbItem))
            {
                var mapped = mapper.Map<ItemAppHeaderDto>(dbItem) ?? new ItemAppHeaderDto();
                mapped.AppId = appItem.AppId;
                result.Add(mapped);
                usedDbIds.Add(appItem.DbId);
            }
            else
            {
                // item exists only in app (no DbId) or Db item missing -> minimal header with AppId
                result.Add(new ItemAppHeaderDto { AppId = appItem.AppId });
            }
        }
    }
    private void AddRestItems(List<Item> dbItems, HashSet<long> usedDbIds, List<ItemAppHeaderDto> result)
    {
        foreach (var dbItem in dbItems)
        {
            if (usedDbIds.Contains(dbItem.Id)) continue;
            if ((dbItem.Status & EItemStatus.Deleted) != 0) continue;

            var mapped = mapper.Map<ItemAppHeaderDto>(dbItem);
            if (mapped is null) continue;

            result.Add(mapped);
        }
    }
    private async Task<ResponseDto<DeviceUpsertDto?>?> SetDevice(StringValues authHeader, long deviceId, Item into)
    {
        if (deviceId == 0) return null;

        var (_, message) = await DevicesService.GetById(authHeader, deviceId);
        if (message?.Result is null)
            return ResponseDto<DeviceUpsertDto?>.Err("can not read Device");

        (into.DeviceData, var success) = message.Result.DeviceDataJson.ToJsonDocument();
        if (!success)
            return ResponseDto<DeviceUpsertDto?>.Err("can not read DeviceData to save");

        (into.ConfigData, success) = message.Result.ConfigDataJson.ToJsonDocument();
        if (!success)
            return ResponseDto<DeviceUpsertDto?>.Err("can not read ConfigData to save");

        into.Label = message.Result.Label;
        return ResponseDto<DeviceUpsertDto?>.Ok(message.Result);
    }
    private static void TryFinishIfEverythingIsGenerated(Item? item, GeneratedDocumentDto dto)
    {
        if (!dto.FinishAfter) return;
        if (item is null) return;
        if (!item.Documents2Print.ToHashSet().SetEquals(item.GeneratedDocuments.Select(d => d.DocumentId))) return;
        item.Status = EItemStatus.Finished;
    }

    #endregion
}
