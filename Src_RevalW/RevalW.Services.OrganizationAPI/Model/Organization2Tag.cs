namespace RevalW.Services.OrganizationAPI.Model;

public class Organization2Tag
{
    public long OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
