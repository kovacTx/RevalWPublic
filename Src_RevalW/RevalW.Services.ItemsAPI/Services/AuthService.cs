using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.ItemsAPI.Services;

public class AuthService(IHttpClientFactory httpClientFactory, CommonData cd) : ControllerBase
{
    public async Task<ResponseDto<string?>?> GetUserData(StringValues headerAuthToken, string? id)
    {
        var client = httpClientFactory.CreateClient("AuthClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.AuthAPIUrl + $"/userData?id={id}"),
            Method = HttpMethod.Get,
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return await ResponseDto<string?>.FromHttpResponseMessage(apiResp);
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
