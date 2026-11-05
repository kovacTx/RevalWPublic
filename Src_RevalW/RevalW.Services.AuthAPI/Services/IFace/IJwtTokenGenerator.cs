using RevalW.Services.AuthAPI.Model;

namespace RevalW.Services.AuthAPI.Services.IFace;

public interface IJwtTokenGenerator
{
    (string token, DateTime expiration) GenerateToken(ApplicationUser appUser, IEnumerable<string> roles, IEnumerable<string> rights);
    string GenerateRefreshToken(ApplicationUser appUser, string? token);
}
