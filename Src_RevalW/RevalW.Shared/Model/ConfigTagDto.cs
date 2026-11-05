namespace RevalW.Shared.Model;

public class ConfigTagDto
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public IEnumerable<ConfigUpsertDto> Configs { get; set; } = [];
    public IEnumerable<ItemUpsertDto> Items { get; set; } = [];
}
