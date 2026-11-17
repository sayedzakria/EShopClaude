namespace EShopClaude.Basket.Api;

public record BasketResponse(Guid BasketId, List<BasketItem> Items);
