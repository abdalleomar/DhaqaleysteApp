using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DhaqaleysteApp.Models;
using DhaqaleysteApp.Services.Localization;

namespace DhaqaleysteApp.Services;

/// <summary>
/// Typed client for the Dhaqaleyste API. One instance (and one HttpClient) is shared through
/// dependency injection. Every method returns an <see cref="ApiResult"/> instead of throwing,
/// so pages can show a friendly message when the network is down.
/// </summary>
public class ApiService
{
    private readonly HttpClient _http;
    private readonly SessionService _session;
    private readonly AppConfig _config;

    public ApiService(HttpClient http, SessionService session, AppConfig config)
    {
        _http = http;
        _session = session;
        _config = config;
        if (config.ApiBaseUrl != null)
            _http.BaseAddress = config.ApiBaseUrl;
        _http.Timeout = TimeSpan.FromSeconds(100);
    }

    public bool IsConfigured => _config.ApiBaseUrl != null;

    /// <summary>Turns a stored media path ("/media/images/x.jpg") into a full URL.</summary>
    public string ResolveMediaUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) && absolute.Scheme.StartsWith("http"))
            return absolute.ToString();
        return _config.ApiBaseUrl == null ? "" : new Uri(_config.ApiBaseUrl, path.TrimStart('/')).ToString();
    }

    // ---------- Products (public) ----------

    public async Task<ApiResult<PagedResult<Product>>> GetProductsAsync(ProductQuery query, CancellationToken ct = default)
    {
        var result = await SendAsync<PagedResult<Product>>(() => new HttpRequestMessage(HttpMethod.Get, $"api/products?{query.ToQueryString()}"), ct);
        if (result.Success)
            result.Data!.Items.ForEach(PrepareProduct);
        return result;
    }

    public async Task<ApiResult<Product>> GetProductAsync(int productId, CancellationToken ct = default)
    {
        var result = await SendAsync<Product>(() => new HttpRequestMessage(HttpMethod.Get, $"api/products/{productId}"), ct);
        if (result.Success)
            PrepareProduct(result.Data!);
        return result;
    }

    /// <summary>Counts a customer opening a product. Failures are ignored.</summary>
    public Task RecordViewAsync(int productId) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"api/products/{productId}/view"));

    public Task<ApiResult<List<Category>>> GetCategoriesAsync(bool includeHidden = false, CancellationToken ct = default) =>
        SendAsync<List<Category>>(() => new HttpRequestMessage(HttpMethod.Get, includeHidden ? "api/categories/all" : "api/categories"), ct);

    public async Task<ApiResult<CompanySettings>> GetCompanyAsync(CancellationToken ct = default)
    {
        var result = await SendAsync<CompanySettings>(() => new HttpRequestMessage(HttpMethod.Get, "api/company"), ct);
        if (result.Success)
        {
            _company = result.Data;
            PriceFormatter.CurrencyCode = string.IsNullOrWhiteSpace(_company!.CurrencyCode) ? "USD" : _company.CurrencyCode;
        }
        return result;
    }

    private CompanySettings? _company;

    /// <summary>Company details from the last successful load (cached for Call/WhatsApp buttons).</summary>
    public async Task<CompanySettings?> GetCachedCompanyAsync()
    {
        if (_company != null)
            return _company;
        var result = await GetCompanyAsync();
        return result.Success ? result.Data : null;
    }

    // ---------- Products (staff) ----------

    public Task<ApiResult<Product>> AddProductAsync(ProductForm form) =>
        SendPreparedAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/products") { Content = JsonContent.Create(form) });

    public Task<ApiResult<Product>> UpdateProductAsync(int productId, ProductForm form) =>
        SendPreparedAsync(() => new HttpRequestMessage(HttpMethod.Put, $"api/products/{productId}") { Content = JsonContent.Create(form) });

    public Task<ApiResult<Product>> UpdateStatusAsync(int productId, bool? isAvailable = null, bool? isFeatured = null, int? quantity = null) =>
        SendPreparedAsync(() => new HttpRequestMessage(HttpMethod.Patch, $"api/products/{productId}/status")
        {
            Content = JsonContent.Create(new { isAvailable, isFeatured, quantity })
        });

    public Task<ApiResult> DeleteProductAsync(int productId) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/products/{productId}"));

    public Task<ApiResult<string>> UploadImageAsync(string filePath) => UploadAsync("api/upload/image", filePath);

    public Task<ApiResult<string>> UploadVideoAsync(string filePath) => UploadAsync("api/upload/video", filePath);

    public Task<ApiResult<CatalogStats>> GetStatsAsync(CancellationToken ct = default) =>
        SendAsync<CatalogStats>(() => new HttpRequestMessage(HttpMethod.Get, "api/stats"), ct);

    // ---------- Categories and company (staff/admin) ----------

    public Task<ApiResult<Category>> SaveCategoryAsync(Category category)
    {
        var body = new { name = category.Name, nameSo = category.NameSo, icon = category.Icon, sortOrder = category.SortOrder, isActive = category.IsActive };
        return category.CategoryId == 0
            ? SendAsync<Category>(() => new HttpRequestMessage(HttpMethod.Post, "api/categories") { Content = JsonContent.Create(body) })
            : SendAsync<Category>(() => new HttpRequestMessage(HttpMethod.Put, $"api/categories/{category.CategoryId}") { Content = JsonContent.Create(body) });
    }

    public Task<ApiResult> DeleteCategoryAsync(int categoryId) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/categories/{categoryId}"));

    public async Task<ApiResult<CompanySettings>> UpdateCompanyAsync(CompanySettings settings)
    {
        var result = await SendAsync<CompanySettings>(() => new HttpRequestMessage(HttpMethod.Put, "api/company") { Content = JsonContent.Create(settings) });
        if (result.Success)
            _company = result.Data;
        return result;
    }

    // ---------- Staff sign-in ----------

    public async Task<ApiResult<AuthResponse>> LoginAsync(string emailOrPhone, string password)
    {
        var result = await SendAsync<AuthResponse>(() => new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { emailOrPhone, password })
        });

        if (result.Success)
            await _session.SaveAsync(result.Data!);
        return result;
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await _session.GetRefreshTokenAsync();
        _session.Clear();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Best effort: the local session is already gone even if the server is unreachable.
            await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, "api/auth/logout")
            {
                Content = JsonContent.Create(new { refreshToken })
            });
        }
    }

    // ---------- Plumbing ----------

    private void PrepareProduct(Product product)
    {
        product.ImageUrl = ResolveMediaUrl(product.ImageUrl);
        foreach (var media in product.Media)
            media.DisplayUrl = ResolveMediaUrl(media.Url);
    }

    private async Task<ApiResult<Product>> SendPreparedAsync(Func<HttpRequestMessage> build)
    {
        var result = await SendAsync<Product>(build);
        if (result.Success)
            PrepareProduct(result.Data!);
        return result;
    }

    private async Task<ApiResult<string>> UploadAsync(string url, string filePath)
    {
        // Built fresh per attempt: a file stream can only be sent once.
        HttpRequestMessage Build()
        {
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));
            return new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        }

        var result = await SendAsync<UploadResult>(Build);
        if (result.IsUnauthorized)
            result = await SendAsync<UploadResult>(Build); // token was refreshed by the first attempt

        return result.Success
            ? ApiResult<string>.Ok(result.Data!.Url)
            : ApiResult<string>.Fail(result.Error, result.IsNetworkError, result.IsUnauthorized);
    }

    private async Task<ApiResult> SendAsync(Func<HttpRequestMessage> build, CancellationToken ct = default)
    {
        var result = await SendAsync<object>(build, ct, expectBody: false);
        return result.Success ? ApiResult.Ok() : ApiResult.Fail(result.Error, result.IsNetworkError, result.IsUnauthorized);
    }

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpRequestMessage> build, CancellationToken ct = default, bool expectBody = true)
    {
        if (!IsConfigured)
            return ApiResult<T>.Fail(Loc.T("ErrNotConfigured"));

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return ApiResult<T>.Fail(Loc.T("ErrNoInternet"), network: true);

        try
        {
            using var request = build();
            using var response = await _http.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                if (!expectBody || response.StatusCode == HttpStatusCode.NoContent)
                    return ApiResult<T>.Ok(default!);
                var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return data == null ? ApiResult<T>.Fail("The server sent an empty response.") : ApiResult<T>.Ok(data);
            }

            var message = await ReadErrorAsync(response);
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ApiResult<T>.Fail(message ?? "Please sign in again.", unauthorized: true),
                HttpStatusCode.Forbidden => ApiResult<T>.Fail("You do not have permission to do this."),
                HttpStatusCode.NotFound => ApiResult<T>.Fail(message ?? Loc.T("ErrNotFound")),
                HttpStatusCode.TooManyRequests => ApiResult<T>.Fail(Loc.T("ErrTooMany")),
                HttpStatusCode.RequestEntityTooLarge => ApiResult<T>.Fail("The file is too large."),
                >= HttpStatusCode.InternalServerError => ApiResult<T>.Fail(Loc.T("ErrServer")),
                _ => ApiResult<T>.Fail(message ?? "The request could not be completed.")
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ApiResult<T>.Fail("Cancelled.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return ApiResult<T>.Fail(Loc.T("ErrUnreachable"), network: true);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Fail(Loc.T("ErrUnexpected"));
        }
    }

    /// <summary>Reads a ProblemDetails / ValidationProblemDetails body into one readable message.</summary>
    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text))
                return null;

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString()))
                    .Where(m => !string.IsNullOrWhiteSpace(m));
                return string.Join("\n", messages);
            }
            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();
            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString();
        }
        catch (JsonException)
        {
        }
        return null;
    }

    private class UploadResult
    {
        public string Url { get; set; } = "";
    }
}
