using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class PublicOrganizationTagDto
{

    [Required]
    public string Name { get; set; } = default!;

    public List<PublicOrganizationDto> Organizations { get; set; } = [];
}
