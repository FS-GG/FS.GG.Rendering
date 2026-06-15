# Implementation Plan: Live Animation Clock (Feature 099)

**Branch**: `099-live-animation-clock` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/099-live-animation-clock/spec.md`

## Summary

Feature 091 conferred a stable `RetainedId` on every matched node and carried a per-identity state map
(`StateByIdentity`) frame to frame — including a slot for a per-control **animation clock**
(`RetainedUiState.Animation`) — **but 091 only *carried* that slot; nothing on the live path ever wrote
it.** Feature 092 then wired the live host to read/write focus and in-progress text through that map, but
could only prove animation survival with a **hand-seeded** clock fixture because no real seam advanced one.

Feature 099 (the "R4" accretion) **makes the animation clock live**: the host **tick** advances each
per-identity clock by an **injected** per-frame time delta (never a wall-clock) and paint **samples** the
active clock onto the identity's own scene through the public feature-073 `Animation.applyAt` opacity
tween. A visual-state transition (e.g. a button's hover) now **animates** over the single pinned framework
default (150 ms, `EaseOut`) on the real
`ControlRuntime.applyRuntimeVisualState` → `RetainedRender.advance` (Tick) → `RetainedRender.step` seam
instead of snapping. Because the clock rides the existing `RetainedId`-keyed map, an in-flight animation
**survives** an unrelated re-render that shifts the control's position and **completes** (replacing 092's
hand-seeded precondition). At rest (no active clock) the wired frame is **byte-identical** to the full
static rebuild — zero recompute, zero remeasure — so the seam is invisible until something animates; and a
removed identity's clock is **garbage-collected** by the same `liveIds` filter that already drops focus and
text.

**This is a backfill plan** (task **C3** of the 2026-06-15 missing-features plan, following the 091/092
pattern). The implementation (`src/Controls/RetainedRender.fs` + `.fsi`, the `ControlRuntime`/`Controls.Elmish`
host tick seam), the accreted `.fsi` surface (`defaultTransitionDuration`, `advance`, `clockActive`,
`updateClockForState`, `sampleOnPaint`, the `AnimationClock` record), and the authoritative Expecto/FsCheck
suites (`tests/Controls.Tests/Feature099AnimationClockTests.fs`,
`tests/Elmish.Tests/Feature099AnimationSeamTests.fs`) **already exist** in the imported, rebranded source,
with captured readiness evidence under `readiness/`. The plan's job is to bring this work under the canonical
`Spec → .fsi → semantic tests → implementation` contract: document the design decisions the code already
embodies, confirm the constitution gates the existing artifacts satisfy, and record the honest deviations
created by importing code ahead of its spec. No new product behavior is designed here; `/speckit-tasks` and
`/speckit-implement` reduce to a **conformance pass** (confirm the suites are green, the readiness evidence
regenerates, and the public-surface delta is zero), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: Expecto + FsCheck (property/semantic tests); the public feature-073
`FS.GG.UI.Scene.Animation` / `Animation.applyAt` opacity sampler; the `Controls` package's
`Scene`/`Layout`/`Control.renderTree` measure-paint path; the `ControlRuntime` visual-state bridge
(`applyRuntimeVisualState`) and the `Controls.Elmish` host tick seam. No new runtime or package dependency —
099 is an internal wiring of code already present.

**Storage**: N/A. The `AnimationClock` rides the host loop's existing mutable-ref `StateByIdentity` map (the
interpreter edge); nothing is persisted.

**Testing**: Default-tier "Local inner loop" across **two** in-assembly suites reaching the internal surface
via `InternalsVisibleTo`:
- `tests/Controls.Tests/Feature099AnimationClockTests.fs` — the **pure clock core**: determinism over a fixed
  injected-delta sequence + 1000 FsCheck cases, the advance edges (non-positive no-op, large-delta clamp,
  mid-flight retarget, settled return-to-`Normal` drop, multi-clock independence), and identity-at-rest
  (byte-identical to the static rebuild, zero recompute) (US3).
- `tests/Elmish.Tests/Feature099AnimationSeamTests.fs` — the **live seam** driven through the real
  `ControlRuntime.applyRuntimeVisualState` + `RetainedRender.advance` (Tick) + `RetainedRender.step`: a
  transition animates rather than snaps (US1), an in-flight animation survives a sibling shift and completes
  (US2), a removed identity's clock is GC'd (US4), and the scoped-repaint work metric (US5).
Deterministic/offscreen — no GL context required (`DeterministicRenderOnly` renderer mode).

**Target Platform**: Linux/dev. 099's proofs are deterministic and headless (structural scene equality,
injected-delta clock trajectories, work-count invariants), independent of the GPU.

**Project Type**: F# UI framework — an internal module inside the `Controls` runtime library plus the
`ControlRuntime`/`Controls.Elmish` host seam, exercised by their in-assembly suites.

**Performance Goals**: No wall-clock target. The measurable goals are correctness/determinism/work-count
invariants: a live transition shows ≥1 intermediate frame before converging exactly to the snap target
(SC-001); an in-flight clock survives a shift with a byte-identical trajectory (SC-002); an at-rest frame is
byte-identical to the static rebuild with zero recompute/remeasure (SC-003); identical injected-delta
sequences produce byte-identical output across 1000 FsCheck cases (SC-004); a removed identity's clock is
collected (SC-005); a single active animation keeps the rest of the tree on the `Keep` fast path (steady-state
recompute = remeasure = 0) while still changing (SC-006).

**Constraints**:
- Surface stays **assembly-internal** — zero public-surface-baseline delta (FR-012). The surface-drift check
  must pass unchanged for `FS.GG.UI.Controls` (and `FS.GG.UI.Controls.Elmish`).
- The wired `advance`/`updateClockForState`/`sampleOnPaint` path MUST be **total** (never throws) and
  **deterministic** — the clock's sole time coordinate is the **injected** per-frame delta; no wall-clock, no
  `Math.random` (FR-003, FR-011, Principle VI).
- `advance` MUST clamp to the animation duration (no overshoot) and treat a non-positive delta as a no-op
  (never rewinds) (FR-004).
- An at-rest identity (no active clock, or a settled return-to-`Normal` clock) MUST emit no animation
  attribute and paint byte-identical to the static rebuild — the seam is invisible at rest (FR-006).
- A removed identity's clock is filtered by the existing `liveIds` filter — no new GC code path (FR-010).
- Output parity is judged by **structural scene equality** (+ bounds + node count); survival by trajectory
  byte-equality; pixel/desktop-visibility proofs are explicitly out of scope (the readiness evidence
  discloses this).

**Scale/Scope**: One internal wiring across the `Controls` module and the host tick seam. **099-in-scope
surface**: the live **opacity-channel** clock — `defaultTransitionDuration` (150 ms, `EaseOut`), `advance`
(injected-delta, clamped, total), `clockActive` (sampling gate), `updateClockForState` (start / retarget /
advance-only / drop), `sampleOnPaint` (composite the active clock's fade-in via `Animation.applyAt`), and
the `AnimationClock` record (`Anim`/`Elapsed`/`Target`/`From`). The same `RetainedRender.fsi` carries the
**two-snapshot cross-fade composite** (the `From` prior-snapshot fading out under the new scene) owned by
feature **103 (R6)** and the **no-alloc idle** reference-equal behavior of `advanceStateClocks` owned by
feature **121** — both **out of scope for 099**, which uses the degenerate `From = []` plain fade-in and
carries those shapes through the same map. Other later accretions in the same `.fsi` (097 layout cache, 110
authored-id, 113 memo, 114 virtualization, 116/117 caches, 120 fingerprint) are owned by their own features.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — it alters observable behavior (visual-state
transitions now animate on the real host instead of snapping; an in-flight animation survives a position-
shifting re-render). The public API surface delta is **intentionally zero** (FR-012): the wired surface is
`internal` in `Controls`, deliberately omitted from the capability `contracts:` lists, so the surface-drift
baselines are unchanged *and that zero-delta is itself an asserted requirement*. Per the vertical-slice rule,
the in-assembly Expecto/FsCheck tests are the user-reachable surface for these internal stories.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: the clock wiring + the accreted `.fsi` + both suites arrived together at migration. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` (`RetainedRender.fsi`) and the FSI-reachable semantic tests already exist and exercise the **real wired seam** (`advance`/`step` driven from the host tick, no hand-seeded clock for US1/US2). Recorded in Complexity Tracking. |
| II. Visibility lives in `.fsi` | ⚠️ Pass with noted drift | `RetainedRender.fsi` is the sole declaration of the 099 surface; the seam (`advance`, `clockActive`, `updateClockForState`, `sampleOnPaint`, `defaultTransitionDuration`, `AnimationClock`) is `internal` (zero baseline delta). The imported `RetainedRender.fs` carries redundant `internal`/`private` access modifiers on top-level bindings, which Principle II discourages when an `.fsi` is present. Pre-existing import condition; the same bounded Tier-2 follow-up the 091/092 plans scoped as **DF-1** (Workstream E1) — not a blocker for this backfill. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions + tree recursion (legitimate branching structure). The clock core (`advance`/`updateClockForState`) is pure value→value; mutation appears only on the existing render/measure hot path, disclosed at the use site. No SRTP/reflection/type-providers/custom operators requiring justification. |
| IV. Elmish/MVU boundary | ✅ Pass | This is squarely an MVU-boundary feature: the `AnimationClock` is durable Model state in `StateByIdentity`; `advance`/`updateClockForState`/`sampleOnPaint` are **pure** transitions/projections; the host tick interprets at the edge (mutable-ref loop), injecting the per-frame delta as data rather than reading a clock inside the transition. Both pure-core tests (`Feature099AnimationClockTests`) and the live-seam tests (`Feature099AnimationSeamTests`) are present. |
| V. Test evidence mandatory | ✅ Pass | `Feature099AnimationSeamTests` proves US1 through the real seam and fails on the no-seam counterfactual (a build without the seam snaps on frame 0); US2 proves survival through the real carry (no hand-seeded clock). `Feature099AnimationClockTests` pins determinism (1000 FsCheck cases), the advance edges, and identity-at-rest. Readiness artifacts captured (`us1-animates-vs-snaps`, `us2-survival`, `us3-determinism`, `us3-identity-at-rest`, `us4-gc`, `scoped-repaint`). The readiness evidence honestly declares it does **not** prove pixels/desktop visibility (`DeterministicRenderOnly`, structural scene equality). |
| VI. Observability & safe failure | ✅ Pass | `advance`/`updateClockForState`/`sampleOnPaint` are total for any input (non-positive delta is a no-op, a large delta clamps, an absent clock is unsampled); no silent failure or swallowed exception on the wired path. The clock consults no wall-clock, so a tick can never fail on a missing time source. |

