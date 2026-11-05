namespace RevalW.Shared.Utils;

public static class ERoles
{
    private const int LowestRole = 4; // basically number of available roles

    public const string UberAdmin = "UBERADMIN";
    public const int UberAdminInt = 1;

    public const string Creator = "CREATOR";
    public const int CreatorInt = 2;

    public const string NoRole = "";
    public const int NoRoleInt = 1000;

    public static int ToInt(string? role)
        => role switch
        {
            UberAdmin => UberAdminInt,
            Creator => CreatorInt,
            _ => NoRoleInt,
        };
    public static string? ToRole(int val)
        => val switch
        {
            UberAdminInt => UberAdmin,
            CreatorInt => Creator,
            _ => NoRole,
        };
    public static IEnumerable<int> UberAdminRoles() => [UberAdminInt, CreatorInt];
    public static IEnumerable<int> CreatorRoles() => [CreatorInt];
    public static IEnumerable<int>? LowerRoles(IEnumerable<string>? roles)
    {
        if (roles is null) return null;
        var highestRole = HighestRole(roles);
        if (highestRole == LowestRole) return [];
        highestRole++; // lower it
        return Enumerable.Range(highestRole, LowestRole - highestRole + 1);
    }
    public static IEnumerable<int>? LowerRoles(IEnumerable<int>? roles)
    {
        if (roles is null) return null;
        var highestRole = HighestRole(roles);
        if (highestRole == LowestRole) return [];
        highestRole++; // lower it
        return Enumerable.Range(highestRole, LowestRole - highestRole + 1);
    }
    public static int HighestRole(IEnumerable<string>? roles)
    {
        if (roles is null) return NoRoleInt;
        return roles.Select(ToInt).Min();

    }
    public static int HighestRole(IEnumerable<int>? roles)
    {
        if (roles is null) return NoRoleInt;
        return roles.Min();

    }

}
