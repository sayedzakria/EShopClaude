# Basket.Api — Spec 01: Get basket

**Status:** Done (aa791d2)

## Goal

Connect `Basket.Api` to Redis and read a basket. Redis wiring and the read endpoint stay in one spec because the empty-basket response is the only visible proof that Redis is reachable.

## Context

- `AppHost.cs` already declares `var redis = builder.AddRedis("redis");` with no data volume, but nothing references it: `basketapi` is registered with only `.WithHttpHealthCheck("/health")`.
- `EShopClaude.Basket.Api.csproj` already references `Aspire.StackExchange.Redis` 13.6.4 (PLAN.md Step 1.2c), but no code uses it.
- `Basket.Api/Program.cs` calls `AddServiceDefaults()`, maps only `/` ("Basket API is running.") and `MapDefaultEndpoints()`. `GET /baskets/{basketId}` isn't mapped, so it returns 404.
- `Basket.Api` has no other source files and no test project.
- Precedent: `Ordering.Api` registers its store with the AppHost resource name as connection name (`AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`), and keeps request/response records in a `*Contracts.cs` file (`OrderContracts.cs`).
- Product ids are `int` in `Catalog.Api` (`Product.Id`), so `productId` in the basket is `int` too.
- `AddRedisClient` registers a Redis health check by default. `MapDefaultEndpoints()` exposes it on `/health` in Development, so `/health` turns `Unhealthy` when Redis is unreachable.

## Depends on

Nothing.

## Scope

- [ ] AppHost: `basketapi` gets `WithReference(redis).WaitFor(redis)`
- [ ] Redis client registered with `AddRedisClient`
- [ ] Basket store that reads a basket (`ProductId, Quantity` pairs only) from Redis
- [ ] `GET /baskets/{basketId}` returns the basket; an unknown id returns an empty basket

## Explicit non-goals

- No write endpoints
- No product name or price stored in the basket
- No call to another service
- No unit tests

## Data model

**Redis storage** (owned by `Basket.Api`, connection name `redis`):

| Item | Value |
|---|---|
| Key | `basket:{basketId}`, where `basketId` is the GUID in lowercase hyphenated form (`Guid.ToString()`), e.g. `basket:3f2504e0-4f89-11d3-9a0c-0305e82c3301` |
| Type | Redis hash |
| Hash field | `productId` as a decimal string (e.g. `"2"`) |
| Hash value | `quantity` as a decimal string (e.g. `"3"`) |
| Expiry | None |

A basket with no key is an empty basket. Reading never creates a key.

**BasketItem**: `ProductId` (int), `Quantity` (int).

**Response of `GET /baskets/{basketId}`** (JSON property names are camelCase, the ASP.NET Core default):

| Field | Type | Rule |
|---|---|---|
| `basketId` | GUID string | The id from the route |
| `items` | list of `{ productId (int), quantity (int) }` | Sorted by `productId` ascending. `[]` when the key doesn't exist |

## Tasks

1. AppHost: change the `basketapi` registration to `.WithReference(redis).WaitFor(redis)` before `.WithHttpHealthCheck("/health")`. (Scope 1)
2. `Program.cs`: call `builder.AddRedisClient("redis")` after `AddServiceDefaults()`. (Scope 2)
3. Add `BasketItem.cs` with the `BasketItem(int ProductId, int Quantity)` record, and `BasketContracts.cs` with `BasketResponse(Guid BasketId, List<BasketItem> Items)`. (Scope 3, 4)
4. Add `IBasketStore.cs` with `GetAsync(Guid basketId)`, returning the basket's items, and `RedisBasketStore.cs`, which implements it with `IConnectionMultiplexer`. It reads the key `basket:{basketId}` with `HGETALL`, parses each field and value as `int`, sorts by `productId` and returns an empty list when the key doesn't exist. Register it in DI as `IBasketStore` → `RedisBasketStore` (singleton, like the multiplexer). (Scope 3)
5. Map `GET /baskets/{basketId:guid}` in `Program.cs`: call `IBasketStore.GetAsync` and return 200 with a `BasketResponse`. A non-GUID id doesn't match the route, so it returns 404. Keep the `/` endpoint and `MapDefaultEndpoints()`. (Scope 4)
6. Build, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.AppHost/AppHost.cs` | modify (`basketapi` registration only) |
| `src/EShopClaude.Basket.Api/Program.cs` | modify |
| `src/EShopClaude.Basket.Api/BasketItem.cs` | new |
| `src/EShopClaude.Basket.Api/BasketContracts.cs` | new |
| `src/EShopClaude.Basket.Api/IBasketStore.cs` | new |
| `src/EShopClaude.Basket.Api/RedisBasketStore.cs` | new |

Not touched: `EShopClaude.Basket.Api.csproj` (the Redis package is already there), `EShopClaude.sln` (no new project), the `redis` resource declaration in `AppHost.cs`, `EShopClaude.Tests`, `EShopClaude.Web`, `Catalog.Api`, `Ordering.Api`. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise. `<basketapi-url>` is the `basketapi` endpoint link in the Aspire dashboard. `<guid>` is any freshly generated GUID in lowercase, e.g. from `[guid]::NewGuid()` in PowerShell.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy. `basketapi` starts only after `redis` is running (it waits for it).
3. In the dashboard, the `basketapi` resource details show a `ConnectionStrings__redis` environment variable.
4. In the dashboard, `basketapi` console logs and structured logs filtered to Error show no entries, and in particular no `RedisConnectionException`.
5. `curl -i <basketapi-url>/health` returns `200` with body `Healthy` (this includes the Redis health check).
6. `curl -i <basketapi-url>/baskets/<guid>` returns `200` with body `{"basketId":"<guid>","items":[]}`.
7. Reading doesn't create a key. Run `docker exec <redis-container> sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning EXISTS basket:<guid>'` and it returns `0`. Get `<redis-container>` from `docker ps --filter name=redis`. If `$REDIS_PASSWORD` isn't set in the container, use the password shown in the `redis` resource details in the dashboard.
8. Real data is read. Run `docker exec <redis-container> sh -c 'redis-cli -a "$REDIS_PASSWORD" --no-auth-warning HSET basket:<guid> 5 1 2 3'`, then `curl -i <basketapi-url>/baskets/<guid>`, which returns `200` with body `{"basketId":"<guid>","items":[{"productId":2,"quantity":3},{"productId":5,"quantity":1}]}` (sorted by `productId`).
9. `curl -i <basketapi-url>/baskets/not-a-guid` returns `404`.
10. Non-goal check: `curl -i -X POST <basketapi-url>/baskets/<guid>/items -H "Content-Type: application/json" -d '{"productId":1,"quantity":1}'` returns `404` or `405`.
11. Regression: `curl -i <basketapi-url>/` returns `200` with `Basket API is running.`
12. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §2 Architecture (Basket.Api stores baskets in Redis), §3 Basket.Api (`BasketItem`, `GET /baskets/{basketId}`), §4 Basket identity (GUID basket id), §9 Implementation notes (Redis registration in AppHost).
- `_agenticdev/PLAN.md` — Step 1.2 (Redis resource and `Aspire.StackExchange.Redis` package), Phase 2 Basket.Api (spec list).
- Precedent: `ordering-api/01-order-schema` (connection name = AppHost resource name), `ordering-api/02-create-order` (`*Contracts.cs` records).
