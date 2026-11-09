# Catalog.Api — Spec 05: Decrement stock

**Status:** Not started

## Goal

Let `Basket.Api` reserve stock during checkout. The endpoint and its unit tests are one spec because the tests are the proof of the decrement logic.

## Depends on

- `catalog-api/02-seed-data`

## Scope

- [ ] `POST /products/{id}/decrement-stock` with body `{ quantity }`
- [ ] 200 and lower `Stock` when stock is sufficient
- [ ] 409 when stock is insufficient, stock unchanged
- [ ] Unknown id returns 404
- [ ] Create the `Catalog.Api` test project; unit tests for success and insufficient stock, with an in-memory store (no real Postgres)

## Explicit non-goals

- No stock increment or restore
- No reservation or compensation logic
- No call to or from another service

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `dotnet test` passes for the `Catalog.Api` tests
- `aspire run` starts every resource healthy
- HTTP check: decrementing within stock returns 200 and `GET /products/{id}` shows lower `Stock`; decrementing past available stock returns 409 and stock is unchanged

## References

