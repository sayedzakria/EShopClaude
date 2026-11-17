# Basket.Api — Spec 02: Add item

**Status:** Not started

## Goal

Let a shopper put a product into a basket, incrementing the quantity when the product is already there.

## Context

- `basket-api/01-get-basket` is merged. `basketapi` already references and waits for `redis` in `AppHost.cs`, and `Program.cs` calls `AddRedisClient("redis")`.
- A basket is stored as a Redis hash `basket:{basketId}` (field = `productId`, value = `quantity`, both decimal strings, no expiry). See the Data model in `01-get-basket`.
- `IBasketStore` has only `GetAsync(Guid basketId)`, which returns the items sorted by `productId`. `RedisBasketStore` implements it with `IConnectionMultiplexer`, registered as a singleton.
- `BasketItem(int ProductId, int Quantity)` is in `BasketItem.cs`. `BasketResponse(Guid BasketId, List<BasketItem> Items)` is in `BasketContracts.cs`.
- `Program.cs` maps `/`, `GET /baskets/{basketId:guid}` and `MapDefaultEndpoints()`. `POST /baskets/{basketId}/items` isn't mapped.
- There is no `Basket.Api` test project. `EShopClaude.Catalog.Api.Tests` and `EShopClaude.Ordering.Api.Tests` are the per-service precedent: xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4, `<Using Include="Xunit" />`, a project reference to the service, and listed in `EShopClaude.sln`.
- Precedent for testable logic: a static class that takes its dependency as a parameter and returns `null` for invalid input (`OrderCreation.CreateAsync` in `Ordering.Api`), with the endpoint mapping `null` to `Results.BadRequest()`. `Catalog.Api`'s decrement-stock returns a plain `400` when `quantity < 1`.

## Depends on

- `basket-api/01-get-basket`

## Scope

- [ ] `POST /baskets/{basketId}/items` with body `{ productId, quantity }`
- [ ] Adds a new line, or increments the quantity of an existing line
- [ ] Rejects `productId < 1` or `quantity < 1` with `400` and writes nothing
- [ ] Create the `Basket.Api` test project; unit tests for add and increment, with an in-memory basket store (no real Redis)

## Explicit non-goals

- No check that the product exists or has stock (no call to `Catalog.Api`)
- No update, remove or clear
- No checkout
- No product name or price stored in the basket
- No upper limit on quantity
- No change to `AppHost.cs` or to `GET /baskets/{basketId}`

## Data model

**Redis storage**: unchanged from `01-get-basket` (hash `basket:{basketId}`, field `productId`, value `quantity`, no expiry). Adding uses `HINCRBY basket:{basketId} {productId} {quantity}`, which creates the key and the field when missing and increments atomically otherwise.

**Request body of `POST /baskets/{basketId}/items`** — `AddItemRequest`:

| Field | Type | Rule |
|---|---|---|
| `productId` | int | Must be ≥ 1, otherwise `400` |
| `quantity` | int | Must be ≥ 1, otherwise `400`. Added to the existing quantity of that product's line |

A missing or malformed JSON body returns `400` (ASP.NET Core default binding).

**Response on success**: `200` with the same `BasketResponse` shape as `GET /baskets/{basketId}`: `{ basketId, items: [{ productId, quantity }] }`, items sorted by `productId` ascending, read back from the store after the add. On `400`, the body is empty and nothing is written.

## Tasks

