using EShopClaude.Catalog.Api;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api.Tests;

public sealed class ProductStockTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CatalogDbContext _db;

    public ProductStockTests()
    {
        // The in-memory SQLite database lives as long as this connection stays open.
        _connection.Open();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new CatalogDbContext(options);
        _db.Database.EnsureCreated();

        _db.Products.Add(new Product { Id = 1, Name = "Widget", Description = "A widget", Price = 10m, Stock = 5 });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<int> StockAsync(int id) =>
        await _db.Products.AsNoTracking().Where(p => p.Id == id).Select(p => p.Stock).SingleAsync();

    [Fact]
    public async Task Decrement_WithSufficientStock_ReturnsOkAndLowersStock()
    {
        var result = await ProductStock.DecrementAsync(_db, 1, 2);

        Assert.Equal(DecrementResult.Ok, result);
        Assert.Equal(3, await StockAsync(1));
    }

    [Fact]
    public async Task Decrement_WithQuantityEqualToStock_ReturnsOkAndLeavesZero()
    {
        var result = await ProductStock.DecrementAsync(_db, 1, 5);

        Assert.Equal(DecrementResult.Ok, result);
        Assert.Equal(0, await StockAsync(1));
    }

    [Fact]
    public async Task Decrement_WithInsufficientStock_ReturnsInsufficientStockAndKeepsStock()
    {
        var result = await ProductStock.DecrementAsync(_db, 1, 6);

        Assert.Equal(DecrementResult.InsufficientStock, result);
        Assert.Equal(5, await StockAsync(1));
    }

    [Fact]
    public async Task Decrement_WithUnknownId_ReturnsNotFound()
    {
        var result = await ProductStock.DecrementAsync(_db, 999, 1);

        Assert.Equal(DecrementResult.NotFound, result);
    }

    [Fact]
    public async Task Decrement_Twice_SecondCallSeesLoweredStock()
    {
        Assert.Equal(DecrementResult.Ok, await ProductStock.DecrementAsync(_db, 1, 3));
        Assert.Equal(DecrementResult.InsufficientStock, await ProductStock.DecrementAsync(_db, 1, 3));
        Assert.Equal(2, await StockAsync(1));
    }
}
