using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class NewRegistrationRequestDto : RegistrationWithinOrganizationRequestDto
{
    [Required]
    public string OrganizationName { get; set; } = null!;

    [Required]
    public string OrganizationDescription { get; set; } = null!;

    [Required]
    public string OrganizationStreet { get; set; } = null!;

    [Required]
    public string OrganizationZip { get; set; } = null!;

    [Required]
    public string OrganizationCity { get; set; } = null!;

    [Required]
    public string OrganizationCountryRegion { get; set; } = null!;

}
