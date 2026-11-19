using Microsoft.AspNetCore.Http;
using RevalW.Shared.Utils;

namespace RevalW.Shared.Model;

public class UploadDocumentsResourceFileDto
{
    public long ConfigId { get; set; }
    public long OrganizationId { get; set; }

    [MaxFileSize(2)]
    public IFormFile? File { get; set; }

}
