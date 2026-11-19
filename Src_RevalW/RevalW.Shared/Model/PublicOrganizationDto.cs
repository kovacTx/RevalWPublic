using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class PublicOrganizationDto
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public string Street { get; set; } = null!;

    [Required]
    public string Zip { get; set; } = null!;

    [Required]
    public string City { get; set; } = null!;

    [Required]
    public string CountryRegion { get; set; } = null!;

    public List<string> Tags { get; set; } = [];
}
