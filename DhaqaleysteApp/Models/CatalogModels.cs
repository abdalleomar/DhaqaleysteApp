using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>Filters for the product list. Matches the API's query string.</summary>
public class ProductQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public bool? Featured { get; set; }
    public string Sort { get; set; } = "newest";
    public bool IncludeHidden { get; set; }
    public string? Ids { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public ProductQuery WithIds(string ids)
    {
        Ids = ids;
        return this;
    }

    public string ToQueryString()
    {
        var parts = new List<string> { $"page={Page}", $"pageSize={PageSize}", $"sort={Uri.EscapeDataString(Sort)}" };
        if (!string.IsNullOrWhiteSpace(Search)) parts.Add($"search={Uri.EscapeDataString(Search.Trim())}");
        if (CategoryId != null) parts.Add($"categoryId={CategoryId}");
        if (MinPrice != null) parts.Add($"minPrice={MinPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (MaxPrice != null) parts.Add($"maxPrice={MaxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        if (InStock != null) parts.Add($"inStock={InStock.Value.ToString().ToLowerInvariant()}");
        if (Featured != null) parts.Add($"featured={Featured.Value.ToString().ToLowerInvariant()}");
        if (IncludeHidden) parts.Add("includeHidden=true");
        if (!string.IsNullOrWhiteSpace(Ids)) parts.Add($"ids={Uri.EscapeDataString(Ids)}");
        return string.Join("&", parts);
    }
}

public class Category : BindableModel
{
    private bool _isSelected;

    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string NameSo { get; set; } = "";
    public string Icon { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int ProductCount { get; set; }

    /// <summary>Used by the category chips on the home screen.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>Name in the selected language, with the icon when there is one.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Icon) ? LocalName : $"{Icon} {LocalName}";
    public string LocalName => Loc.Pick(Name, NameSo);
    public string ProductCountText => Loc.T("ProductsCount", ProductCount);
    public string IconOrDefault => string.IsNullOrWhiteSpace(Icon) ? "🛍️" : Icon;
    public string StaffSummary => $"{ProductCount} product(s){(IsActive ? "" : " · hidden from customers")}";

    /// <summary>The "All" chip.</summary>
    public static Category All() => new() { CategoryId = 0, Name = Loc.T("All") };
}

public class CompanySettings
{
    public string CompanyName { get; set; } = "";
    public string AboutEn { get; set; } = "";
    public string AboutSo { get; set; } = "";
    public string Phone { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string WorkingHours { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string FacebookUrl { get; set; } = "";
    public string InstagramUrl { get; set; } = "";
    public string TikTokUrl { get; set; } = "";
    public string WebsiteUrl { get; set; } = "";
    public string CurrencyCode { get; set; } = "USD";
}

public class CatalogStats
{
    public int TotalProducts { get; set; }
    public int InStock { get; set; }
    public int OutOfStock { get; set; }
    public int Featured { get; set; }
    public int Categories { get; set; }
    public int ActiveStaff { get; set; }
    public List<Product> MostViewed { get; set; } = new();
    public List<Product> LowStock { get; set; } = new();
}
