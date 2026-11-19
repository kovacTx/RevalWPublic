using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.App.Model;
using RevalW.App.Service;
using RevalW.App.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.App.Controllers;

public class OrganizationController(OrganizationService organizationService, AuthService authService, ConfigsService configsService, OTagsService oTagsService) : Controller
{
    [HttpGet]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> Organization([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (id == default) id = currentOrgId;
        if (!isUber && currentOrgId != id)
        {
            TempData["error"] = "have no permission fo different organization. only this:";
            id = currentOrgId;
        }

        var tagsResp = await oTagsService.GetAll();
        if (!tagsResp.IsOk())
        {
            TempData["error"] = tagsResp?.Message;
            return Redirect("/");
        }
        ViewBag.AvailableTags = tagsResp?.Result?.Select(t => t.Name);

        var orgResp = await organizationService.GetById(id);
        if (!orgResp.IsOk())
        {
            TempData["error"] = orgResp?.Message;
            return Redirect("/");
        }
        ViewBag.Tags = orgResp?.Result?.Tags;

        await TryPopulateConfigs(id);
        await TryPopulateAllPermissions();

        ViewBag.NewUserProperties = orgResp!.Result?.NewUserProperties;
        ViewBag.NewItemProperties = orgResp!.Result?.NewItemProperties;
        ViewBag.NewDeviceProperties = orgResp!.Result?.NewDeviceProperties;
        ViewBag.AvailableItemGridColumns = orgResp!.Result?.NewItemProperties.Select(dp => new MainGridColumnDto { Name = dp.Name, Label = dp.Label }).ToList();
        ViewBag.ItemGridColumns = orgResp!.Result?.ItemGridColumns;
        ViewBag.AvailableDeviceGridColumns = orgResp!.Result?.NewDeviceProperties.Select(dp => new MainGridColumnDto { Name = dp.Name, Label = dp.Label }).ToList();
        ViewBag.DeviceGridColumns = orgResp!.Result?.DeviceGridColumns;

        return this.EndOk(dto: orgResp!.Result?.ToInnerOrganizationUpdateDto());
    }

    public async Task<IActionResult> OrganizationUpdate(InnerOrganizationUpdateDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var resp = await organizationService.UpdateAsync(dto.ToOrganizationUpdateDto()!);

        await TryPopulateConfigs(dto.Id);
        await TryPopulateAllPermissions();

        if (!resp.IsOk()) return this.EndErr(viewName: nameof(Organization), dto: dto, message: "could not update organization", resp: resp);
        return this.EndRedirectOk(nameof(Organization), message: "organization updated successfully");
    }

    public async Task<IActionResult> OrganizationsByTags([FromQuery] string tags)
    {
        var resp = await organizationService.GetOrganizationsByTags(tags);
        if (!resp.IsOk())
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        return StatusCode(StatusCodes.Status200OK, resp!.Result);
    }

    [HttpGet]
    public IActionResult OrganizationIndex()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetAll([FromForm] DTRequest request)
    {
        var resp = await organizationService.GetByDataTablesAsync(request);
        if (resp.IsOk()) return Content(resp!.Result!.ToString() ?? "", "application/json");
        TempData["error"] = resp?.Message;
        return Json(new
        {
            draw = request.Draw,
            recordsTotal = 0,
            recordsFiltered = 0,
        });
    }

    private async Task TryPopulateConfigs(long organizationId)
    {
        if (!User.IsInRole(ERoles.UberAdmin)) return;
        var configsResp = await configsService.GetByOrganizationIdAsync(organizationId);
        if (!configsResp.IsOk())
        {
            TempData["error"] = configsResp?.Message;
            return;
        }
        ViewBag.Configs = configsResp?.Result;
        return;
    }
    private async Task TryPopulateAllPermissions()
    {
        var permissionsResp = await authService.GetAllPermissions();
        if (!permissionsResp.IsOk())
        {
            TempData["error"] = permissionsResp?.Message;
            return;
        }
        ViewBag.AllPermissions = permissionsResp?.Result;
        return;
    }
}
