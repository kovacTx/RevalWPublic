using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RevalW.Services.ItemsAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using System.Text;

namespace RevalW.Services.ItemsAPI.Services;

public class DevicesService(IHttpClientFactory httpClientFactory, CommonData cd)
{
    public async Task<(int status, ResponseDto<DeviceHeaderDto?>? message)> GetHeaderById(StringValues headerAuthToken, long id)
    {
        var client = httpClientFactory.CreateClient("DevicesClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.DevicesAPIUrl + $"/headerById?id={id}"),
            Method = HttpMethod.Get
        };
        AddBearer(headerAuthToken, message);

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<DeviceHeaderDto?>.FromHttpResponseMessage(apiResp));
    }
    public async Task<(int status, ResponseDto<DeviceUpsertDto?>? message)> GetById(StringValues headerAuthToken, long id)
    {
        var client = httpClientFactory.CreateClient("DevicesClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.DevicesAPIUrl + $"/byId?id={id}"),
            Method = HttpMethod.Get
        };
        AddBearer(headerAuthToken, message);

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<DeviceUpsertDto?>.FromHttpResponseMessage(apiResp));
    }
    public async Task<(int status, ResponseDto<DeviceUpsertDto?>? message)> Update(StringValues headerAuthToken, DeviceUpsertDto dto)
    {
        var client = httpClientFactory.CreateClient("DevicesClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.DevicesAPIUrl + $"/update"),
            Content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json"),
            Method = HttpMethod.Put
        };
        AddBearer(headerAuthToken, message);

        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<DeviceUpsertDto?>.FromHttpResponseMessage(apiResp));
    }

    private static void AddBearer(StringValues headerAuthToken, HttpRequestMessage message)
    {
        var token = headerAuthToken.ToString();
        if (string.IsNullOrWhiteSpace(token)) return;

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) message.Headers.Add("Authorization", token); // already has Bearer
        else message.Headers.Add("Authorization", $"Bearer {token}"); // add Bearer
    }
}
