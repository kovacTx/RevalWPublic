using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.App.Service;

public class ItemsService(IBaseService baseService, ISimpleHasher simpleHasher, CommonData cd)
{
    public async Task<ResponseDto<ItemUpsertDto?>?> CreateAsync(ItemUpsertDto dto)
    {
        return await baseService.SendAsync<ItemUpsertDto>(new RequestDto(cd.ItemsAPIUrl + $"/create?hash={simpleHasher.ComputeHash(dto.GetMyHash() + "uroboros")}")
        {
            ApiType = EApiType.POST,
            Data = dto,
        });
    }
    public async Task<ResponseDto<ItemUpsertDto?>?> GetItemByIdAsync(long id)
    {
        return await baseService.SendAsync<ItemUpsertDto?>(new RequestDto(cd.ItemsAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<DataTablesResponse<ItemHeaderDto>?>?> GetByDataTablesAsync(DTRequest request)
    {
        return await baseService.SendAsync<DataTablesResponse<ItemHeaderDto>>(new RequestDto(cd.ItemsAPIUrl + "/byDataTables")
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<ItemUpsertDto?>?> UpdateAsync(ItemUpsertDto dto)
    {
        return await baseService.SendAsync<ItemUpsertDto>(new RequestDto(cd.ItemsAPIUrl + "/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDto<EItemStatus>?> UpdateItemStatus(long itemId, EItemStatus newStatus)
    {
        return await baseService.SendAsync<EItemStatus>(new RequestDto(cd.ItemsAPIUrl + $"/updateStatus?id={itemId}&newStatus={newStatus}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDto<EItemStatus>?> MarkAsNew(long id)
    {
        return await baseService.SendAsync<EItemStatus>(new RequestDto(cd.ItemsAPIUrl + $"/markAsNew?id={id}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDto<EItemStatus>?> ForwardState(long id)
    {
        return await baseService.SendAsync<EItemStatus>(new RequestDto(cd.ItemsAPIUrl + $"/forwardState?id={id}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDto<EItemStatus>?> ReadonlyState(long id, bool leaveDevice)
    {
        return await baseService.SendAsync<EItemStatus>(new RequestDto(cd.ItemsAPIUrl + $"/readonlyState?id={id}&leaveDevice={leaveDevice}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDtoo?> AppendDevice(long id, long deviceId)
    {
        return await baseService.SendAsync(new RequestDto(cd.ItemsAPIUrl + $"/appendDevice?id={id}&deviceId={deviceId}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDto<DocumentCreationResult?>?> GenerateDocument(long itemId, long documentId)
    {
        return await baseService.SendAsync<DocumentCreationResult>(new RequestDto(cd.ItemsAPIUrl + $"/generateDocument?itemId={itemId}&documentId={documentId}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDto<string?>?> GenerateDocumentStart(long itemId, long documentId)
    {
        return await baseService.SendAsync<string>(new RequestDto(cd.ItemsAPIUrl + $"/generateDocument-start?itemId={itemId}&documentId={documentId}")
        {
            ApiType = EApiType.PUT,
        });
    }
    public async Task<ResponseDto<DocumentCreationResult?>?> GenerateDocumentProgress(string operationId, string token)
    {
        return await baseService.SendAsync<DocumentCreationResult?>(new RequestDto(cd.ItemsAPIUrl + $"/generateDocument-progress?operationId={operationId}")
        {
            ApiType = EApiType.GET,
        }, withBearer: true, token: token);
    }
    public async Task<ResponseDtoo?> DeleteItemAsync(long id)
    {
        return await baseService.SendAsync(new RequestDto(cd.ItemsAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    public async Task<ResponseDtoo?> UndeleteItemAsync(long id)
    {
        return await baseService.SendAsync(new RequestDto(cd.ItemsAPIUrl + $"/undelete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    public async Task<ResponseDto<Stream?>?> DownloadDocumentAsync(string path)
    {
        return await baseService.SendAndReadStreamAsync(new RequestDto(cd.ItemsAPIUrl + $"/downloadDocument?path={path}")
        {
            ApiType = EApiType.GET,
        });
    }

}
