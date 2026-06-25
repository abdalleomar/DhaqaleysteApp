namespace DhaqaleysteApp.Services;

public static class SessionService
{
    public static async Task SaveUserAsync(int userId, string fullName, string role)
    {
        await SecureStorage.SetAsync("UserId", userId.ToString());
        await SecureStorage.SetAsync("FullName", fullName);
        await SecureStorage.SetAsync("Role", role);
    }

    public static async Task<string> GetRoleAsync()
    {
        return await SecureStorage.GetAsync("Role") ?? "";
    }

    public static async Task<string> GetFullNameAsync()
    {
        return await SecureStorage.GetAsync("FullName") ?? "";
    }

    public static void Logout()
    {
        SecureStorage.Remove("UserId");
        SecureStorage.Remove("FullName");
        SecureStorage.Remove("Role");
    }
}