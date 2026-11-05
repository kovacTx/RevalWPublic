using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using RevalW.Services.AuthAPI.Data;
using RevalW.Services.AuthAPI.Extensions;
using RevalW.Services.AuthAPI.Model;
using RevalW.Services.AuthAPI.Services.IFace;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.DataTablesRequest;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using RevalW.Shared.Utils.IFace;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RevalW.Services.AuthAPI.Services;

public class AuthService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    OrganizationService organizationService,
    DataTablesWorker<ApplicationUser, string, UserDto> dataTablesWorker,
    ClaimService claimService,
    IJwtTokenGenerator tokenGenerator,
    IHttpContextAccessor httpContextAccessor,
    ISimpleHasher simpleHasher,
    IMapper mapper)
{
    public async Task<(int status, ResponseDto<List<UserDto>> response)> GetAllUsers(long currentOrgId, bool isUberAdmin)
    {
        var users = db.ApplicationUsers.Where(u => isUberAdmin || u.OrganizationId == currentOrgId);
        var ret = new List<UserDto>();
        foreach (var u in users)
        {
            var uDto = u.ToUserDto();
            uDto.Roles = await userManager.GetRolesAsync(u);
            ret.Add(uDto);
        }
        return (StatusCodes.Status200OK, ResponseDto<List<UserDto>>.Ok(ret));
    }
    public async Task<(int status, ResponseDto<UserDto> response)> GetUser(long currentOrgId, bool isUberAdmin, bool isCreator, string userId)
    {
        var user = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return (StatusCodes.Status404NotFound, ResponseDto<UserDto>.Err("user not found"));
        if (isUberAdmin) currentOrgId = user.OrganizationId;
        if (user.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("cannot access user info"));
        var userDto = user.ToUserDto();
        userDto.Roles = (await userManager.GetRolesAsync(user)).Select(ClaimService.StripRoleCode);
        return (StatusCodes.Status200OK, ResponseDto<UserDto>.Ok(userDto));
    }
    public async Task<(int status, ResponseDto<string?> response)> GetUserName(long currentOrgId, bool isUberAdmin, bool isCreator, string userId)
    {
        var user = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return (StatusCodes.Status404NotFound, ResponseDto<string?>.Err("user not found"));
        if (isUberAdmin) currentOrgId = user.OrganizationId;
        if (user.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<string?>.Err("cannot access user info"));
        var userDto = user.ToUserDto();
        return (StatusCodes.Status200OK, ResponseDto<string?>.Ok(userDto.Name));
    }
    public async Task<(int status, ResponseDto<UserDto> response)> GetByEmail(long currentOrgId, bool isUberAdmin, string? email)
    {
        if (string.IsNullOrEmpty(email)) return (StatusCodes.Status404NotFound, ResponseDto<UserDto>.Err("user not found"));
        email = email.ToLower();
        var user = await db.ApplicationUsers.FirstOrDefaultAsync(u => (u.Email != null) && u.Email.ToLower() == email);
        if (user is null) return (StatusCodes.Status404NotFound, ResponseDto<UserDto>.Err("user not found"));
        if (!isUberAdmin && user.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("cannot access user info"));
        var userDto = user.ToUserDto();
        userDto.Roles = await userManager.GetRolesAsync(user);
        return (StatusCodes.Status200OK, ResponseDto<UserDto>.Ok(userDto));
    }
    public async Task<(int status, ResponseDto<string> response)> GetUserData(long currentOrgId, bool isUberAdmin, string? id, string? askerEmail)
    {
        if (string.IsNullOrEmpty(id))
            id = (await db.ApplicationUsers.FirstOrDefaultAsync(u => (u.Email != null) && u.Email.ToLower() == askerEmail))?.Id;
        if (string.IsNullOrEmpty(id)) return (StatusCodes.Status404NotFound, ResponseDto<string>.Err("user not found"));
        id = id.ToLower();
        var user = await db.ApplicationUsers.FirstOrDefaultAsync(u => (u.Id != null) && u.Id.ToLower() == id);
        if (user is null) return (StatusCodes.Status404NotFound, ResponseDto<string>.Err("user not found"));
        if (!isUberAdmin && user.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("cannot access user info"));
        var ret = AddUserData(user.UserDataJson, user.Name ?? "", user.Email ?? "");
        return (StatusCodes.Status200OK, ResponseDto<string>.Ok(ret));
    }
    public async Task<ResponseDto<DataTablesResponse<UserDto>>> GetByDataTables(long currentOrgId, bool isUber, Request request)
    {
        var queryable = db.ApplicationUsers.Where(c => isUber || c.OrganizationId == currentOrgId);

        var mainGridColumns = AppendDbColumnsForDataTables(GetJsonColumnsForDataTables(null));
        var ret = await dataTablesWorker.DoDataTablesSelect(queryable, request, mainGridColumns, async (dtu) =>
        {
            var uDto = dtu.ToUserDto();
            uDto.Roles = (await userManager.GetRolesAsync(dtu)).Select(ClaimService.StripRoleCode);
            return uDto;
        });
        return ResponseDto<DataTablesResponse<UserDto>>.Ok(ret);
    }
    public async Task<(int status, ResponseDtoo response)> NewRegister(bool isLogged, NewRegistrationRequestDto request)
    {
        var transactionSuccessful = false;
        using var transaction = await db.Database.BeginTransactionAsync();
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        long orgId = 0;
        try
        {
            string errMessage;
            if (isLogged) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("cannot be logged in"));
            var userRegistrationReq = request.ToUserRegistrationRequest();
            var orgRegistrationReq = request.ToOrganizationRegistrationRequest();

            var (status, response) = await organizationService.CreateOrganization(authHeader, orgRegistrationReq);
            if (status != StatusCodes.Status200OK) return (status, ResponseDtoo.Err($"failed to create organization: {response?.Message}"));
            orgId = response!.Result!.Id;

            userRegistrationReq.Roles = [ERoles.Creator];
            userRegistrationReq.Permissions = [];
            userRegistrationReq.OrganizationId = orgId;
            errMessage = await Register(userRegistrationReq, asNew: true);

            await claimService.UpsertRole("Admin".ToRoleDto(RightsList.AdminRights), orgId);
            await claimService.UpsertRole("User".ToRoleDto(RightsList.UserRights), orgId);

            if (!string.IsNullOrEmpty(errMessage)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err(errMessage));
            //await messageBus.PublishMessage(request.Email, configuration.GetValue<string>("TopicAndQueueNames:RegisteredUserQueue"));

            transactionSuccessful = true;
            await transaction.CommitAsync();
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
        finally
        {
            if (!transactionSuccessful)
            {
                await transaction.RollbackAsync();
                if (orgId != 0) await organizationService.DeleteOrganization(authHeader, orgId);
            }
        }
    }
    public async Task<(int status, ResponseDtoo response)> RegisterWithinOrganization(long currentOrgId, bool isUberAdmin, bool isAuthorized, RegistrationWithinOrganizationRequestDto dto)
    {
        var transactionSuccessful = false;
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            dto.Roles ??= [];
            if (dto.Roles.Contains(ERoles.UberAdmin)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("UBERADMIN role is forbidden"));
            if (dto.Roles.Contains(ERoles.Creator)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("cannot create new CREATOR user. creator user must be registered as first user (without anyone logged in)"));
            var userRegistrationReq = dto.ToUserRegistrationRequest();
            if (!isAuthorized) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("user is not authorized for this operation"));

            // todo: add possibility to select any organizationId in view (for uberAdmin)
            userRegistrationReq.OrganizationId = (!isUberAdmin || userRegistrationReq.OrganizationId == default) ? currentOrgId : userRegistrationReq.OrganizationId;
            if (userRegistrationReq.OrganizationId == default) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err($"have no organization for which to create the user"));
            var errMessage = await Register(userRegistrationReq);

            if (!string.IsNullOrEmpty(errMessage)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err(errMessage));
            //await messageBus.PublishMessage(request.Email, configuration.GetValue<string>("TopicAndQueueNames:RegisteredUserQueue"));

            transactionSuccessful = true;
            await transaction.CommitAsync();
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
        finally
        {

            if (!transactionSuccessful)
                await transaction.RollbackAsync();
        }
    }
    public async Task<(int status, ResponseDto<string> response)> Delete(long currentOrgId, bool isCreator, bool isUberAdmin, string? id)
    {
        try
        {
            if (string.IsNullOrEmpty(id)) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("no user to delete?"));
            var userRes = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (userRes is null) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("error occurred: user not found"));
            if (userRes.OrganizationId != currentOrgId && !isUberAdmin) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("cannot delete user of different organization"));
            var res = await userManager.DeleteAsync(userRes);
            if (!res.Succeeded) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err(res.Errors.FirstOrDefault()?.Description ?? "error occurred: cannot delete user"));

            return (StatusCodes.Status200OK, ResponseDto<string>.Ok());
        }
        catch (Exception ex)
        {
            return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err($"error occurred: {ex.Message}"));
        }
    }
    public async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
    {
        // logger.LogInformation("checking creds");

        var user = await CheckCredentials(loginRequestDto);
        if (user is null) return new();

        // logger.LogInformation("creds valid");
        var userDto = user.ToUserDto();
        // logger.LogInformation("adding roles");
        userDto.Roles = (await userManager.GetRolesAsync(user)).Select(ClaimService.StripRoleCode);
        // logger.LogInformation("adding permissions");
        userDto.Permissions = await claimService.GetPermissionsForRoles(userDto.Roles, user.OrganizationId);
        // logger.LogInformation("generating token");
        var (token, expiration) = tokenGenerator.GenerateToken(user, userDto.Roles, userDto.Permissions);
        var rToken = tokenGenerator.GenerateRefreshToken(user, token);

        // logger.LogInformation("saving token");
        await SaveRToken(token, expiration, rToken, user.Id);
        // logger.LogInformation("token saved");

        var ret = new LoginResponseDto
        {
            User = userDto,
            Tokens = new RshResponseDto
            {
                Token = token,
                RToken = rToken,
                Expiration = expiration
            }
        };
        // logger.LogInformation("clearing old tokens");
        await ClearOldTokens(true);
        // logger.LogInformation("tokens cleared");
        return ret;
    }
    public async Task<(int status, ResponseDto<string> response)> RshToken(string? token, string rshToken)
    {
        if (token is null) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("token does not exist"));
        var refreshToken = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken is null) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("token does not exist"));
        if (refreshToken.RToken != rshToken) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("tokens does not exist"));
        if (!refreshToken.IsExpired) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("token is not yet expired"));

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);
        if (user is null) return (StatusCodes.Status400BadRequest, ResponseDto<string>.Err("the user does not exist"));
        var userDto = user.ToUserDto();
        userDto.Roles = (await userManager.GetRolesAsync(user)).Select(ClaimService.StripRoleCode);
        userDto.Permissions = await claimService.GetPermissionsForRoles(userDto.Roles, user.OrganizationId);
        var (nToken, expiration) = tokenGenerator.GenerateToken(user, userDto.Roles, userDto.Permissions);
        var nRToken = tokenGenerator.GenerateRefreshToken(user, token);

        refreshToken.Token = nToken;
        refreshToken.RToken = nRToken;
        refreshToken.Expiration = expiration;
        refreshToken.Revoked = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await ClearOldTokens(true);
        return (StatusCodes.Status200OK, ResponseDto<string>.Ok(nRToken));
    }
    public async Task LogoutTokens(string? token, string rshToken)
    {
        db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(rt => rt.Token == token || rt.RToken == rshToken));
        await ClearOldTokens(false);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            Console.WriteLine("The token was modified or removed by another process before this operation.");
        }
    }
    public async Task<(int status, ResponseDtoo response)> ChangePassword(bool isUber, bool isCreator, long currentOrgId, ChangePasswordRequestDto request, string? hash)
    {
        if (simpleHasher.ComputeHash(request.Id + request.Password + "uroboros") != hash) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("cannot change this password"));
        if (string.IsNullOrEmpty(request.Id)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("Id is empty"));
        if (string.IsNullOrEmpty(request.Password)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("password is empty"));
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err($"cannot find user {request.Id}"));
        if (isUber) currentOrgId = user.OrganizationId;
        if (currentOrgId != user.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("cannot change this password"));
        if (!isCreator) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("have no permissions"));
        try
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var res = await userManager.ResetPasswordAsync(user, token, request.Password);
            if (!res.Succeeded) return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(res.Errors.FirstOrDefault()?.Description ?? "error occurred: cannot change password"));

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception ex)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err($"error occurred: {ex.Message}"));
        }
    }
    public async Task<UserDto?> GetUserByEmail(string? email)
    {
        if (email is null) return null;
        var lowerName = email.ToLower();
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName!.ToLower() == lowerName);
        if (user is null) return null;
        var ret = user.ToUserDto();
        ret.Roles = await userManager.GetRolesAsync(user);
        return ret;
    }
    public async Task<(int status, ResponseDto<UserDto> response)> Update(bool isUber, bool isCreator, long currentOrgId, UserDto? user)
    {
        if (user is null) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("no use to edit?"));
        if (!isUber && !isCreator) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("have no permissions"));
        var appUser = mapper.Map<ApplicationUser>(user);

        var myUser = await userManager.Users.FirstOrDefaultAsync(u => u.Id == appUser.Id);
        if (myUser is null) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("the user to edit is wrong"));
        if (isUber) currentOrgId = myUser.OrganizationId;
        if (currentOrgId != myUser.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("cannot edit user of different organization"));

        myUser.UserDataJson = appUser.UserDataJson;
        myUser.PhoneNumber = appUser.PhoneNumber;
        myUser.Name = appUser.Name;

        await claimService.UpdateUsersRoles(myUser, user.Roles ?? []);
        await claimService.UpdateUsersClaims(myUser, user.Permissions ?? []);

        await db.SaveChangesAsync();

        var editedUser = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == appUser.Id);
        if (editedUser is null) return (StatusCodes.Status400BadRequest, ResponseDto<UserDto>.Err("could not convert user back to dto"));
        var userDto = editedUser.ToUserDto();
        userDto.Roles = await userManager.GetRolesAsync(editedUser);
        return (StatusCodes.Status200OK, ResponseDto<UserDto>.Ok(userDto));
    }

    #region private
    /// <summary>
    /// we are in transaction so dont need to revert if problem occurred
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task<string> Register(UserRegistrationRequest request, bool asNew = false)
    {
        try
        {
            if (UberAdminAllowed(request.Email))
                request.Roles = ERoles.UberAdminRoles().Select((r) => ERoles.ToRole(r)!);
            var res = await userManager.CreateAsync(request.ToApplicationUser(), request.Password);
            if (!res.Succeeded) return res.Errors.FirstOrDefault()?.Description ?? "error occurred: cannot create user";

            var user = userManager.Users.FirstOrDefault(u => u.UserName == request.Email && u.OrganizationId == request.OrganizationId);
            await claimService.UpdateUsersRoles(user, request.Roles ?? [], asNew);

            var createdUser = await db.ApplicationUsers.FirstOrDefaultAsync(u => u.UserName!.ToLower() == request.Email);
            if (createdUser is null) return "error occurred: user was not created";
            return string.Empty;
        }
        catch (Exception ex)
        {
            return $"error occurred: {ex.Message}";
        }
    }
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> AppendDbColumnsForDataTables(IEnumerable<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? jsonColumns)
    {
        List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)> ret;
        if (jsonColumns is null) ret = [];
        else ret = [.. jsonColumns];
        ret.Add((nameof(ApplicationUser.Name), null));
        ret.Add((nameof(ApplicationUser.Email), null));
        ret.Add((nameof(ApplicationUser.PhoneNumber), null));
        return ret;
    }
    private static List<(string? jsonColName, IEnumerable<string>? jsonKeyNames)>? GetJsonColumnsForDataTables(List<MainGridColumnDto>? mainGridColumns) => null;

    private static string AddUserData(string? json, string name, string email)
    {
        JsonObject obj;

        if (string.IsNullOrWhiteSpace(json)) obj = [];
        else obj = JsonNode.Parse(json)?.AsObject() ?? [];

        obj["name"] = name;
        obj["email"] = email;

        return obj.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
    private async Task SaveRToken(string token, DateTime expiration, string rToken, string userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            RToken = rToken,
            UserId = userId,
            Expiration = expiration,
            Created = DateTime.UtcNow
        };
        db.RefreshTokens.Add(refreshToken);

        await db.SaveChangesAsync();
    }
    private async Task<ApplicationUser?> CheckCredentials(LoginRequestDto loginRequestDto)
    {
        var lowerName = loginRequestDto.Email?.ToLower();
        if (string.IsNullOrEmpty(lowerName)) return null;
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName!.ToLower() == lowerName);
        if (user is null) return null;
        bool passValid = await userManager.CheckPasswordAsync(user, loginRequestDto.Password ?? "");
        if (!passValid) return null;
        return user;
    }
    private async Task ClearOldTokens(bool saveAfter)
    {
        //clean everything old enough
        var today = DateTime.UtcNow;
        var oldTokens = await db.RefreshTokens.Where(rt => rt.Expiration.AddDays(7) <= today).ToListAsync();
        db.RefreshTokens.RemoveRange(oldTokens);
        if (saveAfter) await db.SaveChangesAsync();
    }
    private bool UberAdminAllowed(string? email) => (email == "kovac.tx@gmail.com" || email == "revalion22@hotmail.com" || email == "revalion22@gmail.com");

    #endregion
}
