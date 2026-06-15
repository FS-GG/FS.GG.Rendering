# Phase 0 — Research: Retained Pointer Routing → Authored Control ID (Feature 110)

Conformance backfill — "research" recovers the design the imported code embodies. No open
`NEEDS CLARIFICATION`. Reconstructed from `RetainedRender.fsi`/`.fs`, `ControlsElmish.fsi`/`.fs`, and the
three `Feature110*Tests` suites.

## Decision 1 — Reproduce the authored-id climb from retained identity, don't re-render

- **Decision**: `authoredControlIds boundIds retained` walks the retained tree and, for each node, climbs to
  the nearest ancestor (including self) that is keyed (`Key ?? path <> path`) or whose canonical id is in
  `boundIds` — yielding `Map<RetainedId, ControlId>`. This reproduces, from retained identity, exactly the
  climb `Control.nearestAuthored` performs over a freshly-rendered tree (098).
- **Rationale**: The retained frame already holds the structure and identities; re-rendering to re-derive the
  binding is pure waste. Reproducing the *same* climb guarantees dispatch parity (FR-006) without `host.View`.
- **Alternatives considered**: Caching authored bindings on each node at build time — rejected: more state to
  keep in sync; the climb is cheap and the retained tree is already present.

## Decision 2 — Preserve the full-render path as a parity oracle + counted escape hatch

- **Decision**: The old `host.View` + `Control.renderTree` + `nearestAuthored` path is kept, but only as (a)
  the parity oracle the tests compare against and (b) a fallback when a bindable hit cannot be resolved from
  the retained frame — incrementing `FullRenderFallbackCount` by exactly one.
- **Rationale**: Routing must be provably dispatch-identical; keeping the oracle in-tree makes parity a
  checked invariant. The counted fallback makes "did we silently regress to full renders?" observable
  (SC-005) and keeps a correct path for the rare unroutable case.
- **Alternatives considered**: Deleting the full-render path — rejected: loses the parity oracle and the safe
  fallback; a routing bug would then be a silent dispatch error.

## Decision 3 — A pure routing frame runs no view and coalesces moves

- **Decision**: Routing sets `ViewCalled = false` and `FullRenderCount = 0`; move bursts coalesce to ≤ 1
  processed move (the existing `PointerMovesProcessed` infra), and interleaved discretes survive.
- **Rationale**: The win is *not running the view per event*; coalescing keeps drag/freehand fidelity without
  per-sample renders (FR-012). Asserting `ViewCalled = false` is the direct proof the view didn't run.
- **Alternatives considered**: Processing every raw move — rejected: needless work; coalescing is the
  established pointer discipline.

## Decision 4 — Unkeyed same-kind siblings resolve through retained identity

- **Decision**: Two unkeyed same-kind siblings (indistinguishable by kind/key) are distinguished by their
  stable `RetainedId`, so clicking the second fires *its own* binding.
- **Rationale**: Path-derived ids alone can't tell identical siblings apart after a shift; retained identity
  can. This is the correctness case that justifies routing through `RetainedId` rather than path (FR-005).
- **Alternatives considered**: Routing by path index — rejected: breaks under sibling shifts, exactly what
  retained identity fixes.

## Decision 5 — Surface the fallback as a public additive metric

- **Decision**: `FullRenderFallbackCount` is added to the public `FrameMetrics`; `FullRenderCount`/`ViewCalled`
  are narrowed so retained routing increments neither.
- **Rationale**: Consumers/tests need to see that routing isn't silently falling back. The baseline is
  type-granular, so the additive field is zero new surface delta.
- **Alternatives considered**: An internal-only counter — rejected: the no-fallback guarantee (SC-005) is a
  consumer-visible property worth exposing.

## Renderer-mode / evidence honesty

All proofs are deterministic and headless (scripted pointer interactions, message-list and work-count
comparison). The readiness (authored in `/speckit-implement`, since 110 imported without it) does not claim
pixel/desktop visibility — consistent with the prior backfills.
</content>
