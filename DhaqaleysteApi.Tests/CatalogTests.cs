using System.Net;
using System.Net.Http.Json;
using DhaqaleysteApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Tests;

public class CatalogTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CatalogTests(ApiFactory factory) => _factory = factory;

    private HttpClient Anonymous() => _factory.CreateClient();

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateClient();
        var auth = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        return client.WithToken(auth.AccessToken);
    }

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..Math.Min(60, prefix.Length + 33)];

    private static async Task<CategoryDto> CreateCategoryAsync(HttpClient admin, bool isActive = true)
    {
        var response = await admin.PostAsJsonAsync("/api/categories",
            new { name = Unique("Category"), nameSo = "Qaybta", icon = "📱", sortOrder = 1, isActive });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private static async Task<ProductDetailDto> CreateProductAsync(HttpClient admin, object body)
    {
        var response = await admin.PostAsJsonAsync("/api/products", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDetailDto>())!;
    }

    private static async Task<string> UploadImageAsync(HttpClient admin)
    {
        var response = await admin.PostAsync("/api/upload/image", TestHelpers.FileContent(TestHelpers.PngBytes, "photo.png"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UploadResult>())!.Url;
    }

    // ---------- Listing, paging, filtering ----------

    [Fact]
    public async Task Products_are_paged_and_filtered_by_category()
    {
        var admin = await AdminAsync();
        var category = await CreateCategoryAsync(admin);
        for (var i = 0; i < 3; i++)
            await CreateProductAsync(admin, TestHelpers.ValidProduct($"Paged {i}", category.CategoryId));

        var page1 = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/api/products?categoryId={category.CategoryId}&pageSize=2&page=1");
        Assert.Equal(3, page1!.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.All(page1.Items, p => Assert.Equal(category.Name, p.CategoryName));

        var page2 = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/api/products?categoryId={category.CategoryId}&pageSize=2&page=2");
        Assert.Single(page2!.Items);
        Assert.False(page2.HasMore);
    }

    [Fact]
    public async Task Search_matches_product_and_category_names()
    {
        var admin = await AdminAsync();
        var category = await CreateCategoryAsync(admin);
        var token = Guid.NewGuid().ToString("N")[..10];
        await CreateProductAsync(admin, TestHelpers.ValidProduct($"Radio {token}", category.CategoryId));

        var byName = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>($"/api/products?search={token}");
        Assert.Single(byName!.Items);

        var byCategory = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/api/products?search={Uri.EscapeDataString(category.Name)}");
        Assert.Contains(byCategory!.Items, p => p.ProductName == $"Radio {token}");
    }

    [Fact]
    public async Task Search_treats_wildcards_as_text()
    {
        var response = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>("/api/products?search=%25%25%25");
        Assert.Empty(response!.Items);
    }

    [Fact]
    public async Task Sort_by_price_and_stock_filter_work()
    {
        var admin = await AdminAsync();
        var category = await CreateCategoryAsync(admin);
        await CreateProductAsync(admin, TestHelpers.ValidProduct("Expensive", category.CategoryId, price: 90));
        await CreateProductAsync(admin, TestHelpers.ValidProduct("Cheap", category.CategoryId, price: 5));
        await CreateProductAsync(admin, TestHelpers.ValidProduct("Sold out", category.CategoryId, price: 50, quantity: 0));

        var sorted = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/api/products?categoryId={category.CategoryId}&sort=price_asc");
        Assert.Equal(new[] { "Cheap", "Sold out", "Expensive" }, sorted!.Items.Select(p => p.ProductName));

        var inStock = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/api/products?categoryId={category.CategoryId}&inStock=true");
        Assert.DoesNotContain(inStock!.Items, p => p.ProductName == "Sold out");

        var bad = await Anonymous().GetAsync("/api/products?sort=drop_table");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var tooBig = await Anonymous().GetAsync("/api/products?pageSize=1000");
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);
    }

    [Fact]
    public async Task Marking_sold_out_and_featured_updates_listings()
    {
        var admin = await AdminAsync();
        var category = await CreateCategoryAsync(admin);
        var product = await CreateProductAsync(admin, TestHelpers.ValidProduct("Toggle me", category.CategoryId));

        var patch = await admin.PatchAsJsonAsync($"/api/products/{product.ProductId}/status", new { isAvailable = false, isFeatured = true });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var updated = (await patch.Content.ReadFromJsonAsync<ProductDetailDto>())!;
        Assert.False(updated.InStock);
        Assert.True(updated.IsFeatured);

        var featured = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/api/products?categoryId={category.CategoryId}&featured=true");
        Assert.Single(featured!.Items);

        var anonymousPatch = await Anonymous().PatchAsJsonAsync($"/api/products/{product.ProductId}/status", new { isAvailable = true });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousPatch.StatusCode);
    }

    // ---------- Media ----------

    [Fact]
    public async Task Products_keep_multiple_images_in_order_and_unused_files_are_removed()
    {
        var admin = await AdminAsync();
        var first = await UploadImageAsync(admin);
        var second = await UploadImageAsync(admin);

        var product = await CreateProductAsync(admin,
            TestHelpers.ValidProduct("Gallery", media: new[] { TestHelpers.Image(second), TestHelpers.Image(first) }));
        Assert.Equal(new[] { second, first }, product.Media.Select(m => m.Url));
        Assert.Equal(second, product.ImageUrl);

        // Remove the second image: its file is deleted from the server.
        var update = await admin.PutAsJsonAsync($"/api/products/{product.ProductId}",
            TestHelpers.ValidProduct("Gallery", media: new[] { TestHelpers.Image(first) }));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync(second)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous().GetAsync(first)).StatusCode);
    }

    [Fact]
    public async Task Too_many_images_are_rejected()
    {
        var admin = await AdminAsync();
        var url = await UploadImageAsync(admin);
        var media = Enumerable.Range(0, 11).Select(i => TestHelpers.Image(url + "?" + i)).ToArray();
        var response = await admin.PostAsJsonAsync("/api/products", TestHelpers.ValidProduct("Too many", media: media));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Image_url_cannot_be_used_as_video()
    {
        var admin = await AdminAsync();
        var url = await UploadImageAsync(admin);
        var response = await admin.PostAsJsonAsync("/api/products", TestHelpers.ValidProduct("Wrong kind", media: new[] { TestHelpers.Video(url) }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Soft delete and hidden categories ----------

    [Fact]
    public async Task Deleted_products_are_hidden_but_kept()
    {
        var admin = await AdminAsync();
        var category = await CreateCategoryAsync(admin);
        var product = await CreateProductAsync(admin, TestHelpers.ValidProduct("Delete me", category.CategoryId));

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/products/{product.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync($"/api/products/{product.ProductId}")).StatusCode);

        var list = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>($"/api/products?categoryId={category.CategoryId}");
        Assert.Empty(list!.Items);

        await using var db = _factory.CreateDbContext();
        Assert.True(await db.Products.IgnoreQueryFilters().AnyAsync(p => p.ProductId == product.ProductId && p.IsDeleted));
    }

    [Fact]
    public async Task Products_in_hidden_categories_are_not_shown_to_customers()
    {
        var admin = await AdminAsync();
        var hidden = await CreateCategoryAsync(admin, isActive: false);
        var product = await CreateProductAsync(admin, TestHelpers.ValidProduct("Secret", hidden.CategoryId));

        var list = await Anonymous().GetFromJsonAsync<PagedResult<ProductSummaryDto>>($"/api/products?categoryId={hidden.CategoryId}");
        Assert.Empty(list!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync($"/api/products/{product.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/products/{product.ProductId}")).StatusCode);

        var publicCategories = await Anonymous().GetFromJsonAsync<List<CategoryDto>>("/api/categories");
        Assert.DoesNotContain(publicCategories!, c => c.CategoryId == hidden.CategoryId);
        var allCategories = await admin.GetFromJsonAsync<List<CategoryDto>>("/api/categories/all");
        Assert.Contains(allCategories!, c => c.CategoryId == hidden.CategoryId);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task Categories_with_products_cannot_be_deleted()
    {
        var admin = await AdminAsync();
        var used = await CreateCategoryAsync(admin);
        await CreateProductAsync(admin, TestHelpers.ValidProduct("Keeps category", used.CategoryId));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/categories/{used.CategoryId}")).StatusCode);

        var empty = await CreateCategoryAsync(admin);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/categories/{empty.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task Category_names_are_unique_and_changes_require_staff()
    {
        var admin = await AdminAsync();
        var category = await CreateCategoryAsync(admin);

        var duplicate = await admin.PostAsJsonAsync("/api/categories", new { name = category.Name.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        var anonymous = await Anonymous().PostAsJsonAsync("/api/categories", new { name = "Anonymous" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Product_with_unknown_category_is_rejected()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsJsonAsync("/api/products", TestHelpers.ValidProduct("Orphan", categoryId: 987654));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Company settings ----------

    [Fact]
    public async Task Company_settings_are_public_and_only_admins_change_them()
    {
        var company = await Anonymous().GetFromJsonAsync<CompanySettingsDto>("/api/company");
        Assert.False(string.IsNullOrEmpty(company!.CompanyName));

        var admin = await AdminAsync();
        var settings = new
        {
            companyName = "Dhaqaleyste Trading",
            phone = "+252 61 5000000",
            whatsApp = "+252615000000",
            email = "info@example.com",
            city = "Mogadishu",
            mapUrl = "https://maps.example.com/place",
            currencyCode = "USD"
        };
        var update = await admin.PutAsJsonAsync("/api/company", settings);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("+252615000000", (await Anonymous().GetFromJsonAsync<CompanySettingsDto>("/api/company"))!.WhatsApp);

        var unsafeLink = await admin.PutAsJsonAsync("/api/company", new { companyName = "X Co", mapUrl = "javascript:alert(1)" });
        Assert.Equal(HttpStatusCode.BadRequest, unsafeLink.StatusCode);

        var anonymous = await Anonymous().PutAsJsonAsync("/api/company", settings);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Staff_cannot_change_company_settings()
    {
        var admin = await AdminAsync();
        var email = $"staff-{Guid.NewGuid():N}@dhaqaleyste.test";
        await admin.PostAsJsonAsync("/api/staff", new { fullName = "Staff", email, password = "Staff12345", role = "Staff" });
        var staff = _factory.CreateClient();
        staff.WithToken((await staff.LoginAsync(email, "Staff12345")).AccessToken);

        var response = await staff.PutAsJsonAsync("/api/company", new { companyName = "Hijacked" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Stats and views ----------

    [Fact]
    public async Task Views_are_counted_and_stats_require_staff()
    {
        var admin = await AdminAsync();
        var product = await CreateProductAsync(admin, TestHelpers.ValidProduct("Popular one"));
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.NoContent, (await Anonymous().PostAsync($"/api/products/{product.ProductId}/view", null)).StatusCode);

        var detail = await Anonymous().GetFromJsonAsync<ProductDetailDto>($"/api/products/{product.ProductId}");
        Assert.Equal(3, detail!.ViewCount);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync("/api/stats")).StatusCode);
        var stats = await admin.GetFromJsonAsync<CatalogStatsDto>("/api/stats");
        Assert.True(stats!.TotalProducts > 0);
        Assert.Contains(stats.MostViewed, p => p.ProductId == product.ProductId);
    }
}
