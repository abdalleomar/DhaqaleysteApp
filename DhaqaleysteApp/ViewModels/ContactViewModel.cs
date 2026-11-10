using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

/// <summary>Company details, contact actions, language and appearance settings, and the staff entrance.</summary>
public partial class ContactViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly ThemeService _theme;
    private readonly NavigationService _navigation;
    private readonly SessionService _session;

    public ContactViewModel(ApiService api, ThemeService theme, NavigationService navigation, SessionService session)
    {
        _api = api;
        _theme = theme;
        _navigation = navigation;
        _session = session;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(About), nameof(HasAbout), nameof(HasPhone), nameof(HasWhatsApp), nameof(HasEmail),
        nameof(HasAddress), nameof(HasHours), nameof(HasMap), nameof(HasWebsite), nameof(HasSocial),
        nameof(HasFacebook), nameof(HasInstagram), nameof(HasTikTok), nameof(LogoUrl), nameof(HasLogo),
        nameof(FullAddress), nameof(HasAnyContact))]
    private CompanySettings _company = new() { CompanyName = "Dhaqaleyste" };

    [ObservableProperty] private string _staffText = Loc.T("StaffSignIn");

    public string About => Loc.Pick(Company.AboutEn, Company.AboutSo);
    public bool HasAbout => About.Length > 0;
    public bool HasPhone => ContactLauncher.CleanNumber(Company.Phone).Length > 0;
    public bool HasWhatsApp => ContactLauncher.CleanNumber(Company.WhatsApp).Length > 0;
    public bool HasEmail => !string.IsNullOrWhiteSpace(Company.Email);
    public string FullAddress => string.Join(", ", new[] { Company.Address, Company.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public bool HasAddress => FullAddress.Length > 0;
    public bool HasHours => !string.IsNullOrWhiteSpace(Company.WorkingHours);
    public bool HasMap => !string.IsNullOrWhiteSpace(Company.MapUrl);
    public bool HasWebsite => !string.IsNullOrWhiteSpace(Company.WebsiteUrl);
    public bool HasFacebook => !string.IsNullOrWhiteSpace(Company.FacebookUrl);
    public bool HasInstagram => !string.IsNullOrWhiteSpace(Company.InstagramUrl);
    public bool HasTikTok => !string.IsNullOrWhiteSpace(Company.TikTokUrl);
    public bool HasSocial => HasFacebook || HasInstagram || HasTikTok;
    public bool HasAnyContact => HasPhone || HasWhatsApp || HasEmail || HasAddress;
    public string LogoUrl => _api.ResolveMediaUrl(Company.LogoUrl);
    public bool HasLogo => LogoUrl.Length > 0;

    public bool IsEnglish => Loc.Language == AppLanguage.English;
    public bool IsSomali => Loc.Language == AppLanguage.Somali;

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { Loc.T("ThemeSystem"), Loc.T("ThemeLight"), Loc.T("ThemeDark") };

    public int ThemeIndex
    {
        get => (int)_theme.Choice;
        set
        {
            if (value < 0 || value == (int)_theme.Choice)
                return;
            _theme.SetChoice((ThemeChoice)value);
            OnPropertyChanged();
        }
    }

    public async Task OnAppearingAsync()
    {
        StaffText = await _session.IsSignedInAsync() ? Loc.T("StaffDashboard") : Loc.T("StaffSignIn");

        IsLoading = Company.CompanyName == "Dhaqaleyste" && !HasAnyContact;
        var result = await _api.GetCompanyAsync();
        IsLoading = false;

        if (result.Success)
        {
            ClearError();
            Company = result.Data!;
        }
        else if (!HasAnyContact)
        {
            SetError(result.Error, result.IsNetworkError);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await OnAppearingAsync();
        IsRefreshing = false;
    }

    [RelayCommand] private Task CallAsync() => ContactLauncher.CallAsync(Company.Phone);
    [RelayCommand] private Task WhatsAppAsync() => ContactLauncher.WhatsAppAsync(Company.WhatsApp, "");
    [RelayCommand] private Task EmailAsync() => ContactLauncher.EmailAsync(Company.Email);
    [RelayCommand] private Task OpenMapAsync() => ContactLauncher.OpenUrlAsync(Company.MapUrl);
    [RelayCommand] private Task OpenWebsiteAsync() => ContactLauncher.OpenUrlAsync(Company.WebsiteUrl);
    [RelayCommand] private Task OpenFacebookAsync() => ContactLauncher.OpenUrlAsync(Company.FacebookUrl);
    [RelayCommand] private Task OpenInstagramAsync() => ContactLauncher.OpenUrlAsync(Company.InstagramUrl);
    [RelayCommand] private Task OpenTikTokAsync() => ContactLauncher.OpenUrlAsync(Company.TikTokUrl);

    [RelayCommand]
    private void SetLanguage(string language)
    {
        var chosen = language == "so" ? AppLanguage.Somali : AppLanguage.English;
        if (chosen == Loc.Language)
            return;

        Loc.SetLanguage(chosen);
        // Rebuild the screens so every text, including product labels, switches language.
        App.RestartShell(selectTab: "contact");
    }

    [RelayCommand]
    private Task OpenStaffAsync() => _navigation.OpenStaffAreaAsync();
}
