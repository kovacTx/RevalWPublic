using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.OrganizationAPI.Model;

public class Organization
{
    [Key]
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

    [Required]
    public List<Tag> Tags { get; set; } = [];

    public List<NewUserProperty> NewUserProperties { get; set; } = [];

    public List<NewItemProperty> NewItemProperties { get; set; } = [];

    public List<NewDeviceProperty> NewDeviceProperties { get; set; } = [];

    public List<ItemGridColumn> ItemGridColumns { get; set; } = [];

    public List<DeviceGridColumn> DeviceGridColumns { get; set; } = [];

    public List<string> UserIds { get; set; } = [];
}
