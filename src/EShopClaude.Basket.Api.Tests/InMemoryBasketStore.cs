using EShopClaude.Basket.Api;

namespace EShopClaude.Basket.Api.Tests;

/// <summary>
/// Dictionary-backed <see cref="IBasketStore"/> that behaves like the Redis hash store:
/// adding creates or increments a line, reads are sorted by product id.
/// </summary>
public sealed class InMemoryBasketStore : IBasketStore
{
    private readonly Dictionary<Guid, Dictionary<int, int>> _baskets = new();

    public bool Contains(Guid basketId) => _baskets.ContainsKey(basketId);

    public Task<List<BasketItem>> GetAsync(Guid basketId)
    {
        var items = _baskets.TryGetValue(basketId, out var lines)
            ? lines.Select(l => new BasketItem(l.Key, l.Value)).OrderBy(i => i.ProductId).ToList()
            : [];

        return Task.FromResult(items);
    }

    public Task AddAsync(Guid basketId, int productId, int quantity)
    {
        if (!_baskets.TryGetValue(basketId, out var lines))
        {
            lines = new Dictionary<int, int>();
            _baskets[basketId] = lines;
        }

        lines[productId] = lines.GetValueOrDefault(productId) + quantity;
        return Task.CompletedTask;
    }

    public Task<bool> SetQuantityAsync(Guid basketId, int productId, int quantity)
    {
        if (!_baskets.TryGetValue(basketId, out var lines) || !lines.ContainsKey(productId))
        {
            return Task.FromResult(false);
        }

        lines[productId] = quantity;
        return Task.FromResult(true);
    }
}
