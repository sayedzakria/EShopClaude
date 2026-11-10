# Ordering.Api — Spec 02: Create order

**Status:** Not started

## Goal

Let `Basket.Api` place an order at the end of checkout. Creating the order and calculating its total stay in one spec because the total is only observable on the created order.

## Context

- Spec `ordering-api/01-order-schema` is merged: `Ordering.Api` has the `Order` entity (`Id, Total, Status, CreatedDate, Items`), the `OrderItem` entity (`Id, OrderId, ProductId, ProductName, UnitPrice, Quantity`), `OrderingDbContext` and `AddNpgsqlDbContext<OrderingDbContext>("orderingdb")`. The tables `Order` and `OrderItem` are created with `EnsureCreated` at startup.
- `Order.Status` and `Order.CreatedDate` have no default: this spec sets them. `CreatedDate` is stored as `timestamp with time zone`, so the value must be UTC.
- `Program.cs` maps only `/` ("Ordering API is running.") and the default endpoints. `POST /orders` is not mapped, so it returns 404 (or 405).
- `AppHost.cs` already registers `orderingapi` with `WithReference(orderingdb).WaitFor(orderingdb)`.
- There is no test project for `Ordering.Api`. `EShopClaude.Tests` has no test files and references only `EShopClaude.Web`.
- Precedent: `catalog-api/05-decrement-stock` puts the logic in a static class (`ProductStock`), returns a bare 400 for invalid input, and tests it in `EShopClaude.Catalog.Api.Tests` (xUnit, SQLite in-memory via `Microsoft.EntityFrameworkCore.Sqlite`).

## Depends on

- `ordering-api/01-order-schema`

## Scope

- [ ] `POST /orders` with body `{ items: [{ productId, productName, unitPrice, quantity }] }`
- [ ] Computes `Total` as the sum of `unitPrice * quantity` over all items, sets `Status = "Placed"` and `CreatedDate` to the current UTC time
- [ ] Saves the order and its items in one `SaveChanges`
- [ ] Returns 200 with the created order: `{ id, items: [{ productId, productName, unitPrice, quantity }], total, status, createdDate }`. The item `Id` and `OrderId` columns are not exposed
- [ ] `items` missing, null or empty returns 400; any item with `quantity` below 1 or `unitPrice` below 0 returns 400. In every 400 case nothing is saved. The 400 has no body, as in Catalog
- [ ] Create the `Ordering.Api` test project (`EShopClaude.Ordering.Api.Tests`); unit tests for order creation and total calculation, with an in-memory store (no real Postgres)

## Explicit non-goals

- No `GET /orders/{id}`
- No order list or history
- No payment, customer or other status values
- No call to another service
- No change to the schema, `AppHost.cs`, `OrderingDbContext.cs`, `Order.cs` or `OrderItem.cs`
- No validation of `productName` (blank or longer than 200 characters) and no check that `unitPrice` has at most 2 decimals
- No custom error body
- No `Location` header: `GET /orders/{id}` does not exist until spec 03

## Data model

n/a. No schema change; the endpoint inserts one row into `Order` and one row per item into `OrderItem`, using the existing tables.

Request and response shapes (JSON property names are camelCase, the ASP.NET Core default):

| Shape | Fields |
|---|---|
| Request | `items`: list of `{ productId (int), productName (string), unitPrice (decimal), quantity (int) }` |
| Response | `id` (int), `items`: list of `{ productId, productName, unitPrice, quantity }`, `total` (decimal), `status` (string, always `"Placed"`), `createdDate` (UTC date-time) |

## Tasks

1. Add `OrderContracts.cs` to `Ordering.Api` with the request records (`CreateOrderRequest` with a nullable `Items` list, `OrderItemRequest`) and the response records (`OrderResponse`, `OrderItemResponse`), plus a way to map an `Order` to an `OrderResponse` so spec 03 can reuse it.
2. Add `OrderCreation.cs` to `Ordering.Api` with a static `CreateAsync(db, request)`. It returns no order (null) when `items` is null or empty, or when any item has `quantity` below 1 or `unitPrice` below 0. Otherwise it builds the `Order` (`Status = "Placed"`, `CreatedDate = DateTime.UtcNow`, `Total` = sum of `UnitPrice * Quantity`, one `OrderItem` per request item), adds it to the DbContext, calls `SaveChangesAsync` once and returns the saved order.
3. Map `POST /orders` in `Program.cs`: call `OrderCreation.CreateAsync`, return 400 when it returns null, otherwise 200 with the `OrderResponse`.
4. Create `src/EShopClaude.Ordering.Api.Tests` (xUnit, net10.0, SQLite in-memory via `Microsoft.EntityFrameworkCore.Sqlite`, project reference to `Ordering.Api`, same package versions as `EShopClaude.Catalog.Api.Tests`) and add it to `EShopClaude.sln`.
5. Write unit tests: the total is the sum of `unitPrice * quantity` over several items; a single item gives its own line total; `Status` is `"Placed"`; `CreatedDate` is UTC and within a few seconds of now; the saved order and all its items can be read back from the store with the request values; an order with an empty items list, a `quantity` of 0 and a negative `unitPrice` each return null and leave the store with no orders.
6. Build, run the tests, then run every check in the Definition of Done.

