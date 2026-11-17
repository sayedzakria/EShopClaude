namespace EShopClaude.Basket.Api;

public interface IBasketStore
{
    Task<List<BasketItem>> GetAsync(Guid basketId);

    Task AddAsync(Guid basketId, int productId, int quantity);

    /// <summary>
    /// Sets the product's line to exactly <paramref name="quantity"/>, only if the line already exists.
    /// Returns false (and writes nothing) when the line does not exist.
    /// </summary>
    Task<bool> SetQuantityAsync(Guid basketId, int productId, int quantity);
}
