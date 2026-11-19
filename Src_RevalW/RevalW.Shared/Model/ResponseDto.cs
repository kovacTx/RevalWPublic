using Newtonsoft.Json;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Model.IFace;
using System.Net;

namespace RevalW.Shared.Model;

public class ResponseDtoo : IResponseDto
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    public static ResponseDtoo Ok()
    {
        return new ResponseDtoo
        {
            IsSuccess = true,
        };
    }
    public static ResponseDtoo Err(string message)
    {
        return new ResponseDtoo
        {
            IsSuccess = false,
            Message = message
        };
    }
    public static async Task<ResponseDtoo?> FromHttpResponseMessage(HttpResponseMessage? response)
    {
        if (response is null) return null;
        string apiContent;
        var responseDto = await ToResponseDto(response);
        if (responseDto is not null) return responseDto;
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                return new() { IsSuccess = false, Message = "Not found" };
            case HttpStatusCode.Forbidden:
                return new() { IsSuccess = false, Message = "Access denied" };
            case HttpStatusCode.Unauthorized:
                return new() { IsSuccess = false, Message = "Unauthorized" };
            case HttpStatusCode.InternalServerError:
                return new() { IsSuccess = false, Message = "Internal server error" };
            case HttpStatusCode.BadRequest:
                apiContent = await response.Content.ReadAsStringAsync();
                return new() { IsSuccess = false, Message = $"bad request: {apiContent}" };
            case HttpStatusCode.UnsupportedMediaType:
                apiContent = await response.Content.ReadAsStringAsync();
                return new() { IsSuccess = false, Message = $"unsupported media type: {apiContent}" };
            default:
                if (response.StatusCode == HttpStatusCode.OK) return new() { IsSuccess = true };
                return null;
        }
    }
    public ResponseDto<T> ToResponseDto<T>()
        => new()
        {
            IsSuccess = IsSuccess,
            Message = Message
        };
    private static async Task<ResponseDtoo?> ToResponseDto(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var apiContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ResponseDtoo>(apiContent);
        }
        catch
        {
            return null;
        }
    }
}