namespace RevalW.Shared.Utils;

public enum EItemStatus
{
    New = 0x01,
    Exported = 0x02,
    Imported = 0x04,
    Finished = 0x08,
    Readonly = 0x10,
    Deleted = 0x80
}
