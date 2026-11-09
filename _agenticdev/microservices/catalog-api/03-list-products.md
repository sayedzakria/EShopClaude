# Catalog.Api — Spec 03: List products

**Status:** Not started

## Goal

Expose the full product list so the storefront can render the products page.

## Depends on

- `catalog-api/02-seed-data`

## Scope

- [ ] `GET /products` returns every product (`Id, Name, Description, Price, Stock`)

## Explicit non-goals

- No paging, filtering, sorting or API versioning
- No `GET /products/{id}`
- No change to stock

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy
- HTTP check: `GET /products` returns 200 with the seeded products and the expected fields

## References

