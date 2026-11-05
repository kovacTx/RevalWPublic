using Microsoft.Extensions.Primitives;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.ItemsAPI.Services;

public class CTagsService(IHttpClientFactory httpClientFactory, CommonData cd)
{
    public async Task<(int status, ResponseDto<IEnumerable<TagDto>?>? message)> GetAll(StringValues headerAuthToken, bool? deleted)
    {
        var client = httpClientFactory.CreateClient("TemplatesClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.ConfigTemplatesAPIUrl + $"/all?deleted={deleted}"),
            Method = HttpMethod.Get
        };
        AddBearer(headerAuthToken, message);

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<IEnumerable<TagDto>?>.FromHttpResponseMessage(apiResp));// ResponseDto<List<ItemHeaderDto>>.Err(JsonConvert.SerializeObject(data) + JsonConvert.SerializeObject(apiResp)));
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
