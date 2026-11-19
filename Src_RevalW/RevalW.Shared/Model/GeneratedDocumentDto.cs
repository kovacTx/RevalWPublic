namespace RevalW.Shared.Model;

public class GeneratedDocumentDto
{
    public long ItemId { get; set; }
    public long DocumentId { get; set; }
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
    public bool FinishAfter { get; set; }
}
