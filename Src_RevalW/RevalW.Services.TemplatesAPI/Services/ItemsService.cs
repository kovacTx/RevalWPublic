using Microsoft.Extensions.Primitives;
using RevalW.Services.TemplatesAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.TemplatesAPI.Services;

public class ItemsService(IHttpClientFactory httpClientFactory, CommonData cd)
{
    public async Task<(int status, ResponseDto<List<ItemHeaderDto>>? message)> GetByTag(StringValues headerAuthToken, string tag)
    {
        var client = httpClientFactory.CreateClient("ItemsClient");

        var message = new HttpRequestMessage();
        AddBearer(headerAuthToken, message);

        message.RequestUri = new Uri(cd.ItemsAPIUrl + $"/byTag?tag={tag}");
        message.Method = HttpMethod.Get;
        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<List<ItemHeaderDto>>.FromHttpResponseMessage(apiResp));// ResponseDto<List<ItemHeaderDto>>.Err(JsonConvert.SerializeObject(data) + JsonConvert.SerializeObject(apiResp)));
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
