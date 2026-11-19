using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class OrganizationDto
{
    public long Id { get; set; }

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

    public List<DynamicPropertyDto> NewUserProperties { get; set; } = [];

    public List<DynamicPropertyDto> NewItemProperties { get; set; } = [];

    public List<RoleDto> Roles { get; set; } = [];

    public List<DynamicPropertyDto> NewDeviceProperties { get; set; } = [];

    public List<MainGridColumnDto> ItemGridColumns { get; set; } = [];

    public List<MainGridColumnDto> DeviceGridColumns { get; set; } = [];

    public List<string> UserIds { get; set; } = [];
}
