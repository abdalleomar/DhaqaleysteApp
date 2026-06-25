using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class ManageProductsPage : ContentPage
{
    public ObservableCollection<Product> Products { get; set; } = new();

    public ManageProductsPage()
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

        foreach (var item in products)
            Products.Add(item);
    }

    private async void EditProduct_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Product product)
        {
            await Navigation.PushAsync(new EditProductPage(product));
        }
    }

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn &&
            btn.BindingContext is Product product)
        {
            bool confirm =
                await DisplayAlert(
                    "Delete",
                    $"Delete {product.ProductName} ?",
                    "Yes",
                    "No");

            if (!confirm)
                return;

            var api = new ApiService();

            await api.DeleteProductAsync(
                product.ProductId);

            Products.Remove(product);
        }
    }
}