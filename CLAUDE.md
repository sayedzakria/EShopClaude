# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

EShopClaude is a minimal e-commerce demo built on .NET 10 + .NET Aspire, developed via spec-driven agentic coding. It is a learning/demo project, not production software — no login, payments, or deployment story. Full spec: `_agenticdev/PROJECT.md`. Phased build plan (what to build, in what order, how to verify each step): `_agenticdev/PLAN.md`. **Read both before making architectural changes** — they define scope, data ownership, and what is deliberately left out (no retries/circuit breakers beyond Aspire defaults, no stock-decrement rollback, no order history, no API pagination/versioning).

The plan is actively in progress (currently around Phase 0/1 — services exist as empty shells with only `/health`, no domain logic, no databases wired yet). Check `_agenticdev/PLAN.md` to see which step is current before assuming a feature exists, and update your mental model from the actual code rather than the plan's descriptions of future steps.

## Commands

All commands run from `src/`.

- Build: `dotnet build EShopClaude.sln`
- Run the whole distributed app (all services + dashboard): `dotnet run --project EShopClaude.AppHost`
- Run all tests: `dotnet test EShopClaude.sln`
- Run a single test project: `dotnet test EShopClaude.Tests/EShopClaude.Tests.csproj`
- Run a single test: `dotnet test EShopClaude.Tests/EShopClaude.Tests.csproj --filter "FullyQualifiedName~MethodOrClassName"`

There is no standalone way to run one microservice meaningfully — they're designed to run together under AppHost since they depend on Aspire service discovery, and `/health` is the only thing hit in isolation. Use the Aspire dashboard (opened by `dotnet run --project EShopClaude.AppHost`) to confirm resources are green and to inspect traces — this is the primary way to verify cross-service calls and Postgres/Redis wiring during development, per the "Aspire run" / "HTTP check" / "Traces" / "Browser" verification steps in `_agenticdev/PLAN.md`.

## Architecture

- **`EShopClaude.AppHost`** — orchestrates everything (Aspire). All service registration, resource wiring (Postgres, Redis, pgAdmin once added), and service references live in `AppHost.cs`. This is the single place that knows the full topology.
- **`EShopClaude.ServiceDefaults`** — shared cross-cutting wiring (OpenTelemetry, health checks, service discovery, standard HTTP resilience). Referenced by every service project; add instrumentation/resilience changes here, not per-service.
- **`EShopClaude.Catalog.Api`** — owns product data. Will own Postgres `catalogdb`.
- **`EShopClaude.Basket.Api`** — owns basket data in Redis (`{productId, quantity}` pairs only — no cached product name/price). Also **orchestrates checkout**: calls Catalog.Api to price/reserve stock, then Ordering.Api to create the order, then clears the basket. This is the one place synchronous cross-service HTTP calls happen.
- **`EShopClaude.Ordering.Api`** — owns order data. Will own Postgres `orderingdb`. Orders store a denormalized snapshot of item name/price at creation time (so order detail never calls back to Catalog.Api).
- **`EShopClaude.Web`** — Blazor Server storefront, three pages in journey order: products → cart → order confirmation. Basket identity is a scoped DI service holding a lazily-generated GUID tied to the Blazor circuit lifetime (no cookies, no login, no persistence beyond the browser session).
- **`EShopClaude.Tests`** — unit tests only (xunit), per spec §8: mocked/in-memory dependencies, no Aspire test host, no real Postgres/Redis spun up in tests.

Communication is synchronous HTTP only via Aspire service discovery (`AddProject<...>().WithReference(...).WaitFor(...)` in `AppHost.cs`) — no message broker, no API gateway. Checkout is the only multi-service call chain (`Web → Basket.Api → Catalog.Api`, `Basket.Api → Ordering.Api`) and deliberately has no compensation/rollback logic if it fails partway through.
