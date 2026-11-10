using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private const int PageSize = 20;

    private readonly ApiService _api;
    private readonly LocalStore _store;
    private readonly NavigationService _navigation;

    private int _page;
    private bool _hasMore;
    private bool _isLoadingMore;
    private bool _loadedOnce;

    public HomeViewModel(ApiService api, LocalStore store, NavigationService navigation)
    {
        _api = api;
        _store = store;
        _navigation = navigation;
    }

    public ObservableCollection<Product> Featured { get; } = new();
    public ObservableCollection<Product> NewArrivals { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Product> Recent { get; } = new();

    [ObservableProperty] private string _companyName = "Dhaqaleyste";
    [ObservableProperty] private bool _hasFeatured;
    [ObservableProperty] private bool _hasCategories;
    [ObservableProperty] private bool _hasRecent;
    [ObservableProperty] private bool _isLoadingMoreVisible;

    /// <summary>Called every time the page appears.</summary>
    public async Task OnAppearingAsync()
    {
        await LoadRecentAsync();
        if (!_loadedOnce)
            await LoadAsync(showSpinner: true);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(showSpinner: false);
        IsRefreshing = false;
    }

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showSpinner: true);

    private async Task LoadAsync(bool showSpinner)
    {
        if (IsLoading)
            return;

        IsLoading = showSpinner && NewArrivals.Count == 0;
        try
        {
            // The four requests run in parallel.
            var companyTask = _api.GetCompanyAsync();
            var categoriesTask = _api.GetCategoriesAsync();
            var featuredTask = _api.GetProductsAsync(new ProductQuery { Featured = true, PageSize = 10 });
            var newestTask = _api.GetProductsAsync(new ProductQuery { Page = 1, PageSize = PageSize });
            await Task.WhenAll(companyTask, categoriesTask, featuredTask, newestTask);

            var newest = newestTask.Result;
            if (!newest.Success)
            {
                if (NewArrivals.Count == 0)
                    SetError(newest.Error, newest.IsNetworkError);
                else if (Shell.Current?.CurrentPage is { } page)
                    await page.DisplayAlert(Loc.T("CouldNotRefresh"), newest.Error, Loc.T("Ok"));
                return;
            }

            ClearError();
            _loadedOnce = true;

            if (companyTask.Result.Success && !string.IsNullOrWhiteSpace(companyTask.Result.Data!.CompanyName))
                CompanyName = companyTask.Result.Data.CompanyName;

            Replace(Categories, categoriesTask.Result.Success
                ? categoriesTask.Result.Data!.Where(c => c.ProductCount > 0)
                : Enumerable.Empty<Category>());
            HasCategories = Categories.Count > 0;

            Replace(Featured, featuredTask.Result.Success ? featuredTask.Result.Data!.Items : Enumerable.Empty<Product>());
            HasFeatured = Featured.Count > 0;

            _page = 1;
            _hasMore = newest.Data!.HasMore;
            Replace(NewArrivals, newest.Data.Items);
            IsEmpty = NewArrivals.Count == 0;
            NotifyStateText();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!_hasMore || _isLoadingMore)
            return;

        _isLoadingMore = true;
        IsLoadingMoreVisible = true;
        try
        {
            var result = await _api.GetProductsAsync(new ProductQuery { Page = _page + 1, PageSize = PageSize });
            if (!result.Success)
                return; // scrolling again retries

            _page++;
            _hasMore = result.Data!.HasMore;
            foreach (var item in result.Data.Items.Where(n => NewArrivals.All(p => p.ProductId != n.ProductId)))
                NewArrivals.Add(item);
        }
        finally
        {
            _isLoadingMore = false;
            IsLoadingMoreVisible = false;
        }
    }

    private async Task LoadRecentAsync()
    {
        var recent = await _store.GetRecentAsync();
        Replace(Recent, recent.Take(10).Select(r => r.ToProduct()));
        HasRecent = Recent.Count > 0;
    }

    [RelayCommand]
    private Task OpenProductAsync(Product product) => _navigation.OpenProductAsync(product);

    [RelayCommand]
    private Task OpenCategoryAsync(Category category) => _navigation.OpenProductListAsync(new ProductListOptions
    {
        Title = category.LocalName,
        CategoryId = category.CategoryId
    });

    [RelayCommand]
    private Task OpenSearchAsync() => _navigation.OpenProductListAsync(new ProductListOptions
    {
        Title = Loc.T("SearchResults"),
        FocusSearch = true
    });

    [RelayCommand]
    private Task SeeAllFeaturedAsync() => _navigation.OpenProductListAsync(new ProductListOptions
    {
        Title = Loc.T("Featured"),
        Featured = true
    });

    [RelayCommand]
    private Task SeeAllProductsAsync() => _navigation.OpenProductListAsync(new ProductListOptions
    {
        Title = Loc.T("AllProducts")
    });

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}
