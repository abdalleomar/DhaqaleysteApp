using DhaqaleysteApp.Resources.Styles;

namespace DhaqaleysteApp.Services;

public enum ThemeChoice { System, Light, Dark }

/// <summary>
/// Applies light or dark colours by swapping the theme dictionary. All pages use DynamicResource,
/// so the change is immediate. The choice is remembered on the device.
/// </summary>
public class ThemeService
{
    private const string PreferenceKey = "Theme";
    private ResourceDictionary? _current;

    public ThemeChoice Choice
    {
        get => Enum.TryParse<ThemeChoice>(Preferences.Default.Get(PreferenceKey, nameof(ThemeChoice.System)), out var c) ? c : ThemeChoice.System;
        private set => Preferences.Default.Set(PreferenceKey, value.ToString());
    }

    public void Initialize(Application app)
    {
        app.RequestedThemeChanged += (_, _) => Apply(app);
        app.UserAppTheme = ToAppTheme(Choice);
        Apply(app);
    }

    public void SetChoice(ThemeChoice choice)
    {
        Choice = choice;
        var app = Application.Current!;
        app.UserAppTheme = ToAppTheme(choice);
        Apply(app);
    }

    private void Apply(Application app)
    {
        var dark = app.RequestedTheme == AppTheme.Dark;
        if (_current is DarkTheme && dark || _current is LightTheme && !dark)
            return;

        var merged = app.Resources.MergedDictionaries;
        if (_current != null)
            merged.Remove(_current);
        _current = dark ? new DarkTheme() : new LightTheme();
        merged.Add(_current);
    }

    private static AppTheme ToAppTheme(ThemeChoice choice) => choice switch
    {
        ThemeChoice.Light => AppTheme.Light,
        ThemeChoice.Dark => AppTheme.Dark,
        _ => AppTheme.Unspecified
    };
}
