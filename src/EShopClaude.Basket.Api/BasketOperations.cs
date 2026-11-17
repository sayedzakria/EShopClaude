namespace EShopClaude.Basket.Api;

public static class BasketOperations
{
    /// <summary>
    /// Adds <paramref name="request"/>'s quantity to the product's line, creating the line if needed.
    /// Returns the basket after the add, or null (without writing) when the request is invalid.
    /// </summary>
    public static async Task<List<BasketItem>?> AddItemAsync(IBasketStore store, Guid basketId, AddItemRequest request)
    {
        if (request.ProductId < 1 || request.Quantity < 1)
        {
            return null;
        }

        await store.AddAsync(basketId, request.ProductId, request.Quantity);
        return await store.GetAsync(basketId);
    }
}
