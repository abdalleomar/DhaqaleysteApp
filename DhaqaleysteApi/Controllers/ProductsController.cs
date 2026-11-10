using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>Reading products is public. Creating, editing and deleting require a staff account.</summary>
    [Route("api/products")]
    public class ProductsController : ApiControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FileStorageService _storage;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext db, FileStorageService storage, ILogger<ProductsController> logger)
        {
            _db = db;
            _storage = storage;
            _logger = logger;
        }

        // GET: api/products?search=&categoryId=&minPrice=&maxPrice=&inStock=&featured=&sort=&page=&pageSize=
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ProductSummaryDto>>> GetProducts([FromQuery] ProductQuery query, CancellationToken ct)
        {
            var sort = (query.Sort ?? ProductSort.Newest).ToLowerInvariant();
            if (!ProductSort.All.Contains(sort))
                return ValidationError(nameof(query.Sort), $"Sort must be one of: {string.Join(", ", ProductSort.All)}.");

            if (query.MinPrice > query.MaxPrice)
                return ValidationError(nameof(query.MinPrice), "Minimum price cannot be more than maximum price.");

            var products = _db.Products.AsNoTracking();

            // Customers never see products in hidden categories; staff can ask to include them.
            if (!(query.IncludeHidden && IsStaff))
                products = products.Where(p => p.CategoryId == null || p.Category!.IsActive);

            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Location.Contains(search) ||
                    (p.Category != null && (p.Category.Name.Contains(search) || p.Category.NameSo.Contains(search))));
            }

            if (query.CategoryId != null)
                products = products.Where(p => p.CategoryId == query.CategoryId);
            if (query.MinPrice != null)
                products = products.Where(p => p.Price >= query.MinPrice);
            if (query.MaxPrice != null)
                products = products.Where(p => p.Price <= query.MaxPrice);
            if (query.InStock == true)
                products = products.Where(p => p.IsAvailable && p.Quantity > 0);
            if (query.InStock == false)
                products = products.Where(p => !p.IsAvailable || p.Quantity == 0);
            if (query.Featured != null)
                products = products.Where(p => p.IsFeatured == query.Featured);

            products = sort switch
            {
                ProductSort.PriceAsc => products.OrderBy(p => p.Price).ThenByDescending(p => p.ProductId),
                ProductSort.PriceDesc => products.OrderByDescending(p => p.Price).ThenByDescending(p => p.ProductId),
                ProductSort.Name => products.OrderBy(p => p.ProductName).ThenByDescending(p => p.ProductId),
                ProductSort.Popular => products.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ProductId),
                _ => products.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.ProductId)
            };

            var total = await products.CountAsync(ct);
            var items = await products
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToSummaries()
                .ToListAsync(ct);

            return new PagedResult<ProductSummaryDto>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = total
            };
        }

        // GET: api/products/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDetailDto>> GetProduct(int id, CancellationToken ct)
        {
            var product = await _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Media)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.ProductId == id, ct);

            if (product == null || product.Category is { IsActive: false } && !IsStaff)
                return NotFound();

            return ProductDetailDto.From(product);
        }

        // POST: api/products/5/view  (counts a customer opening the product; used for "popular")
        [HttpPost("{id:int}/view")]
        [AllowAnonymous]
        public async Task<IActionResult> RecordView(int id, CancellationToken ct)
        {
            var updated = await _db.Products
                .Where(p => p.ProductId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);
            return updated == 0 ? NotFound() : NoContent();
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<ProductDetailDto>> CreateProduct(ProductRequest request)
        {
            var error = await ValidateAsync(request, existing: null);
            if (error != null)
                return error;

            var product = new Product { CreatedByUserId = CurrentUserId, CreatedAtUtc = DateTime.UtcNow };
            Apply(request, product);

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} created product {ProductId}", CurrentUserId, product.ProductId);

            await _db.Entry(product).Reference(p => p.Category).LoadAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, ProductDetailDto.From(product));
        }

        // PUT: api/products/5
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<ProductDetailDto>> UpdateProduct(int id, ProductRequest request)
        {
            var product = await _db.Products
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            var error = await ValidateAsync(request, product);
            if (error != null)
                return error;

            var removedUrls = product.Media.Select(m => m.Url)
                .Except(request.Media.Select(m => m.Url.Trim()))
                .ToList();

            Apply(request, product);
            product.UpdatedAtUtc = DateTime.UtcNow;
            product.UpdatedByUserId = CurrentUserId;
            await _db.SaveChangesAsync();

            await DeleteUnusedFilesAsync(removedUrls);
            _logger.LogInformation("Staff {UserId} updated product {ProductId}", CurrentUserId, product.ProductId);

            await _db.Entry(product).Reference(p => p.Category).LoadAsync();
            return ProductDetailDto.From(product);
        }

        // PATCH: api/products/5/status  (quick "sold out", "featured" and quantity changes)
        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<ProductDetailDto>> UpdateStatus(int id, ProductStatusRequest request)
        {
            var product = await _db.Products
                .Include(p => p.Media)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            if (request.IsAvailable != null) product.IsAvailable = request.IsAvailable.Value;
            if (request.IsFeatured != null) product.IsFeatured = request.IsFeatured.Value;
            if (request.Quantity != null) product.Quantity = request.Quantity.Value;
            product.UpdatedAtUtc = DateTime.UtcNow;
            product.UpdatedByUserId = CurrentUserId;
            await _db.SaveChangesAsync();

            return ProductDetailDto.From(product);
        }

        // DELETE: api/products/5  (soft delete: hidden everywhere, kept in the database)
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
                return NotFound();

            product.IsDeleted = true;
            product.IsFeatured = false;
            product.DeletedAtUtc = DateTime.UtcNow;
            product.UpdatedByUserId = CurrentUserId;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} deleted product {ProductId}", CurrentUserId, id);

            return NoContent();
        }

        private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Staff);

        private static void Apply(ProductRequest request, Product product)
        {
            product.ProductName = request.ProductName.Trim();
            product.Description = request.Description?.Trim() ?? "";
            product.Price = Math.Round(request.Price, 2);
            product.Quantity = request.Quantity;
            product.Contact = request.Contact?.Trim() ?? "";
            product.Location = request.Location?.Trim() ?? "";
            product.CategoryId = request.CategoryId;
            product.IsAvailable = request.IsAvailable;
            product.IsFeatured = request.IsFeatured;

            // Replace media, keeping existing rows (and their ids) where the URL is unchanged.
            var wanted = request.Media.Select((m, index) => (Url: m.Url.Trim(), Kind: m.Kind, Index: index)).ToList();
            product.Media.RemoveAll(existing => wanted.All(w => w.Url != existing.Url));
            foreach (var item in wanted)
            {
                var media = product.Media.FirstOrDefault(m => m.Url == item.Url);
                if (media == null)
                {
                    media = new ProductMedia { Url = item.Url, Kind = item.Kind, CreatedAtUtc = DateTime.UtcNow };
                    product.Media.Add(media);
                }
                media.SortOrder = item.Index;
            }
        }

        private async Task<ActionResult?> ValidateAsync(ProductRequest request, Product? existing)
        {
            if (request.CategoryId != null && !await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId))
                return ValidationError(nameof(request.CategoryId), "The selected category does not exist.");

            var existingUrls = existing?.Media.Select(m => m.Url).ToHashSet() ?? new HashSet<string>();
            var urls = new HashSet<string>();

            foreach (var item in request.Media)
            {
                var url = item.Url.Trim();
                if (!MediaKinds.IsValid(item.Kind))
                    return ValidationError(nameof(request.Media), "Media kind must be Image or Video.");

                if (!urls.Add(url))
                    return ValidationError(nameof(request.Media), "The same file is listed twice.");

                // Only files uploaded through our own upload endpoint, or files the product already had.
                var kind = item.Kind == MediaKinds.Image ? MediaKind.Image : MediaKind.Video;
                if (!FileStorageService.IsOwnMediaPath(url, kind) && !existingUrls.Contains(url))
                    return ValidationError(nameof(request.Media),
                        $"{item.Kind} must be uploaded through /api/upload/{item.Kind.ToLowerInvariant()}.");
            }

            if (request.Media.Count(m => m.Kind == MediaKinds.Image) > ProductRequest.MaxImages)
                return ValidationError(nameof(request.Media), $"A product can have at most {ProductRequest.MaxImages} images.");
            if (request.Media.Count(m => m.Kind == MediaKinds.Video) > ProductRequest.MaxVideos)
                return ValidationError(nameof(request.Media), $"A product can have at most {ProductRequest.MaxVideos} videos.");

            return null;
        }

        /// <summary>Removes files from disk once no product (including deleted ones) uses them.</summary>
        private async Task DeleteUnusedFilesAsync(List<string> urls)
        {
            foreach (var url in urls)
            {
                var stillUsed = await _db.ProductMedia.IgnoreQueryFilters().AnyAsync(m => m.Url == url);
                if (!stillUsed)
                    _storage.TryDelete(url);
            }
        }
    }
}
