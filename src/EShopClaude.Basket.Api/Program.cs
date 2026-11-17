using EShopClaude.Basket.Api;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddRedisClient("redis");

builder.Services.AddSingleton<IBasketStore, RedisBasketStore>();

var app = builder.Build();

app.MapGet("/", () => "Basket API is running.");

app.MapGet("/baskets/{basketId:guid}", async (Guid basketId, IBasketStore store) =>
    Results.Ok(new BasketResponse(basketId, await store.GetAsync(basketId))));

app.MapDefaultEndpoints();

app.Run();
