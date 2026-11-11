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
    private int _loadedVersion = -1;
    private DateTime _loadedAtUtc;

    /// <summary>
    /// Reload when staff changed the catalogue, when nothing is shown yet, or when the data is a few
    /// minutes old. Otherwise switching tabs stays instant.
    /// </summary>
    private bool NeedsReload =>
        !_loadedOnce || IsEmpty || HasError ||
        _loadedVersion != CatalogEvents.Version ||
        DateTime.UtcNow - _loadedAtUtc > TimeSpan.FromMinutes(2);

    public HomeViewModel(ApiService api, LocalStore store, NavigationService navigation)
    {
        _api = api;
        _store = store;
        _navigation = navigation;
    }

    public ObservableCollection<Product> Featured { get; } = new();
    public ObservableCollection<Product> NewArrivals { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();

    [ObservableProperty] private string _companyName = "Dhaqaleyste";
    [ObservableProperty] private string _companyCity = "";
    [ObservableProperty] private bool _hasFeatured;
    [ObservableProperty] private bool _hasCategories;

    /// <summary>The first featured product, shown as the big banner.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHighlight), nameof(HighlightButtonText))]
    private Product? _highlight;

    public bool HasHighlight => Highlight != null;
    public string HighlightButtonText => Highlight == null ? "" : $"{Loc.T("View")} · {Highlight.PriceText}";
    public bool HasCity => CompanyCity.Length > 0;
    public bool IsEnglish => Loc.Language == AppLanguage.English;
    public bool IsSomali => Loc.Language == AppLanguage.Somali;

    partial void OnCompanyCityChanged(string value) => OnPropertyChanged(nameof(HasCity));
    [ObservableProperty] private bool _isLoadingMoreVisible;

    /// <summary>Called every time the page appears.</summary>
    public async Task OnAppearingAsync()
    {
        if (NeedsReload)
            await LoadAsync(showSpinner: !_loadedOnce);
        else
            await MarkFavoritesAsync(); // a heart may have changed on another screen
    }

    private Task MarkFavoritesAsync() =>
        _store.MarkFavoritesAsync(Featured.Concat(NewArrivals).Concat(Highlight != null ? new[] { Highlight } : Array.Empty<Product>()));

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
            _loadedVersion = CatalogEvents.Version;
            _loadedAtUtc = DateTime.UtcNow;

            if (companyTask.Result.Success)
            {
                var company = companyTask.Result.Data!;
                if (!string.IsNullOrWhiteSpace(company.CompanyName))
                    CompanyName = company.CompanyName;
                CompanyCity = company.City?.Trim() ?? "";
            }

            Replace(Categories, categoriesTask.Result.Success
                ? categoriesTask.Result.Data!.Where(c => c.ProductCount > 0)
                : Enumerable.Empty<Category>());
            HasCategories = Categories.Count > 0;

            var featured = featuredTask.Result.Success ? featuredTask.Result.Data!.Items : new List<Product>();
            Highlight = featured.FirstOrDefault();
            Replace(Featured, featured.Skip(1));
            HasFeatured = Featured.Count > 0;

            _page = 1;
            _hasMore = newest.Data!.HasMore;
            Replace(NewArrivals, newest.Data.Items);
            await MarkFavoritesAsync();
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
            var added = result.Data.Items.Where(n => NewArrivals.All(p => p.ProductId != n.ProductId)).ToList();
            await _store.MarkFavoritesAsync(added);
            foreach (var item in added)
                NewArrivals.Add(item);
        }
        finally
        {
            _isLoadingMore = false;
            IsLoadingMoreVisible = false;
        }
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Loc.SetLanguage(IsEnglish ? AppLanguage.Somali : AppLanguage.English);
        App.RestartShell(selectTab: "home");
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
