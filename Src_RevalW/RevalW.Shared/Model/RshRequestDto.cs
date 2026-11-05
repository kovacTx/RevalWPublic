using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class RshRequestDto
{
    [Required]
    public string RToken { get; set; } = null!;
}
