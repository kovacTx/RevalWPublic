using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.Shared.Utils;
using RevalW.Services.TemplatesAPI.Services;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.Services.TemplatesAPI.Controllers;

[Route("api/configs")]
[ApiController]
public class ConfigsAPIController(IServiceProvider serviceProvider) : ControllerBase
{
    private ConfigsService? _configsService;
    private ConfigsService ConfigsService => _configsService ??= serviceProvider.GetRequiredService<ConfigsService>();

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ConfigUpsertDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.Create(currentOrgId, isUber, User, dto, HttpContext);
        return StatusCode(status, response);
    }

    [HttpGet("headers")]
    public IActionResult GetHeaders([FromQuery] int? pageNum, [FromQuery] int? pageSize, [FromQuery] bool? deleted)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = ConfigsService.GetHeaders(currentOrgId, isUber, pageNum, pageSize, deleted);
        return StatusCode(status, response);
    }

    [HttpGet("byId")]
    public async Task<IActionResult> GetById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.GetById(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("headerById")]
    public async Task<IActionResult> GetHeaderById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var (status, response) = await ConfigsService.GetHeaderById(currentOrgId, id);
        return StatusCode(status, response);
    }

    [HttpPost("byDataTables")]
    public async Task<IActionResult> GetByDataTables([FromBody] DTRequest request, [FromQuery] bool? deleted)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var ret = await ConfigsService.GetByDataTables(currentOrgId, isUber, request, deleted);
        return Ok(ret);
    }

    [HttpGet("byTag")]
    public async Task<IActionResult> GetByTag([FromQuery] string tag)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.GetByTag(currentOrgId, isUber, tag);
        return StatusCode(status, response);
    }

    [HttpGet("byTags")]
    public async Task<IActionResult> GetByTags([FromQuery] List<string> tags)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.GetByTags(currentOrgId, isUber, tags);
        return StatusCode(status, response);
    }

    [HttpGet("byOrganizationId")]
    [Authorize(Roles = ERoles.UberAdmin)]
    public IActionResult GetByOrganizationId([FromQuery] int id)
    {
        var isUber = this.IsUberAdmin();
        var (status, response) = ConfigsService.GetByOrganizationId(isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("dynaminProperties4Config")]
    public async Task<IActionResult> GetNewItemProps([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await ConfigsService.DynamicProperties4Config(currentOrgId, isUberAdmin, id);
        return StatusCode(status, response);
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] ConfigUpsertDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.Update(currentOrgId, isUber, User, dto);
        return StatusCode(status, response);
    }

    [HttpPut("autosave")]
    [Authorize]
    public async Task<IActionResult> Autosave([FromBody] MonacoAutosaveRequest dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.Autosave(currentOrgId, isUber, User, dto);
        return StatusCode(status, response);
    }

    [HttpPut("updateImage")]
    [Authorize]
    public async Task<IActionResult> UpdateImage([FromForm] UpdateImageDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.UpdateImage(currentOrgId, isUber, User, dto);
        return StatusCode(status, response);
    }

    [HttpPut("updateDll")]
    [Authorize(Roles = ERoles.UberAdmin)]
    public async Task<IActionResult> UpdateDll([FromForm] UpdateDllDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.UpdateDll(currentOrgId, isUber, User, dto);
        if (status != StatusCodes.Status200OK) return StatusCode(status, response);
        return StatusCode(status, response);
    }

    [HttpGet("downloadDll")]
    public async Task<IActionResult> DownloadDll([FromQuery] long id, [FromQuery] string platform)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.DownloadDllForConfigId(currentOrgId, isUber, id, platform);
        if (status != StatusCodes.Status200OK) return StatusCode(status, response);

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{response.Result.fileName}\"");

        return File(response.Result.stream, "application/octet-stream");
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await ConfigsService.MarkDeleted(currentOrgId, isUber, User, id);
        return StatusCode(status, response);
    }
}
