using Mall.Api.Domain.Identity;
using Mall.Api.Domain.Orders;
using Mall.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Application.Admin;

public sealed class AdminUseCases(MallDbContext db)
{
    public async Task<AdminUserResponse[]> UsersAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Include(x => x.Roles).ThenInclude(x => x.Role).OrderBy(x => x.Id)
            .Select(x => new AdminUserResponse(x.Id, x.Username, x.Mobile, x.Nickname, x.Enabled, x.Roles.Where(r => r.Role != null).Select(r => r.Role!.Code).ToArray()))
            .ToArrayAsync(cancellationToken);

    public async Task<AdminRoleResponse[]> RolesAsync(CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().Include(x => x.Permissions).ThenInclude(x => x.Permission).OrderBy(x => x.Id)
            .Select(x => new AdminRoleResponse(x.Id, x.Name, x.Code, x.Permissions.Where(p => p.Permission != null).Select(p => p.Permission!.Code).ToArray()))
            .ToArrayAsync(cancellationToken);

    public async Task<Permission[]> PermissionsAsync(CancellationToken cancellationToken) =>
        await db.Permissions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(cancellationToken);

    public async Task<Role> SaveRoleAsync(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.Include(x => x.Permissions).SingleOrDefaultAsync(x => x.Code == request.Code, cancellationToken);
        if (role is null) { role = new Role { Name = request.Name, Code = request.Code, CreatedAt = DateTime.UtcNow }; db.Roles.Add(role); }
        else { role.Name = request.Name; db.RolePermissions.RemoveRange(role.Permissions); }
        if (request.PermissionIds is { Length: > 0 })
            role.Permissions = request.PermissionIds.Distinct().Select(id => new RolePermission { Role = role, PermissionId = id }).ToList();
        await db.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task<Permission> SavePermissionAsync(SavePermissionRequest request, CancellationToken cancellationToken)
    {
        var permission = await db.Permissions.SingleOrDefaultAsync(x => x.Code == request.Code, cancellationToken);
        if (permission is null) { permission = new Permission { Name = request.Name, Code = request.Code, CreatedAt = DateTime.UtcNow }; db.Permissions.Add(permission); }
        else permission.Name = request.Name;
        await db.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public async Task<AdminProductResponse[]> ProductsAsync(CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking().OrderByDescending(x => x.Id).Select(x => new AdminProductResponse(x.Id, x.Name, x.IsOnSale, x.MinPrice, x.MaxPrice)).ToArrayAsync(cancellationToken);

    public async Task<bool> SetProductSaleAsync(long id, bool onSale, CancellationToken cancellationToken)
    {
        var affected = await db.Products.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(p => p.IsOnSale, onSale).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);
        return affected == 1;
    }

    public async Task<AdminOrderResponse[]> OrdersAsync(CancellationToken cancellationToken) =>
        await db.Orders.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => new AdminOrderResponse(x.Id, x.OrderNo, x.UserId, x.Status, x.PayableAmount, x.CreatedAt)).ToArrayAsync(cancellationToken);

    public async Task<bool> ShipOrderAsync(long id, ShipOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id && x.Status == OrderStatus.Paid, cancellationToken);
        if (order is null) return false;
        order.Status = OrderStatus.Shipped; order.ShippedAt = DateTime.UtcNow; order.UpdatedAt = DateTime.UtcNow;
        order.ShippingCompany = request.Company; order.TrackingNo = request.TrackingNo;
        db.LogisticsTraces.Add(new LogisticsTrace { OrderId = id, Company = request.Company, TrackingNo = request.TrackingNo, Status = "SHIPPED", TraceJson = "[]", UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
