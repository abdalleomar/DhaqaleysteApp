using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class HomePage : ContentPage
{
    public ObservableCollection<Product> Products { get; set; } = new();

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Products.Clear();

        var api = new ApiService();
        var products = await api.GetProductsAsync();
        var role = await SessionService.GetRoleAsync();

        AdminButton.IsVisible = role == "ADMIN" || string.IsNullOrEmpty(role);
       

        AdminButton.IsVisible =
            string.IsNullOrEmpty(role) || role == "ADMIN";

        LogoutButton.IsVisible =
            !string.IsNullOrEmpty(role);

        foreach (var item in products)
            Products.Add(item);
    }

    private async void Admin_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }

    private async void ViewDetails_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Product product)
        {
            await Navigation.PushAsync(new ProductDetailsPage(product));
        }
    }

    private async void SearchBarProducts_TextChanged(object sender, TextChangedEventArgs e)
    {
        Products.Clear();

        string searchText = e.NewTextValue?.Trim().ToLower() ?? "";

        var api = new ApiService();
        var allProducts = await api.GetProductsAsync();

        var filtered = allProducts.Where(x =>
            string.IsNullOrWhiteSpace(searchText) ||
            (x.ProductName ?? "").ToLower().Contains(searchText) ||
            (x.Description ?? "").ToLower().Contains(searchText) ||
            (x.Category ?? "").ToLower().Contains(searchText) ||
            (x.Location ?? "").ToLower().Contains(searchText)
        );

        foreach (var item in filtered)
            Products.Add(item);
    }


    private async void Logout_Clicked(object sender, EventArgs e)
    {
        SessionService.Logout();

        await DisplayAlert("Logout", "You are logged out", "OK");

        AdminButton.IsVisible = true;
        LogoutButton.IsVisible = false;
    }
}