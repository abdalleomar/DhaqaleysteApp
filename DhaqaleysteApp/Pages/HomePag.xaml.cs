using System.Collections.ObjectModel;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;

namespace DhaqaleysteApp.Pages;

public partial class HomePage : ContentPage
{
    private const int PageSize = 20;

    private readonly ApiService _api = ServiceHelper.Get<ApiService>();
    private readonly SessionService _session = ServiceHelper.Get<SessionService>();

    private CancellationTokenSource? _searchDelay;
    private CancellationTokenSource? _loadCts;
    private int _page;
    private bool _hasMore;
    private bool _isLoadingMore;
    private bool _loadedOnce;
    private int? _categoryId;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        StaffButton.Text = await _session.IsSignedInAsync() ? "Dashboard" : "Staff";

        // Reload when coming back so staff changes show up; keep showing the old list while it loads.
        _ = LoadCategoriesAsync();
        await ReloadAsync(showSpinner: !_loadedOnce);
    }

    private async Task LoadCategoriesAsync()
    {
        var result = await _api.GetCategoriesAsync();
        if (!result.Success)
            return;

        Categories.Clear();
        var all = Category.All();
        all.IsSelected = _categoryId == null;
        Categories.Add(all);
        foreach (var category in result.Data!.Where(c => c.ProductCount > 0))
        {
            category.IsSelected = category.CategoryId == _categoryId;
            Categories.Add(category);
        }
    }

    /// <summary>Loads the first page for the current search and category. Cancels any load in progress.</summary>
    private async Task ReloadAsync(bool showSpinner)
    {
        _loadCts?.Cancel();
        var cts = _loadCts = new CancellationTokenSource();

        if (showSpinner && Products.Count == 0)
            ShowState(loading: true);

        var result = await _api.GetProductsAsync(BuildQuery(page: 1), cts.Token);
        if (cts.IsCancellationRequested)
            return; // a newer search replaced this one

        ProductsRefresh.IsRefreshing = false;

        if (result.Success)
        {
            _loadedOnce = true;
            _page = 1;
            _hasMore = result.Data!.HasMore;
            Products.Clear();
            foreach (var item in result.Data.Items)
                Products.Add(item);
            ShowState(loading: false);
        }
        else if (Products.Count == 0)
        {
            ShowState(loading: false, error: result.Error);
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
            var result = await _api.GetProductsAsync(BuildQuery(page: _page + 1), cts.Token);
            if (cts.IsCancellationRequested || !result.Success)
                return; // scrolling again retries

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

    private ProductQuery BuildQuery(int page) => new()
    {
        Search = SearchBarProducts.Text,
        CategoryId = _categoryId,
        Page = page,
        PageSize = PageSize
    };

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
        else if (!string.IsNullOrWhiteSpace(SearchBarProducts.Text) || _categoryId != null)
        {
            EmptyIcon.Text = "🔍";
            EmptyTitle.Text = "No matching products";
            EmptyMessage.Text = "Try a different search or category.";
        }
        else
        {
            EmptyIcon.Text = "📦";
            EmptyTitle.Text = "No products available";
            EmptyMessage.Text = "Please check back soon.";
        }
    }

    private async void SearchBarProducts_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Debounce: search on the server once typing pauses.
        _searchDelay?.Cancel();
        var delay = _searchDelay = new CancellationTokenSource();
        try
        {
            await Task.Delay(400, delay.Token);
            await ReloadAsync(showSpinner: true);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async void Category_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: Category tapped })
            return;

        _categoryId = tapped.CategoryId == 0 ? null : tapped.CategoryId;
        foreach (var category in Categories)
            category.IsSelected = category.CategoryId == tapped.CategoryId;

        Products.Clear();
        await ReloadAsync(showSpinner: true);
    }

    private async void Products_RemainingItemsThresholdReached(object? sender, EventArgs e) =>
        await LoadMoreAsync();

    private async void ProductsRefresh_Refreshing(object? sender, EventArgs e)
    {
        _ = LoadCategoriesAsync();
        await ReloadAsync(showSpinner: false);
    }

    private async void Retry_Clicked(object sender, EventArgs e) =>
        await ReloadAsync(showSpinner: true);

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
