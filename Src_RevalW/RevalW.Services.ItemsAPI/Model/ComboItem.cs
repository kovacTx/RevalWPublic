using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.ItemsAPI.Model;

public class ComboItem
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ComboSourceId { get; set; }

    [Required]
    public string Value { get; set; } = null!;
}
