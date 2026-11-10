namespace EShopClaude.Ordering.Api;

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
