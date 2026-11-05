using RevalW.Services.ItemsAPI.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using System.Security.Claims;

namespace RevalW.Services.ItemsAPI.Services;

public class Thorizer
{
    public ResponseDtoo? ThorizeUpdate(Item? item, ClaimsPrincipal user)
    {
        if (item is null)
            return ResponseDtoo.Err("there is no such Item to update");
        if (item.Status == EItemStatus.New && !user.IsAuthorized(RightsList.Item_NewEdit))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Exported && item.UserIdExported != user.CurrentUserId())
            return ResponseDtoo.Err("only user that have exported the Item, can update it");
        if (item.Status == EItemStatus.Imported)
            return ResponseDtoo.Err("imported Item can not be updated");
        if (item.Status == EItemStatus.Finished)
            return ResponseDtoo.Err("finished Item can not be updated");
        if (item.Status == EItemStatus.Readonly)
            return ResponseDtoo.Err("readonly Item can not be updated");
        if (item.Status == EItemStatus.Deleted)
            return ResponseDtoo.Err("deleted Item can not be updated");

        return null;
    }
    public ResponseDtoo? ThorizeSetDevice(Item? item, ClaimsPrincipal user)
    {
        if (item is null)
            return ResponseDtoo.Err("there is no such Item to update");
        if (item.Status == EItemStatus.New && !user.IsAuthorized(RightsList.Item_NewSetDevice))
            return ResponseDtoo.Err("the user is not authorized for this operation");
        if (item.Status == EItemStatus.Exported)
            return ResponseDtoo.Err("exported Item can not change device");
        if (item.Status == EItemStatus.Imported && !user.IsAuthorized(RightsList.Item_ImportedSetDevice))
            return ResponseDtoo.Err("the user is not authorized for this operation");
        if (item.Status == EItemStatus.Finished)
            return ResponseDtoo.Err("finished Item can not be updated");
        if (item.Status == EItemStatus.Readonly)
            return ResponseDtoo.Err("readonly Item can not be updated");
        if (item.Status == EItemStatus.Deleted)
            return ResponseDtoo.Err("deleted Item can not be updated");

        return null;
    }
    public ResponseDtoo? ThorizeStatusReset(Item? item, ClaimsPrincipal user)
    {
        if (item is null)
            return ResponseDtoo.Err("there is no such Item to update");
        if (item.Status == EItemStatus.New)
            return ResponseDtoo.Err("new item can not be reset");
        if (item.Status == EItemStatus.Exported && !user.IsAuthorized(RightsList.Item_ExportedReset))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Imported && !user.IsAuthorized(RightsList.Item_ImportedReset))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Finished && !user.IsAuthorized(RightsList.Item_FinishedReset))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Readonly && !user.IsAuthorized(RightsList.Item_ReadonlyReset))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Deleted && !user.IsAuthorized(RightsList.Item_DeletedReset))
            return ResponseDtoo.Err("user is not authorized for this operation");

        return null;
    }
    public ResponseDtoo? ThorizeReadonly(Item? item, ClaimsPrincipal user)
    {
        if (item is null)
            return ResponseDtoo.Err("there is no such Item to readonly");
        if (item.Status == EItemStatus.New && !user.IsAuthorized(RightsList.Item_NewReadonly))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Exported && !user.IsAuthorized(RightsList.Item_ExportedReadonly))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Imported && !user.IsAuthorized(RightsList.Item_ImportedReadonly))
            return ResponseDtoo.Err("user is not authorized for this operation");
        if (item.Status == EItemStatus.Finished)
            return ResponseDtoo.Err("finished item can not be readonlied");
        if (item.Status == EItemStatus.Readonly)
            return ResponseDtoo.Err("readonly item can not be readonlied");
        if (item.Status == EItemStatus.Deleted)
            return ResponseDtoo.Err("deleted item can not be readonlied");

        return null;
    }
}
