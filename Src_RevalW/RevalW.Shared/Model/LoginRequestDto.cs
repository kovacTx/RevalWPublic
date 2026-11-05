using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class LoginRequestDto
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}
