# Implementation Plan: Wire the Keyed Reconciler onto the Render Path (Feature 091)

**Branch**: `091-wire-reconciler-render-path` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/091-wire-reconciler-render-path/spec.md`

## Summary

Feature 091 wires the parked keyed reconciler (`module internal Reconcile`, historical feature 067)
onto the live render path through a new retained structure (`module internal RetainedRender`). Each
frame diffs the next `Control<'msg>` tree against the previous retained tree, confers a **stable,
path-independent identity** (`RetainedId`) on every matched node, re-keys per-control state
(focus/animation/text) to that identity so it survives an unrelated re-render, and reuses the cached
render fragments of unchanged subtrees instead of repainting them — while producing a frame that is
**byte-identical** to a full rebuild (`Control.renderTree next`).

**This is a backfill plan.** The implementation, the `.fsi` surface, and the authoritative
Expecto/FsCheck property tests (`tests/Controls.Tests/Feature091RetainedRenderTests.fs`) already
exist in the imported source, with captured readiness evidence under `readiness/`. The plan's job is
to bring this work under the canonical `Spec → .fsi → semantic tests → implementation` contract: it
documents the design decisions already embodied in the code, confirms the constitution gates the
existing artifacts satisfy, and records the honest deviations created by importing code ahead of its
spec. No new product behavior is designed here; `/speckit-tasks` and `/speckit-implement` reduce to a
**conformance pass** (confirm the tests are green and the surface delta is zero), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: Expecto + FsCheck (property tests); the `Controls` package's own `Scene`,
`Layout`, and `Control.renderTree` measure/paint path. No new runtime or package dependency — 091 is
an internal wiring of code already present.

**Storage**: N/A. The retained structure lives in the host loop's existing mutable-ref state (the
interpreter edge); nothing is persisted.

**Testing**: Default-tier "Local inner loop" — `Controls.Tests` (the authoritative 091 suite is
`Feature091RetainedRenderTests`, reaching the internal surface via
`[<assembly: InternalsVisibleTo("Controls.Tests")>]`). The four FsCheck properties run ≥1000 cases
each. Deterministic/offscreen — no GL context required.

**Target Platform**: Linux/dev. 091's proofs are deterministic and headless (structural scene
equality, work-count invariants), independent of the GPU.

**Project Type**: F# UI framework — an internal module inside the `Controls` runtime library plus its
in-assembly property tests.

**Performance Goals**: No wall-clock target. The measurable goal is a **work-count** invariant
(SC-003): a localized change recomputes ≤ the changed-subtree bound and strictly < the total node
count `N`. Behavior must stay byte-identical to a full rebuild (correctness over cheapness).

**Constraints**:
- Surface stays **assembly-internal** — zero public-surface-baseline delta (FR-010). The
  surface-drift check must pass unchanged.
- The wired step MUST be **total** (never throws) and **deterministic** (per-host counter for
  identity minting; no wall-clock, no randomness).
- Reuse decisions use **structural** equality, never object identity (FR-005).
- Malformed input (duplicate sibling keys) surfaces a `KeyCollision` **Warning** through the existing
  `ControlDiagnostic` channel; it is never fatal (FR-009, Principle VI).
- Output parity is judged by structural scene equality + bounds + node count — the authoritative
  proof; pixel/desktop-visibility proofs are explicitly out of scope (the readiness evidence
  discloses this).

