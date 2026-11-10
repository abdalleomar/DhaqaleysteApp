namespace DhaqaleysteApi.Models
{
    /// <summary>
    /// The company's public details, shown in the app's Contact tab and used for Call/WhatsApp buttons.
    /// There is exactly one row (Id = 1). Editable by admins without releasing a new app version.
    /// </summary>
    public class CompanySettings
    {
        public const int SingletonId = 1;

        public int CompanySettingsId { get; set; } = SingletonId;

        public string CompanyName { get; set; } = "Dhaqaleyste";

        public string AboutEn { get; set; } = "";

        public string AboutSo { get; set; } = "";

        public string Phone { get; set; } = "";

        /// <summary>WhatsApp number in international format, e.g. +252615000000.</summary>
        public string WhatsApp { get; set; } = "";

        public string Email { get; set; } = "";

        public string Address { get; set; } = "";

        public string City { get; set; } = "";

        public string WorkingHours { get; set; } = "";

        /// <summary>Optional https link to the location on a map.</summary>
        public string MapUrl { get; set; } = "";

        /// <summary>Server-relative URL of an uploaded logo image.</summary>
        public string LogoUrl { get; set; } = "";

        public string FacebookUrl { get; set; } = "";

        public string InstagramUrl { get; set; } = "";

        public string TikTokUrl { get; set; } = "";

        public string WebsiteUrl { get; set; } = "";

        /// <summary>ISO 4217 code used to display prices.</summary>
        public string CurrencyCode { get; set; } = "USD";

        public DateTime? UpdatedAtUtc { get; set; }

        public int? UpdatedByUserId { get; set; }
    }
}
