using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RevalW.Services.DevicesAPI.Utils;
using RevalW.Shared.Model;
using System.Text;

namespace RevalW.Services.DevicesAPI.Services;

public class ItemsService(IHttpClientFactory httpClientFactory, CommonData cd)
{
    public async Task<(int status, ResponseDtoo? message)> DeviceChanged(StringValues headerAuthToken, DeviceUpsertDto device)
    {
        var client = httpClientFactory.CreateClient("ItemsClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.ItemsAPIUrl + $"/deviceChanged"),
            Content = new StringContent(JsonConvert.SerializeObject(device), Encoding.UTF8, "application/json"),
            Method = HttpMethod.Put
        };
        AddBearer(headerAuthToken, message);

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDtoo.FromHttpResponseMessage(apiResp));
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
