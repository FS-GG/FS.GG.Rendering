# Contract: US5 — Tame the `RetainedRender.step` god-function

**Target**: `src/Controls/RetainedRender.fs` (2,087 lines; `init` @1254, `step` @1424 ~600 lines, ~30
`let mutable` accumulators, 8 nested recursive walks). **Package**: `FS.GG.UI.Controls`. Inherits all
of [surface-invariance.md](./surface-invariance.md). `StepMetrics` shape: see
[../data-model.md](../data-model.md).

## C-RR-1 — `StepMetrics` record + named passes (FR-007)

`step`'s ~30 ad-hoc `let mutable` accumulators are replaced by a single `StepMetrics` record threaded
through **named passes** (each pass pulled into its own function). `StepMetrics` is **internal** —
absent from `Control.fsi`/`RetainedRender.fsi` and from `FS.GG.UI.Controls.txt` (the surface diff
confirms). The goal is named passes over a typed accumulator, **not** dogmatic immutability.

## C-RR-2 — Unify init/step scaffold (FR-007)

The build/paint scaffolding `step` (@1424) duplicates with `init` (@1254) is unified so neither
duplicates the other; both produce unchanged results.

## C-RR-3 — Mutation retained on the hot path (Constitution III)

Mutation MAY be retained where it is the simpler/faster code on this measured render hot path, each use
disclosed with a one-line `// mutable: hot path` comment. No new per-frame allocation is introduced by
the record threading.

## C-RR-4 — Byte-stable render output (FR-003)

Rendered scene, damage regions, metrics, and promotion decisions are byte-identical to baseline for
every covered scenario.

## Acceptance (maps to spec US5)

1. Extracted `StepMetrics` + named passes: rendered output, damage regions, metrics byte-identical
   (C-RR-1, C-RR-4).
2. Unified scaffold: `init` and `step` no longer duplicate each other; both unchanged (C-RR-2).

## Validation

Build `FS.GG.UI.Controls`; run the retained-render + damage-locality suites; byte-diff rendered scene /
damage regions / step metrics / promotion decisions vs baseline; `scripts/refresh-surface-baselines.fsx`
→ empty `FS.GG.UI.Controls.txt` diff (quickstart Step 1, row US5).

> **Coordination**: US2 also touches `src/Controls/`. Serialize US2 and US5 for a clean per-story
> `FS.GG.UI.Controls.txt` diff.

## Implementation Outcome (2026-06-21) — RETAINED per FR-009 (C-SI-6)

**Retained (not restructured), after inspection.** `step` (`RetainedRender.fs:1424`–2031) is not a clean
~30-accumulator-into-record transform: its 18 `let mutable` accumulators are **deeply entangled with
~15 other derived locals** (`remeasured`, `repaintedNodeCount`, `dirtyRectCount`, `dirtyArea`,
`pictureEntryCount`, `replayCacheNativeBytes`, `avoidedContentWork`, `netSavedWork`, `promotionOverhead`,
`baselineNodeCount`, …) across **8 nested recursive walks + closures**, all feeding **one conditional
`WorkReduction` assembly** (`:1993`–2030) where promotion/demotion/replay fields are *derived* from the
accumulators, not stored. Promoting only the 18 to a `StepMetrics` record leaves a mixed record/local
access pattern that **reduces** legibility, and any reordering on this **measured render hot path** risks
undetected byte-drift in rendered scene / damage regions / metrics / promotion decisions. Mutation
retention is explicitly blessed here (C-RR-3). Per **byte-stable output wins** (FR-002/003) and the
maintainer's tractable-stories scope decision, `step` is **left in its current form**; the FR-007
init/step scaffold unification (C-RR-2) is likewise retained. `FS.GG.UI.Controls.txt` unchanged (no
StepMetrics surface). Recorded SC-005 function-size exception; SC-006 (init/step) retained-with-reason.
