namespace EShopClaude.Ordering.Api;

public static class OrderCreation
{
    /// <summary>
    /// Validates the request, computes the total and saves the order with its items.
    /// Returns null (and saves nothing) when the request is invalid.
    /// </summary>
    public static async Task<Order?> CreateAsync(OrderingDbContext db, CreateOrderRequest request)
    {
        if (request.Items is not { Count: > 0 } items
            || items.Any(i => i.Quantity < 1 || i.UnitPrice < 0))
        {
            return null;
        }

        var order = new Order
        {
            Status = "Placed",
            CreatedDate = DateTime.UtcNow,
            Total = items.Sum(i => i.UnitPrice * i.Quantity),
            Items = items
                .Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                })
                .ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }
}
