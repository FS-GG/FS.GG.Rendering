# Phase 0 — Research: Memoization Seam (DataGrid) (Feature 113)

Conformance backfill — recovers the design the imported code embodies. No open `NEEDS CLARIFICATION`.
Reconstructed from `RetainedRender.fsi`/`.fs`, `ControlsElmish.fsi`, `Diagnostics`, and the four suites.

## Decision 1 — Memoize keyed by stable ControlId + a structural dependency

- **Decision**: `memoize id dependency compute cache` returns `(subtree, cache', outcome)`: a `Hit` (same id,
  equal dependency) returns the stored `Subtree` instance without running `compute`; a `Miss` (cold/changed)
  runs `compute` once and stores it. Keyed by stable `ControlId`.
- **Rationale**: The DataGrid projection is the expensive recompute; keying by the control's stable id + the
  structural inputs `(theme, box, cells)` lets an unchanged grid reuse last frame's subtree verbatim.
- **Alternatives considered**: Keying by object identity of the inputs — rejected: MVU rebuilds inputs every
  frame, so identity would never Hit; structural equality (Decision 2) is required.

## Decision 2 — Structural equality, never object identity

- **Decision**: The dependency is compared with F# structural `=`, so two equal-but-distinct boxed values
  `Hit` (FR-005).
- **Rationale**: MVU produces fresh-but-equal values each frame; only structural equality yields real Hits.
  This is the load-bearing correctness choice.
- **Alternatives considered**: Reference equality — rejected (would never Hit under MVU).

## Decision 3 — An always-miss oracle proves memo-on ≡ memo-off

- **Decision**: `MemoEnabled = false` makes the seam behave as if every call were a Miss (scene-wise),
  proving the rendered scene is byte-identical with the seam disabled (FR-006/FR-008/SC-002).
- **Rationale**: A cache must be invisible to output. The oracle is the parity counterfactual the tests
  compare against. **Note (E2 finding):** the disabled path actually *bypasses* `memoize` via `&&`
  short-circuit, so both counters stay 0/0 — the doc-comment's "force every memoize call down the Miss path"
  overstates this. Behaviour correct; narrative routed to E2.
- **Alternatives considered**: Removing the seam to test parity — rejected: the oracle keeps parity a checked
  in-tree invariant.

## Decision 4 — Surface reuse as public metrics; both 0 when nothing is memoizable

- **Decision**: `MemoHits`/`MemoMisses` → public `FrameMetrics.MemoHitCount`/`MemoMissCount`; both 0 on a
  frame with no memoizable control or an idle frame.
- **Rationale**: Reuse must be measurable and honest. 0/0 when there is nothing to memoize prevents
  false-positive "it's working" readings.
- **Alternatives considered**: Internal-only counters — rejected: steady-state hit accrual (SC-004) is a
  consumer-visible property.

## Decision 5 — Advisory stability diagnostic for reuse-breaking inputs

- **Decision**: `Diagnostics.stabilityReport` flags per-frame event closures, always-new attribute values,
  and unstable keys as `UnstableReuseInput`; a structurally-equal rebuild reports nothing.
- **Rationale**: Memoization silently degrades to all-Misses if inputs are unstable; an advisory report makes
  that authoring mistake visible without enforcing a hard failure.
- **Alternatives considered**: Hard-failing on unstable inputs — rejected: too strict; advisory matches the
  "observability, not enforcement" intent.

## Renderer-mode / evidence honesty

All proofs are deterministic and headless (Hit/Miss outcomes, scene byte-equality, metric regimes, diagnostic
findings). Readiness (authored in `/speckit-implement`, since 113 imported without it) makes no pixel/desktop
claim — consistent with the prior backfills.
</content>
