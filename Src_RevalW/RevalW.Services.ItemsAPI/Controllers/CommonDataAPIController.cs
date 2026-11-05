using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.ItemsAPI.Services;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;

namespace RevalW.Services.ItemsAPI.Controllers;

[Route("api/commonData")]
[ApiController]
public class CommonDataAPIController(CommonDataService commonDataService) : ControllerBase
{
    [HttpPut("upsertCombo")]
    [Authorize]
    public async Task<IActionResult> UpsertCombo([FromBody] ComboSourceDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var (status, response) = await commonDataService.UpsertCombo(currentOrgId, dto);
        return StatusCode(status, response);
    }

    [HttpPut("upsertCommonDataWrapper")]
    [Authorize]
    public async Task<IActionResult> UpsertCommonDataWrapper([FromBody] CommonDataWrapperDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var (status, response) = await commonDataService.UpsertCommonDataWrapper(currentOrgId, dto);
        return StatusCode(status, response);
    }

    [HttpGet("combosByConfigId")]
    public IActionResult GetComboByConfigId([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = commonDataService.GetCombosByConfigId(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpGet("commonDataWrappersByConfigId")]
    public IActionResult GetCommbnDataWrappersByConfigId([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = commonDataService.GetComonDataWrappersByConfigId(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpDelete("deleteComboById")]
    [Authorize]
    public async Task<IActionResult> DeleteComboById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await commonDataService.DeleteCombo(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

    [HttpDelete("deleteCommonDataById")]
    [Authorize]
    public async Task<IActionResult> DeleteCommonDataById([FromQuery] long id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var (status, response) = await commonDataService.DeleteCommonData(currentOrgId, isUber, id);
        return StatusCode(status, response);
    }

}
