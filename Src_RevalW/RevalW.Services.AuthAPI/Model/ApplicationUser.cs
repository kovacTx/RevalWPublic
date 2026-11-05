using Microsoft.AspNetCore.Identity;

namespace RevalW.Services.AuthAPI.Model;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = null!;
    public long OrganizationId { get; set; }
    public string? UserDataJson { get; set; }
}
