namespace RevalW.Services.OrganizationAPI.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? _authAPIUrl;
    public string AuthAPIUrl => _authAPIUrl ??= Builder?.Configuration["ServiceUrls:AuthAPI"] ?? "";

}