**Scale/Scope**: One internal module (`RetainedRender`) wiring one existing internal module
(`Reconcile`). **091-in-scope surface**: `RetainedId`, `RenderFragment`, `RetainedNode`,
`RetainedUiState`, the carried `AnimationClock` (091 only *carries* it; nothing writes it yet),
`RetainedRender` (091 fields `Root`/`NextId`/`StateByIdentity`/`Theme`), `WorkReductionRecord` (091
fields `BaselineNodeCount`/`RecomputedNodeCount`/`ChangedSubtreeBound`), `RetainedRenderStep`, and
`RetainedRender.init`/`step`/`advance` + `Reconcile.diff`/`apply`. The same `RetainedRender.fsi` in
the tree carries **later-feature accretions** (092 `RetainedInit`/theme-reuse, 097 layout cache, 099
live clock, 103 cross-fade, 113 memo, 114 virtualization, 116 picture cache, 117 text cache, 120
fingerprint/replay) — those are **out of scope for 091** and owned by their own features.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — it alters observable behavior (per-control
focus/animation state now survives an unrelated re-render) and adds substantial internal modules.
The public API surface delta is **intentionally zero** (FR-010): the entire surface is `internal`,
deliberately omitted from the `Controls` capability `contracts:` list, so the surface-drift baseline
is unchanged *and that zero-delta is itself an asserted requirement*. Per the vertical-slice rule,
the in-assembly property tests are the user-reachable surface for these internal stories.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: code + `.fsi` + tests arrived together at R4. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` (`RetainedRender.fsi`) and the FSI-reachable semantic tests already exist and exercise the real wired path. Recorded in Complexity Tracking. |
| II. Visibility lives in `.fsi` | ⚠️ Pass with noted drift | `RetainedRender.fsi` is the sole declaration of the surface, and it is `internal` (zero baseline delta). The imported `.fs` carries redundant `internal`/`private` access modifiers on top-level bindings (e.g. `type internal RetainedId`, `let private isMemoizable`), which the constitution discourages when an `.fsi` is present. Pre-existing import condition; logged as a Tier-2 cleanup candidate, not a blocker for this backfill. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions + tree recursion (legitimate branching structure). Mutation appears only on the render/measure hot path, disclosed at the use site. No SRTP/reflection/type-providers/custom operators requiring justification. |
| IV. Elmish/MVU boundary | ✅ Pass | The retained structure is the durable Model; `step : RetainedRender → next → RetainedRenderStep` is a **pure** transition emitting the next structure + render + diagnostics + work record; the host loop interprets at the edge (mutable-ref state). I/O is not performed inside `step`. |
| V. Test evidence mandatory | ✅ Pass | `Feature091RetainedRenderTests` fails-first against the stub, greens on the real path; four FsCheck properties at ≥1000 cases (round-trip, determinism, totality, identity-at-rest); readiness artifacts captured (`retained-parity`, `work-reduction`, `survives-proof`). The duplicate-key test carries the `Synthetic` token and discloses its malformed literal fixture per Principle V. The readiness evidence honestly declares it does **not** prove pixels/desktop visibility. |
| VI. Observability & safe failure | ✅ Pass | `KeyCollision` surfaces as a `Warning` `ControlDiagnostic`; `step` is total (never throws) for any `(prev, next)`, proven by the totality property. No silent failure. |

**Gate result**: PASS (two deviations justified and recorded; neither is a public-contract or
evidence violation). Re-checked post-Phase-1 design below — unchanged: the design artifacts add no
public surface, no dependency, and no new behavior beyond what the existing tests pin.

## Project Structure

### Documentation (this feature)

```text
specs/091-wire-reconciler-render-path/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions recovered from the imported implementation
├── data-model.md        # Phase 1 — the 091-in-scope retained entities
├── quickstart.md        # Phase 1 — how to run + read the 091 validation
├── contracts/
│   └── retained-render.md   # Phase 1 — the internal contract the property tests pin
├── readiness/           # Pre-existing captured evidence (gitignored): retained-parity, work-reduction, survives-proof
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── Reconcile.fsi / Reconcile.fs            # Feature 067 — the parked keyed VDOM diff (diff/apply); pure, total
├── RetainedRender.fsi / RetainedRender.fs  # Feature 091 — the retained structure wiring the diff onto render (init/step/advance)
├── Control.fsi / Control.fs                # renderTree measure/paint — the full-rebuild parity oracle
└── Types.fsi / Types.fs                    # ControlDiagnostic / ControlDiagnosticCode (KeyCollision) / Severity

tests/Controls.Tests/
└── Feature091RetainedRenderTests.fs        # Authoritative semantic surface: US1–US4, SC-001..SC-006, Gen091 generators
```

**Structure Decision**: Single F# project layout. 091 adds the internal `RetainedRender` module to
the existing `Controls` library and its tests to the existing `Controls.Tests` assembly — no new
project, no new package, no public surface. The retained module sits beside the `Reconcile` module it
wires, mirroring the `module internal SceneRenderer` precedent for internal-with-`.fsi` modules.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The reconciler + retained structure + tests were imported wholesale at migration R4; this spec/plan is authored afterward. | Re-deriving the module from a fresh spec would discard working, evidence-backed code and its history. The backfill restores the chain at lower cost and risk. |
| Redundant `internal`/`private` access modifiers in `RetainedRender.fs` | Inherited verbatim from the imported source. | Stripping them is a behavior-neutral Tier-2 cleanup touching one imported file; bundling it into this backfill would mix a documentation pass with a code edit. Logged as a follow-up, not done here. |
| `.fsi` documents later-feature fields (092–120) inseparably from 091 | The single imported `RetainedRender.fsi` accreted later features in place; they cannot be physically removed without breaking those features. | 091's plan scopes its surface explicitly (Scale/Scope) and defers the rest to the owning features, rather than forking the file. |
