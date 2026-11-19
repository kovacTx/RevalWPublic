using Microsoft.AspNetCore.Http;
using RevalW.Shared.Utils;

namespace RevalW.Shared.Model;

public class UpdateDllDto
{
    public long Id { get; set; }

    public long OrganizationId { get; set; }

    [MaxFileSize(5)]
    [AllowedExtension([".dll"])]
    public IFormFile? WDll { get; set; }
    public bool UpdateWDll { get; set; }

    [MaxFileSize(5)]
    [AllowedExtension([".dll"])]
    public IFormFile? ADll { get; set; }
    public bool UpdateADll { get; set; }

    [MaxFileSize(5)]
    [AllowedExtension([".dll"])]
    public IFormFile? IDll { get; set; }
    public bool UpdateIDll { get; set; }

}
