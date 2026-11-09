# Catalog.Api — Spec 05: Decrement stock

**Status:** Done

## Goal

Let `Basket.Api` reserve stock during checkout. The endpoint and its unit tests are one spec because the tests are the proof of the decrement logic.

## Context

- Specs 01 to 04 are merged: `Catalog.Api` has the `Product` entity, `CatalogDbContext` (table `Product`, `DbSet<Product> Products`) and `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`.
- `Program.cs` seeds 8 fixed products (ids 1 to 8) when the table is empty, and maps `/`, `GET /products`, `GET /products/{id}` and the default endpoints.
- `POST /products/{id}/decrement-stock` is not mapped, so it currently returns 404 (or 405) for every id.
- `EShopClaude.Tests` has no test files and references only `EShopClaude.Web`. There is no test project for `Catalog.Api`.

## Depends on

- `catalog-api/02-seed-data`
- `catalog-api/04-get-product` (used by the Definition of Done to read stock back)

## Scope

- [x] `POST /products/{id}/decrement-stock` with body `{ quantity }`
- [x] 200 and lower `Stock` when stock is sufficient
- [x] 409 when stock is insufficient, stock unchanged
- [x] Unknown id returns 404
- [x] `quantity` below 1 returns 400, stock unchanged (a negative quantity would otherwise add stock)
- [x] The decrement is one atomic conditional update, so concurrent requests cannot take stock below zero
- [x] Create the `Catalog.Api` test project (`EShopClaude.Catalog.Api.Tests`); unit tests for success and insufficient stock, with an in-memory store (no real Postgres)

## Explicit non-goals

- No stock increment or restore
- No reservation or compensation logic
- No call to or from another service
- No change to the `Product` schema, `GET` endpoints or seeding
- No change to `AppHost.cs` or to `EShopClaude.Tests`
- No custom error body

## Data model

n/a. No schema change; the endpoint updates `Stock` on the existing `Product` table.

## Tasks

1. Add `ProductStock.cs` to `Catalog.Api` with a `DecrementResult` enum (`Ok`, `NotFound`, `InsufficientStock`) and a static `DecrementAsync(db, id, quantity)`. It runs one conditional update (`Stock = Stock - quantity` where the id matches and `Stock >= quantity`). One row changed means `Ok`; otherwise it checks whether the id exists to return `InsufficientStock` or `NotFound`.
2. Map `POST /products/{id:int}/decrement-stock` in `Program.cs` with body `{ quantity }`. `quantity < 1` returns 400; otherwise map the result to 200, 409 or 404.
3. Create `src/EShopClaude.Catalog.Api.Tests` (xUnit, net10.0, SQLite in-memory via `Microsoft.EntityFrameworkCore.Sqlite`, project reference to `Catalog.Api`) and add it to `EShopClaude.sln`.
4. Write unit tests: success lowers stock by the quantity; quantity equal to stock succeeds and leaves 0; insufficient stock returns `InsufficientStock` with stock unchanged; unknown id returns `NotFound`; a second decrement sees the lowered stock.
5. Build, run the tests, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Catalog.Api/ProductStock.cs` | new |
| `src/EShopClaude.Catalog.Api/Program.cs` | modify |
| `src/EShopClaude.Catalog.Api.Tests/EShopClaude.Catalog.Api.Tests.csproj` | new |
| `src/EShopClaude.Catalog.Api.Tests/ProductStockTests.cs` | new |
| `src/EShopClaude.sln` | modify |

Not touched: `AppHost.cs`, the `Catalog.Api` `.csproj`, `Product.cs`, `CatalogDbContext.cs`, `CatalogSeeder.cs`, `EShopClaude.Tests`, the other services. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `dotnet test EShopClaude.sln` exits 0, and `dotnet test EShopClaude.Catalog.Api.Tests/EShopClaude.Catalog.Api.Tests.csproj` reports at least 5 passing tests and 0 failed.
3. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
4. Dashboard `catalogapi` console logs and structured logs filtered to Error: no entries.
5. `curl -s <catalogapi-url>/products/1` shows stock S. `curl -i -X POST <catalogapi-url>/products/1/decrement-stock -H "Content-Type: application/json" -d '{"quantity":2}'` returns `200`, and `curl -s <catalogapi-url>/products/1` then shows stock S-2.
6. `curl -i -X POST <catalogapi-url>/products/1/decrement-stock -H "Content-Type: application/json" -d '{"quantity":99999}'` returns `409`, and `GET /products/1` still shows S-2.
7. `curl -i -X POST <catalogapi-url>/products/9999/decrement-stock -H "Content-Type: application/json" -d '{"quantity":1}'` returns `404`.
8. `curl -i -X POST <catalogapi-url>/products/1/decrement-stock -H "Content-Type: application/json" -d '{"quantity":0}'` and the same with `-1` return `400`, and stock is unchanged.
9. The stock for id 1 in pgAdmin on `catalogdb` matches the API: `SELECT "Id", "Stock" FROM "Product" WHERE "Id" = 1;`.
10. Regression: `curl -i <catalogapi-url>/` returns 200 with `Catalog API is running.`, `/health` returns 200, `GET /products` returns 200 with an array of exactly 8 objects.
11. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §3 Catalog.Api API surface (`POST /products/{id}/decrement-stock`), §5 step 2 (checkout reserves stock, no rollback), §8 Testing approach (Catalog.Api decrement logic).
- `_agenticdev/PLAN.md` — Phase 2, Catalog.Api (spec list).
