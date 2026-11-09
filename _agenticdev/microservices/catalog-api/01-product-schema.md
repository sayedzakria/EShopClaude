# Catalog.Api — Spec 01: Product schema

**Status:** Done

## Goal

Connect `Catalog.Api` to its own `catalogdb` database and create the `Product` table. The connection and the schema stay in one spec because the table appearing in pgAdmin is the only visible proof that the connection works.

## Context

- `AppHost.cs` already registers `postgres` (with pgAdmin, no data volume), `catalogdb` and `orderingdb`. `catalogapi` is registered with only `WithHttpHealthCheck("/health")`: it has no reference to `catalogdb` yet.
- `EShopClaude.Catalog.Api.csproj` already references `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.6.4, so no package change is needed.
- `Catalog.Api/Program.cs` has only `AddServiceDefaults()`, a `/` route and `MapDefaultEndpoints()`. There is no data access yet.
- Postgres has no data volume, so `catalogdb` is empty on every AppHost start and the schema step runs on every start.

## Depends on

Nothing.

## Scope

- [ ] AppHost: `catalogapi` gets `WithReference(catalogdb).WaitFor(catalogdb)`
- [ ] `Product` entity: `Id, Name, Description, Price, Stock`
- [ ] EF Core DbContext registered with `AddNpgsqlDbContext`
- [ ] Schema applied at startup

## Explicit non-goals

- No seed data
- No product endpoints
- No unit tests

## Data model

Table `Product` in `catalogdb`:

| Column | .NET type | Postgres type | Rules |
|---|---|---|---|
| `Id` | `int` | `integer` | Primary key, database-generated identity |
| `Name` | `string` | `character varying(200)` | Required |
| `Description` | `string` | `character varying(1000)` | Required |
| `Price` | `decimal` | `numeric(18,2)` | Required, explicit column type |
| `Stock` | `int` | `integer` | Required |

- The table is named `Product` (singular), because the Definition of Done checks for that name. EF's default is the `DbSet` name, usually plural, so the table name must be set explicitly.
- Column names stay PascalCase as in the entity (the EF Npgsql default, no snake_case).
- No indexes beyond the primary key, no check constraints, no seed rows.
- The connection string comes from Aspire only: `WithReference(catalogdb)` injects `ConnectionStrings:catalogdb`, and the DbContext is registered under the name `catalogdb`. No appsettings entry.
- The schema is created with `EnsureCreated` at startup. No migrations folder and no `dotnet-ef` tool.

## Tasks

1. AppHost: add `WithReference(catalogdb)` and `WaitFor(catalogdb)` to `catalogapi`.
2. Add the `Product` entity with the five properties above (the two strings non-nullable).
3. Add the DbContext with one `DbSet` for `Product`, mapped to table `Product` with the rules from the Data model.
4. In `Program.cs`, register the DbContext with `AddNpgsqlDbContext` under the name `catalogdb`.
5. In `Program.cs`, after `Build()` and before `app.Run()`, create a service scope and call `EnsureCreated` on the DbContext.
6. Build, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.AppHost/AppHost.cs` | modify |
| `src/EShopClaude.Catalog.Api/Product.cs` | new |
| `src/EShopClaude.Catalog.Api/CatalogDbContext.cs` | new |
| `src/EShopClaude.Catalog.Api/Program.cs` | modify |

Not touched: the `.csproj` (package already present), `EShopClaude.Tests`, the other services, and `microservices/README.md` (no spec is added, split or renamed).

## Definition of Done

Run from `src/` unless stated otherwise.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors and no EF decimal-precision warning.
2. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
3. Dashboard, `catalogapi` console logs and structured logs filtered to Error: no entries, and no Npgsql or connection exception text.
4. Open the `catalogapi` endpoint link from the dashboard and request `/health`: HTTP 200, body `Healthy`.
5. pgAdmin, `catalogdb`, Schemas, public, Tables: shows one table, `Product`.
6. pgAdmin Query Tool on `catalogdb`: `SELECT column_name, data_type, character_maximum_length, numeric_precision, numeric_scale, is_nullable, is_identity FROM information_schema.columns WHERE table_name = 'Product' ORDER BY ordinal_position;` returns exactly 5 rows matching the Data model table, and `Id` has `is_identity = YES`.
7. Same Query Tool: `SELECT count(*) FROM "Product";` returns `0` (no seed).
8. Idempotent restart: restart only `catalogapi` from the dashboard. It returns to Healthy, with no "already exists" error in its logs and the `Product` table still present.
9. Non-goal check: `GET <catalogapi-url>/products` returns 404.
10. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file.

## References

