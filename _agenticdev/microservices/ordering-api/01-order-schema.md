# Ordering.Api — Spec 01: Order schema

**Status:** Not started

## Goal

Connect `Ordering.Api` to its own `orderingdb` database and create the order tables. The connection and the schema stay in one spec because the tables appearing in pgAdmin are the only visible proof that the connection works.

## Depends on

Nothing.

## Scope

- [ ] AppHost: `orderingapi` gets `WithReference(orderingdb).WaitFor(orderingdb)`
- [ ] `Order` entity: `Id, Total, Status, CreatedDate`, with an `Items` collection of `{ ProductId, ProductName, UnitPrice, Quantity }`
- [ ] EF Core DbContext registered with `AddNpgsqlDbContext`
- [ ] Schema applied at startup

## Explicit non-goals

- No order endpoints
- No total calculation
- No customer or user field
- No unit tests

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy, with no connection errors in the `orderingapi` logs
- pgAdmin shows the order and order-item tables inside `orderingdb`

## References

