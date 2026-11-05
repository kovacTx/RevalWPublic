using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.TemplatesAPI.Model;

public class Document
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public long ConfigId { get; set; }

    [Required]
    public string Template { get; set; } = default!;

    [Required]
    public string Label { get; set; } = default!;

    public string? Enabled { get; set; } = "true";

    public string? AlwaysTrue { get; set; } = "false";

    public string? RestParams { get; set; }

    public DateTime LastUpdated { get; set; }

    public bool Deleted { get; set; }
}
