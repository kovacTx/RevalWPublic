namespace RevalW.Shared.Model;

public class DocumentGenerateRequest
{
    public long DocumentTemplateId { get; set; }
    public long ItemId { get; set; }
    public long OrganizationId { get; set; }

    public string DocumentLabel { get; set; } = null!;

    public string? ItemDataJson { get; set; }
    public string? ConfigDataJson { get; set; }
    public string? UserDataJson { get; set; }
    public string? AppDataJson { get; set; }
    public string? Template { get; set; }
    public IEnumerable<(string? name, string? blobPath)>? ResourceFiles { get; set; }
    public bool TryFinishAfter { get; set; }

}
