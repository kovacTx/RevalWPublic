using RevalW.Shared.Model;

namespace RevalW.App.Model;

public class DynamicProperties4Tag
{
    public long TagId { get; set; }
    public string? TagName { get; set; }
    public List<DynamicPropertyDto>? DynamicProperties { get; set; }
}
