using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api;

public enum DecrementResult
{
    Ok,
    NotFound,
    InsufficientStock
}

public static class ProductStock
{
    // One conditional UPDATE keeps the check and the decrement atomic, so concurrent
    // requests cannot take stock below zero.
    public static async Task<DecrementResult> DecrementAsync(CatalogDbContext db, int id, int quantity)
    {
        var updated = await db.Products
            .Where(p => p.Id == id && p.Stock >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - quantity));

        if (updated == 1)
        {
            return DecrementResult.Ok;
        }

        return await db.Products.AnyAsync(p => p.Id == id)
            ? DecrementResult.InsufficientStock
            : DecrementResult.NotFound;
    }
}
