namespace DhaqaleysteApi.Models
{
    public static class Roles
    {
        /// <summary>Full access, including staff account management.</summary>
        public const string Admin = "Admin";

        /// <summary>Manages products and media.</summary>
        public const string Staff = "Staff";

        /// <summary>
        /// Accounts carried over from the old public registration. They cannot sign in
        /// and have no permissions until an admin assigns a real role and a new password.
        /// </summary>
        public const string Legacy = "Legacy";

        public static readonly string[] Assignable = { Admin, Staff };

        public static bool IsAssignable(string role) => Assignable.Contains(role);
    }
}
