using System.Text;

namespace RevalW.Services.DocumentsAPI.Utils;

public class DataIndexes
{
    public class PathPart
    {
        public string ItemName { get; set; } = default!;
        public int? ItemIndex { get; set; }
        public string? IndexName { get; set; }
        public PathPart Duplicate() => (PathPart)MemberwiseClone();
    }
    public LinkedList<PathPart> Parts = new();

    public DataIndexes Duplcate()
    {
        var ret = new DataIndexes();
        foreach (var part in Parts) ret.Parts.AddLast(part.Duplicate());
        return ret;
    }
    public override string ToString()
    {
        var strinBuilder = new StringBuilder();
        foreach (var part in Parts)
        {
            strinBuilder.Append(part.ToString());
            strinBuilder.Append('.');
        }
        var ret = strinBuilder.ToString();
        return ret[..^1];
    }
}
