using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.TemplatesAPI.Model;

public class Tag
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public bool Deleted { get; set; }

    [Required]
    public ICollection<Config> Configs { get; set; } = [];
}
