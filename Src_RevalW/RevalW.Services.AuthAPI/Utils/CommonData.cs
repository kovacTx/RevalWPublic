namespace RevalW.Services.AuthAPI.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? organizationAPIUrl;
    public string OrganizationAPIUrl => organizationAPIUrl ??= Builder?.Configuration["ServiceUrls:OrganizationAPI"] ?? "";
}
