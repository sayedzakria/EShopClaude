# EShopClaude — Project Document

## 1. Overview

EShopClaude is a minimal e-commerce demo built on top of the .NET Aspire starter solution in `src/`. Its purpose is to demonstrate agentic coding with microservices in a minimalist way: small scope, few tokens, fast delivery. It is not a production e-commerce system — there is no login, identity, payments, or deployment story.

The demo supports exactly three user journeys: browse products, add them to a cart, and check out to produce an order.

## 2. Architecture

- **Services**: three ASP.NET Core minimal API microservices — `Catalog.Api`, `Basket.Api`, `Ordering.Api` — plus one Blazor Server storefront (`EShopClaude.Web`). All orchestrated by `EShopClaude.AppHost`.
- **Databases**: one PostgreSQL server hosting two separate databases — `catalogdb` (owned by Catalog.Api) and `orderingdb` (owned by Ordering.Api). `Basket.Api` stores baskets in Redis — no relational database. The Postgres server resource also runs pgAdmin (via Aspire's `WithPgAdmin()`), giving a web UI to inspect both databases during development.
- **Communication**: synchronous HTTP only, via Aspire service discovery (the same `AddProject<...>().WithReference(...).WaitFor(...)` pattern already used in `AppHost.cs` for `apiservice`/`webfrontend`). No API gateway, no message broker, no event-driven communication.
- **Resilience**: only the Aspire defaults from `ServiceDefaults` (standard HTTP resilience handler, service discovery, OpenTelemetry). No custom retries, circuit breakers, or fallback logic anywhere in the system.

```
                 ┌──────────────┐
                 │  EShopClaude │
                 │     .Web      │  (Blazor Server storefront)
                 └──────┬───────┘
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐
  │ Catalog.Api │ │ Basket.Api  │ │ Ordering.Api │
  └──────┬──────┘ └──────┬──────┘ └──────┬───────┘
         │               │               │
         ▼               ▼               ▼
  ┌─────────────┐   ┌─────────┐   ┌───────────────┐
  │  Postgres   │   │  Redis  │   │   Postgres    │
  │ catalogdb   │   │ baskets │   │  orderingdb   │
  └──────┬──────┘   └─────────┘   └───────┬───────┘
         └───────────────┬────────────────┘
                   ┌──────┴──────┐
                   │   pgAdmin   │  (inspect both databases)
                   └─────────────┘

 Basket.Api also calls Catalog.Api (price/stock) and
 Ordering.Api (create order) synchronously during checkout.
```

## 3. Data ownership & API surface

### Catalog.Api — owns product data (Postgres `catalogdb`)

**Product**: `Id, Name, Description, Price, Stock`

| Method | Path | Purpose |
|---|---|---|
| GET | `/products` | List all products (no paging/filtering — full list) |
| GET | `/products/{id}` | Get one product |
| POST | `/products/{id}/decrement-stock` | Body `{ quantity }`. Decrements stock. Returns 200 on success, 409/400 if insufficient stock. Called only by Basket.Api during checkout. |

### Basket.Api — owns basket data (Redis)

Stores, per basket, only `{ productId, quantity }` pairs — no cached product name/price. Also **orchestrates checkout**.

**BasketItem**: `ProductId, Quantity`

| Method | Path | Purpose |
|---|---|---|
| GET | `/baskets/{basketId}` | Get current basket (list of productId + quantity) |
| POST | `/baskets/{basketId}/items` | Body `{ productId, quantity }`. Add item, or increment quantity if already present |
| PUT | `/baskets/{basketId}/items/{productId}` | Body `{ quantity }`. Set exact quantity for a line |
| DELETE | `/baskets/{basketId}/items/{productId}` | Remove one line from the basket |
| DELETE | `/baskets/{basketId}` | Clear the entire basket (also called internally after a successful checkout) |
| POST | `/baskets/{basketId}/checkout` | Runs the checkout flow (§5). Returns `{ orderId }` on success, or an error if any item has insufficient stock |

### Ordering.Api — owns order data (Postgres `orderingdb`)

**Order**: `Id, Items[{ ProductId, ProductName, UnitPrice, Quantity }], Total, Status, CreatedDate`

Items are a denormalized snapshot captured at order-creation time, so the order confirmation page never needs to call Catalog.Api again. `Status` is always `"Placed"` (no fulfillment/payment states). No customer/user field, since there is no login.

| Method | Path | Purpose |
|---|---|---|
| POST | `/orders` | Body `{ items: [{ productId, productName, unitPrice, quantity }] }`. Computes `Total`, sets `Status = "Placed"`, `CreatedDate = now`. Returns the created order (with `Id`). Called only by Basket.Api during checkout. |
| GET | `/orders/{id}` | Get order detail, for the confirmation page |

## 4. Basket identity (no login)

The storefront is Blazor Server, so basket identity is implemented as a **scoped DI service** tied to the user's Blazor circuit: the first time a page needs a basket ID, the service lazily generates a new GUID and holds it for the lifetime of that circuit. No cookies, no browser storage, no JS interop.

- The basket ID is stable across `/products`, `/cart`, and `/orders/{id}` navigation within one browsing session.
- It is lost when the browser tab is closed or the Blazor circuit otherwise resets — there is no persistence beyond that, which is acceptable since there is no login to tie a basket to.

## 5. Checkout flow

Checkout is owned and orchestrated entirely by `Basket.Api`, triggered by a single call from the storefront (`POST /baskets/{basketId}/checkout`):

1. Basket.Api loads the basket (`productId` + `quantity` list) from Redis.
2. For each line item, Basket.Api calls `Catalog.Api`:
   - `GET /products/{id}` to get the current name and price.
   - `POST /products/{id}/decrement-stock` to reserve the quantity.
   - If any decrement fails (insufficient stock), Basket.Api **aborts** the checkout and returns an error. The order is **not** created and the basket is **not** cleared. Items already decremented earlier in the loop are left decremented — there is no compensation/rollback logic (deliberately out of scope for this demo; the one accepted inconsistency in an otherwise happy-path system).
3. If all items succeed, Basket.Api calls `Ordering.Api`: `POST /orders` with the enriched line items (name, price, quantity captured in step 2).
4. On success, Basket.Api clears the basket (`DELETE /baskets/{basketId}`) and returns `{ orderId }` to the storefront.
5. The storefront redirects to `/orders/{orderId}` to show the confirmation page.

No retries, circuit breakers, or fallback behavior are added beyond the Aspire-default HTTP resilience handler.

## 6. Storefront pages & user journey

Three pages, linear journey: **Products → Cart → Order confirmation**.

- **`/products`** — Lists all products from `Catalog.Api` (`GET /products`). Each product has an "Add to cart" action, which calls `Basket.Api` (`POST /baskets/{basketId}/items`) using the circuit-scoped basket ID.
- **`/cart`** — Loads the basket from `Basket.Api` (`GET /baskets/{basketId}`), then enriches each line by calling `Catalog.Api` (`GET /products/{id}`) for name/price/stock to render. Supports:
  - Changing a line's quantity (`PUT /baskets/{basketId}/items/{productId}`)
  - Removing a line (`DELETE /baskets/{basketId}/items/{productId}`)
  - Shows the computed total
  - "Checkout" button calls `POST /baskets/{basketId}/checkout`; on success, navigates to `/orders/{orderId}`; on failure (insufficient stock), shows a plain inline error message and leaves the cart as-is.
- **`/orders/{id}`** — Order confirmation page. Fetches the order from `Ordering.Api` (`GET /orders/{id}`) and displays items, quantities, prices, total, status, and created date.

No persistent cart badge/count is shown in the nav — kept minimal.

## 7. Explicitly out of scope

Already decided before this document:
- Login / identity
- Payments
- Deployment

Decided while scoping this document:
- **Order history / order list** — only the single just-placed order is viewable (by the id returned from checkout). No "my orders" list, since there is no login to scope it to.
- **API versioning, pagination, or filtering on Catalog.Api** — the product list endpoint always returns the full set.
- **Stock compensation/rollback** — if checkout fails partway through decrementing stock for multiple items, already-decremented items are not restored (see §5, step 2).
- **Retries, circuit breakers, fallbacks** beyond the Aspire-default resilience handler.

## 8. Testing approach

Unit tests only, per service, using mocked/in-memory dependencies:
- **Catalog.Api**: stock decrement logic (success and insufficient-stock cases).
- **Basket.Api**: basket item operations (add/increment, update quantity, remove, clear) and the checkout orchestration logic (mocking the Catalog.Api and Ordering.Api HTTP calls).
- **Ordering.Api**: order creation and total calculation.

No Aspire testing host, and no real Postgres/Redis containers are spun up for tests — these stay out of scope for this demo.

## 9. Implementation notes (for later work, not part of this document's scope)

- `EShopClaude.ApiService` (the generic weather sample) and the `Weather`/`Counter` pages in `EShopClaude.Web` are not part of the product and will be removed/replaced when implementation starts.
- `AppHost.cs` will need `Aspire.Hosting.PostgreSQL` (one server resource with `.WithPgAdmin()`, two database resources: `catalogdb`, `orderingdb`) and `Aspire.Hosting.Redis` package references, plus resource registrations for `Catalog.Api`, `Basket.Api`, and `Ordering.Api` following the existing `AddProject<...>().WithReference(...).WaitFor(...)` pattern.
