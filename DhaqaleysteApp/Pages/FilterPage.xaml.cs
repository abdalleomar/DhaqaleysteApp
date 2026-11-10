using System.Globalization;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.Pages;

/// <summary>Filter sheet shown over a product list. <see cref="Result"/> completes when it closes.</summary>
public partial class FilterPage : ContentPage
{
    private static readonly (string Value, string Key)[] SortOptions =
    {
        ("newest", "SortNewest"),
        ("price_asc", "SortPriceAsc"),
        ("price_desc", "SortPriceDesc"),
        ("popular", "SortPopular"),
        ("name", "SortName")
    };

    private readonly TaskCompletionSource<ProductFilters?> _result = new();
    private readonly List<Category?> _categories;

    public Task<ProductFilters?> Result => _result.Task;

    public FilterPage(ProductFilters current, IReadOnlyList<Category> categories)
    {
        InitializeComponent();

        SortPicker.ItemsSource = SortOptions.Select(o => Loc.T(o.Key)).ToList();
        _categories = new List<Category?> { null };
        _categories.AddRange(categories);
        CategoryPicker.ItemsSource = _categories.Select(c => c?.LocalName ?? Loc.T("All")).ToList();

        Show(current);
    }

    private void Show(ProductFilters filters)
    {
        SortPicker.SelectedIndex = Math.Max(0, Array.FindIndex(SortOptions, o => o.Value == filters.Sort));
        CategoryPicker.SelectedIndex = Math.Max(0, _categories.FindIndex(c => c?.CategoryId == filters.CategoryId));
        InStockSwitch.IsToggled = filters.InStockOnly;
        MinEntry.Text = filters.MinPrice?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        MaxEntry.Text = filters.MaxPrice?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        ErrorLabel.IsVisible = false;
    }

    private void Reset_Clicked(object? sender, EventArgs e) => Show(new ProductFilters());

    private async void Cancel_Clicked(object? sender, EventArgs e) => await CloseAsync(null);

    private async void Apply_Clicked(object? sender, EventArgs e)
    {
        if (!TryReadPrice(MinEntry.Text, out var min) || !TryReadPrice(MaxEntry.Text, out var max) || min > max)
        {
            ErrorLabel.Text = Loc.T("InvalidPriceRange");
            ErrorLabel.IsVisible = true;
            return;
        }

        await CloseAsync(new ProductFilters
        {
            Sort = SortOptions[Math.Max(0, SortPicker.SelectedIndex)].Value,
            CategoryId = _categories.ElementAtOrDefault(Math.Max(0, CategoryPicker.SelectedIndex))?.CategoryId,
            InStockOnly = InStockSwitch.IsToggled,
            MinPrice = min,
            MaxPrice = max
        });
    }

    /// <summary>Empty is allowed (no limit). Accepts "12.5" and "12,5".</summary>
    private static bool TryReadPrice(string? text, out decimal? value)
    {
        value = null;
        var normalized = (text ?? "").Trim().Replace(',', '.');
        if (normalized.Length == 0)
            return true;
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
            return false;
        value = parsed;
        return true;
    }

    // Closed some other way (e.g. swiped down on iOS): treat as cancel.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _result.TrySetResult(null);
    }

    // Android back button closes the sheet without applying.
    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync(null);
        return true;
    }

    private async Task CloseAsync(ProductFilters? result)
    {
        if (!_result.TrySetResult(result))
            return; // already closing
        await Navigation.PopModalAsync();
    }
}
