using Microsoft.Extensions.Primitives;
using RevalW.Services.DevicesAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.DevicesAPI.Services;

public class OrganizationService(IHttpClientFactory httpClientFactory, CommonData cd)
{
    public async Task<(int status, ResponseDto<List<MainGridColumnDto>?>? message)> GetDeviceGridColumns(StringValues headerAuthToken)
    {
        var client = httpClientFactory.CreateClient("OrganizationClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.OrganizationAPIUrl + $"/deviceGridColumns"),
            Method = HttpMethod.Get
        };
        AddBearer(headerAuthToken, message);

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<List<MainGridColumnDto>?>.FromHttpResponseMessage(apiResp));
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
