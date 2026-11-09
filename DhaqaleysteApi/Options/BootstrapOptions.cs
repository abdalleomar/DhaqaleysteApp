namespace DhaqaleysteApi.Options
{
    /// <summary>
    /// Creates the first administrator on startup when no active admin exists.
    /// Supply through user-secrets / environment variables, then remove once the account exists.
    /// </summary>
    public class BootstrapOptions
    {
        public const string Section = "Bootstrap";

        public string AdminEmail { get; set; } = "";
        public string AdminPassword { get; set; } = "";
        public string AdminFullName { get; set; } = "Administrator";
    }
}
