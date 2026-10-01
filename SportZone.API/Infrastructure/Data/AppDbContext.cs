using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SportZone.Domain.Entities;

namespace SportZone.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser>(options)
    {
        // ===== DbSet =====
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductSize> ProductSizes { get; set; } = null!; // OK
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<Feature> Features { get; set; } = null!;
        public DbSet<ComboItem> ComboItems { get; set; } = null!;

        // --- CẤU HÌNH TẮT CẢNH BÁO ---
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
            });
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- Filter (Soft Delete) ---
            builder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);

            // --- SEED ROLES ---
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "member-id", Name = "Member", NormalizedName = "MEMBER" },
                new IdentityRole { Id = "staff-id", Name = "Staff", NormalizedName = "STAFF" },
                new IdentityRole { Id = "admin-id", Name = "Admin", NormalizedName = "ADMIN" }
            );

            // --- SEED ADMIN USER ---
            var adminId = "admin-id";
            var adminUser = new AppUser
            {
                Id = adminId,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@gmail.com",
                EmailConfirmed = true,
                SecurityStamp = "STATIC-GUID-SEC-12345",
                ConcurrencyStamp = "STATIC-GUID-CON-67890",
            };

            // Password Hash cứng (Pass: Dat6112005nt!)
            adminUser.PasswordHash = "AQAAAAIAAYagAAAAELuWf8X8+7J8J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+J8+A==";

            builder.Entity<AppUser>().HasData(adminUser);

            // --- ASSIGN ROLE ---
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = "admin-id",
                    UserId = adminId
                }
            );

            // --- Cấu hình Entity ---
            builder.Entity<Order>(entity =>
            {
                entity.Property(o => o.Status).HasConversion<string>();
            });

            // === ProductSize ===
            builder.Entity<ProductSize>(entity =>
            {
                // Khi xóa Product -> Xóa hết ProductSize
                // entity.HasQueryFilter(ps => ps.IsActive);
                entity.HasOne(v => v.Product)
                      .WithMany(p => p.ProductSizes)
                      .HasForeignKey(v => v.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === CartItem (SỬA LẠI QUAN TRỌNG) ===
            builder.Entity<CartItem>(entity =>
            {
                // KHÓA CHÍNH: Phải là CartId + ProductSizeId 
                // (Để cho phép 1 giỏ hàng có 2 dòng: Giày A size 40, Giày A size 41)
                entity.HasKey(ci => new { ci.CartId, ci.ProductSizeId });

                entity.HasOne(ci => ci.Cart)
                      .WithMany(c => c.Items)
                      .HasForeignKey(ci => ci.CartId);

                // Liên kết thông tin chung
                entity.HasOne(ci => ci.Product)
                      .WithMany(p => p.CartItems)
                      .HasForeignKey(ci => ci.ProductId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Liên kết Size cụ thể
                entity.HasOne(ci => ci.ProductSize)
                      .WithMany() // Một Size có thể nằm trong nhiều CartItem
                      .HasForeignKey(ci => ci.ProductSizeId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // === OrderItem (SỬA LẠI QUAN TRỌNG) ===
            builder.Entity<OrderItem>(entity =>
            {
                // KHÓA CHÍNH: OrderId + ProductSizeId
                entity.HasKey(oi => new { oi.OrderId, oi.ProductSizeId });

                entity.HasOne(oi => oi.Order).WithMany(o => o.Items).HasForeignKey(oi => oi.OrderId);

                entity.HasOne(oi => oi.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Liên kết Size cụ thể
                entity.HasOne(oi => oi.ProductSize)
                      .WithMany()
                      .HasForeignKey(oi => oi.ProductSizeId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // === Payment ===
            builder.Entity<Payment>(entity =>
            {
                entity.HasOne(p => p.Order).WithOne(o => o.Payment).HasForeignKey<Payment>(p => p.OrderId);
                entity.Property(p => p.PaymentMethod).HasConversion<string>();
                entity.Property(p => p.PaymentStatus).HasConversion<string>();
            });

            // === Product & Feature (Many-to-Many) ===
            builder.Entity<Feature>(entity =>
            {
                entity.HasMany(f => f.Products)
                      .WithMany(p => p.Features)
                      .UsingEntity<Dictionary<string, object>>(
                          "Product_Features", 
                          j => j.HasOne<Product>().WithMany().HasForeignKey("ProductId"),
                          j => j.HasOne<Feature>().WithMany().HasForeignKey("FeatureId")
                      );
            });

            // === ComboItem ===
            builder.Entity<ComboItem>(entity =>
            {
                entity.HasKey(ci => ci.Id);

                entity.HasOne(ci => ci.ComboProduct)
                      .WithMany(p => p.ComboItems)
                      .HasForeignKey(ci => ci.ComboProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.ComponentProduct)
                      .WithMany(p => p.PartOfCombos)
                      .HasForeignKey(ci => ci.ComponentProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


        }
    }
}