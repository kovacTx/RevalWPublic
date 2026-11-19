using Microsoft.AspNetCore.Mvc;
using RevalW.App.Service;
using RevalW.App.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using System.Text.Json;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;
using SJsonSerializer = RevalM.SharedStorage.Utils.JsonSerializer;

namespace RevalW.App.Controllers;

public class UsersController(AuthService authService, UsersService usersService, OrganizationService organizationService) : Controller
{
    [HttpGet]
    public IActionResult UserIndex()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetAll([FromForm] DTRequest request)
    {
        var resp = await usersService.GetByDataTablesAsync(request);
        if (resp?.Result?.data is null)
        {
            TempData["error"] = resp?.Message;
            return Json(new
            {
                draw = request.Draw,
                recordsTotal = 0,
                recordsFiltered = 0,
            });
        }

        var res = JsonSerializer.Serialize(resp!.Result);
        return Content(res ?? "", "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> UberGetAll([FromForm] DTRequest request)
    {
        var resp = await usersService.UberGetByDataTablesAsync(request);
        if (resp.IsOk()) return Content(resp!.Result!.ToString() ?? "", "application/json");
        TempData["error"] = resp?.Message;
        return Json(new
        {
            draw = request.Draw,
            recordsTotal = 0,
            recordsFiltered = 0,
        });
    }

    public async Task<IActionResult> Upsert(UserDto dto, string[] selectedRoles)
    {
        if (!ModelState.IsValid) return View(dto);

        if (dto.Password != dto.Password2) return this.EndErr(dto: dto, message: "passwords does not match");
        if (selectedRoles.Length == 0) return this.EndErr(dto: dto, message: "at least one role must be selected");

        if (dto.Email == User.Identity?.Name && this.IsCreator()) dto.Roles = [ERoles.Creator, .. selectedRoles];
        else dto.Roles = [.. selectedRoles];

        var resp = await usersService.UpdateAsync(dto);
        if (!resp.IsOk())
        {
            await SetViewBagData(dto);
            return this.EndErr(dto: dto, message: $"could not update user: {resp?.Message}", resp: resp);
        }
        var passResp = await TryChangePassword(dto);
        if (passResp is not null) return passResp;

        return this.EndRedirectOk(nameof(UserIndex), message: "user updated succesfully");
    }

    [HttpGet]
    public async Task<IActionResult> Update([FromQuery] string? id, [FromQuery] string? returnUrl = null)
    {
        ResponseDto<UserDto?>? resp;
        if (string.IsNullOrEmpty(id))
            resp = await usersService.GetMeAsync();
        else
            resp = await usersService.GetByIdAsync(id);

        if (!resp.IsOk()) return this.EndErr(viewName: nameof(UserIndex), resp: resp);
        await SetViewBagData(resp!.Result);
        return this.EndOk(viewName: nameof(Upsert), dto: resp!.Result);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrEmpty(id)) return this.EndRedirectErr(nameof(UserIndex), message: "invalid user ID");
        var resp = await authService.DeleteAsync(id);
        if (!resp.IsOk()) return this.EndRedirectErr(nameof(UserIndex), message: $"failed to remove user: {resp?.Result?.Message}");

        return this.EndRedirectOk(nameof(UserIndex), message: "user removed successfully");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(UserDto dto)
    {
        if (string.IsNullOrEmpty(dto.ID)) return this.EndErr(viewName: nameof(UserIndex), message: "invalid user ID");
        var resp = await usersService.DeleteUserAsync(dto.ID);
        if (!resp.IsOk()) return this.EndErr(resp: resp);
        TempData["success"] = "User deleted";
        return RedirectToAction(nameof(UserIndex));
    }

    private async Task SetViewBagData(UserDto? user)
    {
        ViewBag.Roles = new List<Role>();
        var resp = await authService.GetAssignableRoleNamesAsync();
        if (resp is null)
        {
            TempData["error"] = $"failed to fetch available roles";
            return;
        }
        if (!TrySetRoles(resp?.Result, user?.Roles))
        {
            TempData["error"] = $"no roles available ({resp?.Message})";
            return;
        }

        var pResp = await organizationService.GetNewUserProps();
        if (!pResp.IsOk())
        {
            TempData["error"] = pResp?.Message ?? "";
            return;
        }
        ViewBag.NewUserProperties = pResp!.Result!;
        ViewBag.UserValues = SJsonSerializer.Deserialize<Dictionary<string, string>>(user?.UserDataJson ?? "{}");
    }
    private bool TrySetRoles(IEnumerable<string?>? allRoles, IEnumerable<string?>? myRoles)
    {
        if (allRoles is null) return false;
        ViewBag.Roles = allRoles.Where(r => r is not null).Select(r => new Role() { Name = r!, Selected = myRoles?.Contains(r) ?? false }).ToList();
        return true;
    }
    private async Task<ViewResult?> TryChangePassword(UserDto dto)
    {
        if (dto.Password != dto.Password2) return this.EndErr(dto: dto, message: "passwords does not match");
        if (string.IsNullOrEmpty(dto.Password)) return null;
        if (string.IsNullOrEmpty(dto.ID)) return this.EndErr(dto: dto, message: "no user to change?");
        var changePasswordDto = new ChangePasswordRequestDto()
        {
            Id = dto.ID!,
            Password = dto.Password!,
        };
        var resp = await authService.ChangePasswordAsync(changePasswordDto);
        if (!resp.IsOk())
        {
            await SetViewBagData(dto);
            return this.EndErr(dto: dto, message: $"could not change password: {resp?.Message}", resp: resp);
        }
        return null;
    }
}
