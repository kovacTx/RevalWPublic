using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using System.Text;

namespace RevalW.Services.ItemsAPI.Services;

public class DocumentsService(IHttpClientFactory httpClientFactory, CommonData cd) : ControllerBase
{
    public async Task<(int status, ResponseDto<DocumentCreationResult?>? message)> Generate(StringValues headerAuthToken, DocumentGenerateRequest request)
    {
        var client = httpClientFactory.CreateClient("DocumentsClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.DocumentServiceAPIUrl + $"/generate"),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<DocumentCreationResult?>.FromHttpResponseMessage(apiResp));
    }
    public async Task<(int status, ResponseDto<string?>? message)> GenerateStart(StringValues headerAuthToken, DocumentGenerateRequest request)
    {
        var client = httpClientFactory.CreateClient("DocumentsClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.DocumentServiceAPIUrl + $"/generate-start"),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<string?>.FromHttpResponseMessage(apiResp));
    }
    public async Task<(int status, ResponseDto<DocumentCreationResult?>? message)> GenerateProgress(StringValues headerAuthToken, string operationId)
    {
        var client = httpClientFactory.CreateClient("DocumentsClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.DocumentServiceAPIUrl + $"/generate-progress?operationId={operationId}"),
            Method = HttpMethod.Get,
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<DocumentCreationResult?>.FromHttpResponseMessage(apiResp));
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
