using System.ComponentModel.DataAnnotations;
using DhaqaleysteApi.Models;

namespace DhaqaleysteApi.Dtos
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasMore => Page < TotalPages;
    }

    public static class ProductSort
    {
        public const string Newest = "newest";
        public const string PriceAsc = "price_asc";
        public const string PriceDesc = "price_desc";
        public const string Name = "name";
        public const string Popular = "popular";

        public static readonly string[] All = { Newest, PriceAsc, PriceDesc, Name, Popular };
    }

    /// <summary>Query string for GET /api/products.</summary>
    public class ProductQuery
    {
        [StringLength(100)]
        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        [Range(typeof(decimal), "0", "100000000")]
        public decimal? MinPrice { get; set; }

        [Range(typeof(decimal), "0", "100000000")]
        public decimal? MaxPrice { get; set; }

        /// <summary>true = only products that can be bought now.</summary>
        public bool? InStock { get; set; }

        public bool? Featured { get; set; }

        /// <summary>Comma-separated product ids (max 50), e.g. for a customer's saved favourites.</summary>
        [StringLength(600)]
        [RegularExpression(@"^\d+(,\d+)*$", ErrorMessage = "Ids must be a comma-separated list of numbers.")]
        public string? Ids { get; set; }

        /// <summary>Staff only: also list products in hidden categories. Ignored for customers.</summary>
        public bool IncludeHidden { get; set; }

        /// <summary>newest (default), price_asc, price_desc, name, popular.</summary>
        public string? Sort { get; set; }

        [Range(1, 10_000)]
        public int Page { get; set; } = 1;

        [Range(1, 50)]
        public int PageSize { get; set; } = 20;
    }

    public class MediaDto
    {
        public int ProductMediaId { get; set; }
        public string Kind { get; set; } = "";
        public string Url { get; set; } = "";
        public int SortOrder { get; set; }
    }

    /// <summary>Compact shape for lists and cards.</summary>
    public class ProductSummaryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsAvailable { get; set; }
        public bool InStock { get; set; }
        public bool IsFeatured { get; set; }
        public string Location { get; set; } = "";
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string CategoryNameSo { get; set; } = "";
        /// <summary>Main photo (first image), or empty.</summary>
        public string ImageUrl { get; set; } = "";
        public int ImageCount { get; set; }
        public bool HasVideo { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Full shape for the product details screen and the edit form.</summary>
    public class ProductDetailDto : ProductSummaryDto
    {
        public string Description { get; set; } = "";
        public string Contact { get; set; } = "";
        public int ViewCount { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<MediaDto> Media { get; set; } = new();

        public static ProductDetailDto From(Product p)
        {
            var media = p.Media.OrderBy(m => m.SortOrder).ThenBy(m => m.ProductMediaId).ToList();
            var images = media.Where(m => m.Kind == MediaKinds.Image).ToList();
            return new ProductDetailDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ShortDescription = ProductMapping.Shorten(p.Description),
                Description = p.Description,
                Price = p.Price,
                Quantity = p.Quantity,
                IsAvailable = p.IsAvailable,
                InStock = p.InStock,
                IsFeatured = p.IsFeatured,
                Location = p.Location,
                Contact = p.Contact,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "",
                CategoryNameSo = p.Category?.NameSo ?? "",
                ImageUrl = images.FirstOrDefault()?.Url ?? "",
                ImageCount = images.Count,
                HasVideo = media.Any(m => m.Kind == MediaKinds.Video),
                ViewCount = p.ViewCount,
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc,
                Media = media.Select(m => new MediaDto
                {
                    ProductMediaId = m.ProductMediaId,
                    Kind = m.Kind,
                    Url = m.Url,
                    SortOrder = m.SortOrder
                }).ToList()
            };
        }
    }

    public static class ProductMapping
    {
        public const int ShortDescriptionLength = 160;

        public static string Shorten(string text) =>
            text.Length <= ShortDescriptionLength ? text : text[..ShortDescriptionLength].TrimEnd() + "…";

        /// <summary>Projection used by list queries, so only the needed columns are read.</summary>
        public static IQueryable<ProductSummaryDto> ToSummaries(this IQueryable<Product> query) =>
            query.Select(p => new ProductSummaryDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ShortDescription = p.Description.Length <= ShortDescriptionLength
                    ? p.Description
                    : p.Description.Substring(0, ShortDescriptionLength) + "…",
                Price = p.Price,
                Quantity = p.Quantity,
                IsAvailable = p.IsAvailable,
                InStock = p.IsAvailable && p.Quantity > 0,
                IsFeatured = p.IsFeatured,
                Location = p.Location,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "",
                CategoryNameSo = p.Category != null ? p.Category.NameSo : "",
                ImageUrl = p.Media
                    .Where(m => m.Kind == MediaKinds.Image)
                    .OrderBy(m => m.SortOrder).ThenBy(m => m.ProductMediaId)
                    .Select(m => m.Url)
                    .FirstOrDefault() ?? "",
                ImageCount = p.Media.Count(m => m.Kind == MediaKinds.Image),
                HasVideo = p.Media.Any(m => m.Kind == MediaKinds.Video),
                CreatedAtUtc = p.CreatedAtUtc
            });
    }

    public class MediaItemRequest
    {
        /// <summary>Image or Video.</summary>
        [Required]
        public string Kind { get; set; } = MediaKinds.Image;

        /// <summary>A URL returned by /api/upload/image or /api/upload/video.</summary>
        [Required, StringLength(500)]
        public string Url { get; set; } = "";
    }

    /// <summary>Used for both create and update. The id always comes from the route, never the body.</summary>
    public class ProductRequest
    {
        public const int MaxImages = 10;
        public const int MaxVideos = 3;

        [Required, StringLength(200, MinimumLength = 2)]
        public string ProductName { get; set; } = "";

        [StringLength(4000)]
        public string? Description { get; set; }

        [Range(typeof(decimal), "0", "100000000")]
        public decimal Price { get; set; }

        [Range(0, 1_000_000)]
        public int Quantity { get; set; }

        [StringLength(100)]
        public string? Contact { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public int? CategoryId { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool IsFeatured { get; set; }

        /// <summary>All images and videos in display order. Replaces the product's current media.</summary>
        [MaxLength(MaxImages + MaxVideos)]
        public List<MediaItemRequest> Media { get; set; } = new();
    }

    /// <summary>Quick changes from the staff product list. Only the fields sent are changed.</summary>
    public class ProductStatusRequest
    {
        public bool? IsAvailable { get; set; }
        public bool? IsFeatured { get; set; }

        [Range(0, 1_000_000)]
        public int? Quantity { get; set; }
    }

    public class UploadResult
    {
        /// <summary>Server-relative URL, e.g. /media/images/abc.jpg. Store this on the product.</summary>
        public string Url { get; set; } = "";
    }
}
