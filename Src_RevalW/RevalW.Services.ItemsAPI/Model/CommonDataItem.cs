using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.ItemsAPI.Model;

public class CommonDataItem
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long CommonDataWrapperId { get; set; }

    [Required]
    public string KeyValue { get; set; } = null!;

    public string? Data { get; set; }
}
