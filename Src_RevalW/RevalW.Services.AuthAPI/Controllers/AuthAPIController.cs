using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevalW.Services.AuthAPI.Services;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.Services.AuthAPI.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthAPIController(
    AuthService authService,
    ClaimService claimService,
    ILogger<AuthAPIController> logger)
    : ControllerBase
{
    [HttpPost("byDataTables")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> GetByDataTables([FromBody] DTRequest request)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUber = this.IsUberAdmin();
        var ret = await authService.GetByDataTables(currentOrgId, isUber, request);
        return Ok(ret);
    }

    [HttpGet("byId")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> GetUser([FromQuery] string id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUberAdmin = this.IsUberAdmin();
        var isCreator = this.IsCreator();
        var (status, response) = await authService.GetUser(currentOrgId, isUberAdmin, isCreator, id);
        return StatusCode(status, response);
    }

    [HttpGet("name")]
    public async Task<IActionResult> GetUserName([FromQuery] string id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isUberAdmin = this.IsUberAdmin();
        var isCreator = this.IsCreator();
        var (status, response) = await authService.GetUserName(currentOrgId, isUberAdmin, isCreator, id);
        return StatusCode(status, response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var currentOrgId = this.CurrentOrgId();
        var name = User.Identity?.Name;
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await authService.GetByEmail(currentOrgId, isUberAdmin, name);
        return StatusCode(status, response);
    }

    [HttpGet("userData")]
    public async Task<IActionResult> UserDataById([FromQuery] string? id = null)
    {
        if (!this.IsLogged()) return StatusCode(StatusCodes.Status200OK, ResponseDto<string>.Ok(""));
        var currentOrgId = this.CurrentOrgId();
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await authService.GetUserData(currentOrgId, isUberAdmin, id, User.Identity?.Name);
        return StatusCode(status, response);
    }

    [HttpPost("newRegister")]
    public async Task<IActionResult> NewRegister([FromBody] NewRegistrationRequestDto request)
    {
        var isLogged = this.IsLogged();
        var (status, response) = await authService.NewRegister(isLogged, request);
        return StatusCode(status, response);
    }

    [HttpPost("registerWithinOrganization")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> RegisterWithinOrganization([FromBody] RegistrationWithinOrganizationRequestDto request)
    {
        var currentOrgId = this.CurrentOrgId();
        var isAuthorized = this.IsAuthorized(RightsList.User_Register);
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await authService.RegisterWithinOrganization(currentOrgId, isUberAdmin, isAuthorized, request);
        return StatusCode(status, response);
    }

    [HttpDelete("delete")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> Delete([FromQuery] string id)
    {
        var currentOrgId = this.CurrentOrgId();
        var isCreator = this.IsCreator();
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await authService.Delete(currentOrgId, isCreator, isUberAdmin, id);
        return StatusCode(status, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var begin = DateTime.Now;
        logger.LogInformation("tko: Login attempt started for user: {Email}", request.Email);

        var loginResponse = await authService.Login(request);

        logger.LogInformation("tko: login end, Email: {Email}, Duration: {Duration}ms, Success: {Success}",
            request.Email, (DateTime.Now - begin).Milliseconds, loginResponse.User != null);

        if (loginResponse.User is null) return BadRequest(ResponseDto<LoginResponseDto>.Err("username or password is incorrect"));
        return Ok(ResponseDto<LoginResponseDto>.Ok(loginResponse));
    }

    [HttpPost("rshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RshRequestDto request)
    {
        var currentToken = this.CurrentToken();
        var (status, response) = await authService.RshToken(currentToken, request.RToken);
        return StatusCode(status, response);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout([FromBody] RshRequestDto request)
    {
        var currentToken = this.CurrentToken();
        await authService.LogoutTokens(currentToken, request.RToken);
        return StatusCode(StatusCodes.Status200OK, ResponseDtoo.Ok());
    }

    [HttpGet("allPermissions")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public IActionResult AllPermissions()
    {
        return Ok(ResponseDto<Dictionary<string, string>>.Ok(RightsList.All.ToDictionary(i => i.key, i => i.value)));
    }

    [HttpPut("updateRoles")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> UpdateRoles(List<RoleDto> roles)
    {
        var currentOrgId = this.CurrentOrgId();
        await claimService.UpdateOrganizationRoles(roles, currentOrgId);
        return Ok();
    }

    [HttpGet("assignableRoleNames")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> GetAssignableRoleNames()
    {
        var currentOrgId = this.CurrentOrgId();
        var rolesNames = await claimService.GetAssignableRoles(currentOrgId);
        return Ok(ResponseDto<IEnumerable<string?>>.Ok(rolesNames.Select(r => r.Name)));
    }

    [HttpGet("assignableRoles")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> GetAssignableRoles()
    {
        var currentOrgId = this.CurrentOrgId();
        var rolesNames = await claimService.GetAssignableRoles(currentOrgId);
        return Ok(ResponseDto<IEnumerable<RoleDto?>>.Ok(rolesNames));
    }

    [HttpPut("update")]
    [Authorize(Roles = ERoles.UberAdmin + "," + ERoles.Creator)]
    public async Task<IActionResult> Update([FromBody] UserDto dto)
    {
        var currentOrgId = this.CurrentOrgId();
        var isCreator = this.IsCreator();
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await authService.Update(isUberAdmin, isCreator, currentOrgId, dto);
        return StatusCode(status, response);
    }

    [HttpPost("chng_pwd")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, [FromQuery] string? hash)
    {
        var currentOrgId = this.CurrentOrgId();
        var isCreator = this.IsCreator();
        var isUberAdmin = this.IsUberAdmin();
        var (status, response) = await authService.ChangePassword(isUberAdmin, isCreator, currentOrgId, request, hash);
        return StatusCode(status, response);
    }
}
