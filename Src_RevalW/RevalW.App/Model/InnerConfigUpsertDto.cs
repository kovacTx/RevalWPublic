using RevalW.Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace RevalW.App.Model;

public class InnerConfigUpsertDto
{
    public long Id { get; set; }

    public long OrganizationId { get; set; }

    [Required]
    public string Version { get; set; } = null!;

    [Required]
    public string Designation { get; set; } = null!;

    public string? ImageUrl { get; set; }

    [Required]
    public string Author { get; set; } = null!;

    public bool CanSelectMoreDocuments { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    [MaxFileSize(1)]
    [AllowedExtension([".jpg", ".png"])]
    public IFormFile? Image { get; set; }
    public bool UpdateImage { get; set; }

    [MaxFileSize(5)]
    [AllowedExtension([".dll"])]
    public IFormFile? WDll { get; set; }
    public bool UpdateWDll { get; set; }
    public DateTime? WDllLastUpdated { get; set; }

    [MaxFileSize(5)]
    [AllowedExtension([".dll"])]
    public IFormFile? ADll { get; set; }
    public bool UpdateADll { get; set; }
    public DateTime? ADllLastUpdated { get; set; }

    [MaxFileSize(5)]
    [AllowedExtension([".dll"])]
    public IFormFile? IDll { get; set; }
    public bool UpdateIDll { get; set; }
    public DateTime? IDllLastUpdated { get; set; }

    public string? OldDocumentsResourcesJson { get; set; }
    public List<IFormFile?> NewDocumentsResources { get; set; } = [];

    public string? TagsJson { get; set; }

    public string? PrintableDocumentsJson { get; set; }
    public string? NewItemPropertiesJson { get; set; }

    public bool Deleted { get; set; }

}
