using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.ItemsAPI.Model;

public class CommonDataWrapper
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public long ConfigId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public List<CommonDataItem> Items { get; set; } = [];
}