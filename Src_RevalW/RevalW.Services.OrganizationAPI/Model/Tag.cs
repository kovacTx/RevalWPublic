using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.OrganizationAPI.Model;

public class Tag
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public List<Organization> Organizations { get; set; } = [];
}
