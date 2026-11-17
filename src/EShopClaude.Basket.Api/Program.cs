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

app.MapPost("/baskets/{basketId:guid}/items", async (Guid basketId, AddItemRequest request, IBasketStore store) =>
    await BasketOperations.AddItemAsync(store, basketId, request) is { } items
        ? Results.Ok(new BasketResponse(basketId, items))
        : Results.BadRequest());

app.MapPut("/baskets/{basketId:guid}/items/{productId:int}",
    async (Guid basketId, int productId, UpdateItemQuantityRequest request, IBasketStore store) =>
    {
        var result = await BasketOperations.UpdateItemQuantityAsync(store, basketId, productId, request);

        return result.Outcome switch
        {
            UpdateItemOutcome.Updated => Results.Ok(new BasketResponse(basketId, result.Items!)),
            UpdateItemOutcome.Invalid => Results.BadRequest(),
            _ => Results.NotFound(),
        };
    });

app.MapDefaultEndpoints();

app.Run();
