using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.App.Service.IFace;

// need to accept tken as argument because on document generation we want the token when the endpoint method returns
public interface IBaseService
{
    Task<ResponseDto<T?>?> SendAsync<T>(RequestDto requestDto, bool withBearer = true, string? token = null);
    Task<ResponseDtoo?> SendAsync(RequestDto requestDto, bool withBearer = true, string? token = null);
    Task<ResponseDto<Stream?>?> SendAndReadStreamAsync(RequestDto requestDto, string? token = null);
}
