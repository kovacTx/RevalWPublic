using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RevalW.App.Model;
using RevalW.App.Service;
using RevalW.App.Service.IFace;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RevalW.App.Controllers;

public class AuthController(AuthService authService, ITokenProvider tokenProvider, OrganizationService organizationService) : Controller
{
    public const string ControllerName = "Auth";

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        var loginRequestDto = new LoginRequestDto();
        return await EndOk(dto: loginRequestDto);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var resp = await authService.LoginAsync(dto);
        if (resp is null || !resp.IsOk())
        {
            //ModelState.AddModelError("CustomError", resp?.Message ?? string.Empty);
            return await EndErr(dto, message: resp?.Message);
        }
        var logResp = resp.ToLoginResponseDto();
        tokenProvider.SetToken(logResp?.Tokens?.Token);
        await SignInUser(logResp);
        TempData["success"] = $"welcome {resp?.Result?.User?.Name}";
        return RedirectToAction(nameof(ItemsController.ItemIndex), ItemsController.ControllerName);
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        return await EndOk();
    }

    [HttpPost]
    public async Task<IActionResult> Register(InnerRegistrationRequestDto dto, string[] selectedRoles)
    {
        if (dto.Password != dto.Password2) return await EndErr(dto: dto, message: "passwords does not match");
        var logged = User.Identity?.IsAuthenticated == true;
        if (logged && (selectedRoles.Length == 0)) return await EndErr(dto: dto, message: "at least one role must be selected");
        dto.Roles = [.. selectedRoles];
        var upperRoles = dto.Roles?.Select(r => r.ToUpper());
        if (upperRoles?.Contains(ERoles.UberAdmin) == true) return await EndErr(dto: dto, message: "uberadmin role is forbidden");

        ResponseDtoo? resp;
        if (logged)
            resp = await authService.RegisterWithinOrganizationAsync(dto);
        else
            resp = await authService.NewRegisterAsync(dto);

        if (!resp.IsOk()) return await EndErr(dto: dto, message: $"could not create user", resp: resp);

        TempData["success"] = $"user {dto.Name} created succesfully";
        return RedirectToAction(nameof(AuthController.Login), AuthController.ControllerName);
    }

    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(new RshRequestDto() { RToken = "none" });
        await HttpContext.SignOutAsync();
        tokenProvider.ClearToken();
        TempData["success"] = "logged out succesfully";
        return RedirectToAction(nameof(HomeController.Home), HomeController.ControllerName);
    }

    private async Task SignInUser(LoginResponseDto? dto)
    {
        if (dto is null) return;
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(dto.Tokens?.Token ?? "");
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

        // all this is because i want to have role claims inside and working User.IsInRole(...)
        identity.AddClaims(jwt.Claims.Where(c => c.Type != "role"));
        identity.AddClaims(jwt.Claims.Where(c => c.Type == "role").Select(r => new Claim(ClaimTypes.Role, r.Value)));
        identity.AddClaim(new(ClaimTypes.Name, jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email)?.Value ?? string.Empty));

        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
    private bool TrySetRoles(IEnumerable<string?>? roles)
    {
        if (roles is null) return false;
        ViewBag.Roles = roles.Where(r => r is not null).Select(r => new Role() { Name = r!, Selected = true });
        return true;
    }
    private async Task<ViewResult> EndErr(object? dto = null, string? viewName = null, string? message = null, ResponseDtoo? resp = null)
    {
        await AddViewBagData();
        return Utils.ExtensionMethods.EndErr(this, viewName: viewName, dto: dto, message: message, resp: resp);
    }
    private async Task<ViewResult> EndOk(string? viewName = null, object? dto = null, string? message = null)
    {
        await AddViewBagData();
        return Utils.ExtensionMethods.EndOk(this, viewName: viewName, dto: dto, message: message);
    }

    private async Task AddViewBagData()
    {
        ViewBag.Roles = new List<Role>();
        if (User.Identity?.IsAuthenticated != true)
        {
            TrySetRoles(null);
            return;
        }
        var resp = await authService.GetAssignableRoleNamesAsync();
        if (resp is null)
        {
            TempData["error"] = $"failed to fetch available roles";
            return;
        }
        if (!TrySetRoles(resp?.Result))
        {
            TempData["error"] = $"no roles available ({resp?.Message})";
            return;
        }
        if (User.Identity?.IsAuthenticated == true)
        {
            var pResp = await organizationService.GetNewUserProps();
            if (!pResp.IsOk())
            {
                TempData["error"] = pResp?.Message ?? "";
                return;
            }
            ViewBag.NewUserProperties = pResp!.Result!;
            ViewBag.UserValues = new Dictionary<string, string>();
        }
    }
}
public class Role
{
    public string Name { get; set; } = null!;
    public bool Selected { get; set; }
}
