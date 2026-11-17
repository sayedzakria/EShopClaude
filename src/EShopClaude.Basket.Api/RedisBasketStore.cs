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

    public Task AddAsync(Guid basketId, int productId, int quantity) =>
        redis.GetDatabase().HashIncrementAsync(Key(basketId), productId, quantity);

    public async Task<bool> SetQuantityAsync(Guid basketId, int productId, int quantity)
    {
        var key = Key(basketId);
        var transaction = redis.GetDatabase().CreateTransaction();
        transaction.AddCondition(Condition.HashExists(key, productId));

        // Queued inside MULTI/EXEC; must not be awaited before ExecuteAsync.
        _ = transaction.HashSetAsync(key, productId, quantity);

        return await transaction.ExecuteAsync();
    }

    private static RedisKey Key(Guid basketId) => $"basket:{basketId}";
}
