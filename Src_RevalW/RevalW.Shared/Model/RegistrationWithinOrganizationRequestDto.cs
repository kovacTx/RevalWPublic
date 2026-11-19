using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class RegistrationWithinOrganizationRequestDto
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string PhoneNumber { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    public List<string>? Roles { get; set; }

    public List<string>? Permissions { get; set; }

    public string? UserDataJson { get; set; }
}
