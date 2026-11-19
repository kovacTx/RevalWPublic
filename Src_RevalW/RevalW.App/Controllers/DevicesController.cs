using Microsoft.AspNetCore.Mvc;
using RevalW.App.Model;
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

public class DevicesController(DevicesService deviceService, IServiceProvider serviceProvider, AuthService authService) : Controller
{
    public const string ControllerName = "Devices";
    private bool IsForCreate { get; set; }

    private OrganizationService? _organizationService;
    private OrganizationService OrganizationService => _organizationService ??= serviceProvider.GetRequiredService<OrganizationService>();

    private ConfigsTagsService? _cTagsService;
    private ConfigsTagsService CTagsService => _cTagsService ??= serviceProvider.GetRequiredService<ConfigsTagsService>();

    private AzurePublicBlobService? _azureBlobService;
    private AzurePublicBlobService AzureBlobService => _azureBlobService ??= serviceProvider.GetRequiredService<AzurePublicBlobService>();

    private TagDynamicPropertiesWorker? _dynamicPropertiesWorker;
    private TagDynamicPropertiesWorker DynamicPropertiesWorker => _dynamicPropertiesWorker ??= new TagDynamicPropertiesWorker(serviceProvider, this);

    [HttpGet]
    public async Task<IActionResult> DeviceIndex()
    {
        var vbRet = await AddDeviceIndexViewBagData();
        if (vbRet is not null)
        {
            TempData["error"] = vbRet;
            return View();
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetAll([FromForm] DTRequest request)
    {
        var resp = await deviceService.GetByDataTablesAsync(request);
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

    [HttpGet]
    public async Task<IActionResult> Insert()
    {
        var vbRet = await AddViewBagData(null);
        if (vbRet is not null)
        {
            TempData["error"] = vbRet;
            return View(nameof(DeviceIndex));
        }

        return this.EndOk(viewName: nameof(Upsert), dto: new InnerDeviceUpsertDto());
    }
    public async Task<IActionResult> Upsert(InnerDeviceUpsertDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        ResponseDto<DeviceUpsertDto?>? resp;
        IsForCreate = dto.Id == 0;
        var deviceUpsertDto = dto.ToDeviceUpsertDto()!;
        if (IsForCreate)
            resp = await deviceService.CreateAsync(deviceUpsertDto);
        else
            resp = await deviceService.UpdateAsync(deviceUpsertDto);
        if (!resp.IsOk())
        {
            await AddViewBagData(deviceUpsertDto);
            ViewBag.Tags = deviceUpsertDto.Tags;
            var message = (IsForCreate) ? "could not create Device: " : "could not update Device: ";
            message += resp?.Message;
            return this.EndErr(dto: dto, message: message);//, resp: resp);
        }

        await AddDeviceIndexViewBagData();
        TempData["success"] = (IsForCreate) ? "Device created successfully" : "Device updated successfully";
        return RedirectToAction(nameof(DeviceIndex));
        // return this.EndOk(viewName: nameof(deviceIndex), message: (IsForCreate) ? "Device created successfully" : "Device updated successfully");
    }

    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var resp = await deviceService.GetDeviceByIdAsync(id);
        if (!resp.IsOk())
        {
            await AddDeviceIndexViewBagData();
            TempData["error"] = resp?.Message;
            return RedirectToAction(nameof(DeviceIndex));
        }
        var dto = resp!.Result!;
        var vbRet = await AddViewBagData(dto);
        if (vbRet is not null)
        {
            await AddDeviceIndexViewBagData();
            TempData["error"] = vbRet;
            return RedirectToAction(nameof(DeviceIndex));
        }
        await DynamicPropertiesWorker.TryAddConfigsDynamicProperties(dto.Tags);
        var innerDto = dto.ToInnerDeviceUpsertDto();
        await DynamicPropertiesWorker.TrySetTagsJson(innerDto);

        return this.EndOk(viewName: nameof(Upsert), dto: innerDto);
    }

    [HttpGet]
    public async Task<IActionResult> View(long id)
    {
        var resp = await deviceService.GetDeviceByIdAsync(id);
        if (!resp.IsOk())
        {
            await AddDeviceIndexViewBagData();
            TempData["error"] = resp?.Message;
            return RedirectToAction(nameof(DeviceIndex));
        }
        var dto = resp!.Result!;

        var vbRet = await AddViewBagData(dto);
        if (vbRet is not null)
        {
            await AddDeviceIndexViewBagData();
            TempData["error"] = vbRet;
            return RedirectToAction(nameof(DeviceIndex));
        }
        return this.EndOk(viewName: "ViewDetails", dto: dto.ToInnerDeviceUpsertDto());
    }

    [HttpGet]
    public async Task<IActionResult> Delete(long id)
    {
        var resp = await deviceService.DeleteAsync(id);
        await AddDeviceIndexViewBagData();
        if (!resp.IsOk())
            TempData["error"] = resp?.Message;
        else
            TempData["success"] = "Device deleted";
        return RedirectToAction(nameof(DeviceIndex));
    }

    [HttpGet]
    public async Task<DeviceConfigDataNTagDtosDto?> GetConfigDataNTags(long id)
    {
        var ct = (await deviceService.GetConfigDataNTagsAsync(id))?.Result;
        if (ct is null) return null;
        if (ct.Tags is null || ct.Tags.Count == 0)
            return new DeviceConfigDataNTagDtosDto { ConfigData = ct.ConfigData };

        var tags = await DynamicPropertiesWorker.GetAvailableTags();
        if (tags is null)
            return new DeviceConfigDataNTagDtosDto { ConfigData = ct.ConfigData };

        var ret = new DeviceConfigDataNTagDtosDto
        {
            ConfigData = ct.ConfigData,
            Tags = [.. tags.Where(t => ct.Tags.Contains(t.Name))],
        };
        return ret;
    }

    #region private
    private async Task<string?> AddDeviceIndexViewBagData()
    {
        var pResp = await OrganizationService.GetDeviceGridColumn();
        if (!pResp.IsOk()) return pResp?.Message ?? "";
        ViewBag.DeviceGridColumns = pResp!.Result!;
        return null;
    }
    private async Task<string?> AddViewBagData(DeviceUpsertDto? dto)
    {
        var resp = await DynamicPropertiesWorker.TrySetAvailableTagsToViewBag();
        if (resp is not null) return resp;

        var pResp = await OrganizationService.GetNewDeviceProps();
        if (!pResp.IsOk()) return pResp?.Message ?? "";
        ViewBag.NewDeviceProperties = pResp!.Result!;
        ViewBag.Tags = dto?.Tags;
        ViewBag.DeviceValues = SJsonSerializer.Deserialize<Dictionary<string, string>>(dto?.DeviceDataJson ?? "{}");
        return null;
    }
    #endregion
}
