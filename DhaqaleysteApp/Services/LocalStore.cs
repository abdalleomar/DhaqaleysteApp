using System.Text.Json;
using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Services;

/// <summary>A product remembered on the device, enough to show a card before the server answers.</summary>
public class ProductSnapshot
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string CategoryNameSo { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
    public int Quantity { get; set; }
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

    public static ProductSnapshot From(Product p) => new()
    {
        ProductId = p.ProductId,
        ProductName = p.ProductName,
        Price = p.Price,
        ImageUrl = p.ImageUrl,
        CategoryName = p.CategoryName,
        CategoryNameSo = p.CategoryNameSo,
        IsAvailable = p.IsAvailable,
        Quantity = p.Quantity
    };

    public Product ToProduct() => new()
    {
        ProductId = ProductId,
        ProductName = ProductName,
        Price = Price,
        ImageUrl = ImageUrl,
        CategoryName = CategoryName,
        CategoryNameSo = CategoryNameSo,
        IsAvailable = IsAvailable,
        Quantity = Quantity
    };
}

/// <summary>
/// Favourites and recently viewed products, saved in a small JSON file on the device.
/// Customers have no account, so nothing is sent to the server.
/// </summary>
public class LocalStore
{
    public const int MaxFavorites = 50;   // the API returns at most 50 products by id
    public const int MaxRecent = 20;

    private readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "local-store.json");
    private readonly SemaphoreSlim _lock = new(1, 1);
    private StoreData? _data;

    public event EventHandler? FavoritesChanged;

    private class StoreData
    {
        public List<ProductSnapshot> Favorites { get; set; } = new();
        public List<ProductSnapshot> Recent { get; set; } = new();
    }

    public async Task<IReadOnlyList<ProductSnapshot>> GetFavoritesAsync() => (await LoadAsync()).Favorites.ToList();

    public async Task<IReadOnlyList<ProductSnapshot>> GetRecentAsync() => (await LoadAsync()).Recent.ToList();

    public async Task<bool> IsFavoriteAsync(int productId) => (await LoadAsync()).Favorites.Any(f => f.ProductId == productId);

    /// <summary>Adds or removes a favourite. Returns true when the product is now a favourite.</summary>
    public async Task<bool> ToggleFavoriteAsync(Product product)
    {
        var data = await LoadAsync();
        bool isFavorite;
        var existing = data.Favorites.FirstOrDefault(f => f.ProductId == product.ProductId);
        if (existing != null)
        {
            data.Favorites.Remove(existing);
            isFavorite = false;
        }
        else
        {
            data.Favorites.Insert(0, ProductSnapshot.From(product));
            if (data.Favorites.Count > MaxFavorites)
                data.Favorites.RemoveRange(MaxFavorites, data.Favorites.Count - MaxFavorites);
            isFavorite = true;
        }

        await SaveAsync(data);
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
        return isFavorite;
    }

    /// <summary>Replaces saved favourites with fresh server data; products that no longer exist are dropped.</summary>
    public async Task RefreshFavoritesAsync(IEnumerable<Product> current)
    {
        var data = await LoadAsync();
        var byId = current.ToDictionary(p => p.ProductId);
        data.Favorites = data.Favorites
            .Where(f => byId.ContainsKey(f.ProductId))
            .Select(f => { var s = ProductSnapshot.From(byId[f.ProductId]); s.SavedAtUtc = f.SavedAtUtc; return s; })
            .ToList();
        await SaveAsync(data);
    }

    public async Task AddRecentAsync(Product product)
    {
        var data = await LoadAsync();
        data.Recent.RemoveAll(r => r.ProductId == product.ProductId);
        data.Recent.Insert(0, ProductSnapshot.From(product));
        if (data.Recent.Count > MaxRecent)
            data.Recent.RemoveRange(MaxRecent, data.Recent.Count - MaxRecent);
        await SaveAsync(data);
    }

    public async Task RemoveRecentAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.ToHashSet();
        var data = await LoadAsync();
        if (data.Recent.RemoveAll(r => ids.Contains(r.ProductId)) > 0)
            await SaveAsync(data);
    }

    private async Task<StoreData> LoadAsync()
    {
        if (_data != null)
            return _data;

        await _lock.WaitAsync();
        try
        {
            if (_data != null)
                return _data;

            if (File.Exists(_path))
            {
                try
                {
                    await using var stream = File.OpenRead(_path);
                    _data = await JsonSerializer.DeserializeAsync<StoreData>(stream);
                }
                catch (Exception ex) when (ex is JsonException or IOException)
                {
                    // A damaged file should never stop the app; start again with an empty list.
                    System.Diagnostics.Debug.WriteLine($"local-store.json could not be read: {ex.Message}");
                }
            }
            return _data ??= new StoreData();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveAsync(StoreData data)
    {
        await _lock.WaitAsync();
        try
        {
            var temp = _path + ".tmp";
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, data);
            File.Move(temp, _path, overwrite: true);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"local-store.json could not be saved: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }
}
