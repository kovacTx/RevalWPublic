using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class ConfigHeaderDto
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
    public List<string> Tags { get; set; } = [];

    public string? ComboSourcesJson { get; set; }
    public string? CommonDataSourcesJson { get; set; }

    public bool Deleted { get; set; }

    public DateTime LastUpdated { get; set; }

    public DateTime? WDllLastUpdated { get; set; }
    public DateTime? ADllLastUpdated { get; set; }
    public DateTime? IDllLastUpdated { get; set; }
}
