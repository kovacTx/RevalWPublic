using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.ItemsAPI.Model;

public class ComboSource
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public long ConfigId { get; set; }

    public List<ComboItem> Items { get; set; } = [];
}
