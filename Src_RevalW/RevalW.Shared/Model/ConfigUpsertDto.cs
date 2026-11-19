using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class ConfigUpsertDto : ConfigHeaderDto
{
    [Required]
    public string Content { get; set; } = null!;

    public List<DynamicPropertyDto> NewItemProperties { get; set; } = [];

}
