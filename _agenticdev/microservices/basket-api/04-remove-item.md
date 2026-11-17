# Basket.Api — Spec 04: Remove item

**Status:** Done (5b80ee5)

## Goal

Let a shopper remove one line from a basket.

## Context

- `basket-api/01-get-basket`, `02-add-item` and `03-update-item-quantity` are merged. `basketapi` already references and waits for `redis` in `AppHost.cs`, and `Program.cs` calls `AddRedisClient("redis")`.
- A basket is stored as a Redis hash `basket:{basketId}` (field = `productId`, value = `quantity`, both decimal strings, no expiry). Redis deletes a hash key on its own when its last field is removed.
- `IBasketStore` has `GetAsync(Guid basketId)` (items sorted by `productId`), `AddAsync(Guid basketId, int productId, int quantity)` (`HINCRBY`) and `SetQuantityAsync(Guid basketId, int productId, int quantity)` (`Task<bool>`, a transaction conditioned on `HashExists`). `RedisBasketStore` implements all three with `IConnectionMultiplexer`, registered as a singleton.
- `BasketContracts.cs` holds `BasketResponse(Guid BasketId, List<BasketItem> Items)`, `AddItemRequest(int ProductId, int Quantity)` and `UpdateItemQuantityRequest(int Quantity)`. `BasketItem(int ProductId, int Quantity)` is in `BasketItem.cs`.
- `BasketOperations.cs` has static `AddItemAsync` (returns `List<BasketItem>?`, `null` = invalid → `400`) and `UpdateItemQuantityAsync` (returns `UpdateItemResult` with `UpdateItemOutcome { Updated, Invalid, NotFound }`).
- `Program.cs` maps `/`, `GET /baskets/{basketId:guid}`, `POST /baskets/{basketId:guid}/items`, `PUT /baskets/{basketId:guid}/items/{productId:int}` and `MapDefaultEndpoints()`. No `DELETE` is mapped.
- `src/EShopClaude.Basket.Api.Tests` exists (xunit, in the solution) with `InMemoryBasketStore` (dictionary-backed, has a `Contains(Guid)` helper, implements `GetAsync`, `AddAsync`, `SetQuantityAsync`) and `BasketOperationsTests` (16 test cases: 8 for add, 8 for update).

## Depends on

- `basket-api/02-add-item`

## Scope

- [ ] `DELETE /baskets/{basketId}/items/{productId}` removes that line and returns `200` with the remaining basket as `BasketResponse`
- [ ] Other lines are left untouched
- [ ] Line not in the basket (including an unknown basket, or `productId < 1`) returns `404` and writes nothing; `DELETE` never creates a key
- [ ] Removing the last line leaves no `basket:{basketId}` key in Redis and returns `200` with `items: []`
- [ ] The remove is a single atomic Redis command (`HDEL`)
- [ ] Unit tests for remove, with the in-memory basket store (no real Redis)

## Explicit non-goals

- No clearing the whole basket (`DELETE /baskets/{basketId}` is spec 05)
- No quantity changes, and no "decrement by one" behaviour
- No request body on `DELETE`
- No check against catalog stock
- No checkout endpoint
- No change to `AppHost.cs`, `GET /baskets/{basketId}`, `POST /baskets/{basketId}/items` or `PUT /baskets/{basketId}/items/{productId}`

## Data model

**Redis storage**: unchanged (hash `basket:{basketId}`, field `productId`, value `quantity`, no expiry). Removing runs one `HDEL basket:{basketId} {productId}` through `HashDeleteAsync`. It returns `true` when the field existed and was deleted, and `false` when the field or the key was missing (nothing is written). When the last field is deleted, Redis removes the key itself.

**Route**: `basketId` is a GUID (`{basketId:guid}`), `productId` is an int (`{productId:int}`). A non-matching route value returns `404`. No request body.

**Operation result**: `BasketOperations.RemoveItemAsync` returns `List<BasketItem>?`: `null` when the line was not found, otherwise the basket read back after the delete (sorted by `productId`, possibly empty).

**Responses**:

| Result | Status | Body |
|---|---|---|
| line removed | `200` | `BasketResponse`: `{ basketId, items: [{ productId, quantity }] }`, the remaining items sorted by `productId`; `items: []` after the last line is removed |
| line not found (`null`) | `404` | empty |

## Tasks

1. `IBasketStore.cs`: add `Task<bool> RemoveAsync(Guid basketId, int productId)`, returning `false` when the line does not exist. `RedisBasketStore.cs`: implement it as `HashDeleteAsync(Key(basketId), productId)` and return the result. (Scope 1, 3, 4, 5)
2. `BasketOperations.cs`: add static `RemoveItemAsync(IBasketStore store, Guid basketId, int productId)`, returning `Task<List<BasketItem>?>`. It calls `RemoveAsync` and returns `null` on `false`, or `store.GetAsync(basketId)` on `true`. (Scope 1, 2, 3)
3. `Program.cs`: map `DELETE /baskets/{basketId:guid}/items/{productId:int}`. A non-null result returns `Results.Ok(new BasketResponse(basketId, items))` and `null` returns `Results.NotFound()`. Keep the existing endpoints and `MapDefaultEndpoints()`. (Scope 1, 3)
4. `InMemoryBasketStore.cs`: implement `RemoveAsync`. It returns `false` and creates nothing when the basket or the line is missing. Otherwise it removes the line and, if the basket is now empty, removes the basket entry so `Contains(basketId)` is `false` (mirrors Redis). Then it returns `true`. (Scope 4, 6)
5. Add `RemoveItem` tests to `BasketOperationsTests`, at least 6 cases:
   - removing an existing line returns the remaining lines, and the other lines are unchanged;
   - a product not in an existing basket returns `null` and leaves the basket unchanged;
   - an unknown basket returns `null` and `Contains` stays `false`;
   - removing the last line returns an empty list and `Contains` becomes `false`;
   - removing the same line twice returns `null` the second time;
   - removing from one basket does not affect another.

   (Scope 1, 2, 3, 4, 6)
