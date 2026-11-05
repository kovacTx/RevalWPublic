namespace RevalW.Services.ItemsAPI.Model;

public class Item2Tag
{
    public long ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
