namespace RevalW.Shared.Utils;

public enum EApiType
{
    GET,
    POST,
    PUT,
    DELETE,
}
public static class ApiTypeExtension
{
    public static HttpMethod? ToHttpMethod(this EApiType apiType)
        => apiType switch
        {
            EApiType.POST => HttpMethod.Post,
            EApiType.GET => HttpMethod.Get,
            EApiType.PUT => HttpMethod.Put,
            EApiType.DELETE => HttpMethod.Delete,
            _ => null,
        };
}
