using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.DevicesAPI.Model;

public class Tag
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public ICollection<Device> Device { get; set; } = [];
}
