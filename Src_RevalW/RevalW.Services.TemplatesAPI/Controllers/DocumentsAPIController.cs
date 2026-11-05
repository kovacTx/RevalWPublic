using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.Shared.Utils;
using RevalW.Services.TemplatesAPI.Services;
using RevalW.Shared.Model;

namespace RevalW.Services.TemplatesAPI.Controllers;

[Route("api/cdocuments")]
[ApiController]
public class DocumentsAPIController(IServiceProvider serviceProvider) : ControllerBase
{
    private DocumentsService? _documentsService;
    private DocumentsService DocumentsService => _documentsService ??= serviceProvider.GetRequiredService<DocumentsService>();

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] DocumentTemplateDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.Create(currentOrgId, isUber, dto);
        return StatusCode(status, response);
    }

    [HttpGet("byId")]
    [Authorize]
    public async Task<IActionResult> GetById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.GetById(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("byConfigId")]
    [Authorize]
    public IActionResult GetByConfigId([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = DocumentsService.GetByConfigId(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("headersByConfigId")]
    [Authorize]
    public IActionResult GetHeadersByConfigId([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = DocumentsService.GetHeadersByConfigId(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] DocumentTemplateDto document)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.Update(currentOrgId, isUber, document);
        return StatusCode(status, response);
    }

    [HttpPut("autosave")]
    [Authorize]
    public async Task<IActionResult> Autosave([FromBody] MonacoAutosaveRequest dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.Autosave(currentOrgId, isUber, dto);
        return StatusCode(status, response);
    }

    [HttpGet("resourcesByConfigId")]
    [Authorize]
    public IActionResult GetResourcesByConfigIdAsync([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = DocumentsService.GetResourcesByConfigId(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpPut("uploadResource")]
    [Authorize]
    public async Task<IActionResult> UploadResourceAsync([FromForm] UploadDocumentsResourceFileDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.UploadResourceFile(currentOrgId, isUber, dto);
        return StatusCode(status, response);
    }

    [HttpDelete("removeResource")]
    [Authorize]
    public async Task<IActionResult> RemoveResourceAsync([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.RemoveResourceFile(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.MarkDeleted(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpDelete("deleteByConfigId")]
    [Authorize]
    public async Task<IActionResult> DeleteByConfigId([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.MarkDeletedByConfigId(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpDelete("reallyDelete")]
    [Authorize]
    public async Task<IActionResult> ReallyDelete([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.ReallyDelete(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpDelete("undelete")]
    [Authorize]
    public async Task<IActionResult> Undelete([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DocumentsService.MarkAsUndeleted(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

}
