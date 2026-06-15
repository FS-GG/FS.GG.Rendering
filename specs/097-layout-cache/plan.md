# Implementation Plan: Layout Cache — Incremental Re-Measure (Feature 097)

**Branch**: `097-layout-cache` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/097-layout-cache/spec.md`

## Summary

Feature 091 wired the keyed reconciler onto the render path so a `RetainedRender.step` recomputes only the
**changed paint subtree** instead of rebuilding the whole scene — but 091's work reduction covered *paint*,
not *measure*: every `step` still ran a full `Layout.evaluate` over the whole tree, so a one-pixel width
change to a single leaf re-measured every node on screen.

Feature 097 (the "R2" accretion) **adds the measure-side cache**: `RetainedRender` carries the previous
frame's full `LayoutResult` frame to frame as a per-frame **measure/bounds cache** (the `Layout` field);
each `step` derives a **layout-dirty set** from the reconcile patch (`layoutDirtySet` — a node is dirty iff
its `Update` touches an `AttrCategory.Layout` attribute, a geometry-driving name in
`ControlInternals.layoutAffectingAttrNames`, or a non-`Keep` child op; a `Replace` re-measures fresh) and
feeds both into `Layout.evaluateIncremental`, which re-measures only that set — conservatively propagated up
to each dirty node's first **fixed-size ancestor** — and **reuses the cached bounds** for everything else.
The load-bearing guarantee is **equivalence**: the incremental result is **byte-identical** to a full
`Layout.evaluate` of the same frame (INV-1), so the cache changes only *how much work* was done, never
*what was produced*. The saving is surfaced honestly as `RemeasuredNodeCount` (post-propagation set
re-measured) and `LayoutInvalidatedNodeCount` (pre-propagation patch-derived dirty-set size), with
`LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` always.

**This is a backfill plan** (task **C2** of the 2026-06-15 missing-features plan, following the 091
pattern and the 092 / 093 / 095 / 096 / 099 closes). The implementation (`layoutDirtySet` + the `step`
wiring in `src/Controls/RetainedRender.fs`/`.fsi`; the `Layout.evaluateIncremental` evaluator and its
`Invalidated` report in `src/Layout/`), and the authoritative Expecto/FsCheck suites
(`tests/Layout.Tests/Feature097IncrementalTests.fs`, `tests/Layout.Tests/Audit_IncrementalLayout.fs`,
`tests/Controls.Tests/Feature097WiringTests.fs`) **already exist** in the imported, rebranded source.
Unlike 092/099, feature 097 arrived with **no `readiness/` evidence** — authoring it is part of this
backfill. The plan's job is to bring the work under the canonical `Spec → .fsi → semantic tests →
implementation` contract: document the design decisions the code already embodies, confirm the
constitution gates the artifacts satisfy, and record the import-before-spec deviation. No new product
behavior is designed; `/speckit-tasks` and `/speckit-implement` reduce to a **conformance pass** (confirm
the suites are green, author the readiness evidence, and confirm zero new public-surface delta), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: Expecto + FsCheck (property/semantic tests); the public `FS.GG.UI.Layout` package
(`evaluate`, `evaluateIncremental`, `LayoutResult`/`ComputedBounds`/`AvailableSpace`); the `Controls`
package's reconciler (`Reconcile.diff` / `NodePatch`), `Control.renderTree` (the full-rebuild parity
oracle), and the geometry-driving name set `ControlInternals.layoutAffectingAttrNames` (feature 101). No
new runtime or package dependency — 097 is an internal wiring of code already present plus an
already-public Layout evaluator.

**Storage**: N/A. The `LayoutResult` cache rides the retained render record carried frame-to-frame in the
host loop's mutable-ref state (the interpreter edge); nothing is persisted.

**Testing**: Default-tier "Local inner loop" across **three** in-tree suites:
- `tests/Layout.Tests/Feature097IncrementalTests.fs` — the **pure incremental evaluator**: the byte-identity
  invariant over a fixed-size boundary (SC-001), the ≥1000-case FsCheck equivalence over generated
  `(tree, edit-sequence)` cases (SC-002), the empty-dirty-set at-rest case (SC-006), and the partial-vs-full
  re-measure subset property (SC-001/SC-004) — against the public `FS.GG.UI.Layout` package.
- `tests/Layout.Tests/Audit_IncrementalLayout.fs` — the audit cross-check of the incremental evaluator
  (the spec-006 mechanism-audit lineage; equivalence + honest `Invalidated`).
- `tests/Controls.Tests/Feature097WiringTests.fs` — the **live wired `step` path** (vertical slice): the
  `RemeasuredNodeCount` metric honesty (FR-006/SC-003), the patch-derived dirty set
  (FR-003/SC-004 — content-only does not dirty measure; a child op does), and byte-identity of the wired
  render vs a full `Control.renderTree` for localized / geometry / child-insert / content-only / at-rest
  frames (FR-008/SC-005), reaching the internal wiring via `InternalsVisibleTo`.
Deterministic/offscreen — no GL context required.

**Target Platform**: Linux/dev. 097's proofs are deterministic and headless (bounds-map equality, structural
scene equality, re-measure-count invariants), independent of the GPU.

**Project Type**: F# UI framework — the public `FS.GG.UI.Layout` evaluator plus an internal wiring inside the
`Controls` runtime library, exercised by the `Layout.Tests` and `Controls.Tests` suites.

**Performance Goals**: No wall-clock target. The measurable goals are correctness/work-count invariants: a
localized edit under a fixed-size ancestor re-measures a strict subset (SC-001/SC-003); the incremental
result is byte-identical to a full evaluate across ≥1000 FsCheck cases (SC-002); a non-geometry change
re-measures nothing (SC-004); the wired render is byte-identical to a full rebuild (SC-005); a whole-tree
relayout re-measures the baseline without under-claiming (SC-003/FR-010).

**Constraints**:
- **Zero new public-surface-baseline delta** (FR-011). The `Layout.evaluateIncremental` evaluator and the
  `LayoutResult` shape were already in the committed baseline (`tests/surface-baselines/FS.GG.UI.Layout.txt`,
  which is type-granular); the wiring (`layoutDirtySet`, the `RetainedRender.Layout` field, the two
  `WorkReductionRecord` metrics) stays **internal**. The surface-drift check must pass byte-unchanged for
  `FS.GG.UI.Layout` and `FS.GG.UI.Controls`.
- The incremental evaluator MUST be **total** and **deterministic** — no wall-clock, no `Math.random`; the
  same `(previous, dirty set, frame)` always yields the same bounds and the same `Invalidated`, with
  `Revision` advancing by exactly 1 (FR-009, Principle VI).
- **Equivalence (INV-1)** is non-negotiable: the incremental bounds MUST equal a full `evaluate`
  byte-for-byte for any tree and any edit sequence (FR-007). A measure cache that can diverge is worse than
  none.
- `RemeasuredNodeCount` / `LayoutInvalidatedNodeCount` MUST be **honest** — strict subset for a localized
  edit, baseline for a whole-tree relayout, 0 at rest, with `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`
  always (FR-006/FR-010).
- `layoutDirtySet` reads the lock-step name set `ControlInternals.layoutAffectingAttrNames` (feature 101);
  097 **consumes** it and must stay in lock-step via the existing `Feature101LayoutDriftGuardTests` gate —
  it does not own or modify that set.

**Scale/Scope**: One internal wiring across `RetainedRender` (the dirty-set derivation + the incremental
`step` call + the two metrics) plus the public `Layout.evaluateIncremental` evaluator. **097-in-scope
surface**: the per-frame measure/bounds cache (`RetainedRender.Layout`), `layoutDirtySet`,
`Layout.evaluateIncremental` (re-measure dirty set, propagate to fixed-size ancestor, reuse cached bounds,
report honest `Invalidated`), and the `RemeasuredNodeCount` / `LayoutInvalidatedNodeCount` metrics. The
**paint-side** partial repaint is feature **091**; the bounded cross-frame **picture cache** is **116** and
the **text-measure cache** is **117** (separate LRU stores on the same `step`); the **lock-step name set**
drift guard is **101**. Other accretions in the same `RetainedRender.fsi` (099 clock, 110 authored-id, 113
memo, 114 virtualization, 120 fingerprint) are owned by their own features.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — it alters observable behavior (per-frame layout
is now incremental: a localized geometry change re-measures only its boundary subtree instead of the whole
tree, and two honest re-measure metrics are surfaced). The **public** API delta is the already-shipped
`Layout.evaluateIncremental` evaluator (in the committed baseline; **zero new** delta — FR-011); the wiring
surface is `internal`. Per the vertical-slice rule, the in-assembly `Controls.Tests` exercise the internal
wiring and the public-package `Layout.Tests` exercise the public evaluator.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: the incremental evaluator, the dirty-set wiring, the accreted `.fsi` fields, and all three suites arrived together at migration. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` (`Layout.fsi` for the public evaluator; `RetainedRender.fsi` for the internal wiring) and the semantic tests already exist and exercise the **real wired path** (`evaluateIncremental` carrying its own cache, and `RetainedRender.step` driving it). Recorded in Complexity Tracking. |
| II. Visibility lives in `.fsi` | ⚠️ Pass with noted drift | `Layout.fsi` is the sole declaration of the public evaluator; `RetainedRender.fsi` declares the internal `Layout` cache field and the two metrics (zero new baseline delta). The imported `RetainedRender.fs` carries redundant `internal`/`private` access modifiers on top-level bindings, which Principle II discourages when an `.fsi` is present — the same bounded Tier-2 follow-up the 091/092/099 plans scoped as **DF-1** (Workstream E1), not a blocker for this backfill. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions + tree recursion (a `HashSet` accumulator local to the pure `layoutDirtySet` walk; the incremental evaluator is value→value). Mutation appears only on the existing render/measure hot path, disclosed at the use site. No SRTP/reflection/type-providers/custom operators requiring justification. |
| IV. Elmish/MVU boundary | ✅ Pass | The `LayoutResult` cache is durable Model state on the retained render record; `layoutDirtySet` and `evaluateIncremental` are **pure** transitions/projections; the host loop interprets at the edge (mutable-ref carry of the retained state). The equivalence proof (incremental ≡ full) is exactly the determinism the MVU boundary requires. |
| V. Test evidence mandatory | ✅ Pass | `Feature097IncrementalTests` proves equivalence over ≥1000 FsCheck cases (SC-002), the fixed-size-boundary subset (SC-001), the empty-dirty-set at-rest case (SC-006), and the honest-`Invalidated` superset (SC-008); `Audit_IncrementalLayout` cross-checks it; `Feature097WiringTests` proves the wired metric honesty (SC-003), the dirty-set precision (SC-004), and wired byte-identity vs a full rebuild (SC-005), with a whole-tree-relayout counterfactual that the metric must equal baseline (never under-report). Readiness artifacts to be authored in `/speckit-implement` (097 imported without them). |
| VI. Observability & safe failure | ✅ Pass | `layoutDirtySet` and `evaluateIncremental` are total for any input (an empty dirty set re-measures nothing; a content-sized chain falls back to a full re-measure — degenerate-correct, never wrong); no silent failure or swallowed exception. The evaluator consults no wall-clock, so a step can never fail on a missing time source, and `Revision` advances deterministically. |

