# Basket.Api — Spec 06: Checkout: reserve stock

**Status:** Not started

## Goal

First half of checkout: read the current price and name of each basket line from `Catalog.Api` and reserve its stock, aborting when any line has insufficient stock. It is split from order placement because the stock outcome (stock drops, or an error with the basket untouched) can be verified before `Ordering.Api` is involved. Until spec 07 lands the endpoint is intentionally incomplete; the storefront does not call it yet.

## Depends on

- `basket-api/02-add-item`
- `catalog-api/04-get-product`
- `catalog-api/05-decrement-stock`

## Scope

- [ ] AppHost: `basketapi` gets `WithReference(catalogApi).WaitFor(catalogApi)`
- [ ] Typed `HttpClient` for `Catalog.Api` through service discovery
- [ ] `POST /baskets/{basketId}/checkout` loads the basket, then for each line calls `GET /products/{id}` and `POST /products/{id}/decrement-stock`
- [ ] Any failed decrement aborts with an error; the basket is not cleared
- [ ] Unit tests for the happy path and the insufficient-stock abort, with the `Catalog.Api` calls mocked

## Explicit non-goals

- No order creation and no basket clearing (spec 07)
- No rollback or compensation of stock already decremented
- No retries, circuit breakers or fallbacks beyond the Aspire defaults

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Basket.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: checkout with sufficient stock reduces `Stock` in `Catalog.Api`; checkout with insufficient stock returns an error and the basket is unchanged
- Aspire dashboard shows a linked `Basket.Api → Catalog.Api` trace for one checkout call

## References

