using System.ComponentModel.DataAnnotations;
using DhaqaleysteApi.Models;

namespace DhaqaleysteApi.Dtos
{
    public class CompanySettingsDto
    {
        public string CompanyName { get; set; } = "";
        public string AboutEn { get; set; } = "";
        public string AboutSo { get; set; } = "";
        public string Phone { get; set; } = "";
        public string WhatsApp { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string WorkingHours { get; set; } = "";
        public string MapUrl { get; set; } = "";
        public string LogoUrl { get; set; } = "";
        public string FacebookUrl { get; set; } = "";
        public string InstagramUrl { get; set; } = "";
        public string TikTokUrl { get; set; } = "";
        public string WebsiteUrl { get; set; } = "";
        public string CurrencyCode { get; set; } = "USD";
        public DateTime? UpdatedAtUtc { get; set; }

        public static CompanySettingsDto From(CompanySettings c) => new()
        {
            CompanyName = c.CompanyName,
            AboutEn = c.AboutEn,
            AboutSo = c.AboutSo,
            Phone = c.Phone,
            WhatsApp = c.WhatsApp,
            Email = c.Email,
            Address = c.Address,
            City = c.City,
            WorkingHours = c.WorkingHours,
            MapUrl = c.MapUrl,
            LogoUrl = c.LogoUrl,
            FacebookUrl = c.FacebookUrl,
            InstagramUrl = c.InstagramUrl,
            TikTokUrl = c.TikTokUrl,
            WebsiteUrl = c.WebsiteUrl,
            CurrencyCode = c.CurrencyCode,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }

    public class CompanySettingsRequest
    {
        [Required, StringLength(150, MinimumLength = 2)]
        public string CompanyName { get; set; } = "";

        [StringLength(2000)] public string? AboutEn { get; set; }
        [StringLength(2000)] public string? AboutSo { get; set; }

        [RegularExpression(@"^\+?[0-9 ]{6,20}$", ErrorMessage = "Phone must contain only digits, spaces and an optional leading +.")]
        public string? Phone { get; set; }

        [RegularExpression(@"^\+?[0-9 ]{6,20}$", ErrorMessage = "WhatsApp must be a number in international format, e.g. +252615000000.")]
        public string? WhatsApp { get; set; }

        [EmailAddress, StringLength(256)] public string? Email { get; set; }
        [StringLength(300)] public string? Address { get; set; }
        [StringLength(100)] public string? City { get; set; }
        [StringLength(200)] public string? WorkingHours { get; set; }

        [StringLength(500)] public string? MapUrl { get; set; }
        [StringLength(500)] public string? LogoUrl { get; set; }
        [StringLength(500)] public string? FacebookUrl { get; set; }
        [StringLength(500)] public string? InstagramUrl { get; set; }
        [StringLength(500)] public string? TikTokUrl { get; set; }
        [StringLength(500)] public string? WebsiteUrl { get; set; }

        [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter code such as USD.")]
        public string? CurrencyCode { get; set; }
    }

    public class CatalogStatsDto
    {
        public int TotalProducts { get; set; }
        public int InStock { get; set; }
        public int OutOfStock { get; set; }
        public int Featured { get; set; }
        public int Categories { get; set; }
        public int ActiveStaff { get; set; }
        public List<ProductSummaryDto> MostViewed { get; set; } = new();
        public List<ProductSummaryDto> LowStock { get; set; } = new();
    }
}
