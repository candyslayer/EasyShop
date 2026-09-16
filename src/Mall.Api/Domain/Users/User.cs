namespace Mall.Api.Domain.Users;

public sealed class User
{
    public long Id { get; set; }
    public string? WechatOpenId { get; set; }
    public string? WechatUnionId { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Mobile { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<UserAddress> Addresses { get; set; } = [];
    public List<Domain.Identity.UserRole> Roles { get; set; } = [];
}

public sealed class UserAddress
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Consignee { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
}
