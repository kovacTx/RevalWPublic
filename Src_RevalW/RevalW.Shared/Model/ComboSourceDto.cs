using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class ComboSourceDto
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public long ConfigId { get; set; }

    public List<string> Items { get; set; } = [];
}
