namespace RevalW.Shared.Model;

public class DataTablesResponse<T>
{
    public int draw { get; set; }
    public int recordsTotal { get; set; }
    public long recordsFiltered { get; set; }
    public IEnumerable<T>? data { get; set; }
}
