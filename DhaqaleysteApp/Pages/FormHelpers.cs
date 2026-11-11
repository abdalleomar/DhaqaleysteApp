namespace DhaqaleysteApp.Pages;

/// <summary>Small helpers shared by the staff account forms.</summary>
internal static class FormHelpers
{
    /// <summary>Same rule as the server: 8+ characters with letters and numbers. Returns an error or null.</summary>
    public static string? CheckNewPassword(string password, string confirm)
    {
        if (password.Length < 8)
            return "The password must be at least 8 characters.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "The password must contain both letters and numbers.";
        if (password != confirm)
            return "The two passwords do not match.";
        return null;
    }
}
