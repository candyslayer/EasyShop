using Mall.Api.Domain.Promotions;

namespace Mall.Api.Application.Promotions;

public sealed record GroupBuyActivityResponse(long Id, long ProductId, long SkuId, string ProductName, decimal LeaderPrice, decimal MemberPrice, int RequiredMembers, DateTime StartsAt, DateTime EndsAt);
public sealed record GroupBuyTeamResponse(long Id, long ActivityId, long LeaderUserId, int CurrentMembers, int RequiredMembers, GroupBuyTeamStatus Status, DateTime ExpiresAt);
public sealed record CreateGroupBuyTeamRequest(long ActivityId, long OrderId);
public sealed record JoinGroupBuyTeamRequest(long OrderId);
public sealed record FlashSaleResponse(long Id, long ProductId, long SkuId, string ProductName, decimal SalePrice, int RemainingStock, int PerUserLimit, DateTime StartsAt, DateTime EndsAt);
public sealed record FlashSaleReservationRequest(long ActivityId, long OrderId, int Quantity = 1);
public sealed record DistributorResponse(long Id, string InviteCode, long? ParentDistributorId, decimal CommissionRate, bool Enabled);
public sealed record BindDistributorRequest(string InviteCode);
