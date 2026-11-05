using Newtonsoft.Json;
using RevalW.Services.DocumentsAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils.IFace;
using System.Text;

namespace RevalW.Services.DocumentsAPI.Services;

public class ItemsService(IHttpClientFactory httpClientFactory, CommonData cd, ISimpleHasher hasher)
{
    public async Task<(int status, ResponseDto<List<ItemHeaderDto>?>? message)> NotifyDocumentGenerated(GeneratedDocumentDto dto)
    {
        var client = httpClientFactory.CreateClient("ItemsClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.ItemsAPIUrl + $"/documentGenerated"),
            Method = HttpMethod.Put,
            Content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json")
        };
        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<List<ItemHeaderDto>?>.FromHttpResponseMessage(apiResp));
    }
    public async Task<(int status, ResponseDto<List<ItemHeaderDto>?>? message)> TryMarkFinished(long id, long currentOrgId, string userId)
    {
        var hash1 = Guid.NewGuid().ToString() + "uroboros";
        var hash2 = hasher.ComputeHash(hash1);

        var client = httpClientFactory.CreateClient("ItemsClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.ItemsAPIUrl + $"/tryMarkFinished?id={id}&currentOrgId={currentOrgId}&userId={userId}&hash1={hash1}&hash2={hash2}"),
            Method = HttpMethod.Put
        };

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<List<ItemHeaderDto>>.FromHttpResponseMessage(apiResp));
    }
}
