using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RevalW.Services.AuthAPI.Extensions;
using RevalW.Services.AuthAPI.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;
using System.Security.Claims;

namespace RevalW.Services.AuthAPI.Services;

public class ClaimService(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
{

    #region public
    public async Task AddPermissionToRoleAsync(string roleName, long orgId, string permission)
    {
        var role = await GetRole(roleName, orgId);
        if (role is null) return;
        await roleManager.AddClaimAsync(role, new Claim(ClaimNames.Right, permission));
    }
    public async Task AddPermissionToUserAsync(string userEmail, long orgId, List<string> permissions)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == userEmail && u.OrganizationId == orgId);
        if (user is null) return;
        foreach (var permission in permissions)
            await userManager.AddClaimAsync(user, new Claim(ClaimNames.Right, permission));
    }

    public async Task<List<RoleDto>> GetAssignableRoles(long orgId)
    {
        var applicationRoles = await roleManager.Roles.Where(r => r.OrganizationId == orgId).ToListAsync();
        List<HashSet<string>> permissions = [];
        foreach (var applicationRole in applicationRoles)
        {
            permissions.Add([.. await GetPermissionsForRoleAsync(applicationRole)]);
        }

        return [.. applicationRoles.Select((r, i) => r.ToRoleDto(permissions[i]))];
    }

    public async Task UpsertRole(RoleDto dto, long orgId)
    {
        if (dto.Name.Equals(ERoles.UberAdmin, StringComparison.InvariantCultureIgnoreCase)) return; // reserved
        if (dto.Name.Equals(ERoles.Creator, StringComparison.InvariantCultureIgnoreCase)) return; // reserved
        var role = await GetOrCreateRole(dto.Name, orgId);
        await UpdateRole(dto, role);
    }
    public async Task UpdateOrganizationRoles(IEnumerable<RoleDto> roles, long orgId)
    {
        if (orgId == 0) return;
        var actualRoles = await roleManager.Roles.Where(r => r.OrganizationId == orgId).ToListAsync();
        var actualRolesNames = actualRoles.Select(r => r.Name ?? "");
        var roleNames = roles.Select(r => RoleCode(r.Name, orgId));
        var newRoleCodes = roleNames.Except(actualRolesNames);
        var roleNamesToRemove = actualRolesNames.Except(roleNames);
        var roleNamesToUpdate = actualRolesNames.Intersect(roleNames);

        //add
        foreach (var roleCode in newRoleCodes)
        {
            var roleName = StripRoleCode(roleCode);
            var role = roles.FirstOrDefault(r => r.Name == roleName);
            if (role is null) continue;
            var newRole = await GetOrCreateRole(roleName, orgId);
            foreach (var p in role.Permissions)
                await AddPermissionToRoleAsync(newRole, p.Key);
        }
        //remove
        foreach (var roleCode in roleNamesToRemove)
        {
            var role = actualRoles.FirstOrDefault(r => r.Name == roleCode);
            if (role is null) continue;
            await TryRemoveRole(role);
        }
        //edit
        foreach (var roleCode in roleNamesToUpdate)
        {
            var roleName = StripRoleCode(roleCode);
            var newRole = roles.FirstOrDefault(r => r.Name == roleName);
            if (newRole is null) continue;
            var oldRole = actualRoles.FirstOrDefault(r => r.Name == roleCode);
            await UpdateRole(newRole, oldRole);
        }
    }
    public async Task UpdateUsersRoles(ApplicationUser? user, IEnumerable<string> roleNames, bool noOrgIdAppend = false)
    {
        if (user is null) return;
        var actualRoles = (await userManager.GetRolesAsync(user)).Select(StripRoleCode);
        var newRoles = roleNames.Except(actualRoles);
        var rolesToRemove = actualRoles.Except(roleNames);
        foreach (var roleName in newRoles)
            await AssignRoleToUser(user, roleName, noOrgIdAppend);
        foreach (var roleName in rolesToRemove)
            await RemoveRoleFromUser(user, roleName);
    }
    public async Task UpdateUsersClaims(ApplicationUser? user, IEnumerable<string> permissions)
    {
        if (user is null) return;
        var claims = await userManager.GetClaimsAsync(user);
        var actualPermissions = claims.Where(c => c.Type == ClaimNames.Right).Select(c => c.Value).ToList();
        var newPermissions = permissions.Except(actualPermissions);
        var permissionsToRemove = actualPermissions.Except(permissions);
        foreach (var permission in newPermissions)
            await AssignClaimToUser(user, permission);
        foreach (var permission in permissionsToRemove)
            await RemoveClaimFromUserAsync(user, permission);
    }
    public async Task<IEnumerable<string>> GetPermissionsForRoles(IEnumerable<string> roles, long orgId)
    {
        var permissions = new List<string>();
        foreach (var roleName in roles)
        {
            var role = await GetRole(roleName, orgId);
            if (role == null)
            {
                if (roleName == ERoles.UberAdmin) return RightsList.All.Select(r => r.key);
                if (roleName == ERoles.Creator) return RightsList.All.Select(r => r.key);
                continue;
            }
            permissions.AddRange(await GetPermissionsForRoleAsync(role));
        }
        return [.. permissions];
    }
    public static string StripRoleCode(string roleCode)
    {
        int index = roleCode.IndexOf('_');
        if (index < 0) return roleCode;
        return roleCode[(index + 1)..];
    }

    #endregion
    #region private
    public async Task UpdateRole(RoleDto dto, ApplicationRole? role)
    {
        if (role is null) return;
        var newPermissions = dto.Permissions.Keys;
        var actualPermissions = await GetPermissionsForRoleAsync(role);
        var permissionsToAdd = newPermissions.Except(actualPermissions);
        var permissionsToRemove = actualPermissions.Except(newPermissions);
        foreach (var permission in permissionsToAdd)
            await AddPermissionToRoleAsync(role, permission);
        foreach (var permission in permissionsToRemove)
            await RemovePermissionFromRoleAsync(role, permission);
    }
    private async Task AssignRoleToUser(ApplicationUser? user, string? roleName, bool noOrgIdAppend)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return;
        if (user is null) return;
        var role = await GetRole(roleName, (noOrgIdAppend) ? null : user.OrganizationId);
        if (role is null) return;

        if (await userManager.IsInRoleAsync(user, role.Name!)) return;
        await userManager.AddToRoleAsync(user, role.Name!);
    }
    private async Task RemoveRoleFromUser(ApplicationUser? user, string? roleName)
    {
        if (string.IsNullOrEmpty(roleName)) return;
        if (user is null) return;
        var role = await GetRole(roleName, user.OrganizationId);
        if (role is null) return;

        await userManager.RemoveFromRoleAsync(user, role.Name!);
        //await TryRemoveRole(role);
    }
    private async Task AssignClaimToUser(ApplicationUser? user, string? claimName)
    {
        if (string.IsNullOrEmpty(claimName)) return;
        if (user is null) return;
        var claims = await userManager.GetClaimsAsync(user);
        var claim = claims.FirstOrDefault(c => c.Type == ClaimNames.Right && c.Value == claimName);
        if (claim is not null) return;
        await userManager.AddClaimAsync(user, new Claim(ClaimNames.Right, claimName));
    }
    private async Task RemoveClaimFromUserAsync(ApplicationUser? user, string? claimName)
    {
        if (string.IsNullOrEmpty(claimName)) return;
        if (user is null) return;
        var claims = await userManager.GetClaimsAsync(user);
        var claim = claims.FirstOrDefault(c => c.Type == ClaimNames.Right && c.Value == claimName);
        if (claim is null) return;
        await userManager.RemoveClaimAsync(user, claim);
    }
    private async Task<List<string>> GetPermissionsForRoleAsync(ApplicationRole role)
    {
        var claims = await roleManager.GetClaimsAsync(role);
        return [.. claims.Where(c => c.Type == ClaimNames.Right).Select(c => c.Value)];
    }
    private async Task<ApplicationRole?> GetOrCreateRole(string roleName, long? orgId)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return null;
        var role = await GetRole(roleName, orgId);
        if (role != null) return role;

        var roleCode = (orgId.HasValue) ? RoleCode(roleName, orgId.Value) : roleName;

        role = new ApplicationRole(roleCode, orgId ?? 0);
        var result = await roleManager.CreateAsync(role);
        if (result.Succeeded) return role;

        // Handle possible race condition:
        // Another request may have created the role between our check and create
        var organizationId = orgId ?? 0;
        role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Name == roleCode && r.OrganizationId == organizationId);
        if (role is not null) return role;
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to create role: {errors}");
    }
    private async Task<ApplicationRole?> GetRole(string roleName, long? orgId)
    {
        ApplicationRole? ret = null;
        if (string.IsNullOrWhiteSpace(roleName)) return ret;
        if (orgId.HasValue)
        {
            var roleCode = RoleCode(roleName, orgId.Value);
            ret = await roleManager.Roles.FirstOrDefaultAsync(r => r.Name == roleCode && r.OrganizationId == orgId.Value);
        }
        else
        {
            ret = await roleManager.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        }
        return ret;
    }
    private async Task TryRemoveRole(ApplicationRole role)
    {
        if (string.IsNullOrEmpty(role.Name)) return;
        var usersInRole = await userManager.GetUsersInRoleAsync(role.Name);
        var usersInSameOrg = usersInRole.Where(u => u.OrganizationId == role.OrganizationId);
        if (usersInSameOrg.Any()) return;
        await roleManager.DeleteAsync(role);
    }
    private async Task AddPermissionToRoleAsync(ApplicationRole? role, string permission)
    {
        if (role is null) return;
        var claims = await roleManager.GetClaimsAsync(role);
        var alreadyExists = claims.Any(c => c.Type == ClaimNames.Right && c.Value == permission);
        if (alreadyExists) return;
        var result = await roleManager.AddClaimAsync(role, new Claim(ClaimNames.Right, permission));
        if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
    private async Task RemovePermissionFromRoleAsync(ApplicationRole? role, string permission)
    {
        if (role is null) return;
        var claims = await roleManager.GetClaimsAsync(role);
        var claim = claims.FirstOrDefault(c => c.Type == ClaimNames.Right && c.Value == permission);
        if (claim is null) return;
        var result = await roleManager.RemoveClaimAsync(role, claim);
        if (!result.Succeeded) throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }
    private static string RoleCode(string roleName, long orgId)
        => $"{orgId}_{roleName}";

    #endregion
}

