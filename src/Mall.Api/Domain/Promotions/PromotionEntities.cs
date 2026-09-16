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

public enum PromotionActivityType { FullReduction, LimitedDiscount, Bargain, Presale, Gift, GroupBuy, FlashSale }
public enum PromotionActivityStatus { Draft, Published, Running, Paused, Finished, Cancelled }
public enum PromotionOrderStatus { Pending, Paid, Success, Failed, Refunded, Cancelled }

public abstract class PromotionActivity
{
    public long Id { get; set; }
    public PromotionActivityType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PromotionActivityStatus Status { get; set; } = PromotionActivityStatus.Draft;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FullReductionRule
{
    public long Id { get; set; }
    public decimal ThresholdAmount { get; set; }
    public decimal ReductionAmount { get; set; }
    public long? ProductId { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class Coupon
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal ThresholdAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public int TotalQuantity { get; set; }
    public int IssuedQuantity { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class UserCoupon
{
    public long Id { get; set; }
    public long CouponId { get; set; }
    public long UserId { get; set; }
    public long? OrderId { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public DateTime ReceivedAt { get; set; }
}

public sealed class MemberPrice
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long SkuId { get; set; }
    public decimal Price { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class PointsAccount
{
    public long UserId { get; set; }
    public int Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PointsTransaction
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public int Amount { get; set; }
    public int BalanceAfter { get; set; }
    public string Type { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class BargainActivity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal StepAmount { get; set; }
    public int RequiredHelpers { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class BargainRecord
{
    public long Id { get; set; }
    public long ActivityId { get; set; }
    public long UserId { get; set; }
    public decimal CurrentPrice { get; set; }
    public int HelperCount { get; set; }
    public string Status { get; set; } = "ONGOING";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class PresaleActivity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime DeliveryAt { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class GiftRule
{
    public long Id { get; set; }
    public long? ProductId { get; set; }
    public long GiftProductId { get; set; }
    public long GiftSkuId { get; set; }
    public int ThresholdQuantity { get; set; } = 1;
    public int GiftQuantity { get; set; } = 1;
    public bool Enabled { get; set; } = true;
}

public sealed class LimitedDiscount
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public decimal DiscountPrice { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class PromotionOrder
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public PromotionActivityType Type { get; set; }
    public long? ActivityId { get; set; }
    public PromotionOrderStatus Status { get; set; } = PromotionOrderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
