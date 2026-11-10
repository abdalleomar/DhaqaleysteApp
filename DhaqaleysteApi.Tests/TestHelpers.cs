using System.Net.Http.Headers;
using System.Net.Http.Json;
using DhaqaleysteApi.Dtos;

namespace DhaqaleysteApi.Tests;

public static class TestHelpers
{
    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string emailOrPhone, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { emailOrPhone, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static object ValidProduct(string name = "Test Product", int? categoryId = null, decimal price = 12.5m,
        int quantity = 3, bool isAvailable = true, bool isFeatured = false, object[]? media = null) => new
    {
        productName = name,
        description = "A product",
        price,
        quantity,
        contact = "615000000",
        location = "Mogadishu",
        categoryId,
        isAvailable,
        isFeatured,
        media = media ?? Array.Empty<object>()
    };

    public static object Image(string url) => new { kind = "Image", url };
    public static object Video(string url) => new { kind = "Video", url };

    public static readonly byte[] PngBytes =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    };

    public static MultipartFormDataContent FileContent(byte[] bytes, string fileName)
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", fileName);
        return content;
    }
}
