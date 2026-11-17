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

    [Fact]
    public async Task UpdateItemQuantity_ExistingLine_SetsExactQuantity()
    {
        await _store.AddAsync(_basketId, 1, 2);

        var result = await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 1, new UpdateItemQuantityRequest(7));

        Assert.Equal(UpdateItemOutcome.Updated, result.Outcome);
        Assert.Equal([new BasketItem(1, 7)], result.Items);
    }

    [Fact]
    public async Task UpdateItemQuantity_LeavesOtherLinesUnchanged()
    {
        await _store.AddAsync(_basketId, 1, 2);
        await _store.AddAsync(_basketId, 3, 1);

        var result = await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 3, new UpdateItemQuantityRequest(5));

        Assert.Equal(UpdateItemOutcome.Updated, result.Outcome);
        Assert.Equal([new BasketItem(1, 2), new BasketItem(3, 5)], result.Items);
    }

    [Fact]
    public async Task UpdateItemQuantity_ProductNotInBasket_ReturnsNotFoundAndBasketUnchanged()
    {
        await _store.AddAsync(_basketId, 1, 2);

        var result = await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 2, new UpdateItemQuantityRequest(4));

        Assert.Equal(UpdateItemOutcome.NotFound, result.Outcome);
        Assert.Null(result.Items);
        Assert.Equal([new BasketItem(1, 2)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task UpdateItemQuantity_UnknownBasket_ReturnsNotFoundAndCreatesNothing()
    {
        var result = await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 1, new UpdateItemQuantityRequest(3));

        Assert.Equal(UpdateItemOutcome.NotFound, result.Outcome);
        Assert.False(_store.Contains(_basketId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateItemQuantity_WithQuantityBelowOne_ReturnsInvalidAndLeavesLineUnchanged(int quantity)
    {
        await _store.AddAsync(_basketId, 1, 2);

        var result = await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 1, new UpdateItemQuantityRequest(quantity));

        Assert.Equal(UpdateItemOutcome.Invalid, result.Outcome);
        Assert.Null(result.Items);
        Assert.Equal([new BasketItem(1, 2)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task UpdateItemQuantity_InvalidQuantityForMissingLine_ReturnsInvalid()
    {
        await _store.AddAsync(_basketId, 1, 2);

        var result = await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 2, new UpdateItemQuantityRequest(0));

        Assert.Equal(UpdateItemOutcome.Invalid, result.Outcome);
        Assert.Equal([new BasketItem(1, 2)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task UpdateItemQuantity_OneBasket_DoesNotAffectAnotherBasket()
    {
        var otherBasketId = Guid.NewGuid();
        await _store.AddAsync(_basketId, 1, 2);
        await _store.AddAsync(otherBasketId, 1, 4);

        await BasketOperations.UpdateItemQuantityAsync(_store, _basketId, 1, new UpdateItemQuantityRequest(9));

        Assert.Equal([new BasketItem(1, 9)], await _store.GetAsync(_basketId));
        Assert.Equal([new BasketItem(1, 4)], await _store.GetAsync(otherBasketId));
    }

    [Fact]
    public async Task RemoveItem_ExistingLine_ReturnsRemainingLinesAndLeavesOthersUnchanged()
    {
        await _store.AddAsync(_basketId, 1, 2);
        await _store.AddAsync(_basketId, 3, 5);

        var items = await BasketOperations.RemoveItemAsync(_store, _basketId, 1);

        Assert.Equal([new BasketItem(3, 5)], items);
        Assert.Equal([new BasketItem(3, 5)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task RemoveItem_ProductNotInBasket_ReturnsNullAndBasketUnchanged()
    {
        await _store.AddAsync(_basketId, 1, 2);

        var items = await BasketOperations.RemoveItemAsync(_store, _basketId, 2);

        Assert.Null(items);
        Assert.Equal([new BasketItem(1, 2)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task RemoveItem_UnknownBasket_ReturnsNullAndCreatesNothing()
    {
        var items = await BasketOperations.RemoveItemAsync(_store, _basketId, 1);

        Assert.Null(items);
        Assert.False(_store.Contains(_basketId));
    }

    [Fact]
    public async Task RemoveItem_LastLine_ReturnsEmptyListAndRemovesBasket()
    {
        await _store.AddAsync(_basketId, 1, 2);

        var items = await BasketOperations.RemoveItemAsync(_store, _basketId, 1);

        Assert.NotNull(items);
        Assert.Empty(items);
        Assert.False(_store.Contains(_basketId));
    }

    [Fact]
    public async Task RemoveItem_SameLineTwice_ReturnsNullSecondTime()
    {
        await _store.AddAsync(_basketId, 1, 2);
        await _store.AddAsync(_basketId, 3, 1);

        await BasketOperations.RemoveItemAsync(_store, _basketId, 1);
        var second = await BasketOperations.RemoveItemAsync(_store, _basketId, 1);

        Assert.Null(second);
        Assert.Equal([new BasketItem(3, 1)], await _store.GetAsync(_basketId));
    }

    [Fact]
    public async Task RemoveItem_OneBasket_DoesNotAffectAnotherBasket()
    {
        var otherBasketId = Guid.NewGuid();
        await _store.AddAsync(_basketId, 1, 2);
        await _store.AddAsync(otherBasketId, 1, 4);

        await BasketOperations.RemoveItemAsync(_store, _basketId, 1);

        Assert.False(_store.Contains(_basketId));
        Assert.Equal([new BasketItem(1, 4)], await _store.GetAsync(otherBasketId));
    }
}
