using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RevalW.Services.AuthAPI.Model;
using RevalW.Services.AuthAPI.Services.IFace;
using RevalW.Shared.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RevalW.Services.AuthAPI.Services;

public class JwtTokenGenerator(IOptions<JwtOptions> jwtOptions) : IJwtTokenGenerator
{
    private readonly JwtOptions jwtOptions = jwtOptions.Value;

    public (string token, DateTime expiration) GenerateToken(ApplicationUser appUser, IEnumerable<string> roles, IEnumerable<string> rights)
        => InnerGenerateToken(appUser, roles, rights);
    public string GenerateRefreshToken(ApplicationUser appUser, string? token)
    {
        // Goal: produce a refresh token-like string that is long/complicated and non-guessable.
        // We'll create a 3-part base64url string (looks similar to JWT segments), each part
        // derived from secure random bytes + HMAC of user-specific data so it cannot be reproduced
        // without the secret. This is only a formatted token string; store a secure hash in DB if needed.

        // create some random bytes
        Span<byte> rand1 = stackalloc byte[24];
        Span<byte> rand2 = stackalloc byte[32];
        RandomNumberGenerator.Fill(rand1);
        RandomNumberGenerator.Fill(rand2);

        // prepare payload to sign: combine user id, optional previous token and timestamp
        var payloadBuilder = new StringBuilder();
        payloadBuilder.Append(appUser.Id);
        if (!string.IsNullOrEmpty(token)) payloadBuilder.Append('|').Append(token);
        payloadBuilder.Append('|').Append(DateTime.UtcNow.Ticks);

        var payloadBytes = Encoding.UTF8.GetBytes(payloadBuilder.ToString());

        // compute HMAC-SHA512 using configured secret
        var key = Encoding.UTF8.GetBytes(jwtOptions.Secret ?? Guid.NewGuid().ToString());
        using var hmac = new HMACSHA512(key);
        var signature = hmac.ComputeHash(payloadBytes);

        // compose three parts using base64url (no padding) to resemble token segments
        static string Part(ReadOnlySpan<byte> bytes) => Base64UrlEncode(bytes.ToArray());

        var partA = Part(rand1);
        var partB = Part(signature.AsSpan(0, 32)); // first half of signature
        var combined = new byte[rand2.Length + signature.Length];
        rand2.CopyTo(combined.AsSpan(0, rand2.Length));
        signature.CopyTo(combined.AsSpan(rand2.Length));
        var partC = Part(combined);

        // final refresh token string
        var refreshToken = $"{partA}.{partB}.{partC}";
        return refreshToken;
    }

    private (string token, DateTime expiration) InnerGenerateToken(ApplicationUser appUser, IEnumerable<string> roles, IEnumerable<string> rights)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtOptions.Secret);
        var claimList = new List<Claim>
        {
            new(ClaimNames.Id, appUser.Id),
            new(ClaimNames.OrgId, appUser.OrganizationId.ToString() ?? string.Empty),
            new(ClaimNames.Email, appUser.Email ?? string.Empty),
            new(ClaimNames.Name, appUser.Name),
            new(ClaimNames.UniqueName, appUser.UserName ?? string.Empty),
            new(ClaimNames.Phone, appUser.PhoneNumber ?? string.Empty),
        };

        // insert only creator/uberadmin role and claims that are from roles
        claimList.AddRange(roles.Select(role => new Claim(ClaimNames.Role, role)));
        claimList.AddRange(rights.Select(right => new Claim(ClaimNames.Right, right)));

        var (descriptor, expiration) = TokenDescriptor(claimList, key);
        var token = tokenHandler.CreateToken(descriptor);
        return (tokenHandler.WriteToken(token), expiration);
    }
    private (SecurityTokenDescriptor descriptor, DateTime expiration) TokenDescriptor(List<Claim> claimList, byte[]? key)
    {
        var expiration = DateTime.UtcNow.AddDays(7);
        var ret = new SecurityTokenDescriptor()
        {
            Audience = jwtOptions.Audience,
            Issuer = jwtOptions.Issuer,
            Subject = new ClaimsIdentity(claimList, null, null, ClaimNames.Role),
            Expires = expiration,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        };
        return (ret, expiration);
    }
    private static string Base64UrlEncode(byte[] input)
    {
        var s = Convert.ToBase64String(input); // regular base64
        s = s.Split('=')[0]; // remove padding
        s = s.Replace('+', '-'); // 62nd char of encoding
        s = s.Replace('/', '_'); // 63rd char of encoding
        return s;
    }
}
