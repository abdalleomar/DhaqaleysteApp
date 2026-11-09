namespace DhaqaleysteApi.Options
{
    public class JwtOptions
    {
        public const string Section = "Jwt";

        public string Issuer { get; set; } = "Dhaqaleyste";
        public string Audience { get; set; } = "DhaqaleysteApp";

        /// <summary>At least 32 characters. Supply via user-secrets or the Jwt__SigningKey environment variable.</summary>
        public string SigningKey { get; set; } = "";

        public int AccessTokenMinutes { get; set; } = 30;
        public int RefreshTokenDays { get; set; } = 30;
    }
}
