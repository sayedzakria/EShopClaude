# Microservice specs

Status of every spec. Rules are in [`../WOW.md`](../WOW.md), the build order in [`../PLAN.md`](../PLAN.md), the template in [`../templates/spec-template.md`](../templates/spec-template.md).

Suggested build order: catalog 01→05, ordering 01→03, basket 01→05, then basket 06→07 (needs Catalog and Ordering merged). A spec starts only when everything in its "Depends on" is merged to `main`.

| Service | Spec | Branch | Status |
|---|---|---|---|
| catalog-api | [01-product-schema](catalog-api/01-product-schema.md) | `feature/catalog-01-product-schema` | Done |
| catalog-api | [02-seed-data](catalog-api/02-seed-data.md) | `feature/catalog-02-seed-data` | Done |
| catalog-api | [03-list-products](catalog-api/03-list-products.md) | `feature/catalog-03-list-products` | Not started |
| catalog-api | [04-get-product](catalog-api/04-get-product.md) | `feature/catalog-04-get-product` | Not started |
| catalog-api | [05-decrement-stock](catalog-api/05-decrement-stock.md) | `feature/catalog-05-decrement-stock` | Not started |
| ordering-api | [01-order-schema](ordering-api/01-order-schema.md) | `feature/ordering-01-order-schema` | Not started |
| ordering-api | [02-create-order](ordering-api/02-create-order.md) | `feature/ordering-02-create-order` | Not started |
| ordering-api | [03-get-order](ordering-api/03-get-order.md) | `feature/ordering-03-get-order` | Not started |
| basket-api | [01-get-basket](basket-api/01-get-basket.md) | `feature/basket-01-get-basket` | Not started |
| basket-api | [02-add-item](basket-api/02-add-item.md) | `feature/basket-02-add-item` | Not started |
| basket-api | [03-update-item-quantity](basket-api/03-update-item-quantity.md) | `feature/basket-03-update-item-quantity` | Not started |
| basket-api | [04-remove-item](basket-api/04-remove-item.md) | `feature/basket-04-remove-item` | Not started |
| basket-api | [05-clear-basket](basket-api/05-clear-basket.md) | `feature/basket-05-clear-basket` | Not started |
| basket-api | [06-checkout-reserve-stock](basket-api/06-checkout-reserve-stock.md) | `feature/basket-06-checkout-reserve-stock` | Not started |
| basket-api | [07-checkout-place-order](basket-api/07-checkout-place-order.md) | `feature/basket-07-checkout-place-order` | Not started |
