using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class DocumentUpsertDto
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long ConfigId { get; set; }

    public string? Template { get; set; }

    [Required]
    public string Label { get; set; } = default!;

    public string? Enabled { get; set; }
    public string? AlwaysTrue { get; set; }

    public string? RestParams { get; set; }

    public DateTime LastUpdated { get; set; }

}
