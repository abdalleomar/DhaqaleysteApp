using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services, ThemeService theme)
    {
        _services = services;
        Loc.Initialize();
        InitializeComponent();
        theme.Initialize(this);
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell(_services));

    /// <summary>
    /// Rebuilds all screens, e.g. after the language changes, and opens the given tab.
    /// </summary>
    public static void RestartShell(string selectTab = "home")
    {
        if (Current is not App app || app.Windows.Count == 0)
            return;

        var shell = new AppShell(app._services);
        app.Windows[0].Page = shell;
        shell.Dispatcher.Dispatch(async () => await shell.GoToAsync($"//{selectTab}"));
    }
}
