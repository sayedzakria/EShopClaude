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

    /// <summary>
    /// Sets an existing line's quantity to exactly <paramref name="request"/>'s quantity.
    /// Returns Invalid (without touching the store) when the quantity is below 1, NotFound when the
    /// line does not exist (nothing is created), otherwise Updated with the basket after the write.
    /// </summary>
    public static async Task<UpdateItemResult> UpdateItemQuantityAsync(
        IBasketStore store, Guid basketId, int productId, UpdateItemQuantityRequest request)
    {
        if (request.Quantity < 1)
        {
            return new UpdateItemResult(UpdateItemOutcome.Invalid);
        }

        if (!await store.SetQuantityAsync(basketId, productId, request.Quantity))
        {
            return new UpdateItemResult(UpdateItemOutcome.NotFound);
        }

        return new UpdateItemResult(UpdateItemOutcome.Updated, await store.GetAsync(basketId));
    }
}

public enum UpdateItemOutcome
{
    Updated,
    Invalid,
    NotFound,
}

public record UpdateItemResult(UpdateItemOutcome Outcome, List<BasketItem>? Items = null);
