using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class DeviceHeaderDto
{
    public long Id { get; set; }

    public long OrganizationId { get; set; }

    public List<string> Tags { get; set; } = [];
    public List<GeneratedDocumentDto> GeneratedDocuments { get; set; } = [];

    [Required]
    public string Label { get; set; } = null!;
    public string? OwnerEmailsJson { get; set; }
    public string? DeviceDataJson { get; set; } // the keys should be equal to ItemDataJson

    public DateTime LastUpdated { get; set; }

    public virtual string GetMyHash()
    {
        return $"{DeviceDataJson}|{LastUpdated}";
    }
}
