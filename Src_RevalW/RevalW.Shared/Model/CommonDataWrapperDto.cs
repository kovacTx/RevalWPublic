using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class CommonDataWrapperDto
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public long ConfigId { get; set; }

    public Dictionary<string, string?> Items { get; set; } = [];
}