**Gate result**: PASS (two deviations justified and recorded — both inherited verbatim from the 091/092/099
backfills; neither is a public-contract or evidence violation). Re-checked post-Phase-1 design below —
unchanged: the design artifacts add no new public surface, no dependency, and no new behavior beyond what
the existing suites pin.

## Project Structure

### Documentation (this feature)

```text
specs/097-layout-cache/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions recovered from the imported incremental-layout wiring
├── data-model.md        # Phase 1 — the 097-in-scope cache/dirty-set/metric entities
├── quickstart.md        # Phase 1 — how to run + read the 097 validation (all three suites)
├── contracts/
│   └── layout-cache.md  # Phase 1 — the public evaluator + internal wiring contract the suites pin
├── checklists/
│   └── requirements.md  # Pre-existing authoring checklist (from /speckit-specify)
├── readiness/           # AUTHORED in /speckit-implement (097 imported without evidence): equivalence, partial-remeasure, metric-honesty, at-rest, wired-parity
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Layout/
├── Layout.fsi / Layout.fs        # PUBLIC evaluator: evaluate, evaluateIncremental (re-measure dirty set, propagate to fixed-size ancestor, reuse cached bounds, honest Invalidated)
└── Types.fsi / Types.fs          # LayoutResult { Bounds; Diagnostics; Invalidated; Revision }, ComputedBounds, AvailableSpace

src/Controls/
├── RetainedRender.fsi / RetainedRender.fs   # 091 base + 097 accretions: the Layout cache field, layoutDirtySet (internal), the incremental step call, RemeasuredNodeCount / LayoutInvalidatedNodeCount on WorkReductionRecord
├── ControlInternals(.fs)                     # layoutAffectingAttrNames — the geometry-driving name set 097 reads (owned by feature 101)
└── Control.fsi / Control.fs                  # renderTree measure/paint — the full-rebuild parity oracle (byte-identity checks)

tests/Layout.Tests/
├── Feature097IncrementalTests.fs   # pure evaluator: equivalence (≥1000 FsCheck), boundary subset, at-rest, honest Invalidated
└── Audit_IncrementalLayout.fs      # audit cross-check of the incremental evaluator

tests/Controls.Tests/
├── Feature097WiringTests.fs        # wired step: metric honesty, dirty-set precision, byte-identity vs full rebuild
└── Feature101LayoutDriftGuardTests.fs   # the lock-step name-set guard 097 depends on (owned by feature 101)
```

