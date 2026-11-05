namespace RevalW.Shared.Model.DataTablesRequest;

public class Request
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? StatusFilter { get; set; }
    public SearchInfo? Search { get; set; }
    public List<OrderInfo>? Order { get; set; }
    public List<ColumnInfo>? Columns { get; set; }
}