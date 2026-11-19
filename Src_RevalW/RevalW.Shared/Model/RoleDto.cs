using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Model;

public class RoleDto
{
    [Required]
    public string Name { get; set; } = null!;
    public Dictionary<string, string> Permissions { get; set; } = [];
}