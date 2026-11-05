using Microsoft.Extensions.Primitives;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.ItemsAPI.Services;

public class CDocumentsService(IHttpClientFactory httpClientFactory, CommonData cd)
{
    public async Task<ResponseDto<DocumentTemplateDto?>?> GetById(StringValues headerAuthToken, long id)
    {
        var client = httpClientFactory.CreateClient("TemplatesClient");

        var message = new HttpRequestMessage();
        AddBearer(headerAuthToken, message);

        message.RequestUri = new Uri(cd.CDocumentTemplatesAPIUrl + $"/byId?id={id}");
        message.Method = HttpMethod.Get;
        var apiResp = await client.SendAsync(message);

        return await ResponseDto<DocumentTemplateDto?>.FromHttpResponseMessage(apiResp);
    }
    public async Task<ResponseDto<List<DocumentsResourceFileDto>?>?> GetDocumentsResourcesByConfigId(StringValues headerAuthToken, long id)
    {
        var client = httpClientFactory.CreateClient("TemplatesClient");

        var message = new HttpRequestMessage();
        AddBearer(headerAuthToken, message);

        message.RequestUri = new Uri(cd.CDocumentTemplatesAPIUrl + $"/resourcesByConfigId?id={id}");
        message.Method = HttpMethod.Get;
        var apiResp = await client.SendAsync(message);

        return await ResponseDto<List<DocumentsResourceFileDto>?>.FromHttpResponseMessage(apiResp);
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
