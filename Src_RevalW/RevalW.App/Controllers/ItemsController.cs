using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RevalW.App.Hubs;
using RevalW.App.Mapper;
using RevalW.App.Model;
using RevalW.App.Service;
using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;
using System.Text.Json;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;
using SJsonSerializer = RevalM.SharedStorage.Utils.JsonSerializer;

namespace RevalW.App.Controllers;

public class ItemsController(ItemsService itemsService, IServiceProvider serviceProvider, AuthService authService) : Controller
{
    public const string ControllerName = "Items";
    private bool IsForCreate { get; set; }

    private CDocumentsService? _cDocumentsService;
    private CDocumentsService CDocumentsService => _cDocumentsService ??= serviceProvider.GetRequiredService<CDocumentsService>();

    private OrganizationService? _organizationService;
    private OrganizationService OrganizationService => _organizationService ??= serviceProvider.GetRequiredService<OrganizationService>();

    private ConfigsTagsService? _cTagsService;
    private ConfigsTagsService CTagsService => _cTagsService ??= serviceProvider.GetRequiredService<ConfigsTagsService>();

    private ConfigsService? _configsService;
    private ConfigsService ConfigsService => _configsService ??= serviceProvider.GetRequiredService<ConfigsService>();

    private AzurePublicBlobService? _azureBlobService;
    private AzurePublicBlobService AzureBlobService => _azureBlobService ??= serviceProvider.GetRequiredService<AzurePublicBlobService>();

    private ISimpleHasher? _simpleHasher;
    private ISimpleHasher SimpleHasher => _simpleHasher ??= serviceProvider.GetRequiredService<ISimpleHasher>();

    private IHubContext<GenerationHub>? _generationHub;
    private IHubContext<GenerationHub> GenerationHub => _generationHub ??= serviceProvider.GetRequiredService<IHubContext<GenerationHub>>();

    private ITokenProvider? _tokenProvider;
    private ITokenProvider TokenProvider => _tokenProvider ??= serviceProvider.GetRequiredService<ITokenProvider>();

    private TagDynamicPropertiesWorker? _dynamicPropertiesWorker;
    private TagDynamicPropertiesWorker DynamicPropertiesWorker => _dynamicPropertiesWorker ??= new TagDynamicPropertiesWorker(serviceProvider, this);

    private DevicesService? _deviceService;
    private DevicesService DeviceService => _deviceService ??= serviceProvider.GetRequiredService<DevicesService>();

    [HttpGet]
    public async Task<IActionResult> ItemIndex()
    {
        var vbRet = await AddItemIndexViewBagData();
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
        var resp = await itemsService.GetByDataTablesAsync(request);
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
            return View(nameof(ItemIndex));
        }
        await AddDeviceGridViewBagData();

