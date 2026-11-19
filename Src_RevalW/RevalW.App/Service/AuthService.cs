using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;

namespace RevalW.App.Service;

public class AuthService(IBaseService baseService, CommonData cd, ISimpleHasher simpleHasher)
{
    public async Task<ResponseDto<IEnumerable<UserDto>?>?> GetAllUsersAsync()
    {
        return await baseService.SendAsync<IEnumerable<UserDto>>(new RequestDto(cd.AuthAPIUrl + "/all")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<Dictionary<string, string>?>?> GetAllPermissions()
    {
        return await baseService.SendAsync<Dictionary<string, string>?>(new RequestDto(cd.AuthAPIUrl + $"/allPermissions")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<string?>?> GetUserName(string? id)
    {
        if (string.IsNullOrEmpty(id)) return ResponseDto<string?>.Ok(null);
        return await baseService.SendAsync<string?>(new RequestDto(cd.AuthAPIUrl + $"/name?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDtoo?> NewRegisterAsync(NewRegistrationRequestDto request)
    {
        return await baseService.SendAsync(new RequestDto(cd.AuthAPIUrl + "/newRegister")
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDtoo?> RegisterWithinOrganizationAsync(RegistrationWithinOrganizationRequestDto request)
    {
        return await baseService.SendAsync(new RequestDto(cd.AuthAPIUrl + "/registerWithinOrganization")
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<LoginResponseDto?>?> LoginAsync(LoginRequestDto request)
    {
        return await baseService.SendAsync<LoginResponseDto>(new RequestDto(cd.AuthAPIUrl + "/login")
        {
            ApiType = EApiType.POST,
            Data = request,
        }, false);
    }
    public async Task<ResponseDto<ResponseDtoo?>?> LogoutAsync(RshRequestDto request)
    {
        return await baseService.SendAsync<ResponseDtoo>(new RequestDto(cd.AuthAPIUrl + "/logout")
        {
            ApiType = EApiType.GET,
            Data = request,
        });
    }
    public async Task<ResponseDto<IEnumerable<string?>?>?> GetAssignableRoleNamesAsync()
    {
        return await baseService.SendAsync<IEnumerable<string?>>(new RequestDto(cd.AuthAPIUrl + "/assignableRoleNames")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<ResponseDtoo?>?> ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        return await baseService.SendAsync<ResponseDtoo>(new RequestDto(cd.AuthAPIUrl + $"/chng_pwd?hash={simpleHasher.ComputeHash(request.Id + request.Password + "uroboros")}")
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<ResponseDtoo?>?> DeleteAsync(string id)
    {
        return await baseService.SendAsync<ResponseDtoo>(new RequestDto(cd.AuthAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }
}
