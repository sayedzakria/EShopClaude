# Catalog.Api — Spec 01: Product schema

**Status:** Not started

## Goal

Connect `Catalog.Api` to its own `catalogdb` database and create the `Product` table. The connection and the schema stay in one spec because the table appearing in pgAdmin is the only visible proof that the connection works.

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

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy, with no connection errors in the `catalogapi` logs
- pgAdmin shows the `Product` table inside `catalogdb`

## References