6. Build, run the tests, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Basket.Api/IBasketStore.cs` | modify (add `RemoveAsync`) |
| `src/EShopClaude.Basket.Api/RedisBasketStore.cs` | modify (implement `RemoveAsync`) |
| `src/EShopClaude.Basket.Api/BasketOperations.cs` | modify (add `RemoveItemAsync`) |
| `src/EShopClaude.Basket.Api/Program.cs` | modify (map the DELETE endpoint) |
| `src/EShopClaude.Basket.Api.Tests/InMemoryBasketStore.cs` | modify (implement `RemoveAsync`) |
| `src/EShopClaude.Basket.Api.Tests/BasketOperationsTests.cs` | modify (add remove tests) |

Not touched: `src/EShopClaude.AppHost/AppHost.cs`, `BasketContracts.cs`, `BasketItem.cs`, `EShopClaude.Basket.Api.csproj`, `EShopClaude.Basket.Api.Tests.csproj`, `EShopClaude.sln`, `EShopClaude.Tests`, `EShopClaude.Web`, `Catalog.Api`, `Ordering.Api` and their test projects. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise. `<basketapi-url>` is the `basketapi` endpoint link in the Aspire dashboard. `<guid>` is one freshly generated lowercase GUID (e.g. `[guid]::NewGuid()` in PowerShell), used for checks 5 to 15. Find `<redis-container>` and the Redis password as in `01-get-basket` DoD 7.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `dotnet test EShopClaude.Basket.Api.Tests/EShopClaude.Basket.Api.Tests.csproj` reports at least 22 tests passed (the 16 existing add and update tests plus at least 6 remove tests) and 0 failed.
3. `dotnet test EShopClaude.sln` reports 0 failed.
4. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
5. Setup: `curl -i -X POST <basketapi-url>/baskets/<guid>/items -H "Content-Type: application/json" -d '{"productId":1,"quantity":2}'`, then the same with `{"productId":3,"quantity":1}`. Both return `200`, the second with body `{"basketId":"<guid>","items":[{"productId":1,"quantity":2},{"productId":3,"quantity":1}]}`.
6. Remove a line: `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/1` returns `200` with body `{"basketId":"<guid>","items":[{"productId":3,"quantity":1}]}`.
7. `curl -i <basketapi-url>/baskets/<guid>` returns `200` with the same body as check 6.
8. Stored in Redis: `docker exec <redis-container> sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning HGETALL basket:<guid>'` lists exactly one field/value pair, `3` → `1`.
9. Already removed: `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/1` again returns `404`. `GET /baskets/<guid>` still returns the body from check 6.
10. Line never in basket: `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/2` returns `404`. `GET /baskets/<guid>` still returns the body from check 6.
11. Product id below 1: `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/0` returns `404`.
12. Route mismatches: `curl -i -X DELETE <basketapi-url>/baskets/not-a-guid/items/1` returns `404`, and `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/abc` returns `404`.
13. Last line: `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/3` returns `200` with body `{"basketId":"<guid>","items":[]}`. `redis-cli ... EXISTS basket:<guid>` returns `0`, and `GET /baskets/<guid>` returns `200` with `{"basketId":"<guid>","items":[]}`.
14. No basket created: with a second fresh GUID `<guid2>`, `curl -i -X DELETE <basketapi-url>/baskets/<guid2>/items/1` returns `404`, and `redis-cli ... EXISTS basket:<guid2>` returns `0`.
15. Non-goal check: `curl -i -X DELETE <basketapi-url>/baskets/<guid>` returns `404` or `405`.
16. Regression: `curl -i <basketapi-url>/` returns `200` with `Basket API is running.`, and `curl -i <basketapi-url>/health` returns `200` with `Healthy`. POST `{"productId":1,"quantity":2}` to `/baskets/<guid>/items` returns `200` with items `[{"productId":1,"quantity":2}]`. Then PUT `{"quantity":5}` to `/baskets/<guid>/items/1` returns `200` with items `[{"productId":1,"quantity":5}]`.
17. In the dashboard, the `basketapi` console logs and the structured logs filtered to Error show no entries after checks 5–16.
18. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md`: §3 Basket.Api (`BasketItem`, and `DELETE /baskets/{basketId}/items/{productId}`: remove one line), §4 Basket identity (GUID basket id), §6 `/cart` (removing a line), §8 Testing approach (Basket.Api unit tests with in-memory dependencies, no real Redis).
- `_agenticdev/PLAN.md`: Phase 2 Basket.Api (spec list) and ground rules (unit tests without real Redis).
- Precedent: `basket-api/03-update-item-quantity` (missing line returns `404` with nothing written, `{productId:int}` route, `BasketResponse` on success), `basket-api/02-add-item` (`List<BasketItem>?` result shape, `InMemoryBasketStore`), `basket-api/01-get-basket` (Redis data model).
