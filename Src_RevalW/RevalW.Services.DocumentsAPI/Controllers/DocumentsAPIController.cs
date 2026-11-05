using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.DocumentsAPI.Services;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.DocumentsAPI.Controllers;

[Route("api/documents")]
[ApiController]
public class DocumentsAPIController(DocumentsService documentsService, GenerationProgressService progressService) : ControllerBase
{
    [HttpPost("generate")]
    [Authorize]
    public async Task<IActionResult> Generate([FromBody] DocumentGenerateRequest request)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber && request.OrganizationId != currentOrgId)
            return StatusCode(StatusCodes.Status400BadRequest, "cannot generate document");

        var creaateResp = await documentsService.Create(request);
        return Ok(new ResponseDto<DocumentCreationResult> { Result = creaateResp });
    }

    [HttpPost("generate-start")]
    [Authorize]
    public IActionResult GenerateStart([FromBody] DocumentGenerateRequest request)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        if (!isUber && request.OrganizationId != currentOrgId)
            return StatusCode(StatusCodes.Status400BadRequest, "cannot generate document");

        var operationId = documentsService.StartCreate(request);
        return Ok(ResponseDto<string>.Ok(operationId));
    }

    [HttpGet("generate-progress")]
    [Authorize]
    public IActionResult GetGenerateProgress([FromQuery] string operationId)
    {
        var messages = progressService.GetAndClear(operationId).ToArray();
        var result = progressService.GetResult(operationId);
        return Ok(new ResponseDto<DocumentCreationResult>() { IsSuccess = true, Message = string.Join("\n", messages), Result = result });
    }
}
