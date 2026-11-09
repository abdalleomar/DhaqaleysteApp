using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Services;

/// <summary>Holds the signed-in staff member's tokens in the platform's secure storage.</summary>
public class SessionService
{
    private const string AccessTokenKey = "AccessToken";
    private const string AccessExpiresKey = "AccessTokenExpires";
    private const string RefreshTokenKey = "RefreshToken";
    private const string UserIdKey = "UserId";
    private const string FullNameKey = "FullName";
    private const string RoleKey = "Role";

    // Keys written by the previous version of the app.
    private static readonly string[] AllKeys = { AccessTokenKey, AccessExpiresKey, RefreshTokenKey, UserIdKey, FullNameKey, RoleKey };

    public async Task SaveAsync(AuthResponse auth)
    {
        await SecureStorage.SetAsync(AccessTokenKey, auth.AccessToken);
        await SecureStorage.SetAsync(AccessExpiresKey, auth.AccessTokenExpiresAtUtc.ToString("O"));
        await SecureStorage.SetAsync(RefreshTokenKey, auth.RefreshToken);
        await SecureStorage.SetAsync(UserIdKey, auth.User.UserId.ToString());
        await SecureStorage.SetAsync(FullNameKey, auth.User.FullName);
        await SecureStorage.SetAsync(RoleKey, auth.User.Role);
    }

    public Task<string?> GetAccessTokenAsync() => SecureStorage.GetAsync(AccessTokenKey);

    public Task<string?> GetRefreshTokenAsync() => SecureStorage.GetAsync(RefreshTokenKey);

    public async Task<string> GetFullNameAsync() => await SecureStorage.GetAsync(FullNameKey) ?? "";

    public async Task<string> GetRoleAsync() => await SecureStorage.GetAsync(RoleKey) ?? "";

    /// <summary>True when a staff session exists. The server still checks every request.</summary>
    public async Task<bool> IsSignedInAsync() => !string.IsNullOrEmpty(await GetRefreshTokenAsync());

    public void Clear()
    {
        foreach (var key in AllKeys)
            SecureStorage.Remove(key);
    }
}
