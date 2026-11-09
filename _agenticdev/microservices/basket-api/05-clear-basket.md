# Basket.Api — Spec 05: Clear basket

**Status:** Not started

## Goal

Empty a whole basket in one call. The checkout flow uses it after a successful order.

## Depends on

- `basket-api/02-add-item`

## Scope

- [ ] `DELETE /baskets/{basketId}` removes every line
- [ ] Clearing an empty or unknown basket still succeeds
- [ ] Unit tests for clear

## Explicit non-goals

- No checkout logic
- No call to another service

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Basket.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: add items, `DELETE /baskets/{basketId}`, `GET /baskets/{basketId}` returns an empty item list

## References

