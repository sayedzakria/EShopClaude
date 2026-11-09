# Catalog.Api — Spec 02: Seed data

**Status:** Not started

## Goal

Give the catalog demo products to show, so every later endpoint, the cart and checkout have something to work with.

## Depends on

- `catalog-api/01-product-schema`

## Scope

- [ ] Startup seed inserts a small fixed set of demo products (name, description, price, stock)
- [ ] Seed runs only when the `Product` table is empty

## Explicit non-goals

- No product endpoints
- No admin or import feature
- No random or generated data: the set is fixed

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy
- pgAdmin shows the seeded rows in `Product`
- Restarting the AppHost does not duplicate the rows

## References

