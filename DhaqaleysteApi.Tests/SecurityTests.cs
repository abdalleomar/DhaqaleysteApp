using System.Net;
using System.Net.Http.Json;
using System.Text;
using DhaqaleysteApi.Dtos;
using DhaqaleysteApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DhaqaleysteApi.Tests;

public class SecurityTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SecurityTests(ApiFactory factory) => _factory = factory;

    private HttpClient Anonymous() => _factory.CreateClient();

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateClient();
        var auth = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        return client.WithToken(auth.AccessToken);
    }

    private async Task<(HttpClient client, StaffDto staff, string password)> NewStaffAsync(string role = Roles.Staff)
    {
        var admin = await AdminAsync();
        var email = $"staff-{Guid.NewGuid():N}@dhaqaleyste.test";
        const string password = "Staff12345";
        var created = await admin.PostAsJsonAsync("/api/staff", new { fullName = "Staff Member", email, password, role });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var staff = (await created.Content.ReadFromJsonAsync<StaffDto>())!;

        var client = _factory.CreateClient();
        var auth = await client.LoginAsync(email, password);
        return (client.WithToken(auth.AccessToken), staff, password);
    }

    // ---------- Public catalogue ----------

    [Fact]
    public async Task Anyone_can_browse_products_without_signing_in()
    {
        var response = await Anonymous().GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Public_registration_no_longer_exists()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/users/register",
            new { fullName = "x", email = "x@x.com", password = "Password1", role = "ADMIN" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Product write protection ----------

    [Theory]
    [InlineData("POST", "/api/products")]
    [InlineData("PUT", "/api/products/1")]
    [InlineData("DELETE", "/api/products/1")]
    public async Task Product_changes_require_sign_in(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method != "DELETE")
            request.Content = JsonContent.Create(TestHelpers.ValidProduct());

        var response = await Anonymous().SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Staff_can_create_update_and_delete_products()
    {
        var (client, _, _) = await NewStaffAsync();

        var created = await client.PostAsJsonAsync("/api/products", TestHelpers.ValidProduct("Phone"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = (await created.Content.ReadFromJsonAsync<ProductDetailDto>())!;

        var updated = await client.PutAsJsonAsync($"/api/products/{product.ProductId}", TestHelpers.ValidProduct("Phone v2"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Phone v2", (await updated.Content.ReadFromJsonAsync<ProductDetailDto>())!.ProductName);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/products/{product.ProductId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Anonymous().GetAsync($"/api/products/{product.ProductId}")).StatusCode);
    }

    [Fact]
    public async Task Updating_a_missing_product_returns_404_not_500()
    {
        var admin = await AdminAsync();
        var response = await admin.PutAsJsonAsync("/api/products/999999", TestHelpers.ValidProduct());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_product_input_is_rejected()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsJsonAsync("/api/products", new { productName = "", price = -5, quantity = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Products_cannot_point_at_external_media()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsJsonAsync("/api/products",
            TestHelpers.ValidProduct("Bad image", media: new[] { TestHelpers.Image("https://evil.example/tracker.png") }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Wrong_password_is_rejected_and_responses_never_contain_passwords()
    {
        var bad = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = ApiFactory.AdminEmail, password = "wrong-password-1" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        var good = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        var body = await good.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ApiFactory.AdminPassword, body);
    }

    [Fact]
    public async Task Passwords_are_stored_hashed()
    {
        await using var db = _factory.CreateDbContext();
        var admin = await db.Users.SingleAsync(x => x.Email == ApiFactory.AdminEmail);
        Assert.NotEqual(ApiFactory.AdminPassword, admin.PasswordHash);
        Assert.StartsWith("AQAAAA", admin.PasswordHash); // ASP.NET Core Identity v3 hash format
    }

    [Fact]
    public async Task Empty_phone_cannot_be_used_to_sign_in_as_someone_else()
    {
        // The old API matched any account with an empty phone number.
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "", password = ApiFactory.AdminPassword });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Account_locks_after_repeated_failures()
    {
        var (_, staff, password) = await NewStaffAsync();
        for (var i = 0; i < 5; i++)
            await Anonymous().PostAsJsonAsync("/api/auth/login", new { emailOrPhone = staff.Email, password = "wrong-pass-1" });

        // Even the correct password is refused while locked.
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login", new { emailOrPhone = staff.Email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Legacy_accounts_cannot_sign_in()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.Users.Add(new User { FullName = "Old customer", Email = "legacy@dhaqaleyste.test", Role = Roles.Legacy, IsActive = false });
            await db.SaveChangesAsync();
        }

        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { emailOrPhone = "legacy@dhaqaleyste.test", password = "" });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Roles and token lifecycle ----------

    [Fact]
    public async Task Staff_cannot_manage_staff_accounts()
    {
        var (client, _, _) = await NewStaffAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/staff")).StatusCode);
        var create = await client.PostAsJsonAsync("/api/staff",
            new { fullName = "Sneaky", email = "sneaky@x.com", password = "Sneaky12345", role = "Admin" });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Deactivated_staff_lose_access_immediately()
    {
        var (client, staff, _) = await NewStaffAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        var admin = await AdminAsync();
        var deactivate = await admin.PutAsJsonAsync($"/api/staff/{staff.UserId}",
            new { fullName = staff.FullName, email = staff.Email, role = Roles.Staff, isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_themselves()
    {
        var admin = await AdminAsync();
        var me = (await admin.GetFromJsonAsync<StaffDto>("/api/auth/me"))!;
        var response = await admin.PutAsJsonAsync($"/api/staff/{me.UserId}",
            new { fullName = me.FullName, email = me.Email, role = Roles.Admin, isActive = false });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_tokens_rotate_and_reuse_revokes_the_session()
    {
        var client = Anonymous();
        var first = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = (await refreshed.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        // Re-using the old token looks like theft: it fails and the new token is revoked too.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        var afterReuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = second.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Tampered_tokens_are_rejected()
    {
        var auth = await Anonymous().LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        var client = Anonymous().WithToken(auth.AccessToken[..^4] + "AAAA");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/staff")).StatusCode);
    }

    // ---------- Uploads ----------

    [Fact]
    public async Task Uploads_require_sign_in()
    {
        var response = await Anonymous().PostAsync("/api/upload/image", TestHelpers.FileContent(TestHelpers.PngBytes, "a.png"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_image_is_stored_with_a_server_name_and_served_as_an_image()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsync("/api/upload/image", TestHelpers.FileContent(TestHelpers.PngBytes, "../../evil name.png"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var url = (await response.Content.ReadFromJsonAsync<UploadResult>())!.Url;
        Assert.Matches("^/media/images/[0-9a-f]{32}\\.png$", url);

        var file = await Anonymous().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("image/png", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", file.Headers.GetValues("X-Content-Type-Options").Single());

        // The uploaded path can be used on a product.
        var product = await admin.PostAsJsonAsync("/api/products",
            TestHelpers.ValidProduct("With image", media: new[] { TestHelpers.Image(url) }));
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("script.js")]
    [InlineData("noextension")]
    public async Task Dangerous_file_types_are_rejected(string fileName)
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsync("/api/upload/image",
            TestHelpers.FileContent(Encoding.UTF8.GetBytes("<script>alert(1)</script>"), fileName));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Html_disguised_as_png_is_rejected()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsync("/api/upload/image",
            TestHelpers.FileContent(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), "photo.png"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Image_cannot_be_uploaded_as_video()
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsync("/api/upload/video", TestHelpers.FileContent(TestHelpers.PngBytes, "a.png"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oversized_image_is_rejected()
    {
        var admin = await AdminAsync();
        var big = new byte[6 * 1024 * 1024];
        TestHelpers.PngBytes.CopyTo(big, 0);
        var response = await admin.PostAsync("/api/upload/image", TestHelpers.FileContent(big, "big.png"));
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge,
            $"Expected 400 or 413 but got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Server_errors_do_not_leak_details()
    {
        var response = await Anonymous().GetAsync("/api/products/not-a-number");
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("at DhaqaleysteApi", body);
    }
}
