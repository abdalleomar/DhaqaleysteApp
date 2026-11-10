using DhaqaleysteApp.Pages;

namespace DhaqaleysteApp;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        // Each tab's page (and its view model) comes from dependency injection.
        AddPage<HomePage>(HomeTab, services);
        AddPage<CategoriesPage>(CategoriesTab, services);
        AddPage<FavoritesPage>(FavoritesTab, services);
        AddPage<ContactPage>(ContactTab, services);
    }

    private static void AddPage<TPage>(Tab tab, IServiceProvider services) where TPage : Page =>
        tab.Items.Add(new ShellContent
        {
            Route = tab.Route + "-root",
            ContentTemplate = new DataTemplate(() => services.GetRequiredService<TPage>())
        });
}
