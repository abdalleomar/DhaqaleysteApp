using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class AdminDashboardPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private readonly SessionService _session = ServiceHelper.Get<SessionService>();

    public AdminDashboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // The server checks every staff request; this only avoids showing the page when signed out.
        if (!await _session.IsSignedInAsync())
        {
            await Navigation.PopToRootAsync();
            return;
        }

        var name = await _session.GetFullNameAsync();
        var role = await _session.GetRoleAsync();
        WelcomeLabel.Text = $"Signed in as {name}";
        RoleLabel.Text = role;
        CompanyRow.IsVisible = role == "Admin";

        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        var result = await _api.GetStatsAsync();
        Refresh.IsRefreshing = false;

        if (!result.Success)
        {
            ErrorLabel.Text = result.Error;
            ErrorLabel.IsVisible = true;
            if (result.IsUnauthorized)
            {
                await DisplayAlert("Signed out", "Your session has ended. Please sign in again.", "OK");
                await Navigation.PopToRootAsync();
            }
            return;
        }

        ErrorLabel.IsVisible = false;
        var stats = result.Data!;
        TotalLabel.Text = stats.TotalProducts.ToString();
        InStockLabel.Text = stats.InStock.ToString();
        OutOfStockLabel.Text = stats.OutOfStock.ToString();
        FeaturedLabel.Text = stats.Featured.ToString();

        LowStockList.Children.Clear();
        foreach (var product in stats.LowStock)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)) };
            row.Add(new Label { Text = product.ProductName, FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation });
            var left = new Label { Text = $"{product.Quantity} left", FontSize = 14, FontFamily = "BodySemibold" };
            left.SetDynamicResource(Label.TextColorProperty, "AccentColor");
            row.Add(left, 1);
            LowStockList.Children.Add(row);
        }
        LowStockCard.IsVisible = stats.LowStock.Count > 0;
    }

    private async void Refresh_Refreshing(object? sender, EventArgs e) => await LoadStatsAsync();

    private async void Back_Tapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();

    private async void AddProduct_Clicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new ProductFormPage());

    private async void ManageProducts_Clicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new ManageProductsPage());

    private async void Categories_Clicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new ManageCategoriesPage());

    private async void Company_Clicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new CompanySettingsPage());

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        if (!await DisplayAlert("Sign out", "Sign out of the staff account?", "Sign out", "Cancel"))
            return;

        await _api.LogoutAsync();
        await Navigation.PopToRootAsync();
    }
}
