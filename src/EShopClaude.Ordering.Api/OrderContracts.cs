namespace EShopClaude.Ordering.Api;

public record CreateOrderRequest(List<OrderItemRequest>? Items);

public record OrderItemRequest(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderResponse(int Id, List<OrderItemResponse> Items, decimal Total, string Status, DateTime CreatedDate)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.Items
            .Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity))
            .ToList(),
        order.Total,
        order.Status,
        order.CreatedDate);
}
