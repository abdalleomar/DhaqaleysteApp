using DhaqaleysteApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductMedia> ProductMedia { get; set; }
        public DbSet<CompanySettings> CompanySettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(user =>
            {
                user.Property(x => x.FullName).HasMaxLength(150);
                user.Property(x => x.Email).HasMaxLength(256);
                user.Property(x => x.Phone).HasMaxLength(32);
                user.Property(x => x.Role).HasMaxLength(20);
                user.Property(x => x.SecurityStamp).HasMaxLength(64);
                user.Property(x => x.IsActive).HasDefaultValue(true);

                user.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] <> ''");
                user.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] <> ''");
            });

            modelBuilder.Entity<Product>(product =>
            {
                product.Property(x => x.ProductName).HasMaxLength(200);
                product.Property(x => x.Contact).HasMaxLength(100);
                product.Property(x => x.Location).HasMaxLength(200);
                product.Property(x => x.Price).HasPrecision(18, 2);
                product.Property(x => x.IsAvailable).HasDefaultValue(true);
                product.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                product.Ignore(x => x.InStock);

                // Deleted products disappear from every query unless IgnoreQueryFilters() is used.
                product.HasQueryFilter(x => !x.IsDeleted);

                product.HasOne(x => x.Category)
                    .WithMany(x => x.Products)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                product.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
                product.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);

                product.HasIndex(x => new { x.IsDeleted, x.CreatedAtUtc });
                product.HasIndex(x => x.CategoryId);
                product.HasIndex(x => x.Price);
                product.HasIndex(x => x.IsFeatured).HasFilter("[IsFeatured] = 1");
            });

            modelBuilder.Entity<ProductMedia>(media =>
            {
                media.Property(x => x.Kind).HasMaxLength(10);
                media.Property(x => x.Url).HasMaxLength(500);
                media.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

                media.HasOne(x => x.Product)
                    .WithMany(x => x.Media)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                media.HasIndex(x => new { x.ProductId, x.SortOrder });

                // Matches the Product filter, so media of deleted products is hidden too.
                media.HasQueryFilter(x => !x.Product!.IsDeleted);
            });

            modelBuilder.Entity<Category>(category =>
            {
                category.Property(x => x.Name).HasMaxLength(100);
                category.Property(x => x.NameSo).HasMaxLength(100);
                category.Property(x => x.Icon).HasMaxLength(50);
                category.Property(x => x.IsActive).HasDefaultValue(true);
                category.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

                category.HasIndex(x => x.Name).IsUnique();
                category.HasIndex(x => new { x.IsActive, x.SortOrder });
            });

            modelBuilder.Entity<CompanySettings>(company =>
            {
                company.Property(x => x.CompanySettingsId).ValueGeneratedNever();
                company.Property(x => x.CompanyName).HasMaxLength(150);
                company.Property(x => x.AboutEn).HasMaxLength(2000);
                company.Property(x => x.AboutSo).HasMaxLength(2000);
                company.Property(x => x.Phone).HasMaxLength(32);
                company.Property(x => x.WhatsApp).HasMaxLength(32);
                company.Property(x => x.Email).HasMaxLength(256);
                company.Property(x => x.Address).HasMaxLength(300);
                company.Property(x => x.City).HasMaxLength(100);
                company.Property(x => x.WorkingHours).HasMaxLength(200);
                company.Property(x => x.MapUrl).HasMaxLength(500);
                company.Property(x => x.LogoUrl).HasMaxLength(500);
                company.Property(x => x.FacebookUrl).HasMaxLength(500);
                company.Property(x => x.InstagramUrl).HasMaxLength(500);
                company.Property(x => x.TikTokUrl).HasMaxLength(500);
                company.Property(x => x.WebsiteUrl).HasMaxLength(500);
                company.Property(x => x.CurrencyCode).HasMaxLength(3);

                company.HasData(new Models.CompanySettings { CompanySettingsId = Models.CompanySettings.SingletonId });
            });

            modelBuilder.Entity<RefreshToken>(token =>
            {
                token.Property(x => x.TokenHash).HasMaxLength(64);
                token.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
                token.Property(x => x.CreatedByIp).HasMaxLength(64);
                token.Ignore(x => x.IsActive);

                token.HasIndex(x => x.TokenHash).IsUnique();

                token.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
