using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>The company's public contact details. Anyone can read them; only admins change them.</summary>
    [Route("api/company")]
    public class CompanyController : ApiControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FileStorageService _storage;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(AppDbContext db, FileStorageService storage, ILogger<CompanyController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<CompanySettingsDto> Get(CancellationToken ct) =>
            CompanySettingsDto.From(await LoadAsync(ct));

        [HttpPut]
        [Authorize(Policy = AuthPolicies.Admin)]
        public async Task<ActionResult<CompanySettingsDto>> Update(CompanySettingsRequest request, CancellationToken ct)
        {
            var settings = await LoadAsync(ct);

            var logo = request.LogoUrl?.Trim() ?? "";
            if (logo.Length > 0 && logo != settings.LogoUrl && !FileStorageService.IsOwnMediaPath(logo, MediaKind.Image))
                return ValidationError(nameof(request.LogoUrl), "The logo must be uploaded through /api/upload/image.");

            foreach (var (field, value) in new[]
            {
                (nameof(request.MapUrl), request.MapUrl),
                (nameof(request.FacebookUrl), request.FacebookUrl),
                (nameof(request.InstagramUrl), request.InstagramUrl),
                (nameof(request.TikTokUrl), request.TikTokUrl),
                (nameof(request.WebsiteUrl), request.WebsiteUrl)
            })
            {
                if (!IsHttpsUrlOrEmpty(value))
                    return ValidationError(field, "Links must be full https:// addresses.");
            }

            var oldLogo = settings.LogoUrl;

            settings.CompanyName = request.CompanyName.Trim();
            settings.AboutEn = request.AboutEn?.Trim() ?? "";
            settings.AboutSo = request.AboutSo?.Trim() ?? "";
            settings.Phone = request.Phone?.Trim() ?? "";
            settings.WhatsApp = request.WhatsApp?.Trim() ?? "";
            settings.Email = request.Email?.Trim() ?? "";
            settings.Address = request.Address?.Trim() ?? "";
            settings.City = request.City?.Trim() ?? "";
            settings.WorkingHours = request.WorkingHours?.Trim() ?? "";
            settings.MapUrl = request.MapUrl?.Trim() ?? "";
            settings.LogoUrl = logo;
            settings.FacebookUrl = request.FacebookUrl?.Trim() ?? "";
            settings.InstagramUrl = request.InstagramUrl?.Trim() ?? "";
            settings.TikTokUrl = request.TikTokUrl?.Trim() ?? "";
            settings.WebsiteUrl = request.WebsiteUrl?.Trim() ?? "";
            settings.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim();
            settings.UpdatedAtUtc = DateTime.UtcNow;
            settings.UpdatedByUserId = CurrentUserId;

            await _db.SaveChangesAsync(ct);

            if (oldLogo.Length > 0 && oldLogo != logo)
                _storage.TryDelete(oldLogo);

            _logger.LogInformation("Admin {UserId} updated company settings", CurrentUserId);
            return CompanySettingsDto.From(settings);
        }

        private async Task<CompanySettings> LoadAsync(CancellationToken ct)
        {
            var settings = await _db.CompanySettings.FirstOrDefaultAsync(c => c.CompanySettingsId == CompanySettings.SingletonId, ct);
            if (settings != null)
                return settings;

            // Normally seeded by the migration; recreate if someone removed the row.
            settings = new CompanySettings();
            _db.CompanySettings.Add(settings);
            await _db.SaveChangesAsync(ct);
            return settings;
        }

        private static bool IsHttpsUrlOrEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ||
            Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
