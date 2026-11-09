# Basket.Api — Spec 07: Checkout: place order

**Status:** Not started

## Goal

Second half of checkout: after stock is reserved, create the order in `Ordering.Api`, clear the basket and return the order id. This completes the checkout flow.

## Depends on

- `basket-api/05-clear-basket`
- `basket-api/06-checkout-reserve-stock`
- `ordering-api/02-create-order`

## Scope

- [ ] AppHost: `basketapi` gets `WithReference(orderingApi).WaitFor(orderingApi)`
- [ ] Typed `HttpClient` for `Ordering.Api` through service discovery
- [ ] After all lines are reserved, `POST /orders` with the enriched lines (name, price, quantity)
- [ ] On success, clear the basket and return `{ orderId }`
- [ ] Unit tests for the full happy path and the failure path (no order, basket untouched), with the `Ordering.Api` calls mocked

## Explicit non-goals

- No rollback or compensation if the order call fails
- No retries, circuit breakers or fallbacks beyond the Aspire defaults
- No storefront changes

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Basket.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: checkout with sufficient stock returns `{ orderId }`, the basket is empty afterward, `GET /orders/{orderId}` on `Ordering.Api` returns the order, and `Catalog.Api` stock is reduced
- HTTP check: checkout with insufficient stock returns an error, the basket is untouched and no order exists
- Aspire dashboard shows the `Basket.Api → Catalog.Api` and `Basket.Api → Ordering.Api` calls linked in one trace

## References

