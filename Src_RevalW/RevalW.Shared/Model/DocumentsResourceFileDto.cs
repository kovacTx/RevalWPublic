namespace RevalW.Shared.Model;

public class DocumentsResourceFileDto
{
    public long Id { get; set; }
    public long ConfigId { get; set; }
    public long OrganizationId { get; set; }

    public string? Name { get; set; }
    public string? PrivatePath { get; set; }

}
