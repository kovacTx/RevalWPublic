using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.OrganizationAPI.Services;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.Services.OrganizationAPI.Controllers
{
    [Route("api/organization")]
    [ApiController]
    public class OrganizationAPIController(IServiceProvider serviceProvider) : ControllerBase
    {
        private OrganizationService? _organizationService;
        private OrganizationService OrganizationService => _organizationService ??= serviceProvider.GetRequiredService<OrganizationService>();

        [HttpGet("all")]
        [Authorize(Roles = ERoles.UberAdmin)]
        public IActionResult All()
        {
            var users = OrganizationService.GetAllOrganizations();
            return Ok(ResponseDto<IEnumerable<OrganizationDto>>.Ok(users));
        }

        [HttpGet("byId")]
        [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
        public async Task<IActionResult> GetById([FromQuery] long id)
        {
            var currentOrgId = this.CurrentOrgId();
            var isCreator = this.IsCreator();
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = await OrganizationService.GetOrganizationById(currentOrgId, isCreator, isUberAdmin, id);
            return StatusCode(status, response);
        }

        [HttpPost("byDataTables")]
        public async Task<IActionResult> GetByDataTables([FromBody] DTRequest request, [FromQuery] bool? deleted)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();
            if (!isUber) return BadRequest("cannot access this endpoint");
            var ret = await OrganizationService.GetByDataTables(currentOrgId, isUber, request, deleted);
            return Ok(ret);
        }

        [HttpGet("newItemProperties")]
        public IActionResult GetNewItemProps([FromQuery] long? id = null)
        {
            var currentOrgId = this.CurrentOrgId();
            id ??= currentOrgId;
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = OrganizationService.GetNewItemPropsById(currentOrgId, isUberAdmin, id.Value);
            return StatusCode(status, response);
        }

        [HttpGet("newUserProperties")]
        [Authorize]
        public IActionResult GetNewUserProps([FromQuery] long? id = null)
        {
            var currentOrgId = this.CurrentOrgId();
            id ??= currentOrgId;
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = OrganizationService.GetNewUserPropsById(currentOrgId, isUberAdmin, id.Value);
            return StatusCode(status, response);
        }

        [HttpGet("newDeviceProperties")]
        [Authorize]
        public IActionResult GetDeviceUserProps([FromQuery] long? id = null)
        {
            var currentOrgId = this.CurrentOrgId();
            id ??= currentOrgId;
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = OrganizationService.GetNewDevicePropsById(currentOrgId, isUberAdmin, id.Value);
            return StatusCode(status, response);
        }

        [HttpGet("itemGridColumns")]
        public IActionResult GetItemGridColsById([FromQuery] long? id = null)
        {
            var currentOrgId = this.CurrentOrgId();
            id ??= currentOrgId;
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = OrganizationService.GetItemGridColsById(currentOrgId, isUberAdmin, id.Value);
            return StatusCode(status, response);
        }

        [HttpGet("deviceGridColumns")]
        public IActionResult GetDeviceGridColsById([FromQuery] long? id = null)
        {
            var currentOrgId = this.CurrentOrgId();
            id ??= currentOrgId;
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = OrganizationService.GetDeviceGridColsById(currentOrgId, isUberAdmin, id.Value);
            return StatusCode(status, response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] OrganizationDto dto, [FromQuery] string? hash)
        {
            var isUber = this.IsUberAdmin();
            var (status, response) = await OrganizationService.Create(dto, hash, isUber);
            return StatusCode(status, response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] long id, [FromQuery] string? hash)
        {
            var isUber = this.IsUberAdmin();
            var (status, response) = await OrganizationService.ReallyDelete(id, hash, isUber);
            return StatusCode(status, response);
        }

        [HttpPost("doesExist")]
        public IActionResult DoesExist([FromBody] OrganizationDto dto)
        {
            return Ok(OrganizationService.DoesExist(dto));
        }

        [HttpPut("update")]
        [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
        public async Task<IActionResult> Update([FromBody] OrganizationDto dto)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUberAdmin = this.IsUberAdmin();
            var (status, response) = await OrganizationService.UpdateOrganization(currentOrgId, isUberAdmin, dto);
            return StatusCode(status, response);
        }

    }
}
