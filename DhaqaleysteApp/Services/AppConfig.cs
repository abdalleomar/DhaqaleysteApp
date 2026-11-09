using System.Text.Json;

namespace DhaqaleysteApp.Services;

/// <summary>
/// App configuration. The API address comes from Resources/Raw/appsettings.json so that no
/// production URL is hard-coded. In Debug builds an empty value falls back to the local API.
/// </summary>
public class AppConfig
{
    public Uri? ApiBaseUrl { get; private init; }

    public static AppConfig Load()
    {
        string? configured = null;
        try
        {
            // Read off the UI thread; the file is tiny and only read once at startup.
            configured = Task.Run(async () =>
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
                using var doc = await JsonDocument.ParseAsync(stream);
                return doc.RootElement.TryGetProperty("ApiBaseUrl", out var value) ? value.GetString() : null;
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"appsettings.json could not be read: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(configured))
            configured = DevelopmentApiUrl();

        if (string.IsNullOrWhiteSpace(configured) || !Uri.TryCreate(EnsureTrailingSlash(configured), UriKind.Absolute, out var uri))
            return new AppConfig();

        return new AppConfig { ApiBaseUrl = uri };
    }

    private static string? DevelopmentApiUrl()
    {
#if DEBUG
        // The Android emulator reaches the development machine at 10.0.2.2.
        return DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5092/"
            : "http://localhost:5092/";
#else
        return null;
#endif
    }

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
