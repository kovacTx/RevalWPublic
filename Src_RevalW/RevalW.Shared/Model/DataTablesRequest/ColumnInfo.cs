namespace RevalW.Shared.Model.DataTablesRequest;

public class ColumnInfo
{
    public string? Data { get; set; }
    public string? Name { get; set; }
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public SearchInfo? Search { get; set; }
}