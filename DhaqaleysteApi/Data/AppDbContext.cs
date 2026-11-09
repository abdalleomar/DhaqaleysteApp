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
