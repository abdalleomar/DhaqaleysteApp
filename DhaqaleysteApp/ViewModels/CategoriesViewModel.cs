using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

public partial class CategoriesViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly NavigationService _navigation;
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

    public CategoriesViewModel(ApiService api, NavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    public ObservableCollection<Category> Categories { get; } = new();

    protected override string EmptyTitle => Loc.T("NoCategoriesTitle");
    protected override string EmptyMessage => Loc.T("NoProductsMessage");
    protected override string EmptyIcon => "";

    public async Task OnAppearingAsync()
    {
        if (NeedsReload)
            await LoadAsync(showSpinner: !_loadedOnce);
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
        IsLoading = showSpinner && Categories.Count == 0;
        var result = await _api.GetCategoriesAsync();
        IsLoading = false;

        if (!result.Success)
        {
            if (Categories.Count == 0)
                SetError(result.Error, result.IsNetworkError);
            return;
        }

        ClearError();
        _loadedOnce = true;
        _loadedVersion = CatalogEvents.Version;
        _loadedAtUtc = DateTime.UtcNow;
        Categories.Clear();
        foreach (var category in result.Data!.Where(c => c.ProductCount > 0))
            Categories.Add(category);
        IsEmpty = Categories.Count == 0;
        NotifyStateText();
    }

    [RelayCommand]
    private Task OpenCategoryAsync(Category category) => _navigation.OpenProductListAsync(new ProductListOptions
    {
        Title = category.LocalName,
        CategoryId = category.CategoryId
    });

    [RelayCommand]
    private Task SeeAllAsync() => _navigation.OpenProductListAsync(new ProductListOptions { Title = Loc.T("AllProducts") });
}
