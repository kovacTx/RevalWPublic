using Newtonsoft.Json;
using RevalW.Shared.Model.IFace;
using System.Net;

namespace RevalW.Shared.Model.Generic;

public class ResponseDto<T> : IResponseDto
{
    public T? Result { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    public static ResponseDto<T> Ok(T? result = default)
    {
        return new ResponseDto<T>
        {
            IsSuccess = true,
            Result = result
        };
    }
    public static ResponseDto<T> Err(string message)
    {
        return new ResponseDto<T>
        {
            IsSuccess = false,
            Message = message
        };
    }
    public static async Task<ResponseDto<T?>?> FromHttpResponseMessage(HttpResponseMessage? response)
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
            case HttpStatusCode.NoContent:
                return new() { IsSuccess = false, Message = "No Content" };
            case HttpStatusCode.InternalServerError:
                apiContent = await response.Content.ReadAsStringAsync();
                return new() { IsSuccess = false, Message = $"Internal server error: {apiContent}" };
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
    public ResponseDtoo ToResponseDtoo()
        => new()
        {
            IsSuccess = IsSuccess,
            Message = Message
        };
    public ResponseDto<TResult> ToResponseDto<TResult>()
        => new()
        {
            IsSuccess = IsSuccess,
            Message = Message
        };
    private static async Task<ResponseDto<T?>?> ToResponseDto(HttpResponseMessage? response)
    {
        if (response is null) return null;
        try
        {
            var apiContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ResponseDto<T?>>(apiContent);
        }
        catch
        {
            return null;
        }
    }
}