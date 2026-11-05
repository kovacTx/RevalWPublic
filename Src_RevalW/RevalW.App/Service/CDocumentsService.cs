using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;

namespace RevalW.App.Service;

public class CDocumentsService(IBaseService baseService, CommonData cd)
{
    public async Task<ResponseDto<DocumentTemplateDto?>?> Create(DocumentTemplateDto dto)
    {
        return await baseService.SendAsync<DocumentTemplateDto>(new RequestDto(cd.CDocumentsAPIUrl + "/create")
        {
            ApiType = EApiType.POST,
            Data = dto,
        });
    }
    public async Task<ResponseDto<DocumentTemplateDto?>?> GetById(long id)
    {
        return await baseService.SendAsync<DocumentTemplateDto>(new RequestDto(cd.CDocumentsAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<IEnumerable<DocumentTemplateDto>?>?> GetByConfigId(long id)
    {
        return await baseService.SendAsync<IEnumerable<DocumentTemplateDto>>(new RequestDto(cd.CDocumentsAPIUrl + $"/byConfigId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<IEnumerable<DocumentHeaderDto>?>?> GetHeadersByConfigId(long id)
    {
        return await baseService.SendAsync<IEnumerable<DocumentHeaderDto>>(new RequestDto(cd.CDocumentsAPIUrl + $"/headersByConfigId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<DocumentTemplateDto?>?> Update(DocumentTemplateDto dto)
    {
        return await baseService.SendAsync<DocumentTemplateDto>(new RequestDto(cd.CDocumentsAPIUrl + $"/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDto<ConfigUpsertDto?>?> AutosaveAsync(MonacoAutosaveRequest dto)
    {
        return await baseService.SendAsync<ConfigUpsertDto>(new RequestDto(cd.CDocumentsAPIUrl + "/autosave")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDto<List<DocumentsResourceFileDto>?>?> GetResourcesByConfigIdAsync(long id)
    {
        return await baseService.SendAsync<List<DocumentsResourceFileDto>>(new RequestDto(cd.CDocumentsAPIUrl + $"/resourcesByConfigId?id={id}")
        {
            ApiType = EApiType.GET
        });
    }
    public async Task<ResponseDtoo?> UploadResourceFileAsync(UploadDocumentsResourceFileDto dto)
    {
        return await baseService.SendAsync(new RequestDto(cd.CDocumentsAPIUrl + "/uploadResource")
        {
            ApiType = EApiType.PUT,
            Data = dto,
            ContentType = EContentType.MultipartFormData
        });
    }
    public async Task<ResponseDtoo?> RemoveResourceFileAsync(long id)
    {
        return await baseService.SendAsync(new RequestDto(cd.CDocumentsAPIUrl + $"/removeResource?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    public async Task<ResponseDto<ItemUpsertDto?>?> Delete(long id)
    {
        return await baseService.SendAsync<ItemUpsertDto>(new RequestDto(cd.CDocumentsAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    public async Task<ResponseDto<ItemUpsertDto?>?> DeleteByConfigId(long id)
    {
        return await baseService.SendAsync<ItemUpsertDto>(new RequestDto(cd.CDocumentsAPIUrl + $"/deleteByConfigId?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    public async Task<ResponseDto<ItemUpsertDto?>?> ReallyDelete(long id)
    {
        return await baseService.SendAsync<ItemUpsertDto>(new RequestDto(cd.CDocumentsAPIUrl + $"/reallyDelete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    public async Task<ResponseDto<ItemUpsertDto?>?> Undelete(long id)
    {
        return await baseService.SendAsync<ItemUpsertDto>(new RequestDto(cd.CDocumentsAPIUrl + $"/undelete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }

}
