namespace RevalW.Services.ItemsAPI.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? _authAPIUrl;
    public string AuthAPIUrl => _authAPIUrl ??= Builder?.Configuration["ServiceUrls:AuthAPI"] ?? "";

    private string? _configTemplatesAPIUrl;
    public string ConfigTemplatesAPIUrl => _configTemplatesAPIUrl ??= Builder?.Configuration["ServiceUrls:ConfigTemplatesAPI"] ?? "";

    private string? _cDocumentTemplatesAPIUrl;
    public string CDocumentTemplatesAPIUrl => _cDocumentTemplatesAPIUrl ??= Builder?.Configuration["ServiceUrls:CDocumentTemplatesAPI"] ?? "";

    private string? _documentServiceAPIUrl;
    public string DocumentServiceAPIUrl => _documentServiceAPIUrl ??= Builder?.Configuration["ServiceUrls:DocumentServiceAPI"] ?? "";

    private string? _organizationAPIUrl;
    public string OrganizationAPIUrl => _organizationAPIUrl ??= Builder?.Configuration["ServiceUrls:OrganizationAPI"] ?? "";

    private string? _devicesAPIUrl;
    public string DevicesAPIUrl => _devicesAPIUrl ??= _devicesAPIUrl = Builder?.Configuration["ServiceUrls:DevicesAPI"] ?? "";

    private string? _defaultQueueConnection;
    public string DefaultQueueConnection => _defaultQueueConnection ??= Builder?.Configuration["ConnectionStrings:ServiceBusConnectionString"] ?? "";

    private string? _generateDocumentQueueName;
    public string GenerateDocumentQueueName => _generateDocumentQueueName ??= Builder?.Configuration["TopicsAndQueues:GenerateDocumentQueueName"] ?? "";

}
