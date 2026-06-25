using DhaqaleysteApp.Services;

namespace DhaqaleysteApp;

public partial class App : Application
{
    public static DatabaseService Database { get; private set; }

    public App()
    {
        InitializeComponent();

        string dbPath = Path.Combine(
            FileSystem.AppDataDirectory,
            "Dhaqaleyste.db");

        Database = new DatabaseService(dbPath);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(
            new NavigationPage(
                new Pages.HomePage()));
    }
}