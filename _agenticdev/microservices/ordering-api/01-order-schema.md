# Ordering.Api — Spec 01: Order schema

**Status:** Not started

## Goal

Connect `Ordering.Api` to its own `orderingdb` database and create the order tables. The connection and the schema stay in one spec because the tables appearing in pgAdmin are the only visible proof that the connection works.

## Context

- `AppHost.cs` already registers `postgres` (with pgAdmin, no data volume), `catalogdb` and `orderingdb`. `orderingapi` is registered with only `WithHttpHealthCheck("/health")`: it has no reference to `orderingdb` yet. `catalogapi` already has `WithReference(catalogdb).WaitFor(catalogdb)`, which is the pattern to copy.
- `EShopClaude.Ordering.Api.csproj` already references `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.6.4, so no package change is needed.
- `Ordering.Api/Program.cs` has only `AddServiceDefaults()`, a `/` route and `MapDefaultEndpoints()`. There is no data access yet.
- Postgres has no data volume, so `orderingdb` is empty on every AppHost start and the schema step runs on every start.
- Precedent: `Catalog.Api` (`Product.cs`, `CatalogDbContext.cs`, `Program.cs`) already does the same thing for `catalogdb`: `AddNpgsqlDbContext`, `EnsureCreated` in a scope after `Build()`, explicit table name, explicit `numeric(18,2)`.

## Depends on

Nothing.

## Scope

- [ ] AppHost: `orderingapi` gets `WithReference(orderingdb).WaitFor(orderingdb)`
- [ ] `Order` entity: `Id, Total, Status, CreatedDate`, with an `Items` collection of `{ ProductId, ProductName, UnitPrice, Quantity }`
- [ ] EF Core DbContext registered with `AddNpgsqlDbContext`
- [ ] Schema applied at startup

## Explicit non-goals

- No order endpoints
- No total calculation
- No customer or user field
- No unit tests
- No `Status` default or constant: spec 02 sets `"Placed"`
- No `CreatedDate` default: spec 02 sets it

## Data model

Table `Order` in `orderingdb`:

| Column | .NET type | Postgres type | Rules |
|---|---|---|---|
| `Id` | `int` | `integer` | Primary key, database-generated identity |
| `Total` | `decimal` | `numeric(18,2)` | Required, explicit column type |
| `Status` | `string` | `character varying(20)` | Required |
| `CreatedDate` | `DateTime` | `timestamp with time zone` | Required. The value is UTC, set in spec 02 |

Table `OrderItem` in `orderingdb`:

| Column | .NET type | Postgres type | Rules |
|---|---|---|---|
| `Id` | `int` | `integer` | Primary key, database-generated identity |
| `OrderId` | `int` | `integer` | Required, foreign key to `Order.Id`, cascade delete |
| `ProductId` | `int` | `integer` | Required. Plain value: no foreign key, no index (the Catalog database is not ours) |
| `ProductName` | `string` | `character varying(200)` | Required. Same length as `Product.Name` in Catalog |
| `UnitPrice` | `decimal` | `numeric(18,2)` | Required, explicit column type |
| `Quantity` | `int` | `integer` | Required |

- `Order` has an `Items` collection of `OrderItem`. `OrderItem` carries the `OrderId` property; there is no navigation back to `Order`.
- Tables are named `Order` and `OrderItem` (singular), set explicitly because EF defaults to the `DbSet` name. `Order` is a reserved word in SQL: EF quotes it, so queries by hand must write `"Order"`.
- Column names stay PascalCase as in the entities (the EF Npgsql default, no snake_case).
- No indexes beyond the primary keys and the index EF creates by convention on `OrderItem.OrderId`. No check constraints, no seed rows.
- The connection string comes from Aspire only: `WithReference(orderingdb)` injects `ConnectionStrings:orderingdb`, and the DbContext is registered under the name `orderingdb`. No appsettings entry.
- The schema is created with `EnsureCreated` at startup. No migrations folder and no `dotnet-ef` tool.

## Tasks

1. AppHost: add `WithReference(orderingdb)` and `WaitFor(orderingdb)` to `orderingapi`.
2. Add the `Order` entity with the four properties above and an `Items` collection (the `Status` string non-nullable, the collection never null).
3. Add the `OrderItem` entity with the six properties above (the `ProductName` string non-nullable).
4. Add the DbContext with a `DbSet` for each entity, mapped to tables `Order` and `OrderItem` with the rules from the Data model, including the `Order` to `OrderItem` relationship with cascade delete.
5. In `Program.cs`, register the DbContext with `AddNpgsqlDbContext` under the name `orderingdb`.
6. In `Program.cs`, after `Build()` and before the routes, create a service scope and call `EnsureCreated` on the DbContext.
7. Build, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.AppHost/AppHost.cs` | modify |
| `src/EShopClaude.Ordering.Api/Order.cs` | new |
| `src/EShopClaude.Ordering.Api/OrderItem.cs` | new |
| `src/EShopClaude.Ordering.Api/OrderingDbContext.cs` | new |
| `src/EShopClaude.Ordering.Api/Program.cs` | modify |

Not touched: the `.csproj` (package already present), `EShopClaude.Tests`, the other services, and `microservices/README.md` (no spec is added, split or renamed).

## Definition of Done

Run from `src/` unless stated otherwise.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors and no EF decimal-precision warning.
2. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
3. Dashboard, `orderingapi` console logs and structured logs filtered to Error: no entries, and no Npgsql or connection exception text.
4. Open the `orderingapi` endpoint link from the dashboard and request `/health`: HTTP 200, body `Healthy`.
5. pgAdmin, `orderingdb`, Schemas, public, Tables: shows exactly two tables, `Order` and `OrderItem`.
6. pgAdmin Query Tool on `orderingdb`: `SELECT table_name, column_name, data_type, character_maximum_length, numeric_precision, numeric_scale, is_nullable, is_identity FROM information_schema.columns WHERE table_name IN ('Order', 'OrderItem') ORDER BY table_name, ordinal_position;` returns exactly 10 rows (4 for `Order`, 6 for `OrderItem`) matching the Data model tables, and both `Id` columns have `is_identity = YES`.
7. Same Query Tool: `SELECT conname, confdeltype FROM pg_constraint WHERE conrelid = '"OrderItem"'::regclass AND contype = 'f';` returns exactly 1 row, with `confdeltype = 'c'` (cascade).
8. Same Query Tool: `SELECT (SELECT count(*) FROM "Order") AS orders, (SELECT count(*) FROM "OrderItem") AS items;` returns `0` and `0` (no seed).
9. Idempotent restart: restart only `orderingapi` from the dashboard. It returns to Healthy, with no "already exists" error in its logs and both tables still present.
10. Non-goal check: `GET <orderingapi-url>/orders/1` and `POST <orderingapi-url>/orders` both return 404.
11. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file.

## References

- PROJECT.md §2 (databases), §3 (Ordering.Api `Order` data), §9 (AppHost notes)
- PLAN.md Phase 1 (data stores in AppHost), Phase 2 (Ordering.Api)
- Precedent: `catalog-api/01-product-schema`
