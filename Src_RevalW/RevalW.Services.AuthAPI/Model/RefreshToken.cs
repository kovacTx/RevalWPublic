using System.ComponentModel.DataAnnotations;

namespace RevalW.Services.AuthAPI.Model;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string RToken { get; set; } = null!;
    public DateTime Expiration { get; set; }
    public bool IsExpired => DateTime.UtcNow.AddSeconds(-15) >= Expiration;
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }
    public bool IsActive => Revoked == null && !IsExpired;
}
