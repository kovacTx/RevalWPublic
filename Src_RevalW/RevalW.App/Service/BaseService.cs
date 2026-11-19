using Newtonsoft.Json;
using RevalW.App.Service.IFace;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace RevalW.App.Service;

public class BaseService(IHttpClientFactory httpClientFactory, ITokenProvider tokenProvider) : IBaseService
{
    public async Task<ResponseDtoo?> SendAsync(RequestDto requestDto, bool withBearer = true, string? token = null)
    {
        try
        {
            var (response, message) = await SendAsyncInternal(requestDto, withBearer, token);
            if (response is null)
                return ResponseDtoo.Err(message ?? "error in api response");
            return await ResponseDtoo.FromHttpResponseMessage(response);
        }
        catch (Exception e)
        {
            return ResponseDtoo.Err("error in api response: " + e.Message);
        }
    }
    public async Task<ResponseDto<T?>?> SendAsync<T>(RequestDto requestDto, bool withBearer = true, string? token = null)
    {
        try
        {
            var (response, message) = await SendAsyncInternal(requestDto, withBearer, token);
            if (response is null)
                return ResponseDto<T?>.Err(message ?? "error in api response");
            return await ResponseDto<T?>.FromHttpResponseMessage(response);
        }
        catch (Exception e)
        {
            return ResponseDto<T?>.Err("error in api response: " + e.Message);
        }
    }
    public async Task<ResponseDto<Stream?>?> SendAndReadStreamAsync(RequestDto requestDto, string? token = null)
    {
        try
        {
            var (response, message) = await SendAsyncInternal(requestDto, false, token);
            if (response is null)
                return ResponseDto<Stream?>.Err(message ?? "error in api response");
            if (response.StatusCode != HttpStatusCode.OK)
                return await ResponseDto<Stream?>.FromHttpResponseMessage(response);

            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            return new ResponseDto<Stream?>()
            {
                IsSuccess = true,
                Result = stream,
                Message = contentType,
            };
        }
        catch (Exception e)
        {
            return ResponseDto<Stream?>.Err("error in api response: " + e.Message);
        }
    }

    private async Task<(HttpResponseMessage? response, string? message)> SendAsyncInternal(RequestDto requestDto, bool withBearer = true, string? token = null)
    {
        //try
        //{
        var client = httpClientFactory.CreateClient("RevalVAPI");
        var message = new HttpRequestMessage();
        var isMultiForDataContent = requestDto.ContentType == EContentType.MultipartFormData;
        if (isMultiForDataContent)
        {
            message.Headers.Add("Accept", "*/*");
        }
        else
        {
            message.Headers.Add("Accept", "application/json");
        }

        if (withBearer)
        {
            token ??= tokenProvider.GetToken();
            message.Headers.Add("Authorization", $"Bearer {token}");
        }

        message.RequestUri = new Uri(requestDto.Url);
        Console.WriteLine($"BaseService SendAsyncInternal to {requestDto.Url}");


        if (isMultiForDataContent)
        {
            var content = new MultipartFormDataContent();
            foreach (var prop in requestDto.Data.GetType().GetProperties())
            {
                var name = prop.Name;
                var value = prop.GetValue(requestDto.Data);
                if (value is null) continue;
                if (value is IFormFile formFile)
                {
                    var fileContent = new StreamContent(formFile.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);
                    content.Add(fileContent, name, formFile.FileName);
                }
                else if (value is IEnumerable<string> stringList)
                {
                    var strListI = 0;
                    foreach (var s in stringList)
                    {
                        content.Add(new StringContent(s ?? string.Empty, Encoding.UTF8), $"{name}[{strListI}]");
                        strListI++;
                    }
                }
                else if (value is IEnumerable<int> intList)
                {
                    foreach (var i in intList)
                        content.Add(new StringContent(i.ToString(), Encoding.UTF8), name);
                }
                else
                {
                    if (value.GetType().IsPrimitive || value is string or decimal)
                    {
                        content.Add(new StringContent(value.ToString() ?? string.Empty, Encoding.UTF8), name);
                    }
                    else
                    {
                        var json = JsonConvert.SerializeObject(value);
                        content.Add(new StringContent(json, Encoding.UTF8, "application/json"), name);
                    }
                }
            }
            message.Content = content;
        }
        else
        {
            if (requestDto.Data is not null)
            {
                var jsonString = JsonConvert.SerializeObject(requestDto.Data);
                message.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            }
        }

        message.Method = requestDto.ApiType.ToHttpMethod() ?? HttpMethod.Get;
        return (await client.SendAsync(message), null);
        //}
        //catch (Exception ex)
        //{
        //    return (null, ex.Message);
        //}
    }
}
