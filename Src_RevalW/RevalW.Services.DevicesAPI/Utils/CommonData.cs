namespace RevalW.Services.DevicesAPI.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? _itemsAPIUrl;
    public string ItemsAPIUrl => _itemsAPIUrl ??= Builder?.Configuration["ServiceUrls:ItemsAPI"] ?? "";

    private string? _organizationAPIUrl;
    public string OrganizationAPIUrl => _organizationAPIUrl ??= Builder?.Configuration["ServiceUrls:OrganizationAPI"] ?? "";

}
