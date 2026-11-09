using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class ManageProductsPage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private bool _isLoading;

    public ObservableCollection<Product> Products { get; } = new();

    public ManageProductsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = true;
        EmptyLabel.IsVisible = RetryButton.IsVisible = false;
        try
        {
            var result = await _api.GetProductsAsync();
            if (result.Success)
            {
                Products.Clear();
                foreach (var item in result.Data!)
                    Products.Add(item);
                EmptyLabel.Text = "No products to manage";
            }
            else
            {
                EmptyLabel.Text = result.Error;
                RetryButton.IsVisible = Products.Count == 0;
            }
        }
        finally
        {
            LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = false;
            EmptyLabel.IsVisible = true;
            _isLoading = false;
        }
    }

    private async void Retry_Clicked(object sender, EventArgs e) => await LoadAsync();

    private async void EditProduct_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Product product)
            await Navigation.PushAsync(new ProductFormPage(product));
    }

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: Product product } button)
            return;

        if (!await DisplayAlert("Delete", $"Delete \"{product.ProductName}\"? This cannot be undone.", "Delete", "Cancel"))
            return;

        button.IsEnabled = false;
        var result = await _api.DeleteProductAsync(product.ProductId);
        button.IsEnabled = true;

        if (result.Success)
            Products.Remove(product);
        else
            await DisplayAlert("Not deleted", result.Error, "OK");
    }
}
