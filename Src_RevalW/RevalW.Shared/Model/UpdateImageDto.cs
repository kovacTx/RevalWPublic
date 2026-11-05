using Microsoft.AspNetCore.Http;
using RevalW.Shared.Utils;

namespace RevalW.Shared.Model;

public class UpdateImageDto
{
    public long Id { get; set; }

    public long OrganizationId { get; set; }

    [MaxFileSize(1)]
    [AllowedExtension([".jpg", ".png"])]
    public IFormFile? Image { get; set; }

}
