namespace DhaqaleysteApp.Models;

public class StaffUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public bool IsAdmin => Role == "Admin";
    public bool IsRemoved => !IsActive;
    public string Initials => string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));
    public string StatusText => !IsActive ? "Removed" : Role == "Legacy" ? "Old account" : Role;
}

public class InviteCode
{
    public string Code { get; set; } = "";
    public bool Enabled => Code.Length > 0;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public StaffUser User { get; set; } = new();
}
