using DhaqaleysteApi.Data;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Tests;

/// <summary>
/// Runs the real API against a throw-away SQL Server database.
/// Set DHAQ_TEST_SQLSERVER to point at a server (database name is generated per run).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@dhaqaleyste.test";
    public const string AdminPassword = "Admin12345";

    private readonly string _connectionString;
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "dhaq-tests-" + Guid.NewGuid().ToString("N"));

    public ApiFactory()
    {
        var server = Environment.GetEnvironmentVariable("DHAQ_TEST_SQLSERVER")
                     ?? "Server=localhost,1433;User Id=sa;Password=Dev_Passw0rd!;TrustServerCertificate=True";
        _connectionString = $"{server};Database=DhaqTest_{Guid.NewGuid():N}";
    }

    /// <summary>Emails the API "sent" during the tests.</summary>
    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services => services.AddSingleton<IEmailSender>(Emails));
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-long-enough-1234567890");
        builder.UseSetting("Bootstrap:AdminEmail", AdminEmail);
        builder.UseSetting("Bootstrap:AdminPassword", AdminPassword);
        builder.UseSetting("Storage:RootPath", _storageRoot);
        builder.UseSetting("RateLimits:AuthPerMinute", "1000");
        builder.UseSetting("RateLimits:UploadPerMinute", "1000");
    }

    public async Task InitializeAsync()
    {
        // Migrate before the host starts so the admin bootstrap finds the schema.
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var db = CreateDbContext())
            await db.Database.EnsureDeletedAsync();

        if (Directory.Exists(_storageRoot))
            Directory.Delete(_storageRoot, recursive: true);

        await base.DisposeAsync();
    }
}

public class CapturingEmailSender : IEmailSender
{
    private readonly List<(string To, string Subject, string Body)> _sent = new();

    public Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        lock (_sent)
            _sent.Add((to, subject, body));
        return Task.FromResult(true);
    }

    public IReadOnlyList<(string To, string Subject, string Body)> SentTo(string to)
    {
        lock (_sent)
            return _sent.Where(m => m.To == to).ToList();
    }
}
