namespace RevalW.Services.Shared.Utils;

public static class RightsList
{
    public static (string, string) User_Register { get; } = (nameof(User_Register), "User.Register");
    public static (string, string) User_Edit { get; } = (nameof(User_Edit), "User.Edit");
    public static (string, string) User_Erase { get; } = (nameof(User_Erase), "User.Erase");

    public static (string, string) Item_View { get; } = (nameof(Item_View), "Item.View");
    public static (string, string) Item_Import { get; } = (nameof(Item_Import), "Item.Import.ToApp");
    public static (string, string) Item_Create { get; } = (nameof(Item_Create), "Item.Create");

    public static (string, string) Item_DuplicateNormal { get; } = (nameof(Item_DuplicateNormal), "Item.Duplicate.Normal");
    public static (string, string) Item_DuplicateReadonly { get; } = (nameof(Item_DuplicateReadonly), "Item.Duplicate.Readonly");

    public static (string, string) Item_NewEdit { get; } = (nameof(Item_NewEdit), "Item.New.Edit");
    public static (string, string) Item_NewErase { get; } = (nameof(Item_NewErase), "Item.New.Erase");
    public static (string, string) Item_NewForward { get; } = (nameof(Item_NewForward), "Item.New.Forward");
    public static (string, string) Item_NewReadonly { get; } = (nameof(Item_NewReadonly), "Item.New.Readonly");
    public static (string, string) Item_NewSetDevice { get; } = (nameof(Item_NewSetDevice), "Item.New.SetDevice");

    public static (string, string) Item_ExportedReset { get; } = (nameof(Item_ExportedReset), "Item.Exported.Reset");
    public static (string, string) Item_ExportedReadonly { get; } = (nameof(Item_ExportedReadonly), "Item.Exported.Readonly");

    public static (string, string) Item_ImportedReset { get; } = (nameof(Item_ImportedReset), "Item.Imported.Reset");
    public static (string, string) Item_ImportedReadonly { get; } = (nameof(Item_ImportedReadonly), "Item.Imported.Readonly");
    public static (string, string) Item_ImportedForward { get; } = (nameof(Item_ImportedForward), "Item.Imported.Forward");
    public static (string, string) Item_ImportedRegenerateDoc { get; } = (nameof(Item_ImportedRegenerateDoc), "Item.Imported.RegenerateDoc");
    public static (string, string) Item_ImportedSetDevice { get; } = (nameof(Item_ImportedSetDevice), "Item.Imported.SetDevice");

    public static (string, string) Item_FinishedReset { get; } = (nameof(Item_FinishedReset), "Item.Finished.Reset");

    public static (string, string) Item_ReadonlyReset { get; } = (nameof(Item_ReadonlyReset), "Item.Readonly.Reset");

    public static (string, string) Item_DeletedReset { get; } = (nameof(Item_DeletedReset), "Item.Deleted.Reset");

    public static (string, string) Configs_View { get; } = (nameof(Configs_View), "Configs.View");
    public static (string, string) Config_Edit { get; } = (nameof(Config_Edit), "Config.Edit");
    public static (string, string) Config_Create { get; } = (nameof(Config_Create), "Config.Create");
    public static (string, string) Config_Erase { get; } = (nameof(Config_Erase), "Config.Erase");

    public static (string, string) Device_Edit { get; } = (nameof(Device_Edit), "Device.Edit");
    public static (string, string) Device_Create { get; } = (nameof(Device_Create), "Device.Create");
    public static (string, string) Device_Erase { get; } = (nameof(Device_Erase), "Device.Erase");

    public static (string, string) Document_Edit { get; } = (nameof(Document_Edit), "Document.Edit");
    public static (string, string) Document_Create { get; } = (nameof(Document_Create), "Document.Create");
    public static (string, string) Document_Erase { get; } = (nameof(Document_Erase), "Document.Erase");

    public static List<(string key, string value)> All { get; } =
    [
        User_Register,
        User_Edit,
        User_Erase,
        Item_View,
        Item_Import,
        Item_Create,
        Item_DuplicateNormal,
        Item_DuplicateReadonly,
        Item_NewEdit,
        Item_NewErase,
        Item_NewForward,
        Item_NewReadonly,
        Item_NewSetDevice,
        Item_ExportedReset,
        Item_ExportedReadonly,
        Item_ImportedReset,
        Item_ImportedReadonly,
        Item_ImportedForward,
        Item_ImportedRegenerateDoc,
        Item_ImportedSetDevice,
        Item_FinishedReset,
        Item_ReadonlyReset,
        Item_DeletedReset,
        Configs_View,
        Config_Create,
        Config_Edit,
        Config_Erase,
        Device_Create,
        Device_Edit,
        Device_Erase,
        Document_Create,
        Document_Edit,
        Document_Erase
    ];
    public static List<(string key, string value)> AdminRights { get; } =
    [
        Item_View,
        Item_Import,
        Item_Create,
        Item_DuplicateNormal,
        Item_DuplicateReadonly,
        Item_NewEdit,
        Item_NewErase,
        Item_NewForward,
        Item_NewReadonly,
        Item_NewSetDevice,
        Item_ExportedReset,
        Item_ExportedReadonly,
        Item_ImportedReset,
        Item_ImportedReadonly,
        Item_ImportedForward,
        Item_ImportedRegenerateDoc,
        Item_ImportedSetDevice,
        Item_FinishedReset,
        Item_ReadonlyReset,
        Item_DeletedReset,
        Configs_View,
        Config_Create,
        Config_Edit,
        Config_Erase,
        Device_Create,
        Device_Edit,
        Device_Erase,
        Document_Create,
        Document_Edit,
        Document_Erase
    ];
    public static List<(string key, string value)> UserRights { get; } =
    [
        Item_View,
        Item_Import,
        Item_Create,
        Item_NewForward,
        Item_DuplicateNormal,
        Item_DuplicateReadonly,
        Item_NewEdit,
        Item_ImportedRegenerateDoc,
        Item_ImportedSetDevice,
        Device_Edit
    ];
    public static List<(string key, string value)> ConfigRights { get; } =
    [
        Configs_View,
        Config_Create,
        Config_Edit,
        Config_Erase,
    ];
    public static List<(string key, string value)> DeviceRights { get; } =
    [
        Device_Create,
        Device_Edit,
        Device_Erase,
    ];

}
