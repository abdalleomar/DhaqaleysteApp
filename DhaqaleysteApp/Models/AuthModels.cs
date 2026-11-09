namespace DhaqaleysteApp.Models;

public class StaffUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public StaffUser User { get; set; } = new();
}
