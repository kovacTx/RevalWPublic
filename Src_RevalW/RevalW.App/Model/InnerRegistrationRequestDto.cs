using RevalW.Shared.Model;
using System.ComponentModel.DataAnnotations;

namespace RevalW.App.Model;

public class InnerRegistrationRequestDto : NewRegistrationRequestDto
{
    [Required]
    public string Password2 { get; set; } = null!;
}
