namespace RevalW.Shared.Model;

public class UserDto
{
    public string? ID { get; set; }
    public long? OrganizationId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public IEnumerable<string>? Roles { get; set; }
    public IEnumerable<string>? Permissions { get; set; }
    public string? UserDataJson { get; set; }
    public string? Password { get; set; }
    public string? Password2 { get; set; }
}
