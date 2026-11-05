namespace RevalW.Services.DocumentsAPI.Utils;

public class CommonData
{
    public WebApplicationBuilder? Builder { get; set; }

    private string? _defaultQueueConnection;
    public string DefaultQueueConnection => _defaultQueueConnection ??= Builder?.Configuration["ConnectionStrings:ServiceBusConnectionString"] ?? "";

    private string? _generateDocumentQueueName;
    public string GenerateDocumentQueueName => _generateDocumentQueueName ??= Builder?.Configuration["TopicsAndQueues:GenerateDocumentQueueName"] ?? "";

    private string? itemsAPIUrl;
    public string ItemsAPIUrl => itemsAPIUrl ??= Builder?.Configuration["ServiceUrls:ItemsAPI"] ?? "";

}
