using Microsoft.Extensions.Configuration;

namespace RevalW.Services.Shared.Utils;

public class AzurePublicBlobService : AzureBlobService
{
    public AzurePublicBlobService(IConfiguration config) : base(config)
    {
        containerName = config["AzureStorage:PublicContainerName"] ?? "";
    }
}
