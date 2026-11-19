using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class ChangePasswordRequestDto
{
    [Required]
    public string Id { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

}
