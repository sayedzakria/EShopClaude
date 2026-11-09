# Basket.Api — Spec 03: Update item quantity

**Status:** Not started

## Goal

Let a shopper set the exact quantity of one basket line.

## Depends on

- `basket-api/02-add-item`

## Scope

- [ ] `PUT /baskets/{basketId}/items/{productId}` with body `{ quantity }` sets the quantity of that line
- [ ] Line not in the basket returns 404
- [ ] Unit tests for update

## Explicit non-goals

- No creation of a line through `PUT`
- No check against catalog stock
- No removal through `quantity = 0`: removal is its own endpoint

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Basket.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: add an item, `PUT` a new quantity, `GET /baskets/{basketId}` shows it; `PUT` for a product not in the basket returns 404

## References