**Gate result**: PASS (two deviations justified and recorded — both inherited verbatim from the 091/092
backfills; neither is a public-contract or evidence violation). Re-checked post-Phase-1 design below —
unchanged: the design artifacts add no public surface, no dependency, and no new behavior beyond what the
existing suites pin.

## Project Structure

### Documentation (this feature)

```text
specs/099-live-animation-clock/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions recovered from the imported clock wiring
├── data-model.md        # Phase 1 — the 099-in-scope live-clock entities
├── quickstart.md        # Phase 1 — how to run + read the 099 validation (both suites)
├── contracts/
│   └── live-animation-clock.md   # Phase 1 — the internal clock seam contract the suites pin
├── checklists/
│   └── requirements.md  # Pre-existing authoring checklist (from /speckit-specify)
├── readiness/           # Pre-existing captured evidence: us1-animates-vs-snaps, us2-survival, us3-determinism, us3-identity-at-rest, us4-gc, scoped-repaint
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── RetainedRender.fsi / RetainedRender.fs   # 091 base + 099 accretions: AnimationClock, defaultTransitionDuration, advance, clockActive, updateClockForState, sampleOnPaint
├── ControlRuntime.fsi / ControlRuntime.fs   # applyRuntimeVisualState — the visual-state bridge that triggers a transition
├── Control.fsi / Control.fs                 # renderTree measure/paint — the full-rebuild parity oracle (identity-at-rest)
└── Types.fsi / Types.fs                     # VisualState; ControlDiagnostic / Severity

src/Controls.Elmish/
└── ControlsElmish.fsi / ControlsElmish.fs   # The live host tick seam: advances each clock by the injected delta, steps, samples

tests/Controls.Tests/
└── Feature099AnimationClockTests.fs         # US3: pure clock core determinism + edges + identity-at-rest

tests/Elmish.Tests/
└── Feature099AnimationSeamTests.fs          # US1/US2/US4/US5: animate-not-snap, survival, GC, scoped repaint through the real seam
```

