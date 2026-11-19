using Microsoft.Extensions.Configuration;

namespace RevalW.Services.Shared.Utils;

public class AzurePrivateBlobService : AzureBlobService
{
    public AzurePrivateBlobService(IConfiguration config) : base(config)
    {
        containerName = config["AzureStorage:PrivateContainerName"] ?? "";
    }
}

