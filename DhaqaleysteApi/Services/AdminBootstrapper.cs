using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DhaqaleysteApi.Services
{
    /// <summary>
    /// Creates the first administrator from configuration when the database has no active admin.
    /// This replaces the old hard-coded admin/1234 login.
    /// </summary>
    public static class AdminBootstrapper
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<BootstrapOptions>>().Value;
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminBootstrap");

            if (await db.Users.AnyAsync(x => x.Role == Roles.Admin && x.IsActive))
                return;

            if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
            {
                logger.LogWarning("No active administrator exists. Set Bootstrap:AdminEmail and Bootstrap:AdminPassword " +
                                  "(user-secrets or environment variables) and restart to create one.");
                return;
            }

            var passwordError = PasswordRules.Validate(options.AdminPassword);
            if (passwordError != null)
            {
                logger.LogError("Bootstrap admin not created: {Error}", passwordError);
                return;
            }

            var email = options.AdminEmail.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                user = new User { Email = email };
                db.Users.Add(user);
            }

            user.FullName = string.IsNullOrWhiteSpace(options.AdminFullName) ? "Administrator" : options.AdminFullName.Trim();
            user.Role = Roles.Admin;
            user.IsActive = true;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.PasswordHash = hasher.HashPassword(user, options.AdminPassword);
            user.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            logger.LogInformation("Bootstrap administrator {Email} is ready.", email);
        }
    }
}
