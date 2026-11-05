using Microsoft.AspNetCore.Mvc;
using RevalW.App.Service;
using RevalW.App.Utils;
using RevalW.Shared.Utils.IFace;

namespace RevalW.App.Controllers;

[Route("")]
public class OpenController(ISimpleHasher simpleHasher, ItemsService itemsService) : Controller
{
    [HttpGet("open")]
    public async Task<IActionResult> Open(string fp)
    {
        if (string.IsNullOrWhiteSpace(fp))
            return this.EndRedirectErr("/", "Invalid file path");

        var path = simpleHasher.DecodePath(fp);
        var streamResp = await itemsService.DownloadDocumentAsync(path);
        if (streamResp is null || !streamResp.IsSuccess || streamResp.Result is null)
            return this.EndRedirectErr("/", "Could not download the file");

        var stream = streamResp.Result;
        var contentType = "application/pdf";

        return File(stream, contentType, enableRangeProcessing: true);
    }
}
