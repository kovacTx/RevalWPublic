using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.DevicesAPI.Model;

public class GeneratedDocument
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public long DocumentId { get; set; }
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
}
