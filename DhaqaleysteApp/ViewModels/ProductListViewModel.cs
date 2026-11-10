using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

/// <summary>Search results, a category, featured products or all products, with filters and paging.</summary>
public partial class ProductListViewModel : BaseViewModel
{
    private const int PageSize = 20;

    private readonly ApiService _api;
    private readonly NavigationService _navigation;

    private ProductFilters _filters = new();
    private bool? _featured;
    private List<Category> _categories = new();
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchDelay;
    private int _page;
    private bool _hasMore;
    private bool _isLoadingMore;
    private bool _initialized;

    public ProductListViewModel(ApiService api, NavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    public ObservableCollection<Product> Products { get; } = new();

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _resultsText = "";
    [ObservableProperty] private string _filtersText = Loc.T("Filters");
    [ObservableProperty] private bool _isLoadingMoreVisible;

    /// <summary>Set when opened from the search box, so the page focuses the search field.</summary>
    public bool FocusSearch { get; private set; }

    protected override string EmptyTitle => Loc.T("NoResultsTitle");
    protected override string EmptyMessage => Loc.T("NoResultsMessage");
    protected override string EmptyIcon => "";

    public void Initialize(ProductListOptions options)
    {
        Title = options.Title;
        _featured = options.Featured;
        _filters = new ProductFilters { Sort = options.Sort, CategoryId = options.CategoryId };
        FocusSearch = options.FocusSearch;
        // Set the field directly so the first load is not triggered twice.
#pragma warning disable MVVMTK0034
        _searchText = options.Search ?? "";
#pragma warning restore MVVMTK0034
        UpdateFiltersText();
    }

    public async Task OnAppearingAsync()
    {
        if (_initialized)
            return;
        _initialized = true;

        var categories = await _api.GetCategoriesAsync();
        if (categories.Success)
            _categories = categories.Data!.Where(c => c.ProductCount > 0).ToList();

        // Opened from the search box: wait for the customer to type.
        if (FocusSearch && SearchText.Length == 0)
            return;

        await ReloadAsync(showSpinner: true);
    }

    partial void OnSearchTextChanged(string value) => _ = DebouncedReloadAsync();

    private async Task DebouncedReloadAsync()
    {
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

    private ProductQuery BuildQuery(int page) => new()
    {
        Search = SearchText,
        CategoryId = _filters.CategoryId,
        MinPrice = _filters.MinPrice,
        MaxPrice = _filters.MaxPrice,
        InStock = _filters.InStockOnly ? true : null,
        Featured = _featured,
        Sort = _filters.Sort,
        Page = page,
        PageSize = PageSize
    };

    /// <summary>Loads page 1. A newer search cancels an older one, so results never arrive out of order.</summary>
    private async Task ReloadAsync(bool showSpinner)
    {
        _loadCts?.Cancel();
        var cts = _loadCts = new CancellationTokenSource();

        if (showSpinner)
        {
            Products.Clear();
            IsEmpty = false;
            IsLoading = true;
        }

        var result = await _api.GetProductsAsync(BuildQuery(1), cts.Token);
        if (cts.IsCancellationRequested)
            return;

        IsLoading = false;
        IsRefreshing = false;

        if (!result.Success)
        {
            if (Products.Count == 0)
                SetError(result.Error, result.IsNetworkError);
            return;
        }

        ClearError();
        _page = 1;
        _hasMore = result.Data!.HasMore;
        Products.Clear();
        foreach (var item in result.Data.Items)
            Products.Add(item);

        ResultsText = Loc.T("ResultsCount", result.Data.TotalCount);
        IsEmpty = Products.Count == 0;
        NotifyStateText();
    }

    [RelayCommand]
    private Task RefreshAsync() => ReloadAsync(showSpinner: false);

    [RelayCommand]
    private Task RetryAsync() => ReloadAsync(showSpinner: true);

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!_hasMore || _isLoadingMore || _loadCts == null)
            return;

        _isLoadingMore = true;
        IsLoadingMoreVisible = true;
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
            IsLoadingMoreVisible = false;
        }
    }

    [RelayCommand]
    private async Task OpenFiltersAsync()
    {
        var chosen = await _navigation.PickFiltersAsync(_filters.Clone(), _categories);
        if (chosen == null)
            return;

        _filters = chosen;
        UpdateFiltersText();
        await ReloadAsync(showSpinner: true);
    }

    [RelayCommand]
    private Task OpenProductAsync(Product product) => _navigation.OpenProductAsync(product);

    private void UpdateFiltersText() =>
        FiltersText = _filters.ActiveCount == 0 ? Loc.T("Filters") : $"{Loc.T("Filters")} ({_filters.ActiveCount})";
}
