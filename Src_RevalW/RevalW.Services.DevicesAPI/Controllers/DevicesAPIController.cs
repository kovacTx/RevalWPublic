using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.DevicesAPI.Data;
using RevalW.Services.DevicesAPI.Services;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.Services.DevicesAPI.Controllers;

[Route("api/devices")]
[ApiController]
public class DevicesAPIController(AppDbContext db, IServiceProvider serviceProvider) : ControllerBase
{
    private DevicesService? _devicesService;
    private DevicesService DevicesService => _devicesService ??= serviceProvider.GetRequiredService<DevicesService>();

    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] DeviceUpsertDto dto, [FromQuery] string? hash)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var isAuthorized = this.IsAuthorized(RightsList.Device_Create);
        var userId = this.CurrentUserId();
        var (status, response) = await DevicesService.Create(currentOrgId, isUber, isAuthorized, userId, dto, hash);
        return StatusCode(status, response);
    }

    /// <summary>
    /// i dont want it to be authorized because devices from orgId=1 are public
    /// </summary>
    [HttpGet("byId")]
    public async Task<IActionResult> GetById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DevicesService.GetById(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("headerById")]
    public async Task<IActionResult> GetHeaderById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DevicesService.GetHeaderById(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("byTag")]
    [Authorize]
    public IActionResult GetByTag([FromQuery] string tag)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = DevicesService.GetByTag(currentOrgId, isUber, tag);
        return StatusCode(status, response);
    }

    [HttpPost("byDataTables")]
    public async Task<IActionResult> GetByDataTables([FromBody] DTRequest request, [FromQuery] bool? deleted)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var ret = await DevicesService.GetByDataTables(currentOrgId, isUber, request, deleted);
        return Ok(ret);
    }

    [HttpGet("configDataNTags")]
    public async Task<IActionResult> GetConfigData([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await DevicesService.GetConfigDataNTags(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] DeviceUpsertDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var isAuthorized = this.IsAuthorized(RightsList.Device_Edit);
        var userId = this.CurrentUserId();
        var (status, response) = await DevicesService.Update(currentOrgId, isUber, isAuthorized, userId, dto);
        return StatusCode(status, response);
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var isAuthorized = this.IsAuthorized(RightsList.Device_Erase);
        var (status, response) = await DevicesService.Delete(currentOrgId, isUber, isAuthorized, id);
        return StatusCode(status, response);
    }

}
