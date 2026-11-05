using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.ItemsAPI.Data;
using RevalW.Services.ItemsAPI.Services;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.Services.ItemsAPI.Controllers;

[Route("api/items")]
[ApiController]
public class ItemsAPIController(AppDbContext db, IServiceProvider serviceProvider) : ControllerBase
{
    private ItemsService? _itemsService;
    private ItemsService ItemsService => _itemsService ??= serviceProvider.GetRequiredService<ItemsService>();

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ItemUpsertDto dto, [FromQuery] string? hash)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var isAuthorized = this.IsAuthorized(RightsList.Item_Create);
        var userId = this.CurrentUserId();
        var (status, response) = await ItemsService.Create(currentOrgId, isUber, isAuthorized, userId, dto, hash);
        return StatusCode(status, response);
    }

    /// <summary>
    /// i dont want it to be authorized because items from orgId=1 are public
    /// </summary>
    [HttpPost("headersForApp")]
    public async Task<IActionResult> GetHeadersForApp([FromQuery] int? pNum, [FromQuery] int? pSize, [FromQuery] int? sFilter, [FromBody] List<InAppItemDto> inAppItems)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.GetHeadersForApp(currentOrgId, isUber, User, pNum, pSize, (EItemStatus?)sFilter ?? EItemStatus.New, inAppItems);
        return StatusCode(status, response);
    }

    /// <summary>
    /// i dont want it to be authorized because items from orgId=1 are public
    /// </summary>
    [HttpGet("byId")]
    public async Task<IActionResult> GetById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.GetById(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("headerById")]
    [Authorize]
    public async Task<IActionResult> GetHeaderById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.GetHeaderById(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("byTag")]
    public IActionResult GetByTag([FromQuery] string tag)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = ItemsService.GetByTag(currentOrgId, isUber, tag);
        return StatusCode(status, response);
    }

    [HttpPost("byDataTables")]
    public async Task<IActionResult> GetByDataTables([FromBody] DTRequest request)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var ret = await ItemsService.GetByDataTables(currentOrgId, isUber, request);
        return Ok(ret);
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] ItemUpsertDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.Update(currentOrgId, isUber, User, dto);
        return StatusCode(status, response);
    }

    [HttpPut("deviceChanged")]
    [Authorize]
    public async Task<IActionResult> DeviceChanged([FromBody] DeviceUpsertDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.DeviceChanged(currentOrgId, isUber, User, dto);
        return StatusCode(status, response);
    }

    [HttpPut("markExported")]
    [Authorize]
    public async Task<IActionResult> MarkExported([FromQuery] long id, [FromQuery] string? userId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber) userId = this.CurrentUserId();
        var (status, response) = await ItemsService.MarkExported(currentOrgId, isUber, id, userId);
        return StatusCode(status, response);
    }

    [HttpPut("generateDocument")]
    [Authorize]
    public async Task<IActionResult> GenerateDocument([FromQuery] long itemId, [FromQuery] long documentId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.GenerateDoc(currentOrgId, isUber, itemId, documentId);
        return StatusCode(status, response);
    }

    [HttpPut("generateDocument-start")]
    [Authorize]
    public async Task<IActionResult> GenerateDocumentStart([FromQuery] long itemId, [FromQuery] long documentId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.GenerateDocStart(currentOrgId, isUber, itemId, documentId);
        return StatusCode(status, response);
    }

    [HttpGet("generateDocument-progress")]
    [Authorize]
    public async Task<IActionResult> GenerateDocumentProgress([FromQuery] string operationId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.GenerateDocProgress(operationId);
        return StatusCode(status, response);
    }

    [HttpPut("appendDevice")]
    [Authorize]
    public async Task<IActionResult> AppendDevice([FromQuery] long id, [FromQuery] long deviceId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.AppendDevice(currentOrgId, isUber, User, id, deviceId);
        return StatusCode(status, response);
    }

    [HttpPut("tryMarkFinished")]
    public async Task<IActionResult> TryMarkFinished([FromQuery] long id, [FromQuery] long currentOrgId, [FromQuery] string userId, [FromQuery] string hash1, [FromQuery] string hash2)
    {
        var (status, response) = await ItemsService.TryMarkFinished(currentOrgId, false, id, userId, hash1, hash2);
        return StatusCode(status, response);
    }

    [HttpPut("free")]
    [Authorize]
    public async Task<IActionResult> Free([FromQuery] long id, [FromQuery] string? userId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber) userId = this.CurrentUserId();
        var (status, response) = await ItemsService.Free(currentOrgId, isUber, id, userId);
        return StatusCode(status, response);
    }

    [HttpPut("markAsNew")]
    [Authorize]
    public async Task<IActionResult> MarkAsNew([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.MarkAsNew(currentOrgId, isUber, User, id);
        return StatusCode(status, response);
    }

    [HttpPut("forwardState")]
    [Authorize]
    public async Task<IActionResult> ForwardState([FromQuery] long id, [FromQuery] string? userId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber) userId = this.CurrentUserId();
        var (status, response) = await ItemsService.ForwardState(currentOrgId, isUber, id, userId);
        return StatusCode(status, response);
    }

    [HttpPut("readonlyState")]
    [Authorize]
    public async Task<IActionResult> ReadonlyState([FromQuery] long id, [FromQuery] bool leaveDevice)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.ReadonlyState(currentOrgId, isUber, User, id, leaveDevice);
        return StatusCode(status, response);
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromQuery] long id, [FromQuery] string? userId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber) userId = this.CurrentUserId();
        var (status, response) = await ItemsService.MarkDeleted(currentOrgId, isUber, id, userId);
        return StatusCode(status, response);
    }

    [HttpDelete("undelete")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> Undelete([FromQuery] long id, [FromQuery] string? userId)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber) userId = this.CurrentUserId();
        var (status, response) = await ItemsService.MarkAsUndeleted(currentOrgId, isUber, id, userId);
        return StatusCode(status, response);
    }

    //dont want to be authorized, because the messagebus service is calling this
    [HttpPut("documentGenerated")]
    public async Task<IActionResult> DocumentGenerated(GeneratedDocumentDto dto)
    {
        var (status, response) = await ItemsService.DocumentGenerated(dto);
        return StatusCode(status, response);
    }

    [HttpPut("removeDocument")]
    [Authorize]
    public async Task<IActionResult> RemoveDocument([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ItemsService.RemoveDocument(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    //dont want to be authorized, because this should be public
    [HttpGet("downloadDocument")]
    public async Task<IActionResult> DownloadDocument(string path)
    {
        var response = await ItemsService.DownloadDocument(path);
        if (!response.IsOk() || response.Result is null) return StatusCode(StatusCodes.Status500InternalServerError, response);

        return File(response.Result, "application/octet-stream", enableRangeProcessing: true);
    }

}
