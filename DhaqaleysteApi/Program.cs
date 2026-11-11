using System.Security.Cryptography;
using System.Threading.RateLimiting;
using DhaqaleysteApi.Data;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Options;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Configuration (secrets come from user-secrets or environment variables) ----------

var connectionString = config.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not set. Use 'dotnet user-secrets' in development " +
        "or the ConnectionStrings__DefaultConnection environment variable in production.");

builder.Services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
builder.Services.Configure<StorageOptions>(config.GetSection(StorageOptions.Section));
builder.Services.Configure<BootstrapOptions>(config.GetSection(BootstrapOptions.Section));
builder.Services.Configure<EmailOptions>(config.GetSection(EmailOptions.Section));

var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Jwt:SigningKey is not set. Supply a random value of at least 32 characters.");

    // Development convenience only: a random key per run. Sign-ins do not survive a restart.
    jwt.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    builder.Services.PostConfigure<JwtOptions>(o => o.SigningKey = jwt.SigningKey);
    Console.WriteLine("WARNING: Jwt:SigningKey not set; using a temporary development key.");
}
if (jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");

// ---------- Services ----------

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = TokenService.CreateKey(jwt.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = TokenService.NameClaim,
            RoleClaimType = TokenService.RoleClaim
        };
        options.Events = new JwtBearerEvents
        {
            // Reject tokens for accounts that were deactivated, demoted or had their password changed.
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var userId = int.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : 0;
                var stamp = principal.FindFirst(TokenService.SecurityStampClaim)?.Value;

                var valid = await db.Users.AsNoTracking().AnyAsync(u =>
                    u.UserId == userId && u.IsActive && u.SecurityStamp == stamp);

                if (!valid)
                    context.Fail("Account is no longer valid.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Staff, p => p.RequireAuthenticatedUser().RequireRole(Roles.Admin, Roles.Staff));
    options.AddPolicy(AuthPolicies.Admin, p => p.RequireAuthenticatedUser().RequireRole(Roles.Admin));

    // Anything not explicitly marked [AllowAnonymous] requires a signed-in user.
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

var authPermitPerMinute = config.GetValue("RateLimits:AuthPerMinute", 10);
var uploadPermitPerMinute = config.GetValue("RateLimits:UploadPerMinute", 30);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Auth, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    options.AddPolicy(RateLimitPolicies.Upload, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = uploadPermitPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

// ---------- Pipeline ----------

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(error, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

    // Never leak exception details to clients.
    await Results.Problem(
        title: "Something went wrong.",
        detail: "An unexpected error occurred. Please try again.",
        statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
}));
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// Only image and video types are ever served, so an uploaded file can never run as a web page or script.
var mediaTypes = new FileExtensionContentTypeProvider(new Dictionary<string, string>(FileStorageService.ContentTypes));
var storage = app.Services.GetRequiredService<FileStorageService>();

app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = FileStorageService.PublicPrefix,
    FileProvider = new PhysicalFileProvider(storage.RootPath),
    ContentTypeProvider = mediaTypes,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=604800"
});

// Files uploaded before this version live in wwwroot/images and wwwroot/videos.
if (Directory.Exists(app.Environment.WebRootPath))
    app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = mediaTypes });

// Routing runs after static files, so a real media file is served before the 404 fallback route is chosen.
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Unknown URLs (including missing media files) are 404, not "please sign in".
app.MapFallback("{*path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found")).AllowAnonymous();

try
{
    await AdminBootstrapper.RunAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Admin bootstrap failed. Has the database been migrated ('dotnet ef database update')?");
}

app.Run();

/// <summary>Exposed for integration tests.</summary>
public partial class Program { }
