namespace RevalW.Services.AuthAPI.Model;

public class UserRegistrationRequest
{
    public long OrganizationId { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Password { get; set; } = null!;
    public IEnumerable<string>? Roles { get; set; }
    public IEnumerable<string>? Permissions { get; set; }
    public string? UserDataJson { get; set; }
}
