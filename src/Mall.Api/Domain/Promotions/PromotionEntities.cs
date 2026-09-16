namespace Mall.Api.Domain.Promotions;

public sealed class GroupBuyActivity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public decimal LeaderPrice { get; set; }
    public decimal MemberPrice { get; set; }
    public int RequiredMembers { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public enum GroupBuyTeamStatus { Open, Succeeded, Failed, Cancelled }

public sealed class GroupBuyTeam
{
    public long Id { get; set; }
    public long ActivityId { get; set; }
    public long LeaderUserId { get; set; }
    public int CurrentMembers { get; set; } = 1;
    public GroupBuyTeamStatus Status { get; set; } = GroupBuyTeamStatus.Open;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class GroupBuyMember
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long UserId { get; set; }
    public long OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class FlashSaleActivity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public decimal SalePrice { get; set; }
    public int TotalStock { get; set; }
    public int SoldQuantity { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class FlashSaleOrder
{
    public long Id { get; set; }
    public long ActivityId { get; set; }
    public long UserId { get; set; }
    public long OrderId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class Distributor
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? ParentDistributorId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public sealed class DistributionCommission
{
    public long Id { get; set; }
    public long DistributorId { get; set; }
    public long OrderId { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
}
