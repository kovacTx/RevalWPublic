using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace RevalW.Services.Shared.Utils;

public class BackendApiAuthenticationHttpClientHandler(IHttpContextAccessor contextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenAwaiter = contextAccessor.HttpContext?.GetTokenAsync("access_token");
        if (tokenAwaiter is null) return null;
        var token = await tokenAwaiter;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
