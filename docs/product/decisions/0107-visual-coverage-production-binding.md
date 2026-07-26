# ADR-0107: visual coverage starts from production inventory and ends at production rendering

Status: Accepted

## Context

The v1 element-visual catalog remains a useful design artifact, but it cannot prove its own
completeness. `Catalog.validate` derives the declared set from the catalog rows, so deleting a row
deletes both the evidence and the subject. A nonblank `shown` payload also proves only that a string was
typed, not that production can resolve the handle or that the real view exercises it.

Rogue2 exposed all three gaps: doors and trapdoors affected gameplay but were absent from the catalog
and renderer, while stale Pong rows remained and the self-validating gate stayed green.

## Decision

Visual coverage now uses independent witnesses in this order:

1. The product owns a non-empty, typed gameplay-visual inventory in production source.
2. The v1 catalog remains the persisted disposition format and is compared to that inventory with
   `Catalog.audit`; it no longer supplies the subject set.
3. Every `shown` handle must resolve through a product-owned registry consumed by the production view.
4. Every binding declares named required states. The gate exports each element's scene separately,
   rejects byte-identical required states for that element, and rejects byte-identical baseline scenes
   across distinct elements. It also traverses the aggregate production view. Isolation galleries and
   aggregate-only frame differences are not production-binding evidence.
5. Hidden rows use `<mechanic>: <explanation>`. Blank or generic labels such as `scenery`, `other`, or
   `not shown` are `UnsupportedHidden`.
6. A fresh-context critic reviews the inventory, catalog, production projection, required states, and
   candidate evidence at the exact commit proposed for landing. Its verdict lives outside the authored
   tree as an independently attributable PR review or equivalent immutable review-system receipt.
   In-repo JSON, author-entered identity, and same-context fallback cannot attest independence.
7. Landing requires both a complete mechanical report and a clean external critic. Critic approval
   cannot override mechanical `Missing`, `Stale`, `Unbound`, `Unobserved`, or `UnsupportedHidden`, and
   mechanical completeness cannot manufacture an independent review.

The scaffold's `GameplayVisualInventory.project` is the route its real `View.view` consumes. The game
coverage gate derives observed handles from that same projection over representative state.

## Compatibility and rollout

The `# fs-gg element-visual catalog v1` syntax and `Catalog.parse`, `render`, `coverage`, and `validate`
semantics are preserved. The stronger `audit` API is additive. Existing consumers may keep
parsing v1 catalogs, but a game with no production inventory receives an explicit `EmptyInventory`
result and cannot claim complete production-bound coverage.

Rollout order is producer first: merge and publish the coherent FS.GG.UI package/template set, then
advance generated consumers. The template pins the coherent version carrying `Catalog.audit` before its
generated gate calls that API. Consumer migration consists of adding a production inventory/registry,
wiring the view through its projection, replacing stale catalog rows, declaring per-element required
states, and obtaining the external exact-commit critic review.

## Consequences

Catalog deletion can no longer hide a gameplay element. Orphan handles and catalogued-but-never-rendered
visuals are distinct findings. Stale starter rows are visible. The critic adds an independent semantic
check without becoming an oracle or manufacturing missing machine evidence.
