using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.Services;

/// <summary>Opens the phone dialer, WhatsApp, email and links, with a friendly message when a device cannot.</summary>
public static class ContactLauncher
{
    /// <summary>Keeps digits and a leading +, e.g. "+252 61-500 0000" becomes "+252615000000".</summary>
    public static string CleanNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return "";
        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return "";
        return number.TrimStart().StartsWith('+') ? "+" + digits : digits;
    }

    public static async Task CallAsync(string number)
    {
        number = CleanNumber(number);
        if (number.Length == 0)
            return;
        try
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(number);
                return;
            }
        }
        catch (Exception)
        {
        }
        await ShowAsync(number);
    }

    public static Task WhatsAppAsync(string number, string message)
    {
        number = CleanNumber(number).TrimStart('+');
        if (number.Length == 0)
            return Task.CompletedTask;
        return OpenUrlAsync($"https://wa.me/{number}?text={Uri.EscapeDataString(message)}");
    }

    public static async Task EmailAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return;
        try
        {
            if (Microsoft.Maui.ApplicationModel.Communication.Email.Default.IsComposeSupported)
            {
                await Microsoft.Maui.ApplicationModel.Communication.Email.Default.ComposeAsync(new EmailMessage { To = new List<string> { address } });
                return;
            }
        }
        catch (Exception)
        {
        }
        await ShowAsync(address);
    }

    public static async Task OpenUrlAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return;
        try
        {
            if (await Launcher.Default.OpenAsync(uri))
                return;
        }
        catch (Exception)
        {
        }
        await ShowAsync(Loc.T("CannotOpen"));
    }

    private static Task ShowAsync(string message) =>
        Shell.Current?.CurrentPage?.DisplayAlert(Loc.T("NotAvailable"), message, Loc.T("Ok")) ?? Task.CompletedTask;
}
