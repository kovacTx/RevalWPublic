using Microsoft.AspNetCore.Mvc;
using RevalW.App.Model;
using RevalW.App.Service;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using System.Diagnostics;

namespace RevalW.App.Controllers
{
    public class HomeController(OTagsService oTagsService, OrganizationService organizationService, ILogger<HomeController> logger) : Controller
    {
        public const string ControllerName = "Home";
        public IActionResult About()
            => View();
        public IActionResult RevalM()
            => View();

        public IActionResult Privacy()
            => View();

        public async Task<IActionResult> Home()
        {
            var tagsResp = await oTagsService.GetAll();
            if (!tagsResp.IsOk())
            {
                TempData["error"] = "failed to read organization tags" + tagsResp?.Message;
                return View();
            }
            var tags = tagsResp?.Result?.Select(t => t.Name).OrderBy(n => n).ToList();
            ViewBag.AvailableTags = tags ?? [];

            var tagsStr = string.Join(' ', tags ?? []);
            var organizations = await organizationService.GetOrganizationsByTags(tagsStr);
            if (!organizations.IsOk())
            {
                TempData["error"] = organizations?.Message;
                return View();
            }
            ViewBag.Addresses = organizations!.Result.ToAddresses();

            return View();
        }

        [HttpPost]
        public IActionResult OrganizationsMap([FromBody] List<PublicOrganizationTagDto> tags)
            => ViewComponent("Map", new { addresses = tags.ToAddresses() });

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        public IActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}
