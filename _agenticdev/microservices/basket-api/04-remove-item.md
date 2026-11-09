# Basket.Api — Spec 04: Remove item

**Status:** Not started

## Goal

Let a shopper remove one line from a basket.

## Depends on

- `basket-api/02-add-item`

## Scope

- [ ] `DELETE /baskets/{basketId}/items/{productId}` removes that line
- [ ] Other lines are left untouched
- [ ] Line not in the basket returns 404
- [ ] Unit tests for remove

## Explicit non-goals

- No clearing the whole basket
- No quantity changes

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Basket.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: add two items, delete one, `GET /baskets/{basketId}` shows only the other; deleting a product not in the basket returns 404

## References

