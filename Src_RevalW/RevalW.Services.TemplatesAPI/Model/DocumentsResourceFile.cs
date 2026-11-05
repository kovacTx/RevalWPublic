using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.TemplatesAPI.Model;

public class DocumentsResourceFile
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ConfigId { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public string? Name { get; set; }

    [Required]
    public string? PrivatePath { get; set; }
}
