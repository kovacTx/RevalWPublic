using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RevalW.Services.OrganizationAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using System.Text;

namespace RevalW.Services.OrganizationAPI.Services;

public class AuthService(IHttpClientFactory httpClientFactory, CommonData cd) : ControllerBase
{
    public async Task<ResponseDto<IEnumerable<RoleDto>?>?> GetAssignableRoles(StringValues headerAuthToken)
    {
        var client = httpClientFactory.CreateClient("AuthClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.AuthAPIUrl + $"/assignableRoles"),
            Method = HttpMethod.Get,
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return await ResponseDto<IEnumerable<RoleDto>?>.FromHttpResponseMessage(apiResp);
    }
    public async Task<ResponseDtoo?> UpdateRoles(StringValues headerAuthToken, List<RoleDto> roles)
    {
        var client = httpClientFactory.CreateClient("AuthClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.AuthAPIUrl + $"/updateRoles"),
            Content = new StringContent(JsonConvert.SerializeObject(roles), Encoding.UTF8, "application/json"),
            Method = HttpMethod.Put,
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return await ResponseDtoo.FromHttpResponseMessage(apiResp);
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
