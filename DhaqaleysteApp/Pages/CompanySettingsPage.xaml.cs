using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

/// <summary>Admins edit the company's public contact details.</summary>
public partial class CompanySettingsPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private CompanySettings _settings = new();
    private string _newLogoPath = "";
    private bool _loaded;

    public CompanySettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
            return;

        SetBusy(true);
        var result = await _api.GetCompanyAsync();
        SetBusy(false);

        if (!result.Success)
        {
            ShowError(result.Error);
            SaveButton.IsEnabled = false;
            return;
        }

        _loaded = true;
        _settings = result.Data!;
        NameEntry.Text = _settings.CompanyName;
        PhoneEntry.Text = _settings.Phone;
        WhatsAppEntry.Text = _settings.WhatsApp;
        EmailEntry.Text = _settings.Email;
        AddressEntry.Text = _settings.Address;
        CityEntry.Text = _settings.City;
        HoursEntry.Text = _settings.WorkingHours;
        AboutEnEditor.Text = _settings.AboutEn;
        AboutSoEditor.Text = _settings.AboutSo;
        MapEntry.Text = _settings.MapUrl;
        WebsiteEntry.Text = _settings.WebsiteUrl;
        FacebookEntry.Text = _settings.FacebookUrl;
        InstagramEntry.Text = _settings.InstagramUrl;
        TikTokEntry.Text = _settings.TikTokUrl;

        var logo = _api.ResolveMediaUrl(_settings.LogoUrl);
        if (logo.Length > 0)
            LogoImage.Source = new UriImageSource { Uri = new Uri(logo) };
    }

    private async void ChooseLogo_Clicked(object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select logo", FileTypes = FilePickerFileType.Images });
            if (file == null)
                return;
            if (new FileInfo(file.FullPath).Length > 5 * 1024 * 1024)
            {
                await DisplayAlert("File too large", "The maximum image size is 5 MB.", "OK");
                return;
            }
            _newLogoPath = file.FullPath;
            LogoImage.Source = ImageSource.FromFile(file.FullPath);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Could not open image", ex.Message, "OK");
        }
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (Busy.IsRunning)
            return;

        if (string.IsNullOrWhiteSpace(NameEntry.Text) || NameEntry.Text.Trim().Length < 2)
        {
            ShowError("Enter the company name.");
            return;
        }

        SetBusy(true);
        try
        {
            var logoUrl = _settings.LogoUrl;
            if (_newLogoPath.Length > 0)
            {
                var upload = await _api.UploadImageAsync(_newLogoPath);
                if (!upload.Success)
                {
                    ShowError($"Logo upload failed: {upload.Error}");
                    return;
                }
                logoUrl = upload.Data!;
                _newLogoPath = "";
            }

            var updated = new CompanySettings
            {
                CompanyName = NameEntry.Text!.Trim(),
                Phone = Clean(PhoneEntry.Text),
                WhatsApp = Clean(WhatsAppEntry.Text),
                Email = Clean(EmailEntry.Text),
                Address = Clean(AddressEntry.Text),
                City = Clean(CityEntry.Text),
                WorkingHours = Clean(HoursEntry.Text),
                AboutEn = Clean(AboutEnEditor.Text),
                AboutSo = Clean(AboutSoEditor.Text),
                MapUrl = Clean(MapEntry.Text),
                WebsiteUrl = Clean(WebsiteEntry.Text),
                FacebookUrl = Clean(FacebookEntry.Text),
                InstagramUrl = Clean(InstagramEntry.Text),
                TikTokUrl = Clean(TikTokEntry.Text),
                LogoUrl = logoUrl,
                CurrencyCode = string.IsNullOrWhiteSpace(_settings.CurrencyCode) ? "USD" : _settings.CurrencyCode
            };

            var result = await _api.UpdateCompanyAsync(updated);
            if (!result.Success)
            {
                ShowError(result.Error);
                return;
            }

            _settings = result.Data!;
            await DisplayAlert("Saved", "Company details updated.", "OK");
            await Navigation.PopAsync();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static string Clean(string? value) => value?.Trim() ?? "";

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        Busy.IsVisible = Busy.IsRunning = busy;
        SaveButton.IsEnabled = !busy;
        if (busy)
            ErrorLabel.IsVisible = false;
    }
}
