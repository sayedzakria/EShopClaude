# Basket.Api — Spec 02: Add item

**Status:** Not started

## Goal

Let a shopper put a product into a basket, incrementing the quantity when the product is already there.

## Depends on

- `basket-api/01-get-basket`

## Scope

- [ ] `POST /baskets/{basketId}/items` with body `{ productId, quantity }`
- [ ] Adds a new line, or increments the quantity of an existing line
- [ ] Create the `Basket.Api` test project; unit tests for add and increment, with an in-memory basket store (no real Redis)

## Explicit non-goals

- No check that the product exists or has stock (no call to `Catalog.Api`)
- No update, remove or clear
- No checkout

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Basket.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: two `POST` calls with different products give two lines; a repeated `POST` for one product increments its quantity; `GET /baskets/{basketId}` shows the result

## References