1. `BasketContracts.cs`: add `AddItemRequest(int ProductId, int Quantity)`. (Scope 1)
2. `IBasketStore.cs`: add `AddAsync(Guid basketId, int productId, int quantity)`. `RedisBasketStore.cs`: implement it with `HashIncrementAsync` (`HINCRBY`) on the existing `basket:{basketId}` key. (Scope 2)
3. Add `BasketOperations.cs` with static `AddItemAsync(IBasketStore store, Guid basketId, AddItemRequest request)`. It returns `null` without touching the store when `ProductId < 1` or `Quantity < 1`. Otherwise it calls `store.AddAsync`, then returns `store.GetAsync(basketId)`. (Scope 2, 3)
4. `Program.cs`: map `POST /baskets/{basketId:guid}/items`. Call `BasketOperations.AddItemAsync`; a `null` result returns `Results.BadRequest()`, otherwise `Results.Ok(new BasketResponse(basketId, items))`. Keep the existing endpoints and `MapDefaultEndpoints()`. (Scope 1, 3)
5. Create `src/EShopClaude.Basket.Api.Tests` (`net10.0`, `IsPackable` false) with the same xunit, runner, Test SDK and coverlet versions as `EShopClaude.Ordering.Api.Tests` (no Sqlite), `<Using Include="Xunit" />`, and a project reference to `EShopClaude.Basket.Api`. Add it to `EShopClaude.sln`. (Scope 4)
6. In the test project, add `InMemoryBasketStore` implementing `IBasketStore` with a dictionary (same sorting rule as Redis: items sorted by `productId`). (Scope 4)
7. Add `BasketOperationsTests` covering: add to an empty basket gives one line; two different products give two lines sorted by `productId`; adding the same product twice sums the quantities; `quantity` 0 and negative return `null` and store nothing; `productId` 0 returns `null` and stores nothing; adds to one basket don't affect another basket. (Scope 2, 3, 4)
8. Build, run the tests, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Basket.Api/BasketContracts.cs` | modify (add `AddItemRequest`) |
| `src/EShopClaude.Basket.Api/IBasketStore.cs` | modify (add `AddAsync`) |
| `src/EShopClaude.Basket.Api/RedisBasketStore.cs` | modify (implement `AddAsync`) |
| `src/EShopClaude.Basket.Api/BasketOperations.cs` | new |
| `src/EShopClaude.Basket.Api/Program.cs` | modify (map the POST endpoint) |
| `src/EShopClaude.Basket.Api.Tests/EShopClaude.Basket.Api.Tests.csproj` | new |
| `src/EShopClaude.Basket.Api.Tests/InMemoryBasketStore.cs` | new |
| `src/EShopClaude.Basket.Api.Tests/BasketOperationsTests.cs` | new |
| `src/EShopClaude.sln` | modify (add the test project only) |

Not touched: `src/EShopClaude.AppHost/AppHost.cs` (the `redis` reference already exists), `EShopClaude.Basket.Api.csproj`, `BasketItem.cs`, `EShopClaude.Tests`, `EShopClaude.Web`, `Catalog.Api`, `Ordering.Api` and their test projects. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise. `<basketapi-url>` is the `basketapi` endpoint link in the Aspire dashboard. `<guid>` is one freshly generated lowercase GUID (e.g. `[guid]::NewGuid()` in PowerShell), used for every check from 5 to 12. `<redis-container>` and the Redis password are found as in `01-get-basket` DoD 7.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `dotnet test EShopClaude.Basket.Api.Tests/EShopClaude.Basket.Api.Tests.csproj` reports at least 6 tests passed and 0 failed.
3. `dotnet test EShopClaude.sln` reports 0 failed (the Catalog and Ordering tests still pass).
4. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
5. New line: `curl -i -X POST <basketapi-url>/baskets/<guid>/items -H "Content-Type: application/json" -d '{"productId":1,"quantity":2}'` returns `200` with body `{"basketId":"<guid>","items":[{"productId":1,"quantity":2}]}`.
6. Second product: the same request with `{"productId":3,"quantity":1}` returns `200` with body `{"basketId":"<guid>","items":[{"productId":1,"quantity":2},{"productId":3,"quantity":1}]}`.
7. Increment: the same request with `{"productId":1,"quantity":3}` returns `200` with body `{"basketId":"<guid>","items":[{"productId":1,"quantity":5},{"productId":3,"quantity":1}]}`.
8. `curl -i <basketapi-url>/baskets/<guid>` returns `200` with the same body as check 7.
9. Stored in Redis: `docker exec <redis-container> sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning HGETALL basket:<guid>'` lists exactly two field/value pairs, `1` → `5` and `3` → `1` (in any order).
10. Invalid quantity: POST with `{"productId":1,"quantity":0}` returns `400`; then `GET /baskets/<guid>` still returns the body from check 7.
11. Invalid product id: POST with `{"productId":0,"quantity":1}` returns `400`; then `GET /baskets/<guid>` still returns the body from check 7.
12. Bad body: POST with `-d 'not json'` returns `400`.
13. Nothing written on rejection: with a second fresh GUID `<guid2>`, POST `{"productId":1,"quantity":-1}` to `/baskets/<guid2>/items` returns `400`, and `redis-cli ... EXISTS basket:<guid2>` returns `0`.
14. `curl -i -X POST <basketapi-url>/baskets/not-a-guid/items -H "Content-Type: application/json" -d '{"productId":1,"quantity":1}'` returns `404`.
15. Non-goal checks: `curl -i -X PUT <basketapi-url>/baskets/<guid>/items/1 -H "Content-Type: application/json" -d '{"quantity":1}'` and `curl -i -X DELETE <basketapi-url>/baskets/<guid>` each return `404` or `405`.
16. Regression: `curl -i <basketapi-url>/` returns `200` with `Basket API is running.`, and `curl -i <basketapi-url>/health` returns `200` with `Healthy`.
17. In the dashboard, `basketapi` console logs and structured logs filtered to Error show no entries after checks 5–15.
18. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §3 Basket.Api (`BasketItem`, `POST /baskets/{basketId}/items`: add or increment), §4 Basket identity (GUID basket id), §8 Testing approach (Basket.Api unit tests with in-memory dependencies, no real Redis).
- `_agenticdev/PLAN.md` — Phase 2 Basket.Api (spec list), ground rules (unit tests without real Redis).
- Precedent: `basket-api/01-get-basket` (Redis data model, `IBasketStore`), `ordering-api/02-create-order` (static logic class returning `null` → `400`, per-service test project), `catalog-api/05-decrement-stock` (`quantity < 1` → `400`).

