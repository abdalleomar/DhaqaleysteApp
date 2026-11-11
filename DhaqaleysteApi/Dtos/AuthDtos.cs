using System.ComponentModel.DataAnnotations;

namespace DhaqaleysteApi.Dtos
{
    public class LoginRequest
    {
        /// <summary>Email address or phone number.</summary>
        [Required, StringLength(256)]
        public string EmailOrPhone { get; set; } = "";

        [Required, StringLength(128)]
        public string Password { get; set; } = "";
    }

    public class RefreshRequest
    {
        [Required, StringLength(200)]
        public string RefreshToken { get; set; } = "";
    }

    public class ChangePasswordRequest
    {
        [Required, StringLength(128)]
        public string CurrentPassword { get; set; } = "";

        [Required, StringLength(128, MinimumLength = PasswordRules.MinLength)]
        public string NewPassword { get; set; } = "";
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = "";
        public DateTime AccessTokenExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; } = "";
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public StaffDto User { get; set; } = new();
    }

    public static class PasswordRules
    {
        public const int MinLength = 8;

        /// <summary>Returns an error message, or null when the password is acceptable.</summary>
        public static string? Validate(string password)
        {
            if (password.Length < MinLength)
                return $"Password must be at least {MinLength} characters.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "Password must contain both letters and numbers.";
            return null;
        }
    }
}

namespace DhaqaleysteApi.Dtos
{
    public class RegisterStaffRequest
    {
        [Required, StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = "";

        [OptionalPhone, StringLength(32)]
        public string? Phone { get; set; }

        [Required, StringLength(128, MinimumLength = PasswordRules.MinLength)]
        public string Password { get; set; } = "";

        /// <summary>The staff invite code the admin shared.</summary>
        [Required, StringLength(32)]
        public string InviteCode { get; set; } = "";
    }

    public class ForgotPasswordRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordWithCodeRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = "";

        [Required, RegularExpression(@"^\s*\d{6}\s*$", ErrorMessage = "Enter the 6-digit code from the email.")]
        public string Code { get; set; } = "";

        [Required, StringLength(128, MinimumLength = PasswordRules.MinLength)]
        public string NewPassword { get; set; } = "";
    }

    public class InviteCodeDto
    {
        /// <summary>Empty when staff sign-up is turned off.</summary>
        public string Code { get; set; } = "";
        public bool Enabled => Code.Length > 0;
    }
}
