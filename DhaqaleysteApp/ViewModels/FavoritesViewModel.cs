using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.ViewModels;

/// <summary>Favourites saved on the device, refreshed with current prices and stock from the server.</summary>
public partial class FavoritesViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly LocalStore _store;
    private readonly NavigationService _navigation;

    public FavoritesViewModel(ApiService api, LocalStore store, NavigationService navigation)
    {
        _api = api;
        _store = store;
        _navigation = navigation;
    }

    public ObservableCollection<Product> Favorites { get; } = new();

    /// <summary>Shown when the server could not be reached and saved details are displayed.</summary>
    [ObservableProperty] private bool _showingSavedDetails;

    protected override string EmptyTitle => Loc.T("FavoritesEmptyTitle");
    protected override string EmptyMessage => Loc.T("FavoritesEmptyMessage");
    protected override string EmptyIcon => "";

    public Task OnAppearingAsync() => LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    private async Task LoadAsync()
    {
        // Show what is saved on the phone straight away.
        var saved = await _store.GetFavoritesAsync();
        Replace(saved.Select(s => s.ToProduct()));
        IsEmpty = Favorites.Count == 0;
        NotifyStateText();
        if (Favorites.Count == 0)
            return;

        var ids = string.Join(",", saved.Select(s => s.ProductId));
        var result = await _api.GetProductsAsync(new ProductQuery { PageSize = LocalStore.MaxFavorites }.WithIds(ids));
        if (!result.Success)
        {
            ShowingSavedDetails = true;
            return;
        }

        ShowingSavedDetails = false;
        await _store.RefreshFavoritesAsync(result.Data!.Items);
        var byId = result.Data.Items.ToDictionary(p => p.ProductId);
        Replace(saved.Where(s => byId.ContainsKey(s.ProductId)).Select(s => byId[s.ProductId]));
        IsEmpty = Favorites.Count == 0;
        NotifyStateText();
    }

    [RelayCommand]
    private Task OpenProductAsync(Product product) => _navigation.OpenProductAsync(product);

    [RelayCommand]
    private Task BrowseAsync() => _navigation.GoToTabAsync("home");

    private void Replace(IEnumerable<Product> items)
    {
        Favorites.Clear();
        foreach (var item in items)
            Favorites.Add(item);
    }
}
