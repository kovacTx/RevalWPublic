using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.Shared.Model;

public class DynamicProperty
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ConfigId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Label { get; set; } = null!;
    public bool Required { get; set; }
    public bool Readonly { get; set; }
    public string? InputType { get; set; }
    public string? Min { get; set; }
    public string? Max { get; set; }
    public string? Step { get; set; }
    public string? ComboOptions { get; set; }
    public string? Default { get; set; }

}
