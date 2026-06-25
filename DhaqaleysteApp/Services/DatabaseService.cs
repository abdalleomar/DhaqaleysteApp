using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using DhaqaleysteApp.Models;

namespace DhaqaleysteApp.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;

    public DatabaseService(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
        _db.CreateTableAsync<Product>().Wait();
    }

    public Task<List<Product>> GetProductsAsync()
    {
        return _db.Table<Product>().ToListAsync();
    }

    public Task<int> AddProductAsync(Product product)
    {
        return _db.InsertAsync(product);
    }

    public Task<int> UpdateProductAsync(Product product)
    {
        return _db.UpdateAsync(product);
    }

    public Task<int> DeleteProductAsync(Product product)
    {
        return _db.DeleteAsync(product);
    }
}