## Files touched

| File | Action |
|---|---|
| `src/EShopClaude.Ordering.Api/OrderContracts.cs` | new |
| `src/EShopClaude.Ordering.Api/OrderCreation.cs` | new |
| `src/EShopClaude.Ordering.Api/Program.cs` | modify |
| `src/EShopClaude.Ordering.Api.Tests/EShopClaude.Ordering.Api.Tests.csproj` | new |
| `src/EShopClaude.Ordering.Api.Tests/OrderCreationTests.cs` | new |
| `src/EShopClaude.sln` | modify |

Not touched: `AppHost.cs`, the `Ordering.Api` `.csproj`, `Order.cs`, `OrderItem.cs`, `OrderingDbContext.cs`, `EShopClaude.Tests`, the other services. `microservices/README.md` only gets the status change.

## Definition of Done

Run from `src/` unless stated otherwise. `<orderingapi-url>` is the `orderingapi` endpoint link in the Aspire dashboard.

1. `dotnet build EShopClaude.sln` exits 0 with 0 errors.
2. `dotnet test EShopClaude.sln` exits 0, and `dotnet test EShopClaude.Ordering.Api.Tests/EShopClaude.Ordering.Api.Tests.csproj` reports at least 6 passing tests and 0 failed.
3. `aspire run`: in the dashboard Resources view, `postgres`, `pgadmin`, `catalogdb`, `orderingdb`, `redis`, `catalogapi`, `basketapi`, `orderingapi` and `webfrontend` all show Running/Healthy.
4. Dashboard `orderingapi` console logs and structured logs filtered to Error: no entries.
5. `curl -i -X POST <orderingapi-url>/orders -H "Content-Type: application/json" -d '{"items":[{"productId":1,"productName":"Widget","unitPrice":10.50,"quantity":2},{"productId":2,"productName":"Gadget","unitPrice":3.25,"quantity":4}]}'` returns `200` with a JSON body where `id` is an integer, `total` is `34` (21.00 + 13.00), `status` is `"Placed"`, `createdDate` is a UTC timestamp close to now, and `items` has the two lines with the values sent and no item `id` or `orderId`.
6. pgAdmin Query Tool on `orderingdb`: `SELECT "Id", "Total", "Status", "CreatedDate" FROM "Order";` returns exactly 1 row matching the response of check 5, and `SELECT "OrderId", "ProductId", "ProductName", "UnitPrice", "Quantity" FROM "OrderItem" ORDER BY "ProductId";` returns exactly 2 rows with the values sent, both with `OrderId` equal to that order's `Id`.
7. `curl -i -X POST <orderingapi-url>/orders -H "Content-Type: application/json" -d '{"items":[{"productId":3,"productName":"Gizmo","unitPrice":5.00,"quantity":1}]}'` returns `200` with a new `id` different from check 5 and `total` `5`.
8. Each of these returns `400`: `-d '{}'`, `-d '{"items":[]}'`, `-d '{"items":[{"productId":1,"productName":"Widget","unitPrice":10.50,"quantity":0}]}'`, `-d '{"items":[{"productId":1,"productName":"Widget","unitPrice":-1,"quantity":1}]}'` (same `curl -i -X POST <orderingapi-url>/orders -H "Content-Type: application/json"` prefix).
9. After checks 5 to 8, in pgAdmin: `SELECT (SELECT count(*) FROM "Order") AS orders, (SELECT count(*) FROM "OrderItem") AS items;` returns `2` and `3` (the 400 requests saved nothing).
10. Non-goal check: `curl -i <orderingapi-url>/orders/1` returns 404 (or 405).
11. Regression: `curl -i <orderingapi-url>/` returns 200 with `Ordering API is running.`, and `curl -i <orderingapi-url>/health` returns 200 with body `Healthy`.
12. From the repo root, after implementation: `git diff --name-only main` lists only the paths under Files touched, plus this spec file and `microservices/README.md`.

## References

- `_agenticdev/PROJECT.md` — §3 Ordering.Api (`Order` data, `POST /orders`), §5 step 3 (checkout places the order), §8 Testing approach (Ordering.Api order creation and total calculation).
- `_agenticdev/PLAN.md` — Phase 2, Ordering.Api (spec list).
- Precedent: `catalog-api/05-decrement-stock`

