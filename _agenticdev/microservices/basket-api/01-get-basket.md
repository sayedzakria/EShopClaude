# Basket.Api — Spec 01: Get basket

**Status:** Not started

## Goal

Connect `Basket.Api` to Redis and read a basket. Redis wiring and the read endpoint stay in one spec because the empty-basket response is the only visible proof that Redis is reachable.

## Depends on

Nothing.

## Scope

- [ ] AppHost: `basketapi` gets `WithReference(redis).WaitFor(redis)`
- [ ] Redis client registered with `AddRedisClient`
- [ ] Basket store that reads a basket (`ProductId, Quantity` pairs only) from Redis
- [ ] `GET /baskets/{basketId}` returns the basket; an unknown id returns an empty basket

## Explicit non-goals

- No write endpoints
- No product name or price stored in the basket
- No call to another service
- No unit tests

## Definition of Done

- `dotnet build EShopClaude.sln` succeeds
- `aspire run` starts every resource healthy, with no Redis connection errors in the `basketapi` logs
- HTTP check: `GET /baskets/{any-guid}` returns 200 with an empty item list

## References

