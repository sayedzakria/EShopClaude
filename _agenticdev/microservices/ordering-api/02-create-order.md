# Ordering.Api — Spec 02: Create order

**Status:** Not started

## Goal

Let `Basket.Api` place an order at the end of checkout. Creating the order and calculating its total stay in one spec because the total is only observable on the created order.

## Depends on

- `ordering-api/01-order-schema`

## Scope

- [ ] `POST /orders` with body `{ items: [{ productId, productName, unitPrice, quantity }] }`
- [ ] Computes `Total`, sets `Status = "Placed"` and `CreatedDate = now`
- [ ] Returns the created order including its `Id`
- [ ] Create the `Ordering.Api` test project; unit tests for order creation and total calculation, with an in-memory store (no real Postgres)

## Explicit non-goals

- No `GET /orders/{id}`
- No order list or history
- No payment, customer or other status values
- No call to another service

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Ordering.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: `POST /orders` with sample items returns the created order with the correct `Total`, `Status = "Placed"`, and the row visible in pgAdmin

## References

