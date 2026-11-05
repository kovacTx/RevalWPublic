using RevalW.Shared.Utils;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using SJsonSerializer = RevalM.SharedStorage.Utils.JsonSerializer;

namespace RevalW.Services.ItemsAPI.Model;

public class Item
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OrganizationId { get; set; }

    [Required]
    public long DeviceId { get; set; }

    public EItemStatus Status { get; set; }

    [Required]
    public List<Tag> Tags { get; set; } = [];

    public string? Label { get; set; }
    public JsonDocument? ConfigData { get; set; }
    public JsonDocument? ItemData { get; set; }
    public JsonDocument? DeviceData { get; set; }

    public JsonDocument? AppData { get; set; }
    public JsonDocument? AppFinishedData { get; set; }

    public List<GeneratedDocument> GeneratedDocuments { get; set; } = [];

    private List<long>? _documents2Print;
    [NotMapped]
    public List<long> Documents2Print
    {
        get => _documents2Print ??= SJsonSerializer.Deserialize<List<long>>(Documents2PrintJson) ?? [];
        set
        {
            _documents2Print = value;
            Documents2PrintJson = JsonSerializer.Serialize(value);
        }
    }

    [Required]
    public string Documents2PrintJson { get; set; } = "";

    public long? UsedConfigId { get; set; }

    public string? UserIdCreated { get; set; }
    public string? UserIdExported { get; set; }
    public string? UserIdImported { get; set; }
    public string? UserIdFinalized { get; set; }
    public string? UserIdDeleted { get; set; }
    public string? UserIdUndeleted { get; set; }
    public string? UserIdReadonlied { get; set; }

    public DateTime LastUpdated { get; set; }
    public bool Public { get; set; }
}
