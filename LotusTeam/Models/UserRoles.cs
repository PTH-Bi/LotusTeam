using LotusTeam.Models;

public class UserRoles
{
    public int UserRoleID { get; set; }

    public int UserID { get; set; }

    public int RoleID { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}