using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class ManageProductsPage : ContentPage
{
    private const int PageSize = 20;

    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchDelay;
    private int _page;
    private bool _hasMore;
    private bool _isLoadingMore;

    public ObservableCollection<Product> Products { get; } = new();

    public ManageProductsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private ProductQuery BuildQuery(int page) => new()
    {
        Search = SearchBarProducts.Text,
        IncludeHidden = true,
        Page = page,
        PageSize = PageSize
    };

    private async Task ReloadAsync()
    {
        _loadCts?.Cancel();
        var cts = _loadCts = new CancellationTokenSource();

        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = Products.Count == 0;
        EmptyLabel.IsVisible = RetryButton.IsVisible = false;

        var result = await _api.GetProductsAsync(BuildQuery(1), cts.Token);
        if (cts.IsCancellationRequested)
            return;

        Refresh.IsRefreshing = false;
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = false;
        EmptyLabel.IsVisible = true;

        if (result.Success)
        {
            _page = 1;
            _hasMore = result.Data!.HasMore;
            Products.Clear();
            foreach (var item in result.Data.Items)
                Products.Add(item);
            EmptyLabel.Text = string.IsNullOrWhiteSpace(SearchBarProducts.Text) ? "No products yet. Tap + Add." : "No matching products.";
        }
        else if (Products.Count == 0)
        {
            EmptyLabel.Text = result.Error;
            RetryButton.IsVisible = true;
        }
        else
        {
            await DisplayAlert("Could not refresh", result.Error, "OK");
        }
    }

    private async Task LoadMoreAsync()
    {
        if (!_hasMore || _isLoadingMore || _loadCts == null)
            return;

        _isLoadingMore = true;
        LoadMoreIndicator.IsVisible = LoadMoreIndicator.IsRunning = true;
        var cts = _loadCts;
        try
        {
            var result = await _api.GetProductsAsync(BuildQuery(_page + 1), cts.Token);
            if (cts.IsCancellationRequested || !result.Success)
                return;

            _page++;
            _hasMore = result.Data!.HasMore;
            foreach (var item in result.Data.Items.Where(n => Products.All(p => p.ProductId != n.ProductId)))
                Products.Add(item);
        }
        finally
        {
            _isLoadingMore = false;
            LoadMoreIndicator.IsVisible = LoadMoreIndicator.IsRunning = false;
        }
    }

    private async void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDelay?.Cancel();
        var delay = _searchDelay = new CancellationTokenSource();
        try
        {
            await Task.Delay(400, delay.Token);
            await ReloadAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void Refresh_Refreshing(object? sender, EventArgs e) => await ReloadAsync();

    private async void Retry_Clicked(object sender, EventArgs e) => await ReloadAsync();

    private async void Products_RemainingItemsThresholdReached(object? sender, EventArgs e) => await LoadMoreAsync();

    private async void Add_Clicked(object sender, EventArgs e) =>
        await Navigation.PushAsync(new ProductFormPage());

    private async void Edit_Clicked(object sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: Product product })
            await Navigation.PushAsync(new ProductFormPage(product));
    }

    private async void ToggleAvailable_Clicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Product product } button)
            await ChangeStatusAsync(button, product, isAvailable: !product.IsAvailable);
    }

    private async void ToggleFeatured_Clicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: Product product } button)
            await ChangeStatusAsync(button, product, isFeatured: !product.IsFeatured);
    }

    private async Task ChangeStatusAsync(Button button, Product product, bool? isAvailable = null, bool? isFeatured = null)
    {
        button.IsEnabled = false;
        var result = await _api.UpdateStatusAsync(product.ProductId, isAvailable, isFeatured);
        button.IsEnabled = true;

        if (result.Success)
            product.CopyFrom(result.Data!);
        else
            await DisplayAlert("Not changed", result.Error, "OK");
    }

    private async void Delete_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: Product product } button)
            return;

        if (!await DisplayAlert("Delete", $"Delete \"{product.ProductName}\"? Customers will no longer see it.", "Delete", "Cancel"))
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
