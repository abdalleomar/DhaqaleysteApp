namespace DhaqaleysteApi.Models
{
    /// <summary>
    /// A company staff account. Customers browse anonymously and have no account.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = "";

        /// <summary>Optional. Stored trimmed; unique when not empty.</summary>
        public string Phone { get; set; } = "";

        /// <summary>Stored trimmed and lower-case; unique when not empty.</summary>
        public string Email { get; set; } = "";

        /// <summary>PBKDF2 hash produced by ASP.NET Core's PasswordHasher. Never returned by the API.</summary>
        public string PasswordHash { get; set; } = "";

        /// <summary>One of <see cref="Roles"/>.</summary>
        public string Role { get; set; } = Roles.Staff;

        public bool IsActive { get; set; } = true;

        /// <summary>Changes whenever credentials, role or status change, invalidating issued access tokens.</summary>
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

        public int AccessFailedCount { get; set; }

        public DateTime? LockoutEndUtc { get; set; }

        public DateTime? LastLoginAtUtc { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
