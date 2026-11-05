using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.App.Service;

public class DevicesService(IBaseService baseService, ISimpleHasher simpleHasher, CommonData cd)
{
    public async Task<ResponseDto<DeviceUpsertDto?>?> CreateAsync(DeviceUpsertDto dto)
    {
        return await baseService.SendAsync<DeviceUpsertDto>(new RequestDto(cd.DevicesAPIUrl + $"/create")
        {
            ApiType = EApiType.POST,
            Data = dto,
        });
    }
    public async Task<ResponseDto<DeviceUpsertDto?>?> GetDeviceByIdAsync(long id)
    {
        return await baseService.SendAsync<DeviceUpsertDto?>(new RequestDto(cd.DevicesAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<DataTablesResponse<DeviceHeaderDto>?>?> GetByDataTablesAsync(DTRequest request)
    {
        return await baseService.SendAsync<DataTablesResponse<DeviceHeaderDto>>(new RequestDto(cd.DevicesAPIUrl + "/byDataTables")
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<DeviceConfigDataNTagsDto?>?> GetConfigDataNTagsAsync(long id)
    {
        return await baseService.SendAsync<DeviceConfigDataNTagsDto>(new RequestDto(cd.DevicesAPIUrl + $"/configDataNTags?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<DeviceUpsertDto?>?> UpdateAsync(DeviceUpsertDto dto)
    {
        return await baseService.SendAsync<DeviceUpsertDto>(new RequestDto(cd.DevicesAPIUrl + "/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDtoo?> DeleteAsync(long id)
    {
        return await baseService.SendAsync(new RequestDto(cd.DevicesAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
}
