using System.Text.Json.Serialization;
using DhaqaleysteApp.Services;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.Models;

/// <summary>
/// A product as returned by the API. List calls fill the summary fields; the details call also
/// fills <see cref="Description"/>, <see cref="Contact"/> and <see cref="Media"/>.
/// </summary>
public class Product : BindableModel
{
    private string _productName = "";
    private decimal _price;
    private int _quantity;
    private bool _isAvailable = true;
    private bool _isFeatured;
    private string _imageUrl = "";
    private bool _isFavorite;

    public int ProductId { get; set; }

    public string ProductName
    {
        get => _productName;
        set => Set(ref _productName, value);
    }

    public string ShortDescription { get; set; } = "";
    public string Description { get; set; } = "";

    public decimal Price
    {
        get => _price;
        set => Set(ref _price, value, alsoChanged: new[] { nameof(PriceText) });
    }

    public int Quantity
    {
        get => _quantity;
        set => Set(ref _quantity, value, alsoChanged: new[] { nameof(InStock), nameof(StockText), nameof(StaffStockText), nameof(IsOutOfStock) });
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        set => Set(ref _isAvailable, value, alsoChanged: new[] { nameof(InStock), nameof(StockText), nameof(StaffStockText), nameof(IsOutOfStock), nameof(AvailabilityActionText) });
    }

    public bool IsFeatured
    {
        get => _isFeatured;
        set => Set(ref _isFeatured, value, alsoChanged: new[] { nameof(FeaturedActionText) });
    }

    public string Location { get; set; } = "";
    public string Contact { get; set; } = "";
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategoryNameSo { get; set; } = "";
    public int ImageCount { get; set; }
    public bool HasVideo { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Main photo. The API sends a server path; ApiService turns it into a full URL.</summary>
    public string ImageUrl
    {
        get => _imageUrl;
        set => Set(ref _imageUrl, value, alsoChanged: new[] { nameof(HasImage) });
    }

    public List<ProductMediaItem> Media { get; set; } = new();

    /// <summary>Saved on this phone as a favourite (set by the screens, not sent by the API).</summary>
    [JsonIgnore]
    public bool IsFavorite
    {
        get => _isFavorite;
        set => Set(ref _isFavorite, value);
    }

    // ---------- Display helpers ----------

    [JsonIgnore] public bool InStock => IsAvailable && Quantity > 0;
    [JsonIgnore] public bool IsOutOfStock => !InStock;
    [JsonIgnore] public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    [JsonIgnore] public string PriceText => PriceFormatter.Format(Price);
    [JsonIgnore] public string StockText => !IsAvailable ? Loc.T("SoldOut") : Quantity > 0 ? Loc.T("InStock") : Loc.T("OutOfStock");
    [JsonIgnore] public string CategoryDisplayName => Loc.Pick(CategoryName, CategoryNameSo);
    [JsonIgnore] public bool HasCategory => !string.IsNullOrWhiteSpace(CategoryName);

    /// <summary>Staff lists show the exact quantity.</summary>
    [JsonIgnore] public string StaffStockText => !IsAvailable ? "Sold out" : Quantity > 0 ? $"In stock ({Quantity})" : "Out of stock";
    [JsonIgnore] public string AvailabilityActionText => IsAvailable ? "Mark sold out" : "Mark available";
    [JsonIgnore] public string FeaturedActionText => IsFeatured ? "Unfeature" : "Feature";

    [JsonIgnore] public IEnumerable<ProductMediaItem> Images => Media.Where(m => m.IsImage);
    [JsonIgnore] public IEnumerable<ProductMediaItem> Videos => Media.Where(m => m.IsVideo);

    /// <summary>Copies list-visible fields from a fresher copy (e.g. after a status change).</summary>
    public void CopyFrom(Product other)
    {
        ProductName = other.ProductName;
        ShortDescription = other.ShortDescription;
        Price = other.Price;
        Quantity = other.Quantity;
        IsAvailable = other.IsAvailable;
        IsFeatured = other.IsFeatured;
        Location = other.Location;
        CategoryId = other.CategoryId;
        CategoryName = other.CategoryName;
        CategoryNameSo = other.CategoryNameSo;
        ImageUrl = other.ImageUrl;
    }
}

public class ProductMediaItem
{
    public const string ImageKind = "Image";
    public const string VideoKind = "Video";

    public int ProductMediaId { get; set; }

    /// <summary>Image or Video.</summary>
    public string Kind { get; set; } = ImageKind;

    /// <summary>Server path as stored by the API (sent back unchanged when saving).</summary>
    public string Url { get; set; } = "";

    public int SortOrder { get; set; }

    /// <summary>Full URL for display, filled in by ApiService.</summary>
    [JsonIgnore] public string DisplayUrl { get; set; } = "";

    [JsonIgnore] public bool IsImage => Kind == ImageKind;
    [JsonIgnore] public bool IsVideo => Kind == VideoKind;
}
