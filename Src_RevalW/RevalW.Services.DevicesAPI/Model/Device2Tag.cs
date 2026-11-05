namespace RevalW.Services.DevicesAPI.Model;

public class Device2Tag
{
    public long DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
