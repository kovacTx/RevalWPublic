namespace RevalW.Services.TemplatesAPI.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? itemsAPIUrl;
    public string ItemsAPIUrl => itemsAPIUrl ??= Builder?.Configuration["ServiceUrls:ItemsAPI"] ?? "";
}