**Structure Decision**: Single F# solution (`FS.GG.Rendering.slnx`). 097 adds no project and no new public
surface; it carries the previous frame's `LayoutResult` on the existing retained render record, derives the
dirty set from the reconcile patch, calls the already-public `Layout.evaluateIncremental`, and surfaces two
internal metrics. The **pure-evaluator** proofs (equivalence, boundary subset, at-rest, honest `Invalidated`)
live in `Layout.Tests` because they are properties of `evaluateIncremental` against the public package; the
**wired-path** proofs (metric honesty, dirty-set precision, byte-identity vs full rebuild) live in
`Controls.Tests/Feature097WiringTests` because each is a property of the `RetainedRender.step` seam, reaching
the internal wiring via `InternalsVisibleTo`. Surface baselines under `tests/surface-baselines/`
(`FS.GG.UI.Layout.txt`, `FS.GG.UI.Controls.txt`) must remain byte-unchanged.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The incremental evaluator, the dirty-set wiring, the accreted `.fsi` fields, and all three suites were imported wholesale at migration; this spec/plan is authored afterward (task C2). | Re-deriving the wiring from a fresh spec would discard working, evidence-backed code and its history. The backfill restores the chain at lower cost and risk. |
| No `readiness/` evidence imported (unlike 092/099) | 097 arrived with executable suites but no captured readiness artifacts. | Authoring readiness as part of `/speckit-implement` (drawing on the three existing suites) is cheaper and more honest than fabricating retroactive evidence; the suites are the authoritative proof, readiness is the captured snapshot. |
| Redundant `internal`/`private` access modifiers in `RetainedRender.fs` | Inherited verbatim from the imported source. | Stripping them is a behavior-neutral Tier-2 cleanup; bundling it into this backfill would mix a documentation pass with a code edit. Already scoped as the shared bounded follow-up **DF-1** (Workstream E1), not done here. |
| One `RetainedRender.fsi` documents many features' fields together (097 alongside 091/099/110/113/114/116/117/120) | The single imported `.fsi` accreted later features in place; it cannot be physically split without breaking those features. | 097's plan scopes its surface explicitly (Scale/Scope) — the measure/bounds cache, dirty set, incremental call, and the two re-measure metrics — and defers the paint cache (116), text cache (117), and the name-set guard (101) to their owning features, rather than forking the file. |
</content>
