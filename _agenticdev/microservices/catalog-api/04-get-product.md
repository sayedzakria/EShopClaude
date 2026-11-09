# Catalog.Api — Spec 04: Get product

**Status:** In progress

## Goal

Expose a single product by id, which the cart page and the checkout flow use to read the current name and price.

## Context

- Specs 01 to 03 are merged: `Catalog.Api` has the `Product` entity, `CatalogDbContext` (table `Product`, `DbSet<Product> Products`) and `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`.
- `Program.cs` seeds 8 fixed products (ids 1 to 8) on every start and maps `/`, `GET /products` and the default endpoints.
- `GET /products/{id}` currently returns 404 for every id because it is not mapped.
- `EShopClaude.Tests` has no test files.

## Depends on

- `catalog-api/02-seed-data`
- `catalog-api/03-list-products`

## Scope

- [ ] `GET /products/{id}` returns the product (`Id, Name, Description, Price, Stock`)
- [ ] Unknown id returns 404

## Explicit non-goals

- No list endpoint changes
- No stock changes
- No caching
- No new tests (PROJECT.md only requires Catalog unit tests for stock decrement)
- No new packages and no change to `AppHost.cs`
- No custom error body

## Data model

n/a. No schema change; the endpoint reads the existing `Product` table (`Id, Name, Description, Price, Stock`).

## Tasks

1. Map `GET /products/{id}` in `Program.cs`, with `id` constrained to an integer. It reads the row with that id from `CatalogDbContext` and returns it as JSON with 200; if no row exists it returns 404. A non-integer id does not match the route and also returns 404.
2. Build, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Catalog.Api/Program.cs` | modify |

Not touched: `AppHost.cs`, the `.csproj`, `Product.cs`, `CatalogDbContext.cs`, `CatalogSeeder.cs`, `EShopClaude.Tests`, the other services. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
3. Dashboard `catalogapi` console logs and structured logs filtered to Error: no entries.
4. `curl -i <catalogapi-url>/products/1` returns `200`, `Content-Type: application/json`, and one object with exactly the fields `id`, `name`, `description`, `price`, `stock`. Repeat for `/products/8`.
5. The values returned for ids 1 and 8 match pgAdmin on `catalogdb`: `SELECT "Id", "Name", "Description", "Price", "Stock" FROM "Product" WHERE "Id" IN (1, 8);`.
6. `curl -i <catalogapi-url>/products/9999` and `curl -i <catalogapi-url>/products/0` return `404`.
7. `curl -i <catalogapi-url>/products/abc` returns `404`.
8. Regression: `curl -i <catalogapi-url>/` returns 200 with `Catalog API is running.`, `curl -i <catalogapi-url>/health` returns 200, and `curl -i <catalogapi-url>/products` returns 200 with an array of exactly 8 objects.
9. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §3 Catalog.Api API surface (`GET /products/{id}`), §6 cart page enrichment via `GET /products/{id}`.
- `_agenticdev/PLAN.md` — Phase 2, Catalog.Api (spec list).
