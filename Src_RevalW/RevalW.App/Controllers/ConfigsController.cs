using AutoMapper;
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

namespace RevalW.App.Controllers;

public class ConfigsController(ConfigsService configService, ConfigsTagsService tagsService, CDocumentsService cDocumentsService, IMapper mapper) : Controller
{
    [HttpGet]
    public IActionResult ConfigsIndex()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetAll([FromForm] DTRequest request)
    {
        var resp = await configService.GetByDataTablesAsync(request, false);
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
        var resp = await tagsService.GetAllAsync(false);
        if (!resp.IsOk()) TempData["error"] = resp?.Message;
        ViewBag.TakenTags = resp!.Result;
        return this.EndOk(viewName: nameof(Upsert), dto: new InnerConfigUpsertDto());
    }
    [HttpPost]
    public async Task<IActionResult> Autosave([FromBody] MonacoAutosaveRequest dto)
    {
        if (dto is null) return BadRequest();
        if (dto.ContainerId is null) return BadRequest();
        if (dto.Content is null) return BadRequest();

        if (dto.ContainerId == "contentContainer")
        {
            await configService.AutosaveAsync(dto);
        }
        else if (dto.ContainerId == "documentContainer")
        {
            await cDocumentsService.AutosaveAsync(dto);
        }
        return Ok();
    }
    public async Task<IActionResult> Upsert(InnerConfigUpsertDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        ResponseDto<ConfigUpsertDto?>? resp;

        var isForCreate = dto.Id == 0;

        var configUpsertDto = dto.ToConfigUpsertDto()!;
        var documents = dto.ToPrintableDocuments();
        var documentsResources = dto.ToDocumentsResources();

        if (isForCreate)
            resp = await configService.CreateAsync(configUpsertDto);
        else
            resp = await configService.UpdateAsync(configUpsertDto);
        if (!resp.IsOk())
        {
            SetViewBagData(configUpsertDto, documents, documentsResources);
            return this.EndErr(dto: dto, message: (isForCreate) ? "could not create config" : "could not update config", resp: resp);
        }
        dto.Id = resp!.Result!.Id;
        dto.OrganizationId = resp!.Result!.OrganizationId;
        var errMsg = "";
        var resFilErrMsg = await RemoveNUploadResourceFiles(dto.OrganizationId, dto.Id, documentsResources ?? [], dto.NewDocumentsResources);
        if (!string.IsNullOrEmpty(resFilErrMsg)) errMsg += $" resource files had problem: {resFilErrMsg}";
        var imgErrMsg = await UpdateImage(dto);
        if (!string.IsNullOrEmpty(imgErrMsg)) errMsg += $" image had problem: {imgErrMsg}";
        var dllErrMsg = await UpdateDlls(dto);
        if (!string.IsNullOrEmpty(dllErrMsg)) errMsg += $" dlls had problem: {dllErrMsg}";
        var docErrMsg = await UpdateDocuments(resp!.Result!.Id, documents);
        if (!string.IsNullOrEmpty(docErrMsg)) errMsg += $" documents had problem: {docErrMsg}";

        if (!string.IsNullOrEmpty(errMsg))
        {
            SetViewBagData(configUpsertDto, documents, documentsResources);
            return this.EndErr(dto: dto, message: errMsg);
        }

        return this.EndOk(viewName: nameof(ConfigsIndex), message: (isForCreate) ? "config created successfully" : "config updated successfully");
    }
    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var resp = await configService.GetByIdAsync(id);
        if (!resp.IsOk()) return this.EndErr(viewName: nameof(ConfigsIndex), resp: resp);

        var pDResp = await cDocumentsService.GetByConfigId(id);
        if (!pDResp.IsOk()) return this.EndErr(viewName: nameof(ConfigsIndex), resp: pDResp);

        var rResp = await cDocumentsService.GetResourcesByConfigIdAsync(id);
        if (!rResp.IsOk()) return this.EndErr(viewName: nameof(ConfigsIndex), resp: rResp);

        SetViewBagData(resp!.Result, pDResp?.Result, rResp?.Result);

