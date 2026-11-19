namespace RevalW.Shared.Model;

public class ItemUpsertDto : ItemHeaderDto
{
    public string? ConfigDataJson { get; set; }
    public string? AppDataJson { get; set; }
    public string? FinishedDataJson { get; set; }

    public override string GetMyHash()
    {
        return $"{base.GetMyHash()}|{AppDataJson}";
    }
}
