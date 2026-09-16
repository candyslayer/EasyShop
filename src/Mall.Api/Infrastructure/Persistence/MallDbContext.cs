using Mall.Api.Domain.Cart;
using Mall.Api.Domain.Catalog;
using Mall.Api.Domain.Identity;
using Mall.Api.Domain.Orders;
using Mall.Api.Domain.Payments;
using Mall.Api.Domain.Users;
using Mall.Api.Domain.Promotions;
using Microsoft.EntityFrameworkCore;

namespace Mall.Api.Infrastructure.Persistence;

public sealed class MallDbContext(DbContextOptions<MallDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSku> ProductSkus => Set<ProductSku>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<GroupBuyActivity> GroupBuyActivities => Set<GroupBuyActivity>();
    public DbSet<GroupBuyTeam> GroupBuyTeams => Set<GroupBuyTeam>();
    public DbSet<GroupBuyMember> GroupBuyMembers => Set<GroupBuyMember>();
    public DbSet<FlashSaleActivity> FlashSaleActivities => Set<FlashSaleActivity>();
    public DbSet<FlashSaleOrder> FlashSaleOrders => Set<FlashSaleOrder>();
    public DbSet<Distributor> Distributors => Set<Distributor>();
    public DbSet<DistributionCommission> DistributionCommissions => Set<DistributionCommission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("sys_user");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.Nickname).HasMaxLength(100).IsRequired();
            entity.Property(x => x.WechatOpenId).HasMaxLength(100);
            entity.Property(x => x.WechatUnionId).HasMaxLength(100);
            entity.HasIndex(x => x.WechatOpenId).IsUnique().HasFilter("wechat_open_id IS NOT NULL");
            entity.HasIndex(x => x.Mobile).IsUnique().HasFilter("mobile IS NOT NULL");
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("user_address");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.UserId, x.IsDefault });
            entity.HasOne(x => x.User).WithMany(x => x.Addresses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("mall_category");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.ParentId, x.SortOrder });
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("mall_product");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.MinPrice).HasPrecision(18, 2);
            entity.Property(x => x.MaxPrice).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.CategoryId, x.IsOnSale });
            entity.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductSku>(entity =>
        {
            entity.ToTable("mall_product_sku");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.Specification).HasColumnType("jsonb");
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.MarketPrice).HasPrecision(18, 2);
            entity.HasIndex(x => x.SkuCode).IsUnique();
            entity.HasIndex(x => new { x.ProductId, x.Enabled });
            entity.HasOne(x => x.Product).WithMany(x => x.Skus).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("mall_product_image");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.ProductId, x.SortOrder });
            entity.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("mall_cart");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.UserId, x.SkuId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Checked });
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("mall_order");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.GoodsAmount).HasPrecision(18, 2);
            entity.Property(x => x.FreightAmount).HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            entity.Property(x => x.PayableAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.OrderNo).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("mall_order_item");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.OrderId);
            entity.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("payment_record");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasIndex(x => x.PaymentNo).IsUnique();
            entity.HasIndex(x => new { x.OrderId, x.Status });
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("sys_role");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("sys_permission");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("sys_user_role");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("sys_role_permission");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission).WithMany(x => x.Roles).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupBuyActivity>(entity =>
        {
            entity.ToTable("promotion_group_buy_activity"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.LeaderPrice).HasPrecision(18, 2); entity.Property(x => x.MemberPrice).HasPrecision(18, 2);
            entity.HasIndex(x => new { x.Enabled, x.StartsAt, x.EndsAt });
        });
        modelBuilder.Entity<GroupBuyTeam>(entity =>
        {
            entity.ToTable("promotion_group_buy_team"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); entity.HasIndex(x => new { x.ActivityId, x.Status, x.ExpiresAt });
        });
        modelBuilder.Entity<GroupBuyMember>(entity =>
        {
            entity.ToTable("promotion_group_buy_member"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique(); entity.HasIndex(x => x.OrderId).IsUnique();
        });
        modelBuilder.Entity<FlashSaleActivity>(entity =>
        {
            entity.ToTable("promotion_flash_sale_activity"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.SalePrice).HasPrecision(18, 2); entity.HasIndex(x => new { x.Enabled, x.StartsAt, x.EndsAt });
        });
        modelBuilder.Entity<FlashSaleOrder>(entity =>
        {
            entity.ToTable("promotion_flash_sale_order"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.ActivityId, x.UserId }).IsUnique(); entity.HasIndex(x => x.OrderId).IsUnique();
        });
        modelBuilder.Entity<Distributor>(entity =>
        {
            entity.ToTable("distribution_distributor"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.CommissionRate).HasPrecision(5, 4); entity.HasIndex(x => x.UserId).IsUnique(); entity.HasIndex(x => x.InviteCode).IsUnique();
        });
        modelBuilder.Entity<DistributionCommission>(entity =>
        {
            entity.ToTable("distribution_commission"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.Property(x => x.Rate).HasPrecision(5, 4); entity.Property(x => x.Amount).HasPrecision(18, 2); entity.HasIndex(x => x.OrderId).IsUnique();
        });
    }
}
