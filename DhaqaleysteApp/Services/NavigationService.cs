using DhaqaleysteApp.Models;
using DhaqaleysteApp.Pages;
using DhaqaleysteApp.ViewModels;

namespace DhaqaleysteApp.Services;

/// <summary>Opens screens on the current tab, so view models never create pages themselves.</summary>
public class NavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    private static INavigation Navigation => Shell.Current.Navigation;

    public Task OpenProductAsync(Product product)
    {
        var viewModel = _services.GetRequiredService<ProductDetailsViewModel>();
        viewModel.Initialize(product);
        return Navigation.PushAsync(new ProductDetailsPage(viewModel));
    }

    public Task OpenProductListAsync(ProductListOptions options)
    {
        var viewModel = _services.GetRequiredService<ProductListViewModel>();
        viewModel.Initialize(options);
        return Navigation.PushAsync(new ProductListPage(viewModel));
    }

    /// <summary>Shows the filter sheet and returns the chosen filters, or null when cancelled.</summary>
    public async Task<ProductFilters?> PickFiltersAsync(ProductFilters current, IReadOnlyList<Category> categories)
    {
        var page = new FilterPage(current, categories);
        await Navigation.PushModalAsync(new NavigationPage(page));
        return await page.Result;
    }

    public async Task OpenStaffAreaAsync()
    {
        var session = _services.GetRequiredService<SessionService>();
        Page page = await session.IsSignedInAsync() ? new AdminDashboardPage() : new LoginPage();
        await Navigation.PushAsync(page);
    }

    public Task GoToTabAsync(string route) => Shell.Current.GoToAsync($"//{route}");
}

/// <summary>What a product list shows when it opens.</summary>
public class ProductListOptions
{
    public string Title { get; set; } = "";
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string Sort { get; set; } = "newest";
    public bool? Featured { get; set; }
    public bool FocusSearch { get; set; }
}

/// <summary>Filters chosen in the filter sheet.</summary>
public class ProductFilters
{
    public string Sort { get; set; } = "newest";
    public bool InStockOnly { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? CategoryId { get; set; }

    public int ActiveCount =>
        (Sort != "newest" ? 1 : 0) + (InStockOnly ? 1 : 0) + (MinPrice != null || MaxPrice != null ? 1 : 0) + (CategoryId != null ? 1 : 0);

    public ProductFilters Clone() => (ProductFilters)MemberwiseClone();
}
