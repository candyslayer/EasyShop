using Mall.Api.Domain.Orders;
using Mall.Api.Application.Orders;

namespace Mall.Api.Application.Admin;

public sealed record AdminUserResponse(long Id, string? Username, string? Mobile, string Nickname, bool Enabled, string[] Roles);
public sealed record AdminUserDetailResponse(long Id, string? Username, string? Mobile, string Nickname, string? AvatarUrl, bool Enabled, DateTime CreatedAt, DateTime UpdatedAt, string[] Roles, AdminUserAddressResponse[] Addresses);
public sealed record AdminUserAddressResponse(long Id, string Consignee, string Mobile, string Province, string City, string District, string Detail, bool IsDefault);
public sealed record UpdateAdminUserStatusRequest(bool Enabled);
public sealed record AdminRoleResponse(long Id, string Name, string Code, string[] Permissions);
public sealed record SaveRoleRequest(string Name, string Code, long[]? PermissionIds = null);
public sealed record SavePermissionRequest(string Name, string Code);
public enum DeleteAdminRoleResult { NotFound, InUse, Deleted }
public sealed record SaveAdminCategoryRequest(long? ParentId, string Name, string? IconUrl, int SortOrder = 0, bool Enabled = true);
public sealed record UpdateAdminCategoryStatusRequest(bool Enabled);
public enum DeleteAdminCategoryResult { NotFound, InUse, Deleted }
public sealed record AdminProductListItem(long Id, string Name, long CategoryId, string CategoryName, string? PrimaryImageUrl, bool IsOnSale, decimal MinPrice, decimal MaxPrice, int TotalStock, int LockedStock, int SalesCount);
public sealed record AdminProductListResponse(int Page, int PageSize, int Total, AdminProductListItem[] Items);
public sealed record AdminProductImageRequest(string ImageUrl, int SortOrder = 0, bool IsPrimary = false);
public sealed record AdminProductSkuRequest(long? Id, string SkuCode, string? Specification, decimal Price, decimal MarketPrice, int Stock, bool Enabled = true);
public sealed record SaveAdminProductRequest(string Name, long CategoryId, string? Subtitle, string? Description, decimal MinPrice, decimal MaxPrice, bool IsOnSale, string? Brand, string? Tags, string? Attributes, bool IsRecommended, AdminProductImageRequest[]? Images, AdminProductSkuRequest[]? Skus);
public sealed record AdminProductDetailResponse(long Id, string Name, long CategoryId, string CategoryName, string? Subtitle, string? Description, decimal MinPrice, decimal MaxPrice, bool IsOnSale, string? Brand, string Tags, string Attributes, bool IsRecommended, AdminProductImageResponse[] Images, AdminProductSkuResponse[] Skus);
public sealed record AdminProductImageResponse(long Id, string ImageUrl, int SortOrder, bool IsPrimary);
public sealed record AdminProductSkuResponse(long Id, string SkuCode, string? Specification, decimal Price, decimal MarketPrice, int Stock, int LockedStock, int AvailableStock, bool Enabled);
public sealed record AdminProductInventoryResponse(long ProductId, int TotalStock, int LockedStock, int AvailableStock, AdminProductSkuResponse[] Skus);
public sealed record AdminOrderListItem(long Id, string OrderNo, long UserId, OrderStatus Status, decimal PayableAmount, DateTime CreatedAt, string ProductSummary, int ItemCount);
public sealed record AdminOrderListResponse(int Page, int PageSize, int Total, AdminOrderListItem[] Items);
public sealed record AdminOrderDetailResponse(long Id, string OrderNo, long UserId, OrderStatus Status, decimal GoodsAmount, decimal FreightAmount, decimal DiscountAmount, decimal PayableAmount, string Consignee, string Mobile, string Address, DateTime CreatedAt, DateTime? PaidAt, DateTime? ShippedAt, DateTime? FinishedAt, DateTime? CancelledAt, string Remark, DateTime? PaymentExpiredAt, string? ShippingCompany, string? TrackingNo, OrderItemResponse[] Items);
public sealed record ShipOrderRequest(string Company, string TrackingNo);
