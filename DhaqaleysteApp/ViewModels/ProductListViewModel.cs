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

    private static readonly (string Value, string Key)[] SortOptions =
    {
        ("newest", "SortNewest"),
        ("price_asc", "SortPriceAsc"),
        ("price_desc", "SortPriceDesc"),
        ("popular", "SortPopular"),
        ("name", "SortName")
    };

    private readonly ApiService _api;
    private readonly NavigationService _navigation;
    private readonly LocalStore _store;
    private string _categoryName = "";

    private ProductFilters _filters = new();
    private bool? _featured;
    private List<Category> _categories = new();
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _searchDelay;
    private int _page;
    private bool _hasMore;
    private bool _isLoadingMore;
    private bool _initialized;

    public ProductListViewModel(ApiService api, NavigationService navigation, LocalStore store)
    {
        _api = api;
        _navigation = navigation;
        _store = store;
    }

    public ObservableCollection<Product> Products { get; } = new();

    /// <summary>Filters in use, shown as chips the customer can tap to remove.</summary>
    public ObservableCollection<FilterChip> ActiveChips { get; } = new();

    [ObservableProperty] private string _sortText = Loc.T("SortNewest");
    [ObservableProperty] private bool _hasSearchText;

    partial void OnSearchTextChanging(string value) => HasSearchText = !string.IsNullOrEmpty(value);

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _resultsText = "";
    [ObservableProperty] private string _filtersText = Loc.T("Filters");
    [ObservableProperty] private bool _isLoadingMoreVisible;

    /// <summary>Set when opened from the search box, so the page focuses the search field.</summary>
    public bool FocusSearch { get; private set; }

    protected override string EmptyTitle => Loc.T("NoResultsTitle");
    protected override string EmptyMessage => Loc.T("NoResultsMessage");
    protected override string EmptyIcon => Icons.Search;

    public void Initialize(ProductListOptions options)
    {
        Title = options.Title;
        _featured = options.Featured;
        _filters = new ProductFilters { Sort = options.Sort, CategoryId = options.CategoryId };
        _categoryName = options.CategoryId != null ? options.Title : "";
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
        {
            await _store.MarkFavoritesAsync(Products); // a heart may have changed on the product page
            return;
        }
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
        await _store.MarkFavoritesAsync(result.Data.Items);
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
            var added = result.Data.Items.Where(n => Products.All(p => p.ProductId != n.ProductId)).ToList();
            await _store.MarkFavoritesAsync(added);
            foreach (var item in added)
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
        _categoryName = _categories.FirstOrDefault(c => c.CategoryId == chosen.CategoryId)?.LocalName ?? "";
        UpdateFiltersText();
        await ReloadAsync(showSpinner: true);
    }

    [RelayCommand]
    private async Task ChooseSortAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page == null)
            return;

        var labels = SortOptions.Select(o => Loc.T(o.Key)).ToArray();
        var choice = await page.DisplayActionSheet(Loc.T("Sort"), Loc.T("Cancel"), null, labels);
        var index = Array.IndexOf(labels, choice);
        if (index < 0 || SortOptions[index].Value == _filters.Sort)
            return;

        _filters.Sort = SortOptions[index].Value;
        UpdateFiltersText();
        await ReloadAsync(showSpinner: true);
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = "";

    [RelayCommand]
    private Task GoBackAsync() => Shell.Current.Navigation.PopAsync();

    [RelayCommand]
    private Task OpenProductAsync(Product product) => _navigation.OpenProductAsync(product);

    private void UpdateFiltersText()
    {
        var count = _filters.ActiveCount - (_filters.Sort != "newest" ? 1 : 0) + (_featured == true ? 1 : 0);
        FiltersText = count == 0 ? Loc.T("Filters") : Loc.T("FiltersCount", count);
        SortText = Loc.T(SortOptions.FirstOrDefault(o => o.Value == _filters.Sort).Key ?? "SortNewest");

        ActiveChips.Clear();
        if (_featured == true)
            ActiveChips.Add(new FilterChip(Loc.T("Featured"), () => { _featured = null; }, this));
        if (_filters.CategoryId != null && _categoryName.Length > 0)
            ActiveChips.Add(new FilterChip(_categoryName, () => { _filters.CategoryId = null; _categoryName = ""; }, this));
        if (_filters.InStockOnly)
            ActiveChips.Add(new FilterChip(Loc.T("InStock"), () => _filters.InStockOnly = false, this));
        if (_filters.MinPrice != null || _filters.MaxPrice != null)
        {
            var text = _filters.MinPrice != null && _filters.MaxPrice != null
                ? Loc.T("PriceBetween", PriceFormatter.Format(_filters.MinPrice.Value), PriceFormatter.Format(_filters.MaxPrice.Value))
                : _filters.MaxPrice != null
                    ? Loc.T("PriceUnder", PriceFormatter.Format(_filters.MaxPrice.Value))
                    : Loc.T("PriceOver", PriceFormatter.Format(_filters.MinPrice!.Value));
            ActiveChips.Add(new FilterChip(text, () => { _filters.MinPrice = null; _filters.MaxPrice = null; }, this));
        }
    }

    /// <summary>Removes one filter (called by a chip) and reloads.</summary>
    internal async Task RemoveFilterAsync(Action clear)
    {
        clear();
        UpdateFiltersText();
        await ReloadAsync(showSpinner: true);
    }
}

/// <summary>An active filter shown as a removable chip on the product list.</summary>
public class FilterChip
{
    public FilterChip(string text, Action clear, ProductListViewModel owner)
    {
        Text = text;
        RemoveCommand = new AsyncRelayCommand(() => owner.RemoveFilterAsync(clear));
    }

    public string Text { get; }
    public IAsyncRelayCommand RemoveCommand { get; }
}
