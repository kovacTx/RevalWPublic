using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RevalW.Shared.Utils;

public static class ClaimNames
{
    public const string Id = JwtRegisteredClaimNames.Sub;
    public const string Email = JwtRegisteredClaimNames.Email;
    public const string Name = JwtRegisteredClaimNames.Name;
    public const string OrgId = "orgId";
    public const string Role = ClaimTypes.Role;
    public const string Phone = "phone";
    public const string UniqueName = JwtRegisteredClaimNames.UniqueName;
    public const string Right = "right";
}
