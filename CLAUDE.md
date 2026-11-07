# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Spec & plan

- Full spec: `_agenticdev/PROJECT.md`. Phased build plan: `_agenticdev/PLAN.md`. Read both before architectural changes — they define scope and what's deliberately out of scope (no rollback on failed checkout, no order history, no API pagination/versioning, etc).
- The plan is a work in progress — check which step is current before assuming a feature exists.

## Architecture rules

- Three microservices — `Catalog.Api`, `Basket.Api`, `Ordering.Api` — plus a Blazor storefront, orchestrated by the Aspire AppHost.
- Database per service: one PostgreSQL server with `catalogdb` for Catalog and `orderingdb` for Ordering, and Redis for Basket. No service reads another service's database.
- Checkout is owned and orchestrated by `Basket.Api`.
- Services talk over synchronous HTTP through Aspire service discovery. No API gateway, no message broker, no events.
- Out of scope: login, identity, payments, deployment. Don't propose them.

## Workflow

- Foundation work is committed on `main`. From the first spec, one spec is one sub-feature on one feature branch, merged with a pull request.
- Before committing, the solution must build and `aspire run` must start every resource healthy.
- Detailed scope lives in `_agenticdev/PROJECT.md`, build order in `_agenticdev/PLAN.md`. Reference them, don't copy them.

## Commands

Run from `src/`:

- `dotnet build EShopClaude.sln` — build the solution
- `aspire run` — run the full distributed app (all services + dashboard)
- `dotnet test EShopClaude.sln` — run all tests
- `dotnet test EShopClaude.Tests/EShopClaude.Tests.csproj --filter "FullyQualifiedName~Name"` — run a single test

## Conventions

- All projects target `net10.0`.
- The API services (`Catalog.Api`, `Basket.Api`, `Ordering.Api`) use minimal APIs — no controllers.
- Every API project's `Program.cs` calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`. Keep this when adding a new service.
- Project naming: `EShopClaude.<Name>` (e.g. `EShopClaude.Catalog.Api`).
- AppHost resource naming (registered in `AppHost.cs`): `catalogapi`, `basketapi`, `orderingapi`, `webfrontend`.