        return this.EndOk(viewName: nameof(Upsert), dto: resp!.Result.ToInnerConfigUpsertDto(pDResp?.Result, rResp?.Result));
    }
    [HttpGet]
    public async Task<IActionResult> Delete(long id)
    {
        var resp = await configService.GetByIdAsync(id);
        if (!resp.IsOk()) return this.EndErr(resp: resp);

        var dResp = await cDocumentsService.GetByConfigId(id);
        if (!dResp.IsOk()) return this.EndErr(viewName: nameof(ConfigsIndex), resp: resp);

        var rResp = await cDocumentsService.GetResourcesByConfigIdAsync(id);
        if (!rResp.IsOk()) return this.EndErr(viewName: nameof(ConfigsIndex), resp: rResp);

        SetViewBagData(resp!.Result, dResp?.Result, rResp?.Result);

        return this.EndOk(dto: resp!.Result.ToInnerConfigUpsertDto(dResp?.Result, rResp?.Result));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(InnerConfigUpsertDto dto)
    {
        var resp = await configService.DeleteConfigAsync(dto.Id);
        if (!resp.IsOk()) return this.EndErr(resp: resp);
        var docResp = await cDocumentsService.DeleteByConfigId(dto.Id);
        if (!docResp.IsOk()) return this.EndErr(resp: resp);
        TempData["success"] = "Config deleted";
        return RedirectToAction(nameof(ConfigsIndex));
    }

    private async Task<string> UpdateDocuments(long configId, List<DocumentTemplateDto>? documents)
    {
        var oldDocsResp = await cDocumentsService.GetByConfigId(configId);
        if (!oldDocsResp.IsOk()) return " cannot read old documents";

        var oldDocs = oldDocsResp!.Result?.ToList() ?? [];
        documents ??= [];

        var errMsg = "";
        foreach (var d in documents)
        {
            d.ConfigId = configId;
            if (d.Id != 0)
            {
                var dUpdateResp = await cDocumentsService.Update(d);
                if (!dUpdateResp.IsOk())
                {
                    errMsg += " " + (dUpdateResp?.Message ?? $"cannot update document {d.Label}");
                    break;
                }
                oldDocs.RemoveAll(x => x.Id == d.Id);
            }
            else
            {
                var dCreateResp = await cDocumentsService.Create(d);
                if (!dCreateResp.IsOk())
                {
                    errMsg += " " + (dCreateResp?.Message ?? $"cannot create new document {d.Label}");
                    break;
                }
            }
        }
        foreach (var d in oldDocs)
        {
            var dDeleteResp = await cDocumentsService.Delete(d.Id);
            if (!dDeleteResp.IsOk())
            {
                errMsg += " " + (dDeleteResp?.Message ?? $"cannot delete document {d.Label}");
                break;
            }
        }
        return errMsg;
    }
    private async Task<string?> RemoveNUploadResourceFiles(long organizationId, long configId, List<DocumentsResourceFileDto> resourcesToLeave, List<IFormFile?> newResources)
    {
        var resourcesRes = await cDocumentsService.GetResourcesByConfigIdAsync(configId);
        var oldResources = resourcesRes?.Result ?? [];
        var remTasks = new List<Task<string?>>();
        foreach (var oldResource in oldResources)
        {
            if (resourcesToLeave.Any(r => r.Name == oldResource.Name)) continue;
            remTasks.Add(RemoveResourceFile(oldResource));
        }

        if (newResources is null) return await GetErrMsg(remTasks, null);
        var uploadTasks = new List<Task<string?>>();
        foreach (var file in newResources)
        {
            uploadTasks.Add(UploadResourceFile(new()
            {
                ConfigId = configId,
                OrganizationId = organizationId,
                File = file
            }));
        }
        return await GetErrMsg(remTasks, uploadTasks);
    }
    private static async Task<string?> GetErrMsg(List<Task<string?>>? remTasks, List<Task<string?>>? uploadTasks)
    {
        var remErr = await GetErrMsg(remTasks);
        var uploadErr = await GetErrMsg(uploadTasks);
        if (string.IsNullOrEmpty(remErr)) return uploadErr;
        if (string.IsNullOrEmpty(uploadErr)) return remErr;
        return remErr + ", " + uploadErr;
    }
    private static async Task<string?> GetErrMsg(List<Task<string?>>? tasks)
    {
        if (tasks is null || tasks.Count == 0) return null;
        var results = await Task.WhenAll(tasks);
        var errMsg = string.Join(", ", results.Where(r => !string.IsNullOrEmpty(r)));
        return string.IsNullOrEmpty(errMsg) ? null : errMsg;
    }
    private async Task<string?> UploadResourceFile(UploadDocumentsResourceFileDto dto)
    {
        if (!this.IsAuthorized(RightsList.Document_Create) && !this.IsAuthorized(RightsList.Document_Edit))
            return "user is not authorized for this operation";
        var resp = await cDocumentsService.UploadResourceFileAsync(dto);
        if (!resp.IsOk()) return resp?.Message;
        return null;
    }
    private async Task<string?> RemoveResourceFile(DocumentsResourceFileDto dto)
    {
        if (!this.IsAuthorized(RightsList.Document_Create) && !this.IsAuthorized(RightsList.Document_Edit))
            return "user is not authorized for this operation";
        var resp = await cDocumentsService.RemoveResourceFileAsync(dto.Id);
        if (!resp.IsOk()) return resp?.Message;
        return null;
    }
    private async Task<string?> UpdateImage(InnerConfigUpsertDto dto)
    {
        if (!dto.UpdateImage) return null;
        if (!User.IsInRole(ERoles.UberAdmin)) return "no permissions";
        var updateImageDto = mapper.Map<UpdateImageDto>(dto);
        var resp = await configService.UpdateImageAsync(updateImageDto);
        if (!resp.IsOk()) return resp?.Message;
        return null;
    }
    private async Task<string?> UpdateDlls(InnerConfigUpsertDto dto)
    {
        if (!dto.UpdateWDll && !dto.UpdateADll && !dto.UpdateIDll) return null;
        if (!User.IsInRole(ERoles.UberAdmin)) return "no permissions";
        var updateDllDto = mapper.Map<UpdateDllDto>(dto);
        var resp = await configService.UpdateDllAsync(updateDllDto);
        if (!resp.IsOk()) return resp?.Message;
        return null;
    }

    private void SetViewBagData(ConfigUpsertDto? config, IEnumerable<DocumentTemplateDto>? documents, IEnumerable<DocumentsResourceFileDto>? resources)
    {
        ViewBag.Tags = config?.Tags;
        ViewBag.PrintableDocuments = documents;
        ViewBag.DocumentsResources = resources;
    }
}
