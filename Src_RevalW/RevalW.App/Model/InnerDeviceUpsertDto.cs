using RevalW.App.Model.IFace;
using RevalW.Shared.Model;

namespace RevalW.App.Model;

public class InnerDeviceUpsertDto : DeviceUpsertDto, ITaggable
{
    public string? TagsJson { get; set; }

}
