namespace Mall.Api.Domain.Identity;

public sealed class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<UserRole> Users { get; set; } = [];
    public List<RolePermission> Permissions { get; set; } = [];
}

public sealed class Permission
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<RolePermission> Roles { get; set; } = [];
}

public sealed class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public Users.User? User { get; set; }
    public Role? Role { get; set; }
}

public sealed class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
