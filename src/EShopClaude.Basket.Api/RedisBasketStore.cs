using StackExchange.Redis;

namespace EShopClaude.Basket.Api;

public class RedisBasketStore(IConnectionMultiplexer redis) : IBasketStore
{
    public async Task<List<BasketItem>> GetAsync(Guid basketId)
    {
        var entries = await redis.GetDatabase().HashGetAllAsync(Key(basketId));

        return entries
            .Select(e => new BasketItem(int.Parse(e.Name!), int.Parse(e.Value!)))
            .OrderBy(i => i.ProductId)
            .ToList();
    }

    private static RedisKey Key(Guid basketId) => $"basket:{basketId}";
}
