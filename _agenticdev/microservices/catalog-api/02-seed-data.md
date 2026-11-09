# Catalog.Api — Spec 02: Seed data

**Status:** In progress

## Goal

Give the catalog demo products to show, so every later endpoint, the cart and checkout have something to work with.

## Context

- Spec 01 is merged: `Catalog.Api` has the `Product` entity, `CatalogDbContext` (table `Product`) and `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`.
- `Program.cs` calls `Database.EnsureCreated()` inside a service scope after `Build()`. There is no seed step yet, so `Product` is empty after every start.
- Postgres has no data volume, so `catalogdb` is recreated empty on every AppHost start. Restarting only `catalogapi` keeps the database and its rows.

## Depends on

- `catalog-api/01-product-schema`

## Scope

- [ ] Startup seed inserts a small fixed set of demo products (name, description, price, stock)
- [ ] Seed runs only when the `Product` table is empty

## Explicit non-goals

- No product endpoints
- No admin or import feature
- No random or generated data: the set is fixed

## Data model

No schema change. The seed fills the existing `Product` table (`Id, Name, Description, Price, Stock`) with 8 fixed rows:

- `Id` is left to the database identity, so the rows get ids 1 to 8 in insertion order.
- `Name` and `Description` are hand-written and within the column limits (200 and 1000).
- `Price` is a positive value with at most 2 decimals; `Stock` is greater than 0.

## Tasks

1. Add a seeder that inserts the fixed set of 8 products with one `SaveChanges`, and does nothing if `Product` already has any row.
2. In `Program.cs`, call the seeder inside the existing scope, right after `EnsureCreated`.
3. Build, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Catalog.Api/CatalogSeeder.cs` | new |
| `src/EShopClaude.Catalog.Api/Program.cs` | modify |

Not touched: `AppHost.cs`, the `.csproj`, `Product.cs`, `CatalogDbContext.cs`, `EShopClaude.Tests`, the other services. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
3. Dashboard `catalogapi` console logs and structured logs filtered to Error: no entries.
4. pgAdmin Query Tool on `catalogdb`: `SELECT count(*) FROM "Product";` returns `8`.
5. Same Query Tool: `SELECT "Id", "Name", "Price", "Stock" FROM "Product" ORDER BY "Id";` returns ids 1 to 8, each with a non-empty name, a price above 0 and a stock above 0.
6. Idempotent restart: restart only `catalogapi` from the dashboard. It returns to Healthy, and `SELECT count(*) FROM "Product";` still returns `8` with the same ids.
7. Non-goal check: `GET <catalogapi-url>/products` returns 404.
8. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — Catalog.Api section (product data in `catalogdb`).
- `_agenticdev/PLAN.md` — Phase 2, Catalog.Api (spec list).
