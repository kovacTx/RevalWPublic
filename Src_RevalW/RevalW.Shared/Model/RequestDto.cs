using RevalW.Shared.Utils;

namespace RevalW.Shared.Model;

public class RequestDto(string url)
{
    public EApiType ApiType { get; set; } = EApiType.GET;
    public string Url => url;
    public object? Data { get; set; }
    public EContentType ContentType { get; set; } = EContentType.Json;
    public EServiceType Service { get; set; }
}
public enum EServiceType
{
    Item,
    Auth,
}
