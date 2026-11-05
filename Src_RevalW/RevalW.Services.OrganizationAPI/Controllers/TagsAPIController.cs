using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.OrganizationAPI.Services;
using RevalW.Shared.Utils;

namespace RevalW.Services.OrganizationAPI.Controllers
{
    [Route("api/otags")]
    [ApiController]
    public class TagsAPIController(TagsService tagsService) : ControllerBase
    {
        [HttpGet("allFull")]
        [Authorize(Roles = ERoles.UberAdmin)]
        public IActionResult AllFull()
        {
            var (status, response) = tagsService.GetAllFullTags();
            return StatusCode(status, response);
        }

        [HttpGet("all")]
        public IActionResult All()
        {
            var (status, response) = tagsService.GetAllTags();
            return StatusCode(status, response);
        }

        [HttpGet("organizationsByTags")]
        public IActionResult GetOrganizationsByTags([FromQuery] string tags)
        {
            var (status, response) = tagsService.GetTags(tags.Split(' '));
            return StatusCode(status, response);
        }
    }
}
