using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DhaqaleysteApp.Models;

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

    public async Task<ApiResult<List<Product>>> GetProductsAsync(CancellationToken ct = default)
    {
        var result = await SendAsync<List<Product>>(() => new HttpRequestMessage(HttpMethod.Get, "api/products"), ct);
        if (result.Success)
            result.Data!.ForEach(PrepareProduct);
        return result;
    }

    // ---------- Products (staff) ----------

    public Task<ApiResult<Product>> AddProductAsync(Product product) =>
        SendAsync<Product>(() => new HttpRequestMessage(HttpMethod.Post, "api/products") { Content = JsonContent.Create(ToRequest(product)) });

    public Task<ApiResult<Product>> UpdateProductAsync(Product product) =>
        SendAsync<Product>(() => new HttpRequestMessage(HttpMethod.Put, $"api/products/{product.ProductId}") { Content = JsonContent.Create(ToRequest(product)) });

    public Task<ApiResult> DeleteProductAsync(int productId) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"api/products/{productId}"));

    public Task<ApiResult<string>> UploadImageAsync(string filePath) => UploadAsync("api/upload/image", filePath);

    public Task<ApiResult<string>> UploadVideoAsync(string filePath) => UploadAsync("api/upload/video", filePath);

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
        product.ImageUrl = ResolveMediaUrl(product.ImagePath);
        product.VideoUrl = ResolveMediaUrl(product.VideoPath);
    }

    private static object ToRequest(Product p) => new
    {
        productName = p.ProductName,
        description = p.Description,
        price = p.Price,
        quantity = p.Quantity,
        contact = p.Contact,
        location = p.Location,
        category = p.Category,
        imagePath = p.ImagePath,
        videoPath = p.VideoPath
    };

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
            return ApiResult<T>.Fail("The app is not connected to a server. Please contact the company.");

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return ApiResult<T>.Fail("No internet connection. Check your connection and try again.", network: true);

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
                HttpStatusCode.NotFound => ApiResult<T>.Fail(message ?? "This item no longer exists."),
                HttpStatusCode.TooManyRequests => ApiResult<T>.Fail("Too many attempts. Please wait a minute and try again."),
                HttpStatusCode.RequestEntityTooLarge => ApiResult<T>.Fail("The file is too large."),
                >= HttpStatusCode.InternalServerError => ApiResult<T>.Fail("The server had a problem. Please try again later."),
                _ => ApiResult<T>.Fail(message ?? "The request could not be completed.")
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ApiResult<T>.Fail("Cancelled.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return ApiResult<T>.Fail("Could not reach the server. Check your connection and try again.", network: true);
        }
        catch (JsonException)
        {
            return ApiResult<T>.Fail("The server sent an unexpected response.");
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
