using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.App.Service;

public class ConfigsService(IBaseService baseService, CommonData cd)
{
    public async Task<ResponseDto<ConfigUpsertDto?>?> CreateAsync(ConfigUpsertDto dto)
    {
        return await baseService.SendAsync<ConfigUpsertDto>(new RequestDto(cd.ConfigsAPIUrl + "/create")
        {
            ApiType = EApiType.POST,
            Data = dto,
        });
    }
    public async Task<ResponseDto<DataTablesResponse<ConfigHeaderDto>?>?> GetByDataTablesAsync(DTRequest request, bool? deleted)
    {
        var path = "/byDataTables";
        if (deleted.HasValue) path += $"?deleted={deleted}";

        return await baseService.SendAsync<DataTablesResponse<ConfigHeaderDto>>(new RequestDto(cd.ConfigsAPIUrl + path)
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<ConfigUpsertDto?>?> GetByIdAsync(long id)
    {
        return await baseService.SendAsync<ConfigUpsertDto?>(new RequestDto(cd.ConfigsAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<ConfigHeaderDto>?>?> GetByTagAsync(string tag)
    {
        return await baseService.SendAsync<List<ConfigHeaderDto>?>(new RequestDto(cd.ConfigsAPIUrl + $"/byTag?tag={tag}")
        {
            ApiType = EApiType.GET,
        });
    }

    public async Task<ResponseDto<List<ConfigHeaderDto>?>?> GetByOrganizationIdAsync(long id)
    {
        return await baseService.SendAsync<List<ConfigHeaderDto>?>(new RequestDto(cd.ConfigsAPIUrl + $"/byOrganizationId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<DynamicPropertyDto>?>?> GetDynamicProps4Config(long id)
    {
        return await baseService.SendAsync<List<DynamicPropertyDto>>(new RequestDto(cd.ConfigsAPIUrl + $"/dynaminProperties4Config?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<ConfigUpsertDto?>?> UpdateAsync(ConfigUpsertDto dto)
    {
        return await baseService.SendAsync<ConfigUpsertDto>(new RequestDto(cd.ConfigsAPIUrl + "/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDto<ConfigUpsertDto?>?> AutosaveAsync(MonacoAutosaveRequest dto)
    {
        return await baseService.SendAsync<ConfigUpsertDto>(new RequestDto(cd.ConfigsAPIUrl + "/autosave")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDto<ConfigUpsertDto?>?> UpdateImageAsync(UpdateImageDto dto)
    {
        return await baseService.SendAsync<ConfigUpsertDto>(new RequestDto(cd.ConfigsAPIUrl + "/updateImage")
        {
            ApiType = EApiType.PUT,
            Data = dto,
            ContentType = EContentType.MultipartFormData
        });
    }
    public async Task<ResponseDto<ConfigUpsertDto?>?> UpdateDllAsync(UpdateDllDto dto)
    {
        return await baseService.SendAsync<ConfigUpsertDto>(new RequestDto(cd.ConfigsAPIUrl + "/updateDll")
        {
            ApiType = EApiType.PUT,
            Data = dto,
            ContentType = EContentType.MultipartFormData
        });
    }
    public async Task<ResponseDtoo?> DeleteConfigAsync(long id)
    {
        return await baseService.SendAsync(new RequestDto(cd.ConfigsAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }

}
