namespace RevalW.Shared.Model;

public class MonacoAutosaveRequest
{
    public long Id { get; set; }
    public long ConfigId { get; set; }
    public string? ContainerId { get; set; }
    public string? Content { get; set; }
}
