using Microsoft.AspNetCore.Identity;

namespace RevalW.Services.AuthAPI.Model;

public class ApplicationRole : IdentityRole
{
    public long OrganizationId { get; private set; }

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
    public ApplicationRole(string roleName, long orgId) : base(roleName)
    {
        OrganizationId = orgId;
    }
}
