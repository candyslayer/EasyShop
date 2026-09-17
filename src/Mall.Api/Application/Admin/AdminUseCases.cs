using Mall.Api.Domain.Catalog;
using Mall.Api.Domain.Identity;
using Mall.Api.Domain.Orders;
using Mall.Api.Application.Orders;
using Mall.Api.Infrastructure.Persistence;
using Mall.Api.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Admin;

public sealed class AdminUseCases(MallDbContext db, CatalogCache catalogCache)
{
    public async Task<AdminUserResponse[]> UsersAsync(CancellationToken ct) => await db.Users.AsNoTracking().Include(x => x.Roles).ThenInclude(x => x.Role).OrderBy(x => x.Id).Select(x => new AdminUserResponse(x.Id, x.Username, x.Mobile, x.Nickname, x.Enabled, x.Roles.Where(r => r.Role != null).Select(r => r.Role!.Code).ToArray())).ToArrayAsync(ct);
    public async Task<AdminUserDetailResponse?> UserAsync(long id, CancellationToken ct) => await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminUserDetailResponse(x.Id, x.Username, x.Mobile, x.Nickname, x.AvatarUrl, x.Enabled, x.CreatedAt, x.UpdatedAt, x.Roles.Where(r => r.Role != null).Select(r => r.Role!.Code).ToArray(), x.Addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.Id).Select(a => new AdminUserAddressResponse(a.Id, a.Consignee, a.Mobile, a.Province, a.City, a.District, a.Detail, a.IsDefault)).ToArray())).SingleOrDefaultAsync(ct);
    public async Task<AdminUserResponse?> UpdateUserStatusAsync(long id, UpdateAdminUserStatusRequest request, CancellationToken ct)
    {
        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return null;
        user.Enabled = request.Enabled;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new AdminUserResponse(user.Id, user.Username, user.Mobile, user.Nickname, user.Enabled, user.Roles.Where(r => r.Role != null).Select(r => r.Role!.Code).ToArray());
    }
    public async Task<AdminRoleResponse[]> RolesAsync(CancellationToken ct) => await db.Roles.AsNoTracking().Include(x => x.Permissions).ThenInclude(x => x.Permission).OrderBy(x => x.Id).Select(x => new AdminRoleResponse(x.Id, x.Name, x.Code, x.Permissions.Where(p => p.Permission != null).Select(p => p.Permission!.Code).ToArray())).ToArrayAsync(ct);
    public async Task<Permission[]> PermissionsAsync(CancellationToken ct) => await db.Permissions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(ct);
    public async Task<Role> SaveRoleAsync(SaveRoleRequest r, CancellationToken ct) { var x = await db.Roles.Include(x => x.Permissions).SingleOrDefaultAsync(x => x.Code == r.Code, ct); if (x is null) { x = new Role { Name = r.Name, Code = r.Code, CreatedAt = DateTime.UtcNow }; db.Roles.Add(x); } else { x.Name = r.Name; db.RolePermissions.RemoveRange(x.Permissions); } if (r.PermissionIds is { Length: > 0 }) x.Permissions = r.PermissionIds.Distinct().Select(id => new RolePermission { Role = x, PermissionId = id }).ToList(); await db.SaveChangesAsync(ct); return x; }
    public async Task<Permission> SavePermissionAsync(SavePermissionRequest r, CancellationToken ct) { var x = await db.Permissions.SingleOrDefaultAsync(x => x.Code == r.Code, ct); if (x is null) { x = new Permission { Name = r.Name, Code = r.Code, CreatedAt = DateTime.UtcNow }; db.Permissions.Add(x); } else x.Name = r.Name; await db.SaveChangesAsync(ct); return x; }
    public async Task<long> CreateCategoryAsync(SaveAdminCategoryRequest request, CancellationToken ct)
    {
        await ValidateCategoryParentAsync(request.ParentId, null, ct);
        var category = new Category { ParentId = request.ParentId, Name = request.Name.Trim(), IconUrl = request.IconUrl, SortOrder = request.SortOrder, Enabled = request.Enabled, CreatedAt = DateTime.UtcNow };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        await catalogCache.InvalidateCategoriesAsync(ct);
        return category.Id;
    }
    public async Task<bool> UpdateCategoryAsync(long id, SaveAdminCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (category is null) return false;
        await ValidateCategoryParentAsync(request.ParentId, id, ct);
        category.ParentId = request.ParentId; category.Name = request.Name.Trim(); category.IconUrl = request.IconUrl; category.SortOrder = request.SortOrder; category.Enabled = request.Enabled;
        await db.SaveChangesAsync(ct); await catalogCache.InvalidateCategoriesAsync(ct); return true;
    }
    public async Task<DeleteAdminCategoryResult> DeleteCategoryAsync(long id, CancellationToken ct)
    {
        var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (category is null) return DeleteAdminCategoryResult.NotFound;
        if (await db.Products.AnyAsync(x => x.CategoryId == id, ct) || await db.Categories.AnyAsync(x => x.ParentId == id, ct)) return DeleteAdminCategoryResult.InUse;
        await db.Categories.Where(x => x.Id == id).ExecuteDeleteAsync(ct); await catalogCache.InvalidateCategoriesAsync(ct); return DeleteAdminCategoryResult.Deleted;
    }
    public async Task<bool> UpdateCategoryStatusAsync(long id, UpdateAdminCategoryStatusRequest request, CancellationToken ct)
    {
        var affected = await db.Categories.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(c => c.Enabled, request.Enabled), ct);
        if (affected == 0) return false;
        await catalogCache.InvalidateCategoriesAsync(ct); return true;
    }
    private async Task ValidateCategoryParentAsync(long? parentId, long? currentId, CancellationToken ct)
    {
        if (parentId is null) return;
        if (currentId == parentId || !await db.Categories.AnyAsync(x => x.Id == parentId, ct)) throw new ArgumentException("父级分类不存在或不能设置为自身。");
    }
    public async Task<DeleteAdminRoleResult> DeleteRoleAsync(long id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (role is null) return DeleteAdminRoleResult.NotFound;
        if (await db.UserRoles.AnyAsync(x => x.RoleId == id, ct)) return DeleteAdminRoleResult.InUse;
        await db.Roles.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
        return DeleteAdminRoleResult.Deleted;
    }

    public async Task<AdminProductListResponse> ProductsAsync(int page, int pageSize, string? keyword, long? categoryId, bool? onSale, CancellationToken ct)
    { page = Math.Clamp(page, 1, 100000); pageSize = Math.Clamp(pageSize, 1, 100); var q = db.Products.AsNoTracking().AsQueryable(); if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(x => EF.Functions.ILike(x.Name, $"%{keyword.Trim()}%")); if (categoryId is not null) q = q.Where(x => x.CategoryId == categoryId); if (onSale is not null) q = q.Where(x => x.IsOnSale == onSale); var total = await q.CountAsync(ct); var items = await q.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AdminProductListItem(x.Id, x.Name, x.CategoryId, x.Category!.Name, x.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault() ?? x.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault(), x.IsOnSale, x.MinPrice, x.MaxPrice, x.Skus.Sum(s => s.Stock), x.Skus.Sum(s => s.LockedStock), x.SalesCount)).ToArrayAsync(ct); return new AdminProductListResponse(page, pageSize, total, items); }
    public async Task<AdminProductDetailResponse?> ProductAsync(long id, CancellationToken ct) => await db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminProductDetailResponse(x.Id, x.Name, x.CategoryId, x.Category!.Name, x.Subtitle, x.Description, x.MinPrice, x.MaxPrice, x.IsOnSale, x.Brand, x.Tags, x.Attributes, x.IsRecommended, x.Images.OrderBy(i => i.SortOrder).Select(i => new AdminProductImageResponse(i.Id, i.ImageUrl, i.SortOrder, i.IsPrimary)).ToArray(), x.Skus.OrderBy(s => s.Id).Select(s => new AdminProductSkuResponse(s.Id, s.SkuCode, s.Specification, s.Price, s.MarketPrice, s.Stock, s.LockedStock, s.Stock - s.LockedStock, s.Enabled)).ToArray())).SingleOrDefaultAsync(ct);
    public async Task<AdminProductInventoryResponse?> InventoryAsync(long id, CancellationToken ct) { var p = await db.Products.AsNoTracking().Include(x => x.Skus).SingleOrDefaultAsync(x => x.Id == id, ct); return p is null ? null : new AdminProductInventoryResponse(id, p.Skus.Sum(x => x.Stock), p.Skus.Sum(x => x.LockedStock), p.Skus.Sum(x => x.Stock - x.LockedStock), p.Skus.OrderBy(x => x.Id).Select(Sku).ToArray()); }
    public async Task<long> CreateProductAsync(SaveAdminProductRequest r, CancellationToken ct) { var p = new Product { CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; Apply(p, r); AddChildren(p, r); db.Products.Add(p); await db.SaveChangesAsync(ct); return p.Id; }
    public async Task<bool> UpdateProductAsync(long id, SaveAdminProductRequest r, CancellationToken ct) { var p = await db.Products.Include(x => x.Images).Include(x => x.Skus).SingleOrDefaultAsync(x => x.Id == id, ct); if (p is null) return false; Apply(p, r); db.ProductImages.RemoveRange(p.Images); foreach (var x in r.Images ?? []) p.Images.Add(new ProductImage { ImageUrl = x.ImageUrl, SortOrder = x.SortOrder, IsPrimary = x.IsPrimary }); var incoming = (r.Skus ?? []).Where(x => x.Id is not null).ToDictionary(x => x.Id!.Value); foreach (var s in p.Skus.ToArray()) { if (incoming.TryGetValue(s.Id, out var x)) { s.SkuCode = x.SkuCode; s.Specification = x.Specification; s.Price = x.Price; s.MarketPrice = x.MarketPrice; s.Stock = Math.Max(x.Stock, s.LockedStock); s.Enabled = x.Enabled; s.UpdatedAt = DateTime.UtcNow; incoming.Remove(s.Id); } else if (s.LockedStock == 0) db.ProductSkus.Remove(s); } foreach (var x in incoming.Values) p.Skus.Add(new ProductSku { SkuCode = x.SkuCode, Specification = x.Specification, Price = x.Price, MarketPrice = x.MarketPrice, Stock = x.Stock, Enabled = x.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync(ct); return true; }
    public async Task<bool> DeleteProductAsync(long id, CancellationToken ct) => await db.Products.Where(x => x.Id == id).ExecuteDeleteAsync(ct) == 1;
    public async Task<bool> SetProductSaleAsync(long id, bool onSale, CancellationToken ct) => await db.Products.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsOnSale, onSale).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct) == 1;
    private static void Apply(Product p, SaveAdminProductRequest r) { p.Name = r.Name.Trim(); p.CategoryId = r.CategoryId; p.Subtitle = r.Subtitle; p.Description = r.Description; p.MinPrice = r.MinPrice; p.MaxPrice = r.MaxPrice; p.IsOnSale = r.IsOnSale; p.Brand = r.Brand; p.Tags = string.IsNullOrWhiteSpace(r.Tags) ? "[]" : r.Tags!; p.Attributes = string.IsNullOrWhiteSpace(r.Attributes) ? "{}" : r.Attributes!; p.IsRecommended = r.IsRecommended; p.UpdatedAt = DateTime.UtcNow; }
    private static void AddChildren(Product p, SaveAdminProductRequest r) { foreach (var x in r.Images ?? []) p.Images.Add(new ProductImage { ImageUrl = x.ImageUrl, SortOrder = x.SortOrder, IsPrimary = x.IsPrimary }); foreach (var x in r.Skus ?? []) p.Skus.Add(new ProductSku { SkuCode = x.SkuCode, Specification = x.Specification, Price = x.Price, MarketPrice = x.MarketPrice, Stock = x.Stock, Enabled = x.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); }
    private static AdminProductSkuResponse Sku(ProductSku s) => new(s.Id, s.SkuCode, s.Specification, s.Price, s.MarketPrice, s.Stock, s.LockedStock, s.Stock - s.LockedStock, s.Enabled);
    public async Task<AdminOrderListResponse> OrdersAsync(int page, int pageSize, string? keyword, OrderStatus? status, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = long.TryParse(value, out var userId)
                ? query.Where(x => EF.Functions.ILike(x.OrderNo, $"%{value}%") || x.UserId == userId)
                : query.Where(x => EF.Functions.ILike(x.OrderNo, $"%{value}%"));
        }
        var total = await query.CountAsync(ct);
        var orders = await query.Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        var items = orders.Select(x => new AdminOrderListItem(
            x.Id, x.OrderNo, x.UserId, x.Status, x.PayableAmount, x.CreatedAt,
            x.Items.Count == 0 ? string.Empty : x.Items.Count == 1
                ? $"{x.Items[0].ProductName} × {x.Items[0].Quantity}"
                : $"{x.Items[0].ProductName} 等 {x.Items.Count} 件",
            x.Items.Sum(i => i.Quantity))).ToArray();
        return new(page, pageSize, total, items);
    }

    public async Task<AdminOrderDetailResponse?> OrderAsync(long id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
        return order is null ? null : new AdminOrderDetailResponse(order.Id, order.OrderNo, order.UserId, order.Status,
            order.GoodsAmount, order.FreightAmount, order.DiscountAmount, order.PayableAmount, order.Consignee,
            order.Mobile, order.Address, order.CreatedAt, order.PaidAt, order.ShippedAt, order.FinishedAt,
            order.CancelledAt, order.Remark, order.PaymentExpiredAt, order.ShippingCompany, order.TrackingNo,
            order.Items.Select(x => new OrderItemResponse(x.Id, x.ProductId, x.SkuId, x.ProductName, x.SkuCode,
                x.UnitPrice, x.Quantity, x.TotalAmount)).ToArray());
    }
    public async Task<bool> ShipOrderAsync(long id, ShipOrderRequest r, CancellationToken ct) { var x = await db.Orders.SingleOrDefaultAsync(x => x.Id == id && x.Status == OrderStatus.Paid, ct); if (x is null) return false; x.Status = OrderStatus.Shipped; x.ShippedAt = DateTime.UtcNow; x.UpdatedAt = DateTime.UtcNow; x.ShippingCompany = r.Company; x.TrackingNo = r.TrackingNo; db.LogisticsTraces.Add(new LogisticsTrace { OrderId = id, Company = r.Company, TrackingNo = r.TrackingNo, Status = "SHIPPED", TraceJson = "[]", UpdatedAt = DateTime.UtcNow }); await db.SaveChangesAsync(ct); return true; }
}
