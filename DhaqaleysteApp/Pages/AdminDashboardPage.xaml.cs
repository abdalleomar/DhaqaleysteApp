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
        WelcomeLabel.Text = $"Signed in as {name} ({role})";

        var products = await _api.GetProductsAsync();
        ProductCountLabel.Text = products.Success ? products.Data!.Count.ToString() : "–";
    }

    private async void AddProduct_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProductFormPage());
    }

    private async void ManageProducts_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ManageProductsPage());
    }

    private async void Logout_Clicked(object sender, EventArgs e)
    {
        if (!await DisplayAlert("Sign out", "Sign out of the staff account?", "Sign out", "Cancel"))
            return;

        await _api.LogoutAsync();
        await Navigation.PopToRootAsync();
    }
}
