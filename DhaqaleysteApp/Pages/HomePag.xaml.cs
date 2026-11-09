using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class HomePage : ContentPage
{
    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private readonly SessionService _session = ServiceHelper.Get<SessionService>();

    private List<Product> _allProducts = new();
    private CancellationTokenSource? _searchDelay;
    private bool _isLoading;
    private bool _loadedOnce;

    public ObservableCollection<Product> Products { get; } = new();

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        StaffButton.Text = await _session.IsSignedInAsync() ? "Dashboard" : "Staff";

        // Reload when coming back, so staff changes show up, but never run two loads at once.
        await LoadProductsAsync(showSpinner: !_loadedOnce);
    }

    private async Task LoadProductsAsync(bool showSpinner)
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            if (showSpinner && Products.Count == 0)
                ShowState(loading: true);

            var result = await _api.GetProductsAsync();
            if (result.Success)
            {
                _loadedOnce = true;
                _allProducts = result.Data!;
                ApplyFilter();
                ShowState(loading: false);
            }
            else if (Products.Count == 0)
            {
                ShowState(loading: false, error: result.Error);
            }
            else
            {
                // Keep showing the products we already have.
                await DisplayAlert("Could not refresh", result.Error, "OK");
            }
        }
        finally
        {
            _isLoading = false;
            ProductsRefresh.IsRefreshing = false;
        }
    }

    private void ShowState(bool loading, string? error = null)
    {
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = loading;
        EmptyIcon.IsVisible = !loading;
        EmptyTitle.IsVisible = !loading;
        EmptyMessage.IsVisible = !loading;
        RetryButton.IsVisible = !loading && error != null;

        if (error != null)
        {
            EmptyIcon.Text = "📡";
            EmptyTitle.Text = "Could not load products";
            EmptyMessage.Text = error;
        }
        else if (!string.IsNullOrWhiteSpace(SearchBarProducts.Text))
        {
            EmptyIcon.Text = "🔍";
            EmptyTitle.Text = "No matching products";
            EmptyMessage.Text = "Try a different search.";
        }
        else
        {
            EmptyIcon.Text = "📦";
            EmptyTitle.Text = "No products available";
            EmptyMessage.Text = "Please check back soon.";
        }
    }

    private void ApplyFilter()
    {
        var search = SearchBarProducts.Text?.Trim() ?? "";

        var filtered = string.IsNullOrEmpty(search)
            ? _allProducts
            : _allProducts.Where(x =>
                Contains(x.ProductName, search) ||
                Contains(x.Description, search) ||
                Contains(x.Category, search) ||
                Contains(x.Location, search)).ToList();

        Products.Clear();
        foreach (var item in filtered)
            Products.Add(item);
    }

    private static bool Contains(string? value, string search) =>
        value?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true;

    private async void SearchBarProducts_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Debounce: filter the products we already have once typing pauses. No network call.
        _searchDelay?.Cancel();
        _searchDelay = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, _searchDelay.Token);
            ApplyFilter();
            ShowState(loading: false);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void ProductsRefresh_Refreshing(object sender, EventArgs e) =>
        await LoadProductsAsync(showSpinner: false);

    private async void Retry_Clicked(object sender, EventArgs e) =>
        await LoadProductsAsync(showSpinner: true);

    private async void Staff_Clicked(object sender, EventArgs e)
    {
        if (await _session.IsSignedInAsync())
            await Navigation.PushAsync(new AdminDashboardPage());
        else
            await Navigation.PushAsync(new LoginPage());
    }

    private async void ViewDetails_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Product product)
            await Navigation.PushAsync(new ProductDetailsPage(product));
    }
}
