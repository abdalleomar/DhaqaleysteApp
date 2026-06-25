using DhaqaleysteApp.Models;
using System.Net.Http.Json;

namespace DhaqaleysteApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient();

        _httpClient.BaseAddress =
            new Uri("https://localhost:7192/");
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Product>>
            ("api/products")
            ?? new List<Product>();
    }

    public async Task<bool> AddProductAsync(Product product)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                "api/products",
                product);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateProductAsync(Product product)
    {
        var response = await _httpClient.PutAsJsonAsync(
            $"api/products/{product.ProductId}",
            product);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var response =
            await _httpClient.DeleteAsync(
                $"api/products/{productId}");

        return response.IsSuccessStatusCode;
    }



    public async Task<string> UploadImageAsync(string filePath)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(File.OpenRead(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync("api/upload/image", content);

        if (!response.IsSuccessStatusCode)
            return "";

        var result = await response.Content.ReadFromJsonAsync<UploadResult>();

        return result?.Url ?? "";
    }

    public async Task<string> UploadVideoAsync(string filePath)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(File.OpenRead(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync("api/upload/video", content);

        if (!response.IsSuccessStatusCode)
            return "";

        var result = await response.Content.ReadFromJsonAsync<UploadResult>();

        return result?.Url ?? "";
    }

    public class UploadResult
    {
        public string Url { get; set; } = "";
    }


    public async Task<bool> RegisterAsync(User user)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                "api/users/register",
                user);

        return response.IsSuccessStatusCode;
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                "api/users/login",
                new User
                {
                    Email = email,
                    Password = password
                });

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<User>();
    }
}