namespace RevalW.App.Model.IFace
{
    public interface ITaggable
    {
        List<string> Tags { get; }
        string? TagsJson { set; }
    }
}
