using RevalW.Shared.Model.Generic;

namespace RevalW.Shared.Model;

public class LoginResponseDto
{
    public UserDto? User { get; set; }
    public RshResponseDto? Tokens { get; set; }
}

public static class LoginResponseExt
{
    public static LoginResponseDto? ToLoginResponseDto(this ResponseDto<LoginResponseDto>? resp)
    {
        if (resp is null) return null;
        if (!resp.IsSuccess) return null;
        if (resp.Result is null) return null;
        return resp.Result;
    }
}