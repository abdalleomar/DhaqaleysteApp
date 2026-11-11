namespace DhaqaleysteApi.Options
{
    /// <summary>
    /// Outgoing email (used for "forgot password" codes). Supply through user-secrets / environment variables.
    /// When SmtpHost is empty: in Development the email is written to the API log; in Production nothing is sent.
    /// </summary>
    public class EmailOptions
    {
        public const string Section = "Email";

        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "Dhaqaleyste";
    }
}
