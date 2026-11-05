using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.ItemsAPI.Model;

public class GeneratedDocument
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public long DocumentId { get; set; }
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
}