**Structure Decision**: Single F# solution (`FS.GG.Rendering.slnx`). 099 adds no project and no public
surface; it wires the existing internal `AnimationClock` slot to the existing `ControlRuntime` visual-state
bridge and the `Controls.Elmish` host tick, and pins the behavior with tests in the existing `Controls.Tests`
and `Elmish.Tests` assemblies. The **pure clock core** proofs (US3 determinism, edges, identity-at-rest) live
in `Feature099AnimationClockTests` because they are properties of `advance`/`updateClockForState` in
isolation; the **live-seam** proofs (US1 animate-not-snap, US2 survival, US4 GC, US5 scoped repaint) live in
`Feature099AnimationSeamTests` because each is a property of the host tick + `step` seam, not of the clock
core alone — mirroring 092's split between `Feature092RetainedRenderTests` and `Feature092LiveSurvivalTests`.
Surface baselines under `tests/surface-baselines/` (`FS.GG.UI.Controls.txt`) must remain byte-unchanged.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The clock wiring, the accreted `.fsi`, and both suites were imported wholesale at migration; this spec/plan is authored afterward (task C3). | Re-deriving the wiring from a fresh spec would discard working, evidence-backed code and its history. The backfill restores the chain at lower cost and risk. |
| Redundant `internal`/`private` access modifiers in `RetainedRender.fs` | Inherited verbatim from the imported source. | Stripping them is a behavior-neutral Tier-2 cleanup; bundling it into this backfill would mix a documentation pass with a code edit. Already scoped as the shared bounded follow-up **DF-1** (Workstream E1), not done here. |
| One `RetainedRender.fsi` documents many features' fields together (099 alongside 103/121 and 097/110/113/114/116/117/120) | The single imported `.fsi` accreted later features in place; it cannot be physically split without breaking those features. | 099's plan scopes its surface explicitly (Scale/Scope) — the live opacity-channel clock — and defers the cross-fade composite (103), the no-alloc idle (121), and the rest to their owning features, rather than forking the file. |
