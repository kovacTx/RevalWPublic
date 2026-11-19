using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RevalW.Services.DevicesAPI.Model;

public class Device
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public List<Tag> Tags { get; set; } = [];

    public List<GeneratedDocument> GeneratedDocuments { get; set; } = [];

    [Required]
    public string Label { get; set; } = null!;
    public JsonDocument? OwnerEmails { get; set; }
    public JsonDocument? ConfigData { get; set; }
    public JsonDocument? DeviceData { get; set; }
    public DateTime LastUpdated { get; set; }

    public bool Deleted { get; set; }
}
