using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RevalW.Services.AuthAPI.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils.IFace;
using System.Text;

namespace RevalW.Services.AuthAPI.Services;

public class OrganizationService(IHttpClientFactory httpClientFactory, CommonData cd, ISimpleHasher simpleHasher)
{
    public async Task<(int status, ResponseDto<OrganizationDto>? message)> CreateOrganization(StringValues headerAuthToken, OrganizationDto dto)
    {
        var client = httpClientFactory.CreateClient("OrganizationClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.OrganizationAPIUrl + $"/create?hash={simpleHasher.ComputeHash(dto.Name + dto.Description + "uroboros")}"),
            Content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json"),
            Method = HttpMethod.Post
        };
        AddBearer(headerAuthToken, message);
        var apiResp = await client.SendAsync(message);

        return ((int)apiResp.StatusCode, await ResponseDto<OrganizationDto>.FromHttpResponseMessage(apiResp));
    }
    public async Task<(int status, ResponseDtoo? message)> DeleteOrganization(StringValues headerAuthToken, long id)
    {
        var client = httpClientFactory.CreateClient("OrganizationClient");

        var message = new HttpRequestMessage
        {
            RequestUri = new Uri(cd.OrganizationAPIUrl + $"/delete?id={id};hash={simpleHasher.ComputeHash((id + 42).ToString() + "uroboros")}"),
            Method = HttpMethod.Delete
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