        return this.EndOk(viewName: nameof(Upsert), dto: new InnerItemUpsertDto { Status = EItemStatus.New });
    }
    public async Task<IActionResult> Upsert(InnerItemUpsertDto dto, string mode)
    {
        dto.Tags = MappingProfile.DeserializeTagsDto(dto.TagsJson)?.Select(t => t.Name)?.ToList() ?? [];
        await DynamicPropertiesWorker.TryAddConfigsDynamicProperties(dto.Tags);
        if (!ModelState.IsValid)
            return View(dto);
        ResponseDto<ItemUpsertDto?>? resp;
        IsForCreate = dto.Id == 0;
        if (IsForCreate)
            resp = await itemsService.CreateAsync(dto);
        else
            resp = await itemsService.UpdateAsync(dto);
        if (!resp.IsOk())
        {
            await AddViewBagData(dto);
            var message = (IsForCreate) ? "could not create Item: " : "could not update Item: ";
            message += resp?.Message;
            return this.EndErr(dto: dto, message: message);//, resp: resp);
        }

        await Task.WhenAll(AddItemIndexViewBagData(), AddViewBagData(dto));
        TempData["success"] = (IsForCreate) ? "Item created successfully" : "Item updated successfully";

        if (mode != "finish")
            return RedirectToAction(nameof(ItemIndex));

        if (dto.Tags.Count != 1)
        {
            TempData["error"] = "there must be one tag only";
            return View(dto);
        }

        var configs = await ConfigsService.GetByTagAsync(dto.Tags[0]);
        if (configs?.Result?.Count != 1)
        {
            TempData["error"] = "there must be one appliable config only";
            return View(dto);
        }

        var documents = await CDocumentsService.GetByConfigId(configs.Result[0].Id);
        if (documents?.Result is null)
        {
            TempData["error"] = "there must be one appliable config";
            return View(dto);
        }

        if (!dto.DeviceId.HasValue)
        {
            TempData["error"] = "the Device must be present";
            return View(dto);
        }

        // go to details view
        var devT = DeviceService.GetDeviceByIdAsync(dto.DeviceId.Value);
        var ngdT = AddNonGeneratedDocuments(dto, documents.Result.Select(d => d.Id));
        var dgvbT = AddDeviceGridViewBagData();

        var vbRet = await AddViewBagData(dto);
        if (vbRet is not null)
        {
            // await AddItemIndexViewBagData(); // it is already here
            TempData["error"] = vbRet;
            return RedirectToAction(nameof(ItemIndex));
        }

        await dgvbT;
        await devT;
        await ngdT;
        dto.Device = devT.Result?.Result;
        return this.EndOk(viewName: "ViewDetails", dto: dto);
    }

    [HttpGet]
    public async Task<IActionResult> Update(long id)
    {
        var resp = await itemsService.GetItemByIdAsync(id);
        if (!resp.IsOk())
        {
            await AddItemIndexViewBagData();
            TempData["error"] = resp?.Message;
            return RedirectToAction(nameof(ItemIndex));
        }
        var dto = resp!.Result!;
        var vbRet = await AddViewBagData(dto);
        if (vbRet is not null)
        {
            await AddItemIndexViewBagData();
            TempData["error"] = vbRet;
            return RedirectToAction(nameof(ItemIndex));
        }
        await DynamicPropertiesWorker.TryAddConfigsDynamicProperties(dto.Tags);
        await AddDeviceGridViewBagData();
        var innerDto = dto.ToInnerItemUpsertDto();
        await DynamicPropertiesWorker.TrySetTagsJson(innerDto);

        return this.EndOk(viewName: nameof(Upsert), dto: innerDto);
    }

    [HttpGet]
    public async Task<IActionResult> View(long id)
    {
        var resp = await itemsService.GetItemByIdAsync(id);
        if (!resp.IsOk())
        {
            await AddItemIndexViewBagData();
            TempData["error"] = resp?.Message;
            return RedirectToAction(nameof(ItemIndex));
        }
        var dto = resp!.Result!;

        var ngdT = AddNonGeneratedDocuments(dto);
        var acdpT = DynamicPropertiesWorker.TryAddConfigsDynamicProperties(dto.Tags);

        var vbRet = await AddViewBagData(dto);
        if (vbRet is not null)
        {
            await AddItemIndexViewBagData();
            TempData["error"] = vbRet;
            return RedirectToAction(nameof(ItemIndex));
        }
        await AddDeviceGridViewBagData();
        await ngdT;
        await acdpT;
        return this.EndOk(viewName: "ViewDetails", dto: dto.ToInnerItemUpsertDto());
    }

    [HttpGet]
    public async Task<IActionResult> MarkAsNew([FromQuery] long id, [FromQuery] string? status)
    {
        var resp = await itemsService.MarkAsNew(id);
        await AddItemIndexViewBagData();
        if (!resp.IsOk())
            TempData["error"] = resp?.Message;
        else
            TempData["success"] = "reset of state successful";
        return RedirectToAction(nameof(ItemIndex), new { status = status ?? "New" });
    }

    [HttpGet]
    public async Task<IActionResult> ForwardState([FromQuery] long id, [FromQuery] string? status)
    {
        await ForwardState(id);
        return RedirectToAction(nameof(ItemIndex), new { status = status ?? "New" });
    }

    [HttpGet]
    public async Task<IActionResult> FinishState([FromQuery] long id)
    {
        var resp = await itemsService.GetItemByIdAsync(id);
        if (!resp.IsOk())
        {
            await AddItemIndexViewBagData();
            TempData["error"] = resp?.Message;
            return RedirectToAction(nameof(ItemIndex));
        }
        var dto = resp!.Result!;
        var vbRet = await AddViewBagData(dto);
        if (vbRet is not null)
        {
            await AddItemIndexViewBagData();
            TempData["error"] = vbRet;
            return RedirectToAction(nameof(ItemIndex));
        }

        await DynamicPropertiesWorker.TryAddConfigsDynamicProperties(dto.Tags);
        await AddDeviceGridViewBagData();
        var innerDto = dto.ToInnerItemUpsertDto();
        await DynamicPropertiesWorker.TrySetTagsJson(innerDto);

        if (dto.Tags.Count != 1)
        {
            TempData["error"] = "there must be one tag only";
            return RedirectToAction(nameof(ItemIndex));
        }

        var configs = await ConfigsService.GetByTagAsync(dto.Tags[0]);
        if (configs?.Result?.Count != 1)
        {
            TempData["error"] = "there must be one appliable config only";
            return RedirectToAction(nameof(ItemIndex));
        }

        var documents = await CDocumentsService.GetByConfigId(configs.Result[0].Id);
        if (documents?.Result is null)
        {
            TempData["error"] = "there must be one appliable config";
            return RedirectToAction(nameof(ItemIndex));
        }

        if (!dto.DeviceId.HasValue)
        {
            TempData["error"] = "the Device must be present";
            return RedirectToAction(nameof(ItemIndex));
        }

        // go to details view
        var devT = DeviceService.GetDeviceByIdAsync(dto.DeviceId.Value);
        var ngdT = AddNonGeneratedDocuments(dto, documents.Result.Select(d => d.Id));
        var dgvbT = AddDeviceGridViewBagData();

        await dgvbT;
        await devT;
        await ngdT;
        dto.Device = devT.Result?.Result;
        return this.EndOk(viewName: "ViewDetails", dto: dto.ToInnerItemUpsertDto());
    }

    [HttpGet]
    public async Task<IActionResult> ReadonlyState([FromQuery] long id, [FromQuery] string? status, [FromQuery] string? variant)
    {
        if (variant?.ToLowerInvariant().StartsWith("a", StringComparison.InvariantCultureIgnoreCase) == true)
        { // leave device as is
            await ReadonlyState(id, true);
        }
        else
        { // remove device
            await ReadonlyState(id, false);
        }
        return RedirectToAction(nameof(ItemIndex), new { status = status ?? "New" });
    }

    [HttpGet]
    public async Task<IActionResult> ForwardStateAndView([FromQuery] long id, long deviceId)
    {
        var apDevResp = await itemsService.AppendDevice(id, deviceId);
        await AddItemIndexViewBagData();
        if (!apDevResp.IsOk())
        {
            TempData["error"] = apDevResp?.Message ?? "failed to append device";
            return RedirectToAction(nameof(View), new { id });
        }

        var forwStResp = await itemsService.ForwardState(id);
        await AddItemIndexViewBagData();
        if (!forwStResp.IsOk())
        {
            TempData["error"] = forwStResp?.Message ?? "failed to forward state";
            return RedirectToAction(nameof(View), new { id });
        }
        await Task.Delay(10000); // leave some time to generate the documents
        return RedirectToAction(nameof(View), new { id });
    }

    public async Task<IActionResult> AppendDevice(long id, long deviceId)
    {
        var resp = await itemsService.AppendDevice(id, deviceId);
        await AddItemIndexViewBagData();
        if (!resp.IsOk())
            return BadRequest(resp?.Message);

        return Ok(true);
    }

    [HttpGet]
    public async Task<IActionResult> GenerateDocument([FromQuery] long itemId, [FromQuery] long documentId)
    {
        var token = TokenProvider.GetToken();
        if (token is null)
            return BadRequest("could not start document generation: could not get token");
        var operationIdResp = await itemsService.GenerateDocumentStart(itemId, documentId);

        if (operationIdResp?.IsSuccess != true || string.IsNullOrEmpty(operationIdResp.Result))
            return BadRequest("could not start document generation");

        var operationId = operationIdResp.Result;
        var generationHub = GenerationHub;
        var simpleHasher = SimpleHasher;
        var end = DateTime.UtcNow.AddMinutes(5);
        _ = Task.Run(async () =>
        {
            try
            {
                while (end > DateTime.UtcNow)
                {
                    var genDocProgressRes = await itemsService.GenerateDocumentProgress(operationId, token);
                    if (genDocProgressRes?.IsSuccess != true || genDocProgressRes.Message is null)
                    {
                        try { await generationHub.Clients.Group(operationId).SendAsync("err", "Document generation progress info get failed"); } catch { }
                        break;
                    }
                    if (genDocProgressRes.Message is not null)
                        await generationHub.Clients.Group(operationId).SendAsync("progress", genDocProgressRes.Message);
                    if (genDocProgressRes.Result is not null)
                    {
                        if (genDocProgressRes.Result.OutPath is not null) genDocProgressRes.Result.OutPath = simpleHasher.EncodePath(genDocProgressRes.Result.OutPath);
                        await generationHub.Clients.Group(operationId).SendAsync("ok", genDocProgressRes.Result);
                        break;
                    }

                    await Task.Delay(2000);
                }
            }
            catch
            {
                try { await generationHub.Clients.Group(operationId).SendAsync("err", "Generation failed"); } catch { }
            }
        });

        return Ok(new { operationId });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(long id)
    {
        var resp = await itemsService.DeleteItemAsync(id);
        await AddItemIndexViewBagData();
        if (!resp.IsOk())
            TempData["error"] = resp?.Message;
        else
            TempData["success"] = "Item deleted";
        return RedirectToAction(nameof(ItemIndex));
    }

    [HttpGet]
    public async Task<IActionResult> Undelete([FromQuery] long id, [FromQuery] string? status)
    {
        var resp = await itemsService.UndeleteItemAsync(id);
        await AddItemIndexViewBagData();
        if (!resp.IsOk())
            TempData["error"] = resp?.Message;
        else
            TempData["success"] = "Item undeleted";
        return RedirectToAction(nameof(ItemIndex), new { status = status ?? "New" });
    }

    [HttpGet]
    public Task<List<DynamicPropertyDto>?> GetDynamicProps4Config(long id)
        => DynamicPropertiesWorker.GetDynamicProps4Config(id);

    #region private
    private async Task ForwardState(long id)
    {
        var resp = await itemsService.ForwardState(id);
        await AddItemIndexViewBagData();
        if (!resp.IsOk())
            TempData["error"] = resp?.Message;
        else
            TempData["success"] = "state forwarded";
    }
    private async Task ReadonlyState(long id, bool leaveDevice)
    {
        var resp = await itemsService.ReadonlyState(id, leaveDevice);
        await AddItemIndexViewBagData();
        if (!resp.IsOk())
            TempData["error"] = resp?.Message;
        else
            TempData["success"] = "marked as readonly";
    }
    private async Task<string?> AddItemIndexViewBagData()
    {
        var pResp = await OrganizationService.GetItemGridColumn();
        if (!pResp.IsOk()) return pResp?.Message ?? "";
        ViewBag.ItemGridColumns = pResp!.Result!;
        return null;
    }
    private async Task<string?> AddViewBagData(ItemUpsertDto? dto)
    {
        var pResp = await OrganizationService.GetNewItemProps();
        if (!pResp.IsOk()) return pResp?.Message ?? "";

        ViewBag.NewItemProperties = pResp!.Result!;
        ViewBag.Tags = dto?.Tags;
        ViewBag.ItemValues = SJsonSerializer.Deserialize<Dictionary<string, string>>(dto?.ItemDataJson ?? "{}");
        ViewBag.IsDisabled = (dto?.Status > EItemStatus.New) == true;

        var users = new Users(authService);
        ViewBag.UserIdCreated = await users.GetUserName(dto?.UserIdCreated);
        ViewBag.UserIdExported = await users.GetUserName(dto?.UserIdExported);
        ViewBag.UserIdImported = await users.GetUserName(dto?.UserIdImported);
        ViewBag.UserIdFinalized = await users.GetUserName(dto?.UserIdFinalized);
        ViewBag.UserIdDeleted = await users.GetUserName(dto?.UserIdDeleted);
        ViewBag.UserIdUndeleted = await users.GetUserName(dto?.UserIdUndeleted);
        ViewBag.UserIdReadonlied = await users.GetUserName(dto?.UserIdReadonlied);
        ViewBag.UsedConfigId = dto?.UsedConfigId;

        ViewBag.CanEditDevice = dto?.Status == EItemStatus.Imported && dto?.DeviceId == 0;

        return null;
    }
    private async Task<string?> AddDeviceGridViewBagData()
    {
        var pResp = await OrganizationService.GetDeviceGridColumn();
        if (!pResp.IsOk()) return pResp?.Message ?? "";
        ViewBag.DeviceGridColumns = pResp!.Result!;
        return null;
    }
    private async Task AddNonGeneratedDocuments(ItemUpsertDto dto, IEnumerable<long> documentsId)
    {
        foreach (var dId in documentsId)
        {
            var generatedDocument = dto.GeneratedDocuments.FirstOrDefault(gd => gd.DocumentId == dId);
            if (generatedDocument is not null) // the document is here already
            {
                generatedDocument.FilePath = SimpleHasher.EncodePath(generatedDocument.FilePath);
                continue;
            }

            // the document is not generated
            var documentsResp = await CDocumentsService.GetById(dId);
            if (documentsResp?.Result is null) continue;

            var documentPlaceholder = new GeneratedDocumentDto
            {
                ItemId = dto.Id,
                DocumentId = dId,
                Title = documentsResp.Result.Label,
            };
            dto.GeneratedDocuments.Add(documentPlaceholder);
        }
    }
    private async Task AddNonGeneratedDocuments(ItemUpsertDto dto)
    {
        var thereWasError = false;
        foreach (var dId in dto.Documents2Print)
        {
            var generatedDocument = dto.GeneratedDocuments.FirstOrDefault(gd => gd.DocumentId == dId);
            if (generatedDocument is not null) // the document is here already
            {
                generatedDocument.FilePath = SimpleHasher.EncodePath(generatedDocument.FilePath);
                continue;
            }

            // the document is not generated
            var documentsResp = await CDocumentsService.GetById(dId);
            if (documentsResp?.Result is null) continue;

            var documentPlaceholder = new GeneratedDocumentDto
            {
                ItemId = dto.Id,
                DocumentId = dId,
                Title = documentsResp.Result.Label,
            };
            dto.GeneratedDocuments.Add(documentPlaceholder);
            thereWasError = true;
        }

        if (thereWasError) await AddPdfGenerationDebugFiles(dto);
    }
    private async Task AddPdfGenerationDebugFiles(ItemUpsertDto dto)
    {
        var debugFileNames = await AzureBlobService.GetFileNamesAndUris(NameGenerator.GetPdfDebugPath(dto.OrganizationId));
        ViewBag.DebugFiles = debugFileNames.Select(f => new DebugFile(Path.GetFileName(f.name), f.uri)).ToList();
    }

    #endregion
    private class Users(AuthService authService)
    {
        private readonly Dictionary<string, string> users = [];
        public async Task<string?> GetUserName(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (users.TryGetValue(id, out string? value)) return value;
            var userName = (await authService.GetUserName(id))?.Result;
            users[id] = userName ?? "";
            return userName;
        }
    }
}
