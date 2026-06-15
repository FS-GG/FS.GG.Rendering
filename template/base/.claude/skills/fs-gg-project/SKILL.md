---
name: fs-gg-project
description: Work on a generated FS.GG.UI product.
---

# Generated Product

## Scope

Owns product application code, product tests, product docs, readiness evidence,
and selected capability skills copied into this product.

## Public Contract

The product references FS.GG.UI capability packages. Product API contracts
belong in product `.fsi` files when public surfaces are introduced.

## Build Commands

Generated FAKE-backed commands (`./fake.sh`, `fake.cmd`, or `dotnet fake`)
share `.fake` state and are not safe to run concurrently. Run multiple
FAKE-backed commands sequentially:

1. `./fake.sh build -t Dev`
2. `./fake.sh build -t Test`
3. `./fake.sh build -t Verify`

Non-FAKE checks may run in parallel when they do not invoke FAKE or depend on
`.fake`.

## Test Commands

Run `./fake.sh build -t Test` for product tests and selected capability usage checks.

## Evidence

Store product evidence under product readiness paths. Do not copy framework
readiness evidence into the product.

## Package Boundary

Reference selected capability packages. Do not copy framework implementation
projects into consumer-mode products.

## Generated Product

Keep product governance focused on product behavior, generated guidance, drift,
and evidence gates.
