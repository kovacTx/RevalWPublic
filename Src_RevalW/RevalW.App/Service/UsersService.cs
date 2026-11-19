using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.App.Service;

public class UsersService(IBaseService baseService, CommonData cd)
{
    public async Task<ResponseDto<DataTablesResponse<UserDto>?>?> GetByDataTablesAsync(DTRequest request)
    {
        var path = "/byDataTables";

        return await baseService.SendAsync<DataTablesResponse<UserDto>>(new RequestDto(cd.AuthAPIUrl + path)
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<dynamic?>?> UberGetByDataTablesAsync(DTRequest request)
    {
        var path = "/uberByDataTables";

        return await baseService.SendAsync<dynamic>(new RequestDto(cd.AuthAPIUrl + path)
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<UserDto?>?> GetByIdAsync(string id)
    {
        return await baseService.SendAsync<UserDto?>(new RequestDto(cd.AuthAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<UserDto?>?> GetMeAsync()
    {
        return await baseService.SendAsync<UserDto?>(new RequestDto(cd.AuthAPIUrl + $"/me")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<UserDto?>?> UpdateAsync(UserDto dto)
    {
        return await baseService.SendAsync<UserDto>(new RequestDto(cd.AuthAPIUrl + "/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDtoo?> DeleteUserAsync(string id)
    {
        return await baseService.SendAsync(new RequestDto(cd.AuthAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
    // todo: finish
    public async Task<ResponseDtoo?> ChangePasswordAsync(string id)
    {
        return await baseService.SendAsync(new RequestDto(cd.AuthAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }

}
