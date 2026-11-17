using EShopClaude.Basket.Api;

namespace EShopClaude.Basket.Api.Tests;

public sealed class BasketOperationsTests
{
    private readonly InMemoryBasketStore _store = new();
    private readonly Guid _basketId = Guid.NewGuid();

    [Fact]
    public async Task AddItem_ToEmptyBasket_CreatesOneLine()
    {
        var items = await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 2));

        Assert.NotNull(items);
        Assert.Equal([new BasketItem(1, 2)], items);
    }

    [Fact]
    public async Task AddItem_TwoDifferentProducts_GivesTwoLinesSortedByProductId()
    {
        await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(3, 1));
        var items = await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 2));

        Assert.NotNull(items);
        Assert.Equal([new BasketItem(1, 2), new BasketItem(3, 1)], items);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_SumsQuantities()
    {
        await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 2));
        var items = await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 3));

        Assert.NotNull(items);
        Assert.Equal([new BasketItem(1, 5)], items);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddItem_WithQuantityBelowOne_ReturnsNullAndStoresNothing(int quantity)
    {
        var items = await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, quantity));

        Assert.Null(items);
        Assert.False(_store.Contains(_basketId));
    }

    [Fact]
    public async Task AddItem_WithProductIdBelowOne_ReturnsNullAndStoresNothing()
    {
        var items = await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(0, 1));

        Assert.Null(items);
        Assert.False(_store.Contains(_basketId));
    }

    [Fact]
    public async Task AddItem_InvalidRequest_LeavesExistingBasketUnchanged()
    {
        await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 2));

        Assert.Null(await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 0)));
        Assert.Equal([new BasketItem(1, 2)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task AddItem_ToOneBasket_DoesNotAffectAnotherBasket()
    {
        var otherBasketId = Guid.NewGuid();

        await BasketOperations.AddItemAsync(_store, _basketId, new AddItemRequest(1, 2));
        await BasketOperations.AddItemAsync(_store, otherBasketId, new AddItemRequest(2, 4));

        Assert.Equal([new BasketItem(1, 2)], await _store.GetAsync(_basketId));
        Assert.Equal([new BasketItem(2, 4)], await _store.GetAsync(otherBasketId));
    }
}
