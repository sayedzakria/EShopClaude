namespace EShopClaude.Basket.Api;

public interface IBasketStore
{
    Task<List<BasketItem>> GetAsync(Guid basketId);
}
