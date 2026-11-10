using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>Numbers for the staff dashboard.</summary>
    [Route("api/stats")]
    [Authorize(Policy = AuthPolicies.Staff)]
    public class StatsController : ApiControllerBase
    {
        private const int LowStockThreshold = 3;

        private readonly AppDbContext _db;

        public StatsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<CatalogStatsDto> Get(CancellationToken ct)
        {
            var products = _db.Products.AsNoTracking();

            return new CatalogStatsDto
            {
                TotalProducts = await products.CountAsync(ct),
                InStock = await products.CountAsync(p => p.IsAvailable && p.Quantity > 0, ct),
                OutOfStock = await products.CountAsync(p => !p.IsAvailable || p.Quantity == 0, ct),
                Featured = await products.CountAsync(p => p.IsFeatured, ct),
                Categories = await _db.Categories.CountAsync(ct),
                ActiveStaff = await _db.Users.CountAsync(u => u.IsActive, ct),
                MostViewed = await products
                    .Where(p => p.ViewCount > 0)
                    .OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ProductId)
                    .Take(5).ToSummaries().ToListAsync(ct),
                LowStock = await products
                    .Where(p => p.IsAvailable && p.Quantity <= LowStockThreshold)
                    .OrderBy(p => p.Quantity).ThenBy(p => p.ProductName)
                    .Take(10).ToSummaries().ToListAsync(ct)
            };
        }
    }
}
