# Catalog.Api — Spec 04: Get product

**Status:** Not started

## Goal

Expose a single product by id, which the cart page and the checkout flow use to read the current name and price.

## Depends on

- `catalog-api/02-seed-data`

## Scope

- [ ] `GET /products/{id}` returns the product
- [ ] Unknown id returns 404

## Explicit non-goals

- No list endpoint changes
- No stock changes
- No caching

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy
- HTTP check: a seeded id returns 200 with that product; an unknown id returns 404

## References

