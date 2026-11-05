using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace RevalW.Services.Shared.Utils;

public class AzureBlobService
{
    private readonly BlobServiceClient blobServiceClient;
    protected string containerName = null!;

    public AzureBlobService(IConfiguration config)
    {
        var connString = config["ConnectionStrings:AzureBlobStorage"];
        blobServiceClient = new BlobServiceClient(connString);
    }

    public async Task<List<string>> GetFileNames(string baseName)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var ret = new List<string>();

        await foreach (var blob in container.GetBlobsAsync(prefix: baseName))
        {
            ret.Add(blob.Name);
        }

        return ret;
    }
    public async Task<List<(string name, string uri)>> GetFileNamesAndUris(string baseName)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var ret = new List<(string name, string uri)>();

        await foreach (var blob in container.GetBlobsAsync(prefix: baseName))
        {
            var blobClient = container.GetBlobClient(blob.Name);
            ret.Add((blob.Name, blobClient.Uri.ToString()));
        }

        return ret;
    }
    public async Task<string> UploadFileAsync(Stream stream, string name, bool openDirectly = false)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(name);

        string contentType = "application/octet-stream";
        if (openDirectly)
        {
            if (name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) contentType = "application/pdf";
            else if (name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) contentType = "application/xml";
            else contentType = "text/plain";
        }

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType,
            ContentDisposition = "inline"
        };

        await blob.DeleteIfExistsAsync();
        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

        return blob.Name;
    }
    public async Task DeleteFileByBaseNameAsync(string baseName)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);

        await foreach (var blob in container.GetBlobsAsync(prefix: baseName))
        {
            var blobClient = container.GetBlobClient(blob.Name);
            await blobClient.DeleteIfExistsAsync();
        }
    }
    public async Task<Stream?> DownloadFileAsync(string fileName)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(fileName);

        if (await blob.ExistsAsync())
        {
            var response = await blob.DownloadAsync();
            return response.Value.Content;
        }

        return null;
    }
}

