namespace DhaqaleysteApi.Services
{
    public static class AuthPolicies
    {
        /// <summary>Any active staff member (Admin or Staff).</summary>
        public const string Staff = "Staff";

        /// <summary>Administrators only.</summary>
        public const string Admin = "Admin";
    }

    public static class RateLimitPolicies
    {
        /// <summary>Per-IP limit on sign-in, refresh and password endpoints.</summary>
        public const string Auth = "auth";

        /// <summary>Per-IP limit on uploads.</summary>
        public const string Upload = "upload";
    }
}
