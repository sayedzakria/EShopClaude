# Ordering.Api — Spec 03: Get order

**Status:** Not started

## Goal

Expose a single order by id, for the order confirmation page.

## Depends on

- `ordering-api/02-create-order`

## Scope

- [ ] `GET /orders/{id}` returns the order with its items, total, status and created date
- [ ] Unknown id returns 404

## Explicit non-goals

- No order list or history endpoint
- No filtering by customer
- No call to `Catalog.Api`: the order already holds its own snapshot of names and prices

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy
- HTTP check: create an order with `POST /orders`, then `GET /orders/{id}` returns it with 200; an unknown id returns 404

## References

