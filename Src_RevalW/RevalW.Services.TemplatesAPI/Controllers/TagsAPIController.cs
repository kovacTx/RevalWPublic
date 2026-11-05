using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.Shared.Utils;
using RevalW.Services.TemplatesAPI.Services;
using RevalW.Shared.Model;

namespace RevalW.Services.TemplatesAPI.Controllers
{
    [Route("api/ctags")]
    [ApiController]
    public class TagsAPIController(IServiceProvider serviceProvider) : ControllerBase
    {
        private TagsService? _tagsService;
        private TagsService TagsService => _tagsService ??= serviceProvider.GetRequiredService<TagsService>();

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ConfigTagDto dto)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();

            var (status, response) = await TagsService.Create(currentOrgId, isUber, dto);
            return StatusCode(status, response);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] bool? deleted)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();

            var (status, response) = await TagsService.GetAll(currentOrgId, isUber, deleted);
            return StatusCode(status, response);
        }

        [HttpGet("byId")]
        public async Task<IActionResult> GetById([FromQuery] int id)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();

            var (status, response) = await TagsService.GetById(currentOrgId, isUber, id);
            return StatusCode(status, response);
        }

        [HttpGet("byName")]
        [Authorize]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();

            var (status, response) = await TagsService.GetByName(currentOrgId, isUber, name);
            return StatusCode(status, response);
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> Update([FromBody] ConfigTagDto dto)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();

            var (status, response) = await TagsService.Update(currentOrgId, isUber, dto);
            return StatusCode(status, response);
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> Delete([FromQuery] long id)
        {
            var currentOrgId = this.CurrentOrgId();
            var isUber = this.IsUberAdmin();

            var (status, response) = await TagsService.MarkAsDeleted(currentOrgId, isUber, id);
            return StatusCode(status, response);
        }
    }
}
