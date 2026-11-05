using RevalW.App.Service.IFace;
using RevalW.App.Utils;

namespace RevalW.App.Service;

public class TokenProvider(IHttpContextAccessor contextAccessor, CommonData cd) : ITokenProvider
{
    public void ClearToken()
    {
        contextAccessor.HttpContext?.Response.Cookies.Delete(cd.TokenCookieName);
    }
    public string? GetToken()
    {
        string? token = null;
        var hasToken = contextAccessor.HttpContext?.Request.Cookies.TryGetValue(cd.TokenCookieName, out token);
        return hasToken is true ? token : null;
    }
    public void SetToken(string? token)
    {
        contextAccessor.HttpContext?.Response.Cookies.Append(cd.TokenCookieName, token ?? "");
    }
}
