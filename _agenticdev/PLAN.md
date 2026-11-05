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

### Step 1.1 — Add PostgreSQL server with `catalogdb` and `orderingdb`, plus pgAdmin
- **Tasks:** Add `Aspire.Hosting.PostgreSQL` package reference to `EShopClaude.AppHost`. In `AppHost.cs`, add one Postgres server resource with `.WithPgAdmin()`, and two database resources from it: `catalogdb`, `orderingdb`.
- **Projects touched:** `EShopClaude.AppHost`.
- **Verify:** Aspire run. Postgres resource and both database resources show green. pgAdmin resource is reachable from the dashboard and can connect to the Postgres server (browser check: log into pgAdmin, see both empty databases listed).

### Step 1.2 — Add Redis for basket storage
- **Tasks:** Add `Aspire.Hosting.Redis` package reference to `EShopClaude.AppHost`. Add a Redis resource in `AppHost.cs`.
- **Projects touched:** `EShopClaude.AppHost`.
- **Verify:** Aspire run. Redis resource shows green in the dashboard.

### Step 1.3 — Connect `Catalog.Api` to `catalogdb` and define the `Product` schema
- **Tasks:** Reference `catalogdb` from `Catalog.Api` in `AppHost.cs` (`WithReference(catalogdb).WaitFor(catalogdb)`). In `Catalog.Api`, add EF Core (or chosen data access) wiring for the `Product` entity (`Id, Name, Description, Price, Stock` per spec §3) and apply the schema (migration or `EnsureCreated`).
- **Projects touched:** `EShopClaude.AppHost`, `Catalog.Api`.
- **Verify:** Aspire run. `Catalog.Api` resource goes green (no startup/connection errors in logs). pgAdmin shows the `Product` table created inside `catalogdb` (browser check).

### Step 1.4 — Connect `Ordering.Api` to `orderingdb` and define the `Order` schema
- **Tasks:** Reference `orderingdb` from `Ordering.Api` in `AppHost.cs`. In `Ordering.Api`, add data access wiring for the `Order` entity and its `Items` collection (`Id, Items[{ProductId, ProductName, UnitPrice, Quantity}], Total, Status, CreatedDate` per spec §3) and apply the schema.
- **Projects touched:** `EShopClaude.AppHost`, `Ordering.Api`.
- **Verify:** Aspire run. `Ordering.Api` resource goes green. pgAdmin shows the `Order` (and items) tables created inside `orderingdb`.

