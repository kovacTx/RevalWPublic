namespace RevalW.App.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? _authAPIUrl;
    public string AuthAPIUrl => _authAPIUrl ??= _authAPIUrl = Builder?.Configuration["ServiceUrls:AuthAPI"] ?? "";

    private string? _itemsAPIUrl;
    public string ItemsAPIUrl => _itemsAPIUrl ??= _itemsAPIUrl = Builder?.Configuration["ServiceUrls:ItemsAPI"] ?? "";

    private string? _devicesAPIUrl;
    public string DevicesAPIUrl => _devicesAPIUrl ??= _devicesAPIUrl = Builder?.Configuration["ServiceUrls:DevicesAPI"] ?? "";

    private string? _configsAPIUrl;
    public string ConfigsAPIUrl => _configsAPIUrl ??= _configsAPIUrl = Builder?.Configuration["ServiceUrls:ConfigsAPI"] ?? "";

    private string? _organizationAPIUrl;
    public string OrganizationAPIUrl => _organizationAPIUrl ??= _organizationAPIUrl = Builder?.Configuration["ServiceUrls:OrganizationAPI"] ?? "";

    private string? _iTagsAPIUrl;
    public string ITagsAPIUrl => _iTagsAPIUrl ??= _iTagsAPIUrl = Builder?.Configuration["ServiceUrls:ITagsAPI"] ?? "";

    private string? _oTagsAPIUrl;
    public string OTagsAPIUrl => _oTagsAPIUrl ??= _oTagsAPIUrl = Builder?.Configuration["ServiceUrls:OTagsAPI"] ?? "";

    private string? _cTagsAPIUrl;
    public string CTagsAPIUrl => _cTagsAPIUrl ??= _cTagsAPIUrl = Builder?.Configuration["ServiceUrls:CTagsAPI"] ?? "";

    private string? _cDocumentsAPIUrl;
    public string CDocumentsAPIUrl => _cDocumentsAPIUrl ??= _cDocumentsAPIUrl = Builder?.Configuration["ServiceUrls:CDocumentsAPI"] ?? "";

    public readonly string TokenCookieName = "JWTToken";
}
