# Basket.Api — Spec 03: Update item quantity

**Status:** Not started

## Goal

Let a shopper set the exact quantity of one basket line.

## Context

- `basket-api/01-get-basket` and `basket-api/02-add-item` are merged. `basketapi` already references and waits for `redis` in `AppHost.cs`, and `Program.cs` calls `AddRedisClient("redis")`.
- A basket is stored as a Redis hash `basket:{basketId}` (field = `productId`, value = `quantity`, both decimal strings, no expiry).
- `IBasketStore` has `GetAsync(Guid basketId)` (items sorted by `productId`) and `AddAsync(Guid basketId, int productId, int quantity)` (`HINCRBY`). `RedisBasketStore` implements both with `IConnectionMultiplexer`, registered as a singleton.
- `BasketContracts.cs` holds `BasketResponse(Guid BasketId, List<BasketItem> Items)` and `AddItemRequest(int ProductId, int Quantity)`. `BasketItem(int ProductId, int Quantity)` is in `BasketItem.cs`.
- `BasketOperations.cs` has static `AddItemAsync(IBasketStore, Guid, AddItemRequest)`, which returns `null` (nothing written) for invalid input; the endpoint maps `null` to `400`.
- `Program.cs` maps `/`, `GET /baskets/{basketId:guid}`, `POST /baskets/{basketId:guid}/items` and `MapDefaultEndpoints()`. No `PUT` is mapped.
- `src/EShopClaude.Basket.Api.Tests` exists (xunit, in the solution) with `InMemoryBasketStore` (dictionary-backed, has a `Contains(Guid)` helper) and `BasketOperationsTests` (8 test cases for add).

## Depends on

- `basket-api/02-add-item`

## Scope

- [ ] `PUT /baskets/{basketId}/items/{productId}` with body `{ quantity }` sets the quantity of that line to exactly `quantity`
- [ ] `quantity < 1` returns `400` and writes nothing; this check runs before the line-exists check
- [ ] Line not in the basket (including an unknown basket, or `productId < 1`) returns `404` and writes nothing; `PUT` never creates a line or a basket
- [ ] The "set only if the line exists" write is atomic in Redis
- [ ] Unit tests for update, with the in-memory basket store (no real Redis)

## Explicit non-goals

- No creation of a line through `PUT`
- No check against catalog stock
- No removal through `quantity = 0`: removal is its own endpoint
- No upper limit on quantity
- No remove, clear or checkout endpoints
- No change to `AppHost.cs`, `GET /baskets/{basketId}` or `POST /baskets/{basketId}/items`

## Data model

**Redis storage**: unchanged (hash `basket:{basketId}`, field `productId`, value `quantity`, no expiry). Updating runs a StackExchange.Redis transaction (`MULTI`/`EXEC`) with the condition `Condition.HashExists(basket:{basketId}, productId)` and a single `HSET basket:{basketId} {productId} {quantity}`. If the field is missing, the transaction does not execute and nothing is written (no key or field is created).

**Request body of `PUT /baskets/{basketId}/items/{productId}`** — `UpdateItemQuantityRequest`:

| Field | Type | Rule |
|---|---|---|
| `quantity` | int | Must be ≥ 1, otherwise `400`. Replaces the line's quantity (not added to it) |

A missing or malformed JSON body returns `400` (ASP.NET Core default binding). Route: `basketId` is a GUID (`{basketId:guid}`), `productId` is an int (`{productId:int}`); a non-matching route value returns `404`.

**Operation result** — `UpdateItemResult(UpdateItemOutcome Outcome, List<BasketItem>? Items)`, with enum `UpdateItemOutcome { Updated, Invalid, NotFound }`. `Items` is set only for `Updated`.

**Responses**:

| Outcome | Status | Body |
|---|---|---|
| `Updated` | `200` | `BasketResponse`: `{ basketId, items: [{ productId, quantity }] }`, items sorted by `productId`, read back after the write |
| `Invalid` (`quantity < 1`) | `400` | empty |
| `NotFound` (line missing) | `404` | empty |

## Tasks