### Step 1.5 — Connect `Basket.Api` to Redis
- **Tasks:** Reference the Redis resource from `Basket.Api` in `AppHost.cs`. In `Basket.Api`, add the Redis client wiring (e.g. `StackExchange.Redis` via Aspire's Redis client integration) with no business logic yet.
- **Projects touched:** `EShopClaude.AppHost`, `Basket.Api`.
- **Verify:** Aspire run. `Basket.Api` resource goes green with no Redis connection errors in logs.

### Step 1.6 — Seed Catalog data
- **Tasks:** Add a startup seed step in `Catalog.Api` that inserts a small fixed set of demo products (name, description, price, stock) into `catalogdb` if empty.
- **Projects touched:** `Catalog.Api`.
- **Verify:** Aspire run. pgAdmin shows seeded rows in the `Product` table (browser check). Re-running AppHost doesn't duplicate rows.

---

## Phase 2 — Microservices, one by one

Build each service's full API surface and unit-test its logic in isolation, before wiring cross-service calls.

### Step 2.1 — Catalog.Api: product endpoints
- **Tasks:** Implement `GET /products`, `GET /products/{id}` per spec §3.
- **Projects touched:** `Catalog.Api`.
- **Verify:** Build. Aspire run + HTTP check: `GET /products` returns the seeded list; `GET /products/{id}` returns one product and 404 for an unknown id.

### Step 2.2 — Catalog.Api: stock decrement + unit tests
- **Tasks:** Implement `POST /products/{id}/decrement-stock` (body `{ quantity }`, 200 on success, 409/400 on insufficient stock, per spec §3). Add unit tests per spec §8: success case and insufficient-stock case, using an in-memory/mocked data store (no real Postgres).
- **Projects touched:** `Catalog.Api`; new `Catalog.Api.Tests` (or shared `EShopClaude.Tests` — pick one project and keep it consistent across all three services).
- **Verify:** Unit tests pass (`dotnet test`). Aspire run + HTTP check: decrementing within stock succeeds and lowers `Stock`; decrementing past available stock returns the error status.

### Step 2.3 — Ordering.Api: order creation + detail + unit tests
- **Tasks:** Implement `POST /orders` (computes `Total`, sets `Status = "Placed"`, `CreatedDate = now`, returns created order with `Id`) and `GET /orders/{id}` per spec §3. Add unit tests per spec §8: order creation and total calculation, using an in-memory/mocked store.
- **Projects touched:** `Ordering.Api`; `Ordering.Api.Tests` (or shared test project).
- **Verify:** Unit tests pass. Aspire run + HTTP check: `POST /orders` with sample items returns a created order with correct computed `Total`; `GET /orders/{id}` returns it back; unknown id returns 404.

### Step 2.4 — Basket.Api: basket item CRUD + unit tests
- **Tasks:** Implement `GET /baskets/{basketId}`, `POST /baskets/{basketId}/items`, `PUT /baskets/{basketId}/items/{productId}`, `DELETE /baskets/{basketId}/items/{productId}`, `DELETE /baskets/{basketId}` per spec §3 (storing only `{productId, quantity}` pairs in Redis). Add unit tests per spec §8: add/increment, update quantity, remove, clear — using a mocked/in-memory basket store.
- **Projects touched:** `Basket.Api`; `Basket.Api.Tests` (or shared test project). *(Checkout endpoint is deliberately excluded from this step — it needs Catalog.Api/Ordering.Api and is built in Phase 3.)*
- **Verify:** Unit tests pass. Aspire run + HTTP check: full CRUD cycle against a basket id — add two lines, increment one via repeated add, change quantity via PUT, remove a line, confirm `GET` reflects state, clear the basket and confirm it's empty.

---

## Phase 3 — Service communication & checkout

Wire the one place services call each other.

### Step 3.1 — Basket.Api → Catalog.Api service discovery wiring
- **Tasks:** Add an `HttpClient` in `Basket.Api` configured for Aspire service discovery against `Catalog.Api` (`WithReference`/`WaitFor` already set in Step 0.3 — add the typed client / base address here). No checkout logic yet — just a throwaway call path (or defer and combine with 3.3) to prove connectivity.
- **Projects touched:** `Basket.Api`, `AppHost.cs` (if the reference needs adjusting from Step 0.3).
- **Verify:** Build. Aspire run: trace check — a manual call from `Basket.Api` to `Catalog.Api`'s `GET /products/{id}` shows up as a linked trace in the Aspire dashboard.

### Step 3.2 — Basket.Api → Ordering.Api service discovery wiring
- **Tasks:** Same as 3.1, targeting `Ordering.Api`.
- **Projects touched:** `Basket.Api`.
- **Verify:** Aspire run: trace check — a manual call from `Basket.Api` to `Ordering.Api`'s `POST /orders` shows up as a linked trace.

### Step 3.3 — Checkout orchestration + unit tests
- **Tasks:** Implement `POST /baskets/{basketId}/checkout` in `Basket.Api` per spec §5: load basket from Redis; for each line, call Catalog.Api for price/name then decrement stock, aborting with no order/no basket-clear on any failure (no compensation, per spec §7); on full success, call Ordering.Api to create the order, then clear the basket and return `{ orderId }`. Add unit tests per spec §8 for the orchestration logic, mocking the Catalog.Api and Ordering.Api HTTP calls.
- **Projects touched:** `Basket.Api`; its test project.
- **Verify:** Unit tests pass (happy path + insufficient-stock-aborts path). Aspire run + HTTP check: checking out a basket with sufficient stock returns an `orderId`, basket is empty afterward, order is fetchable from `Ordering.Api`, and `Catalog.Api` stock is reduced. Checking out with insufficient stock returns an error, basket is untouched, and no order was created. Traces show the full `Basket.Api → Catalog.Api` + `Basket.Api → Ordering.Api` chain for one checkout call.

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
| `EShopClaude.AppHost` | 0.1, 0.2(ref only), 0.3, 1.1, 1.2, 1.3, 1.4, 1.5, 3.1(maybe) |
| `Catalog.Api` | 0.2, 1.3, 1.6, 2.1, 2.2 |
| `Ordering.Api` | 0.2, 1.4, 2.3 |
| `Basket.Api` | 0.2, 1.5, 2.4, 3.1, 3.2, 3.3 |
| `EShopClaude.Web` | 0.1, 4.1, 4.2, 4.3, 4.4 |
| `EShopClaude.Tests` / per-service test projects | 0.1, 2.2, 2.3, 2.4, 3.3 |

## Review focus (spec requirements most likely to be missed if skipped)

- **Insufficient-stock checkout leaves no order and an untouched basket** (spec §5 step 2, §7) — covered by Step 3.3's unit test and Step 4.3's browser check.
- **No stock compensation/rollback on partial multi-item failure** (spec §7) — explicitly a non-requirement; don't accidentally add rollback logic in Step 3.3.
- **No API versioning/pagination/filtering on `/products`** (spec §7) — keep Step 2.1 to the plain full-list endpoint.
- **Basket id scoped to the Blazor circuit, not cookies/storage** (spec §4) — covered by Step 4.1's two-session browser check.
- **No order list/history beyond the single just-placed order** (spec §7) — keep Step 4.4 to single-order lookup by id only, no listing endpoint/page.
