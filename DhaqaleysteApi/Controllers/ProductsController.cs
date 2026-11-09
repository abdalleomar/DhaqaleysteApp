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
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(AppDbContext db, ILogger<ProductsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: api/products
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _db.Products.AsNoTracking()
                .OrderByDescending(x => x.ProductId)
                .ToListAsync();
            return products.Select(ProductDto.From).ToList();
        }

        // GET: api/products/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id);
            return product == null ? NotFound() : ProductDto.From(product);
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<ProductDto>> CreateProduct(ProductRequest request)
        {
            var mediaError = ValidateMedia(request, existing: null);
            if (mediaError != null)
                return mediaError;

            var product = new Product();
            request.ApplyTo(product);

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} created product {ProductId}", CurrentUserId, product.ProductId);

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, ProductDto.From(product));
        }

        // PUT: api/products/5
        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<ProductDto>> UpdateProduct(int id, ProductRequest request)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
                return NotFound();

            var mediaError = ValidateMedia(request, product);
            if (mediaError != null)
                return mediaError;

            request.ApplyTo(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} updated product {ProductId}", CurrentUserId, product.ProductId);

            return ProductDto.From(product);
        }

        // DELETE: api/products/5
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
                return NotFound();

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} deleted product {ProductId}", CurrentUserId, id);

            return NoContent();
        }

        /// <summary>
        /// Media paths must come from our own upload endpoint, so a product can never point at an
        /// arbitrary external URL. Paths saved before this rule (legacy) may be kept unchanged.
        /// </summary>
        private ActionResult? ValidateMedia(ProductRequest request, Product? existing)
        {
            if (!FileStorageService.IsOwnMediaPath(request.ImagePath) && request.ImagePath?.Trim() != existing?.ImagePath)
                return ValidationError(nameof(request.ImagePath), "Image must be uploaded through /api/upload/image.");

            if (!FileStorageService.IsOwnMediaPath(request.VideoPath) && request.VideoPath?.Trim() != existing?.VideoPath)
                return ValidationError(nameof(request.VideoPath), "Video must be uploaded through /api/upload/video.");

            return null;
        }
    }
}
