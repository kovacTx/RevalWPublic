using RevalW.Shared.Utils;

namespace RevalW.Shared.Model;

public class ItemHeaderDto
{
    public long Id { get; set; }

    public long OrganizationId { get; set; }

    public EItemStatus Status { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<long> Documents2Print { get; set; } = [];
    public List<GeneratedDocumentDto> GeneratedDocuments { get; set; } = [];
    public string? ItemDataJson { get; set; }
    public string? DeviceDataJson { get; set; }
    public long? UsedConfigId { get; set; }
    public long? DeviceId { get; set; }
    public DeviceHeaderDto? Device { get; set; }

    public string? UserIdCreated { get; set; }
    public string? UserIdExported { get; set; }
    public string? UserIdImported { get; set; }
    public string? UserIdFinalized { get; set; }
    public string? UserIdDeleted { get; set; }
    public string? UserIdUndeleted { get; set; }
    public string? UserIdReadonlied { get; set; }

    public DateTime LastUpdated { get; set; }

    public virtual string GetMyHash()
    {
        return $"{ItemDataJson}|{UserIdCreated}";
    }
}
