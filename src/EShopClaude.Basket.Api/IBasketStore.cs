namespace EShopClaude.Basket.Api;

public interface IBasketStore
{
    Task<List<BasketItem>> GetAsync(Guid basketId);

    Task AddAsync(Guid basketId, int productId, int quantity);
}
