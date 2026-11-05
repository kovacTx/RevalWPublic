using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class TagNameDto
{
    [Required]
    public string Name { get; set; } = default!;
}
