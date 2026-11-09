using System.ComponentModel.DataAnnotations;
using DhaqaleysteApi.Models;

namespace DhaqaleysteApi.Dtos
{
    public class StaffDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAtUtc { get; set; }

        public static StaffDto From(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAtUtc = u.LastLoginAtUtc
        };
    }

    public class CreateStaffRequest
    {
        [Required, StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = "";

        [Phone, StringLength(32)]
        public string? Phone { get; set; }

        [Required, StringLength(128, MinimumLength = PasswordRules.MinLength)]
        public string Password { get; set; } = "";

        /// <summary>Admin or Staff.</summary>
        [Required]
        public string Role { get; set; } = Roles.Staff;
    }

    public class UpdateStaffRequest
    {
        [Required, StringLength(150, MinimumLength = 2)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = "";

        [Phone, StringLength(32)]
        public string? Phone { get; set; }

        [Required]
        public string Role { get; set; } = Roles.Staff;

        public bool IsActive { get; set; } = true;
    }

    public class ResetPasswordRequest
    {
        [Required, StringLength(128, MinimumLength = PasswordRules.MinLength)]
        public string NewPassword { get; set; } = "";
    }
}
