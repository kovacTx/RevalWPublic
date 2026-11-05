using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.TemplatesAPI.Model;

public class Config
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public string? Version { get; set; }

    [Required]
    public string? Designation { get; set; }

    public string? ImageUrl { get; set; }

    public string? WDllPrivatePath { get; set; }
    public DateTime? WDllLastUpdated { get; set; }
    public string? ADllPrivatePath { get; set; }
    public DateTime? ADllLastUpdated { get; set; }
    public string? IDllPrivatePath { get; set; }
    public DateTime? IDllLastUpdated { get; set; }

    [Required]
    public string? Author { get; set; }

    public bool CanSelectMoreDocuments { get; set; }

    [Required]
    public List<Tag> Tags { get; set; } = [];

    public string? ComboSourcesJson { get; set; }
    public string? CommonDataSourcesJson { get; set; }

    public List<NewItemProperty> NewItemProperties { get; set; } = [];

    [Required]
    public string? Content { get; set; }

    public DateTime LastUpdated { get; set; }
    public bool Public { get; set; }
    public bool Readonly { get; set; }

    public bool Deleted { get; set; }
}
