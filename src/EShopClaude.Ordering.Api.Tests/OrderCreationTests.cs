using EShopClaude.Ordering.Api;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Ordering.Api.Tests;

public sealed class OrderCreationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly OrderingDbContext _db;

    public OrderCreationTests()
    {
        // The in-memory SQLite database lives as long as this connection stays open.
        _connection.Open();

        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new OrderingDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static CreateOrderRequest Request(params OrderItemRequest[] items) => new(items.ToList());

    [Fact]
    public async Task Create_WithSeveralItems_TotalIsSumOfLineTotals()
    {
        var order = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(1, "Widget", 10.50m, 2),
            new OrderItemRequest(2, "Gadget", 3.25m, 4)));

        Assert.NotNull(order);
        Assert.Equal(34.00m, order.Total);
    }

    [Fact]
    public async Task Create_WithOneItem_TotalIsItsLineTotal()
    {
        var order = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(3, "Gizmo", 5.00m, 3)));

        Assert.NotNull(order);
        Assert.Equal(15.00m, order.Total);
    }

    [Fact]
    public async Task Create_SetsStatusToPlaced()
    {
        var order = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(1, "Widget", 10m, 1)));

        Assert.NotNull(order);
        Assert.Equal("Placed", order.Status);
    }

    [Fact]
    public async Task Create_SetsCreatedDateToCurrentUtcTime()
    {
        var before = DateTime.UtcNow;

        var order = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(1, "Widget", 10m, 1)));

        var after = DateTime.UtcNow;
        Assert.NotNull(order);
        Assert.Equal(DateTimeKind.Utc, order.CreatedDate.Kind);
        Assert.InRange(order.CreatedDate, before, after);
    }

    [Fact]
    public async Task Create_SavesOrderAndItemsWithRequestValues()
    {
        var order = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(1, "Widget", 10.50m, 2),
            new OrderItemRequest(2, "Gadget", 3.25m, 4)));

        Assert.NotNull(order);
        Assert.True(order.Id > 0);

        // A fresh context so the read comes from the store, not the change tracker.
        using var readDb = new OrderingDbContext(
            new DbContextOptionsBuilder<OrderingDbContext>().UseSqlite(_connection).Options);
        var saved = await readDb.Orders.AsNoTracking().Include(o => o.Items).SingleAsync(o => o.Id == order.Id);

        Assert.Equal(34.00m, saved.Total);
        Assert.Equal("Placed", saved.Status);
        Assert.Equal(2, saved.Items.Count);

        var widget = saved.Items.Single(i => i.ProductId == 1);
        Assert.Equal("Widget", widget.ProductName);
        Assert.Equal(10.50m, widget.UnitPrice);
        Assert.Equal(2, widget.Quantity);
        Assert.Equal(saved.Id, widget.OrderId);

        var gadget = saved.Items.Single(i => i.ProductId == 2);
        Assert.Equal("Gadget", gadget.ProductName);
        Assert.Equal(3.25m, gadget.UnitPrice);
        Assert.Equal(4, gadget.Quantity);
    }

    [Fact]
    public async Task Create_WithNullItems_ReturnsNullAndSavesNothing()
    {
        Assert.Null(await OrderCreation.CreateAsync(_db, new CreateOrderRequest(null)));
        await AssertNothingSavedAsync();
    }

    [Fact]
    public async Task Create_WithEmptyItems_ReturnsNullAndSavesNothing()
    {
        Assert.Null(await OrderCreation.CreateAsync(_db, Request()));
        await AssertNothingSavedAsync();
    }

    [Fact]
    public async Task Create_WithQuantityBelowOne_ReturnsNullAndSavesNothing()
    {
        var result = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(1, "Widget", 10m, 1),
            new OrderItemRequest(2, "Gadget", 5m, 0)));

        Assert.Null(result);
        await AssertNothingSavedAsync();
    }

    [Fact]
    public async Task Create_WithNegativeUnitPrice_ReturnsNullAndSavesNothing()
    {
        var result = await OrderCreation.CreateAsync(_db, Request(
            new OrderItemRequest(1, "Widget", -1m, 1)));

        Assert.Null(result);
        await AssertNothingSavedAsync();
    }

    private async Task AssertNothingSavedAsync()
    {
        Assert.Equal(0, await _db.Orders.CountAsync());
        Assert.Equal(0, await _db.OrderItems.CountAsync());
    }
}
