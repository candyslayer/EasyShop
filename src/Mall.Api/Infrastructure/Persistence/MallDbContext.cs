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
    public DbSet<ProductFavorite> ProductFavorites => Set<ProductFavorite>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AfterSale> AfterSales => Set<AfterSale>();
    public DbSet<LogisticsTrace> LogisticsTraces => Set<LogisticsTrace>();
    public DbSet<OrderReview> OrderReviews => Set<OrderReview>();
    public DbSet<FreightRule> FreightRules => Set<FreightRule>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<PaymentReconciliation> PaymentReconciliations => Set<PaymentReconciliation>();
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
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<UserCoupon> UserCoupons => Set<UserCoupon>();
    public DbSet<MemberPrice> MemberPrices => Set<MemberPrice>();
    public DbSet<PointsAccount> PointsAccounts => Set<PointsAccount>();
    public DbSet<PointsTransaction> PointsTransactions => Set<PointsTransaction>();
    public DbSet<FullReductionRule> FullReductionRules => Set<FullReductionRule>();
    public DbSet<BargainActivity> BargainActivities => Set<BargainActivity>();
    public DbSet<BargainRecord> BargainRecords => Set<BargainRecord>();
    public DbSet<PresaleActivity> PresaleActivities => Set<PresaleActivity>();
    public DbSet<GiftRule> GiftRules => Set<GiftRule>();
    public DbSet<LimitedDiscount> LimitedDiscounts => Set<LimitedDiscount>();
    public DbSet<PromotionOrder> PromotionOrders => Set<PromotionOrder>();

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
            entity.Property(x => x.Tags).HasColumnType("jsonb"); entity.Property(x => x.Attributes).HasColumnType("jsonb"); entity.Property(x => x.RatingAverage).HasPrecision(4, 2);
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
        modelBuilder.Entity<ProductFavorite>(entity =>
        {
            entity.ToTable("mall_product_favorite"); entity.HasKey(x => x.Id); entity.Property(x => x.Id).UseIdentityByDefaultColumn();
            entity.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique(); entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
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
        modelBuilder.Entity<AfterSale>(e => { e.ToTable("order_after_sale"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.Amount).HasPrecision(18,2); e.Property(x => x.Status).HasConversion<string>(); e.HasIndex(x => new { x.OrderId, x.Status }); });
        modelBuilder.Entity<LogisticsTrace>(e => { e.ToTable("order_logistics_trace"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.HasIndex(x => x.OrderId).IsUnique(); });
        modelBuilder.Entity<OrderReview>(e => { e.ToTable("order_review"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.HasIndex(x => new { x.OrderId, x.UserId }).IsUnique(); });
        modelBuilder.Entity<FreightRule>(e => { e.ToTable("order_freight_rule"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.BaseAmount).HasPrecision(18,2); e.Property(x => x.FreeThreshold).HasPrecision(18,2); });

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
        modelBuilder.Entity<PaymentRefund>(e => { e.ToTable("payment_refund"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.RefundAmount).HasPrecision(18,2); e.HasIndex(x => x.RefundNo).IsUnique(); e.HasIndex(x => new { x.OrderId, x.Status }); });
        modelBuilder.Entity<PaymentReconciliation>(e => { e.ToTable("payment_reconciliation"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.TotalAmount).HasPrecision(18,2); e.HasIndex(x => new { x.TradeDate, x.Channel }).IsUnique(); });

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
        modelBuilder.Entity<Coupon>(e => { e.ToTable("promotion_coupon"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.ThresholdAmount).HasPrecision(18,2); e.Property(x => x.DiscountAmount).HasPrecision(18,2); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<UserCoupon>(e => { e.ToTable("promotion_user_coupon"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.HasIndex(x => new { x.CouponId, x.UserId, x.Status }); });
        modelBuilder.Entity<MemberPrice>(e => { e.ToTable("promotion_member_price"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.Price).HasPrecision(18,2); e.HasIndex(x => new { x.UserId, x.SkuId, x.Enabled }); });
        modelBuilder.Entity<PointsAccount>(e => { e.ToTable("promotion_points_account"); e.HasKey(x => x.UserId); });
        modelBuilder.Entity<PointsTransaction>(e => { e.ToTable("promotion_points_transaction"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.HasIndex(x => new { x.UserId, x.CreatedAt }); });
        modelBuilder.Entity<FullReductionRule>(e => { e.ToTable("promotion_full_reduction_rule"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.ThresholdAmount).HasPrecision(18,2); e.Property(x => x.ReductionAmount).HasPrecision(18,2); });
        modelBuilder.Entity<BargainActivity>(e => { e.ToTable("promotion_bargain_activity"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.OriginalPrice).HasPrecision(18,2); e.Property(x => x.LowestPrice).HasPrecision(18,2); e.Property(x => x.StepAmount).HasPrecision(18,2); });
        modelBuilder.Entity<BargainRecord>(e => { e.ToTable("promotion_bargain_record"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.CurrentPrice).HasPrecision(18,2); e.HasIndex(x => new { x.ActivityId, x.UserId }).IsUnique(); });
        modelBuilder.Entity<PresaleActivity>(e => { e.ToTable("promotion_presale_activity"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.DepositAmount).HasPrecision(18,2); e.Property(x => x.FinalAmount).HasPrecision(18,2); });
        modelBuilder.Entity<GiftRule>(e => { e.ToTable("promotion_gift_rule"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); });
        modelBuilder.Entity<LimitedDiscount>(e => { e.ToTable("promotion_limited_discount"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.DiscountPrice).HasPrecision(18,2); });
        modelBuilder.Entity<PromotionOrder>(e => { e.ToTable("promotion_order"); e.HasKey(x => x.Id); e.Property(x => x.Id).UseIdentityByDefaultColumn(); e.Property(x => x.Type).HasConversion<string>(); e.Property(x => x.Status).HasConversion<string>(); e.HasIndex(x => new { x.OrderId, x.Type }).IsUnique(); });
    }
}
