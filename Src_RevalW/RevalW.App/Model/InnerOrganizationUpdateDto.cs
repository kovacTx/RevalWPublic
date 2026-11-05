using System.ComponentModel.DataAnnotations;

namespace RevalW.App.Model;

public class InnerOrganizationUpdateDto
{
    public long Id { get; set; }

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? Street { get; set; }

    [Required]
    public string? Zip { get; set; }

    [Required]
    public string? City { get; set; }

    [Required]
    public string? CountryRegion { get; set; }

    public string? NewUserPropertiesJson { get; set; }
    public string? RolesJson { get; set; }
    public string? NewItemPropertiesJson { get; set; }
    public string? NewDevicePropertiesJson { get; set; }
    public string? ItemGridColumnsJson { get; set; }
    public string? DeviceGridColumnsJson { get; set; }
    public string? TagsJson { get; set; }
}