1. `BasketContracts.cs`: add `UpdateItemQuantityRequest(int Quantity)`. (Scope 1)
2. `IBasketStore.cs`: add `Task<bool> SetQuantityAsync(Guid basketId, int productId, int quantity)`, returning `false` when the line does not exist. `RedisBasketStore.cs`: implement it with `CreateTransaction()`, `AddCondition(Condition.HashExists(key, productId))`, `HashSetAsync(key, productId, quantity)` and `ExecuteAsync()`; return the `ExecuteAsync` result. (Scope 1, 3, 4)
3. `BasketOperations.cs`: add `UpdateItemOutcome` and `UpdateItemResult`, and static `UpdateItemQuantityAsync(IBasketStore store, Guid basketId, int productId, UpdateItemQuantityRequest request)`. It returns `Invalid` without touching the store when `Quantity < 1`; otherwise it calls `SetQuantityAsync`, returning `NotFound` on `false` and `Updated` with `store.GetAsync(basketId)` on `true`. (Scope 1, 2, 3)
4. `Program.cs`: map `PUT /baskets/{basketId:guid}/items/{productId:int}`. Switch on the outcome: `Updated` → `Results.Ok(new BasketResponse(basketId, items))`, `Invalid` → `Results.BadRequest()`, `NotFound` → `Results.NotFound()`. Keep the existing endpoints and `MapDefaultEndpoints()`. (Scope 1, 2, 3)
5. `InMemoryBasketStore.cs`: implement `SetQuantityAsync`: set the value and return `true` only when the basket and the line exist; otherwise return `false` and create nothing. (Scope 5)
6. Add `UpdateItemQuantity` tests to `BasketOperationsTests`: an existing line is set to the exact new quantity (not summed); other lines in the basket are unchanged; a product not in an existing basket returns `NotFound` and the basket is unchanged; an unknown basket returns `NotFound` and `Contains` stays `false`; `quantity` 0 and negative return `Invalid` and leave the line unchanged; an invalid quantity for a missing line returns `Invalid` (validation runs first); updating one basket does not affect another. (Scope 1, 2, 3, 5)
7. Build, run the tests, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Basket.Api/BasketContracts.cs` | modify (add `UpdateItemQuantityRequest`) |
| `src/EShopClaude.Basket.Api/IBasketStore.cs` | modify (add `SetQuantityAsync`) |
| `src/EShopClaude.Basket.Api/RedisBasketStore.cs` | modify (implement `SetQuantityAsync`) |
| `src/EShopClaude.Basket.Api/BasketOperations.cs` | modify (add `UpdateItemOutcome`, `UpdateItemResult`, `UpdateItemQuantityAsync`) |
| `src/EShopClaude.Basket.Api/Program.cs` | modify (map the PUT endpoint) |
| `src/EShopClaude.Basket.Api.Tests/InMemoryBasketStore.cs` | modify (implement `SetQuantityAsync`) |
| `src/EShopClaude.Basket.Api.Tests/BasketOperationsTests.cs` | modify (add update tests) |

Not touched: `src/EShopClaude.AppHost/AppHost.cs`, `EShopClaude.Basket.Api.csproj`, `EShopClaude.Basket.Api.Tests.csproj`, `EShopClaude.sln`, `BasketItem.cs`, `EShopClaude.Tests`, `EShopClaude.Web`, `Catalog.Api`, `Ordering.Api` and their test projects. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise. `<basketapi-url>` is the `basketapi` endpoint link in the Aspire dashboard. `<guid>` is one freshly generated lowercase GUID (e.g. `[guid]::NewGuid()` in PowerShell), used for checks 5 to 17. `<redis-container>` and the Redis password are found as in `01-get-basket` DoD 7.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `dotnet test EShopClaude.Basket.Api.Tests/EShopClaude.Basket.Api.Tests.csproj` reports at least 16 tests passed (the 8 existing add tests plus at least 8 update tests) and 0 failed.
3. `dotnet test EShopClaude.sln` reports 0 failed.
4. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
5. Setup: `curl -i -X POST <basketapi-url>/baskets/<guid>/items -H "Content-Type: application/json" -d '{"productId":1,"quantity":2}'`, then the same with `{"productId":3,"quantity":1}`; both return `200`, the second with body `{"basketId":"<guid>","items":[{"productId":1,"quantity":2},{"productId":3,"quantity":1}]}`.
6. Set exact quantity: `curl -i -X PUT <basketapi-url>/baskets/<guid>/items/1 -H "Content-Type: application/json" -d '{"quantity":7}'` returns `200` with body `{"basketId":"<guid>","items":[{"productId":1,"quantity":7},{"productId":3,"quantity":1}]}` (7, not 9).
7. `curl -i <basketapi-url>/baskets/<guid>` returns `200` with the same body as check 6.
8. Stored in Redis: `docker exec <redis-container> sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning HGETALL basket:<guid>'` lists exactly two field/value pairs, `1` → `7` and `3` → `1` (in any order).
9. Line not in basket: PUT `{"quantity":4}` to `/baskets/<guid>/items/2` returns `404`; then `GET /baskets/<guid>` still returns the body from check 6, and `redis-cli ... HEXISTS basket:<guid> 2` returns `0`.
10. Invalid quantity: PUT `{"quantity":0}` to `/baskets/<guid>/items/1` returns `400`; PUT `{"quantity":-1}` returns `400`; then `GET /baskets/<guid>` still returns the body from check 6.
11. Validation runs first: PUT `{"quantity":0}` to `/baskets/<guid>/items/2` (line missing) returns `400`, not `404`.
12. Bad body: PUT to `/baskets/<guid>/items/1` with `-d 'not json'` returns `400`.
13. Product id below 1: PUT `{"quantity":1}` to `/baskets/<guid>/items/0` returns `404`.
14. Route mismatches: PUT `{"quantity":1}` to `/baskets/not-a-guid/items/1` returns `404`; PUT to `/baskets/<guid>/items/abc` returns `404`.
15. No basket created: with a second fresh GUID `<guid2>`, PUT `{"quantity":3}` to `/baskets/<guid2>/items/1` returns `404`, and `redis-cli ... EXISTS basket:<guid2>` returns `0`.
16. Non-goal checks: `curl -i -X DELETE <basketapi-url>/baskets/<guid>/items/1` and `curl -i -X DELETE <basketapi-url>/baskets/<guid>` each return `404` or `405`.
17. Regression: `curl -i <basketapi-url>/` returns `200` with `Basket API is running.`, and `curl -i <basketapi-url>/health` returns `200` with `Healthy`; POST `{"productId":1,"quantity":1}` to `/baskets/<guid>/items` returns `200` with line `1` at quantity `8`.
18. In the dashboard, `basketapi` console logs and structured logs filtered to Error show no entries after checks 5–17.
19. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §3 Basket.Api (`BasketItem`, `PUT /baskets/{basketId}/items/{productId}`: set exact quantity), §4 Basket identity (GUID basket id), §8 Testing approach (Basket.Api unit tests with in-memory dependencies, no real Redis).
- `_agenticdev/PLAN.md` — Phase 2 Basket.Api (spec list), ground rules (unit tests without real Redis).
- Precedent: `basket-api/02-add-item` (`BasketOperations`, `quantity < 1` → `400` with nothing written, `BasketResponse` on success, `InMemoryBasketStore`), `basket-api/01-get-basket` (Redis data model).

