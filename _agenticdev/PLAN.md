# EShopClaude — Build Plan

**Spec:** [`_agenticdev/PROJECT.md`](./PROJECT.md) — this plan sequences that spec into buildable, verifiable steps. No code lives here; each step says what to build, which projects it touches, and how to prove it works before moving to the next.

**Ground rules for every step:**
- Each step is sized for its own feature branch and PR.
- "Build" = `dotnet build` on the solution succeeds.
- "Unit tests" = `dotnet test` on the relevant `*.Tests` project(s) — per §8 of the spec, no Aspire test host, no real Postgres/Redis in tests.
- "Aspire run" = `dotnet run --project src/EShopClaude.AppHost`, then check resources go green in the Aspire dashboard.
- "HTTP check" = call the endpoint directly (dashboard's endpoint links, `curl`, or `.http` file) and confirm status code + payload shape.
- "Traces" = confirm the call shows up in the Aspire dashboard's trace view (validates service discovery + OpenTelemetry wiring, not business logic).
- "Browser" = exercise the page in the storefront UI running under Aspire.

---

## Phase 0 — Project skeleton

Get all four projects to exist, build, and start together under one AppHost, with no business logic yet.

### Step 0.1 — Remove the sample weather/counter scaffolding
- **Tasks:** Delete `EShopClaude.ApiService` project and its reference in `AppHost.cs`. Delete `Weather.razor` and `Counter.razor` pages (and their nav entries) from `EShopClaude.Web`. Delete `WeatherForecastTemperatureFTests.cs` from `EShopClaude.Tests`.
- **Projects touched:** `EShopClaude.AppHost`, `EShopClaude.Web`, `EShopClaude.Tests`. Removes `EShopClaude.ApiService` entirely.
- **Verify:** Build succeeds. `dotnet run` on AppHost still starts `EShopClaude.Web` cleanly in the dashboard (no dangling reference to the deleted ApiService).

### Step 0.2 — Scaffold the three microservice projects
- **Tasks:** Create `Catalog.Api`, `Basket.Api`, `Ordering.Api` as ASP.NET Core minimal API projects (same style as the deleted ApiService: `Program.cs` using `AddServiceDefaults()` / `MapDefaultEndpoints()` from `EShopClaude.ServiceDefaults`, `/health` endpoint). No domain code yet — each just boots and responds to `/health`.
- **Projects touched:** new `Catalog.Api`, `Basket.Api`, `Ordering.Api`; each references `EShopClaude.ServiceDefaults`.
- **Verify:** Build succeeds for the whole solution.

### Step 0.3 — Wire all three services + the storefront into AppHost
- **Tasks:** In `AppHost.cs`, register `Catalog.Api`, `Basket.Api`, `Ordering.Api` with `AddProject<...>()` and `.WithHttpHealthCheck("/health")`, following the existing `apiservice`/`webfrontend` pattern. Point `EShopClaude.Web` at all three via `WithReference(...).WaitFor(...)` (no business calls yet, just the reference wiring so service discovery config exists for later steps).
- **Projects touched:** `EShopClaude.AppHost`, `EShopClaude.Web`.
- **Verify:** `dotnet run` on AppHost (Aspire run). All four resources (3 APIs + Web) show green/healthy in the dashboard. Each API's `/health` is reachable from the dashboard's endpoint links (HTTP check).

---

## Phase 1 — Data persistence design

Stand up the data stores in AppHost and confirm each service can reach its own store, before any domain logic uses them.

> **Infrastructure-only slice:** sub-steps 1.1a–1.2c below add the data-store resources to the AppHost and the Aspire client packages to the services. Nothing connects a service to a store yet — no `WithReference`/`WaitFor`, no `AddNpgsqlDbContext`/`AddRedisClient`, no DbContext/entities/seed. That wiring starts at Step 1.3. All Aspire packages are pinned to `13.6.4` to match `Aspire.AppHost.Sdk`.

### Step 1.1 — Add PostgreSQL server with `catalogdb` and `orderingdb`, plus pgAdmin
- **1.1a — Hosting package.** Add `Aspire.Hosting.PostgreSQL` 13.6.4 to `EShopClaude.AppHost`. *Verify:* build succeeds.
- **1.1b — Server + pgAdmin.** In `AppHost.cs`, add one Postgres server resource (`postgres`) with `.WithPgAdmin()`. No data volume. *Verify:* Aspire run — Postgres and pgAdmin show green.
- **1.1c — Databases.** Add two database resources from that server: `catalogdb`, `orderingdb`. *Verify:* Aspire run — both databases show green. pgAdmin is reachable from the dashboard and can connect to the server (browser check: see both empty databases listed).
- **Projects touched:** `EShopClaude.AppHost`.

### Step 1.2 — Add Redis for basket storage, and the service client packages
- **1.2a — Hosting package.** Add `Aspire.Hosting.Redis` 13.6.4 to `EShopClaude.AppHost`. *Verify:* build succeeds.
- **1.2b — Redis resource.** Add a Redis resource (`redis`) in `AppHost.cs`. No data volume. *Verify:* Aspire run — Redis shows green in the dashboard.
- **1.2c — Client integration packages (packages only).** Add `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` 13.6.4 to `Catalog.Api` and `Ordering.Api`; add `Aspire.StackExchange.Redis` 13.6.4 to `Basket.Api`. No code uses them yet. *Verify:* build succeeds; Aspire run — all resources (data stores and the four existing ones) still green.
- **Projects touched:** `EShopClaude.AppHost`, `Catalog.Api`, `Ordering.Api`, `Basket.Api` (1.2c: `.csproj` only).

---

## Phase 2 — Microservices, one by one

Each service is built as a series of small specs, one feature branch each. Scope, dependencies, branch names, status and Definition of Done live in the spec files and in [`microservices/README.md`](./microservices/README.md), not here. Build order: Catalog, then Ordering, then Basket (01–05).

### Catalog.Api
**Owns:** product data in `catalogdb`: listing, lookup and stock decrement.
**Specs:** [`microservices/catalog-api/`](./microservices/catalog-api/)
`01-product-schema.md`, `02-seed-data.md`, `03-list-products.md`, `04-get-product.md`, `05-decrement-stock.md`

### Ordering.Api
**Owns:** order data in `orderingdb`: the order snapshot created at checkout, and its lookup.
**Specs:** [`microservices/ordering-api/`](./microservices/ordering-api/)
`01-order-schema.md`, `02-create-order.md`, `03-get-order.md`

### Basket.Api (basket operations)
**Owns:** baskets in Redis, stored as `{productId, quantity}` pairs only.
**Specs:** [`microservices/basket-api/`](./microservices/basket-api/)
`01-get-basket.md`, `02-add-item.md`, `03-update-item-quantity.md`, `04-remove-item.md`, `05-clear-basket.md`

---

## Phase 3 — Service communication & checkout

The one place services call each other. Starts only after the Catalog and Ordering specs it depends on are merged.

### Basket.Api (checkout)
**Owns:** checkout orchestration across Catalog.Api and Ordering.Api.
**Specs:** [`microservices/basket-api/`](./microservices/basket-api/)
`06-checkout-reserve-stock.md`, `07-checkout-place-order.md`

---

## Phase 4 — Storefront pages

Build the three pages in journey order, each wired to the real services (no more direct HTTP-tool checks needed once a page covers that behavior).

### Step 4.1 — Basket identity service
- **Tasks:** Add a scoped DI service in `EShopClaude.Web` that lazily generates and holds a GUID basket id for the lifetime of the Blazor circuit, per spec §4.
- **Projects touched:** `EShopClaude.Web`.
- **Verify:** Build. Browser check: open the app in two separate browser tabs/sessions and confirm (e.g. via a temporary debug display or logs) each gets a distinct, stable id across page navigations within its own session.

### Step 4.2 — `/products` page
- **Tasks:** Build the products page per spec §6: list from `Catalog.Api` (`GET /products`), "Add to cart" action calling `Basket.Api` (`POST /baskets/{basketId}/items`) with the circuit-scoped basket id.
- **Projects touched:** `EShopClaude.Web`.
- **Verify:** Aspire run. Browser: seeded products render; clicking "Add to cart" succeeds with no error; traces show `Web → Catalog.Api` and `Web → Basket.Api` calls.

### Step 4.3 — `/cart` page
- **Tasks:** Build the cart page per spec §6: load basket (`GET /baskets/{basketId}`), enrich each line via `Catalog.Api` (`GET /products/{id}`), support quantity change (PUT), line removal (DELETE), show computed total, "Checkout" button calling `POST /baskets/{basketId}/checkout` — on success navigate to `/orders/{orderId}`, on failure show an inline error and leave the cart as-is.
- **Projects touched:** `EShopClaude.Web`.
- **Verify:** Aspire run. Browser: add items from `/products`, see them enriched with name/price on `/cart`, change quantity, remove a line, confirm total updates correctly. Trigger an insufficient-stock checkout (e.g. request more than available) and confirm the inline error shows without clearing the cart.

### Step 4.4 — `/orders/{id}` page
- **Tasks:** Build the order confirmation page per spec §6: fetch from `Ordering.Api` (`GET /orders/{id}`), display items, quantities, prices, total, status, created date.
- **Projects touched:** `EShopClaude.Web`.
- **Verify:** Aspire run. Browser: complete a checkout from `/cart` and confirm the redirect lands on `/orders/{orderId}` showing correct order details matching what was in the cart.

---

## Phase 5 — End-to-end verification

### Step 5.1 — Full journey walkthrough
- **Tasks:** No new code. Run the full solution under Aspire and manually execute: browse products → add multiple items to cart → adjust quantities/remove a line → checkout → view order confirmation. Repeat once with a deliberately insufficient-stock item to confirm the failure path (spec §5, §7).
- **Projects touched:** none (verification only).
- **Verify:** Aspire run (all resources green throughout). Browser: both journeys (happy path and insufficient-stock path) behave exactly as spec'd. Dashboard traces show the expected call graph for a checkout (`Web → Basket.Api → Catalog.Api`, `Basket.Api → Ordering.Api`) with no unexpected errors. All unit test suites (`dotnet test` across Catalog/Basket/Ordering test projects) still pass.
- **Exit criteria:** This step passing is the definition of "EShopClaude demo complete" per spec §1's three user journeys.

---

## Summary: steps per project

| Project | Steps |
|---|---|
| `EShopClaude.AppHost` | 0.1, 0.2(ref only), 0.3, 1.1a–c, 1.2a–b, plus each service's own registration inside its specs |
| `Catalog.Api` | 0.2, 1.2c(package), catalog-api specs 01–05 |
| `Ordering.Api` | 0.2, 1.2c(package), ordering-api specs 01–03 |
| `Basket.Api` | 0.2, 1.2c(package), basket-api specs 01–07 |
| `EShopClaude.Web` | 0.1, 4.1, 4.2, 4.3, 4.4 |
| `EShopClaude.Tests` / per-service test projects | 0.1, then inside the service specs |

## Review focus (spec requirements most likely to be missed if skipped)

- **Insufficient-stock checkout leaves no order and an untouched basket** (spec §5 step 2, §7) — covered by basket-api/06 and /07 and Step 4.3's browser check.
- **No stock compensation/rollback on partial multi-item failure** (spec §7) — explicitly a non-requirement; don't accidentally add rollback logic in basket-api/06 or /07.
- **No API versioning/pagination/filtering on `/products`** (spec §7) — keep catalog-api/03 to the plain full-list endpoint.
- **Basket id scoped to the Blazor circuit, not cookies/storage** (spec §4) — covered by Step 4.1's two-session browser check.
- **No order list/history beyond the single just-placed order** (spec §7) — keep Step 4.4 to single-order lookup by id only, no listing endpoint/page.
