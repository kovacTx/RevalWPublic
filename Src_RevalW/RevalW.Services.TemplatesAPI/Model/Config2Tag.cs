namespace RevalW.Services.TemplatesAPI.Model;

public class Config2Tag
{
    public long ConfigId { get; set; }
    public Config Config { get; set; } = null!;
    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
