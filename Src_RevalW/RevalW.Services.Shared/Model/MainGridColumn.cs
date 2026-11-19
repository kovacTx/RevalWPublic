using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.Shared.Model;

public class MainGridColumn
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    public string? Prefix { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Label { get; set; } = null!;
}
