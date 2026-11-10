using DhaqaleysteApi.Data;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using DhaqaleysteApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Controllers
{
    /// <summary>Product categories. Customers see active ones; staff manage them.</summary>
    [Route("api/categories")]
    public class CategoriesController : ApiControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(AppDbContext db, ILogger<CategoriesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: api/categories  (active categories, for customers)
        [HttpGet]
        [AllowAnonymous]
        public Task<List<CategoryDto>> GetActive(CancellationToken ct) =>
            Query(_db.Categories.Where(c => c.IsActive)).ToListAsync(ct);

        // GET: api/categories/all  (including hidden ones, for staff)
        [HttpGet("all")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public Task<List<CategoryDto>> GetAll(CancellationToken ct) =>
            Query(_db.Categories).ToListAsync(ct);

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<CategoryDto>> Get(int id, CancellationToken ct)
        {
            var category = await Query(_db.Categories.Where(c => c.CategoryId == id)).FirstOrDefaultAsync(ct);
            if (category == null || !category.IsActive && !IsStaff)
                return NotFound();
            return category;
        }

        [HttpPost]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request)
        {
            var name = request.Name.Trim();
            if (await _db.Categories.AnyAsync(c => c.Name == name))
                return ValidationError(nameof(request.Name), "A category with this name already exists.");

            var category = new Category { CreatedAtUtc = DateTime.UtcNow };
            Apply(request, category);
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} created category {CategoryId}", CurrentUserId, category.CategoryId);

            var dto = await Query(_db.Categories.Where(c => c.CategoryId == category.CategoryId)).FirstAsync();
            return CreatedAtAction(nameof(Get), new { id = category.CategoryId }, dto);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
                return NotFound();

            var name = request.Name.Trim();
            if (await _db.Categories.AnyAsync(c => c.Name == name && c.CategoryId != id))
                return ValidationError(nameof(request.Name), "A category with this name already exists.");

            Apply(request, category);
            category.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} updated category {CategoryId}", CurrentUserId, id);

            return await Query(_db.Categories.Where(c => c.CategoryId == id)).FirstAsync();
        }

        /// <summary>Only empty categories can be deleted. Hide a category that still has products instead.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthPolicies.Staff)]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
                return NotFound();

            // Deleted products still reference the category, so they are counted too.
            if (await _db.Products.IgnoreQueryFilters().AnyAsync(p => p.CategoryId == id))
                return ProblemResult(StatusCodes.Status409Conflict,
                    "This category still has products. Move them to another category or hide the category instead.");

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Staff {UserId} deleted category {CategoryId}", CurrentUserId, id);
            return NoContent();
        }

        private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Staff);

        private static void Apply(CategoryRequest request, Category category)
        {
            category.Name = request.Name.Trim();
            category.NameSo = request.NameSo?.Trim() ?? "";
            category.Icon = request.Icon?.Trim() ?? "";
            category.SortOrder = request.SortOrder;
            category.IsActive = request.IsActive;
        }

        private static IQueryable<CategoryDto> Query(IQueryable<Category> categories) =>
            categories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    NameSo = c.NameSo,
                    Icon = c.Icon,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive,
                    ProductCount = c.Products.Count()
                });
    }
}
