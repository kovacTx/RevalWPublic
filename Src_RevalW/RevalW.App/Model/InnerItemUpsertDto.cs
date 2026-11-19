using RevalW.App.Model.IFace;
using RevalW.Shared.Model;

namespace RevalW.App.Model;

public class InnerItemUpsertDto : ItemUpsertDto, ITaggable
{
    public string? TagsJson { get; set; }

}
