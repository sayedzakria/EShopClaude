# Catalog.Api — Spec 03: List products

**Status:** In progress

## Goal

Expose the full product list so the storefront can render the products page.

## Context

- Specs 01 and 02 are merged: `Catalog.Api` has the `Product` entity, `CatalogDbContext` (table `Product`, `DbSet<Product> Products`) and `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`.
- `Program.cs` calls `Database.EnsureCreated()` and `CatalogSeeder.Seed` in a service scope after `Build()`, so `Product` holds 8 fixed rows (ids 1 to 8) after every start.
- `Program.cs` maps only `/` and the default endpoints. `GET /products` currently returns 404.
- `EShopClaude.Tests` has no test files.

## Depends on

- `catalog-api/02-seed-data`

## Scope

- [ ] `GET /products` returns every product (`Id, Name, Description, Price, Stock`)

## Explicit non-goals

- No paging, filtering, sorting or API versioning
- No `GET /products/{id}`
- No change to stock
- No new tests (PROJECT.md only requires Catalog unit tests for stock decrement)
- No new packages and no change to `AppHost.cs`

## Data model

n/a. No schema change; the endpoint reads the existing `Product` table (`Id, Name, Description, Price, Stock`).

## Tasks

1. Map `GET /products` in `Program.cs`. It reads all rows from `CatalogDbContext` and returns them as JSON, in `Id` order so the output is deterministic. It takes no query parameters.
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
4. `curl -i <catalogapi-url>/products` returns `200`, `Content-Type: application/json`, and an array of exactly 8 objects.
5. Each object has exactly the fields `id`, `name`, `description`, `price`, `stock`. Ids are 1 to 8; names and descriptions are non-empty; `price` and `stock` are above 0. The values match pgAdmin on `catalogdb`: `SELECT "Id", "Name", "Price", "Stock" FROM "Product" ORDER BY "Id";`.
6. Regression: `curl -i <catalogapi-url>/` returns 200 with `Catalog API is running.`, and `curl -i <catalogapi-url>/health` returns 200.
7. Non-goal check: `curl -i <catalogapi-url>/products/1` returns 404.
8. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §3 Catalog.Api API surface (`GET /products`), §7 out of scope (no paging, filtering or versioning).
- `_agenticdev/PLAN.md` — Phase 2, Catalog.Api (spec list).
