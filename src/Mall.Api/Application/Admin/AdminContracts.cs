using Mall.Api.Domain.Orders;

namespace Mall.Api.Application.Admin;

public sealed record AdminUserResponse(long Id, string? Username, string? Mobile, string Nickname, bool Enabled, string[] Roles);
public sealed record AdminRoleResponse(long Id, string Name, string Code, string[] Permissions);
public sealed record SaveRoleRequest(string Name, string Code, long[]? PermissionIds = null);
public sealed record SavePermissionRequest(string Name, string Code);
public sealed record AdminProductResponse(long Id, string Name, bool IsOnSale, decimal MinPrice, decimal MaxPrice);
public sealed record AdminOrderResponse(long Id, string OrderNo, long UserId, OrderStatus Status, decimal PayableAmount, DateTime CreatedAt);
public sealed record ShipOrderRequest(string Company, string TrackingNo);
