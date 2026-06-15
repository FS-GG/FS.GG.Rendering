# Implementation Plan: Visual-State Cross-Fade (Feature 103)

**Branch**: `103-visual-state-cross-fade` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/103-visual-state-cross-fade/spec.md`

## Summary

Feature 099 made the per-identity **animation clock** live (advance from the host tick, sample on paint) but
scoped itself to the degenerate `From = []` **plain fade-in** — the new appearance fading in from
transparent. A visual-state change whose paint differed in a colour showed only the *next* colour appearing
over emptiness; the prior colour was **absent** mid-flight. That is not a real transition.

Feature 103 (the "R6" accretion) **makes the transition a genuine two-snapshot cross-fade**: the pure
transition trigger (`updateClockForState`) captures the prior state's **static own-scene snapshot** as the
clock's `From`, and the paint composite (`sampleOnPaint`) renders two opacity-driven layers via the public
feature-073 `Animation.applyAt` — the prior `From` fading **OUT** (`1→0`) **under** the next own-scene fading
**IN**. For a region painted in both states (a Switch track fill restyling `Muted→Accent` on hover) the
composite displays a colour **strictly between** the endpoints mid-flight. Because the composite is gated to
**active (mid-flight) clocks only**, an at-rest or settled identity is **byte-identical** to the static
render — the settle and fast paths are untouched, the final frame equals the snapped static render for every
channel, and the at-rest frame emits no animation attribute and takes the zero-recompute fast path.

**This is a backfill plan** (task **C4** of the 2026-06-15 missing-features plan, following the 091 pattern
and the 092 / 093 / 095 / 096 / 099 / 097 closes). The implementation (the cross-fade composite in
`src/Controls/RetainedRender.fs`/`.fsi`, reached through the `ControlRuntime` visual-state bridge and the
`Controls.Elmish` host tick), the accreted `.fsi` surface (`AnimationClock.From`, `updateClockForState`,
`sampleOnPaint` — shared with 099), and the authoritative suite
(`tests/Controls.Tests/Feature103CrossFadeTests.fs`) **already exist** in the imported, rebranded source,
with captured readiness evidence under `readiness/` that the suite **self-writes** on each run. The plan's
job is to bring this work under the canonical `Spec → .fsi → semantic tests → implementation` contract:
document the design decisions the code already embodies, confirm the constitution gates the existing
artifacts satisfy, and record the import-before-spec deviation. No new product behaviour is designed;
`/speckit-tasks` and `/speckit-implement` reduce to a **conformance pass** (confirm the suite is green, the
readiness regenerates, and the public-surface delta is zero), not a build.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.

**Primary Dependencies**: Expecto + FsCheck (property/semantic tests); the public feature-073
`FS.GG.UI.Scene.Animation` / `Animation.applyAt` opacity sampler and `Animation.lerpColor` reference; the
`Controls` package's `Scene`/`Control.renderTree` measure-paint path; the `ControlRuntime` visual-state
bridge (`applyRuntimeVisualState`) and `Style.resolve` (the `Muted→Accent` restyle); the `Controls.Elmish`
host tick seam. No new runtime or package dependency — 103 is an internal composite over code already
present (it builds directly on 099's clock).

**Storage**: N/A. The `AnimationClock` (with its `From` snapshot) rides the host loop's existing mutable-ref
`StateByIdentity` map (the interpreter edge); nothing is persisted.

**Testing**: Default-tier "Local inner loop" in one in-assembly suite reaching the internal surface via
`InternalsVisibleTo`:
- `tests/Controls.Tests/Feature103CrossFadeTests.fs` — driven through the real
  `ControlRuntime.applyRuntimeVisualState` + `RetainedRender.advance` (Tick) + `RetainedRender.step` seam
  (mirroring `Feature099AnimationSeamTests`):
  - **US1** the genuine cross-fade — mid-flight both endpoints present, displayed colour strictly between
    (SC-001/INV-3), red on the pre-R6 fade-in;
  - **US2** at-rest + settled byte-identity to the static render and the unchanged fast path
    (SC-002/SC-003/INV-1/INV-2);
  - **US3** determinism (fixed 7-frame replay + 60 FsCheck cases; non-positive no-op) (SC-004/INV-4);
  - **US4** edge cases (retarget continuity INV-5, held-state scoped repaint INV-6, return-to-Normal drop,
    no-colour-delta no artifact) (SC-006).
  The suite **self-writes** its readiness files under `specs/103-visual-state-cross-fade/readiness/`.
Deterministic/offscreen — no GL context required (`DeterministicRenderOnly` renderer mode).

**Target Platform**: Linux/dev. 103's proofs are deterministic and headless (structural scene equality,
descriptive-scene colour/alpha inspection, work-count invariants), independent of the GPU.

**Project Type**: F# UI framework — an internal composite inside the `Controls` runtime library plus the
`ControlRuntime`/`Controls.Elmish` host seam, exercised by its in-assembly suite.

**Performance Goals**: No wall-clock target. The measurable goals are correctness/determinism/byte-identity:
the mid-flight displayed colour is strictly between the endpoints (SC-001); the at-rest and settled frames
are byte-identical to the static render (SC-002/SC-003); identical injected-delta sequences produce identical
frames across 60 FsCheck cases (SC-004); the edge behaviours hold (SC-006).

**Constraints**:
- Surface stays **assembly-internal** — zero public-surface-baseline delta (FR-009). The surface-drift check
  must pass byte-unchanged for `FS.GG.UI.Controls` (and `FS.GG.UI.Controls.Elmish`).
- The composite is **paint-level only** (opacity, never layout); it consults **no** wall-clock — the clock's
  sole time coordinate is the injected per-frame delta (FR-002, FR-006, Principle VI).
- The cross-fade is a **mid-flight-only overlay**: a settled/absent clock paints `ownScene` verbatim, so the
  at-rest and settle paths are **untouched** and stay byte-identical (FR-004, FR-005).
- `updateClockForState` must capture/re-seed `From` correctly (start = prior own-scene; retarget = previous
  target's snapshot with `Elapsed` reset; advance-only = keep `From`; drop on settled return-to-`Normal`)
  (FR-003).
- Output parity is judged by **structural scene equality**; mid-flight colour by descriptive paint
  colours/alphas; pixel/desktop-visibility proofs are explicitly out of scope (the readiness discloses this).

**Scale/Scope**: One internal composite across the `Controls` module and the host seam. **103-in-scope
surface**: `AnimationClock.From` (the prior own-scene snapshot), the `From`-capturing/re-seeding behaviour of
`updateClockForState`, and the two-layer `sampleOnPaint` composite (prior-out-under-next). The same
`RetainedRender.fsi` carries the **live single-channel clock** (advance + plain `From = []` fade-in) owned by
feature **099 (R4)** and the **no-alloc idle** `advanceStateClocks` owned by feature **121** — both **out of
scope for 103**. Other accretions in the same `.fsi` (097 layout cache, 110 authored-id, 113 memo, 114
virtualization, 116/117 caches, 120 fingerprint) are owned by their own features.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1 (contracted change)** — it alters observable behaviour (a visual-state
transition now genuinely cross-fades both endpoint colours instead of fading the next in from transparent).
The public API surface delta is **intentionally zero** (FR-009): the composite surface is `internal` in
`Controls`, deliberately omitted from the capability `contracts:` lists, so the surface-drift baselines are
unchanged *and that zero-delta is itself an asserted requirement*. Per the vertical-slice rule, the
in-assembly Expecto/FsCheck tests are the user-reachable surface for these internal stories.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ⚠️ Justified deviation | Canonical order was **inverted by import**: the cross-fade composite, the accreted `.fsi` (`AnimationClock.From`, the shared `updateClockForState`/`sampleOnPaint`), and the suite arrived together at migration. This backfill restores the chain by authoring the missing spec/plan and confirming the `.fsi` and the FSI-reachable semantic tests already exist and exercise the **real wired seam** (`applyRuntimeVisualState` → `advance` (Tick) → `step` → `sampleOnPaint`). Recorded in Complexity Tracking. |
| II. Visibility lives in `.fsi` | ⚠️ Pass with noted drift | `RetainedRender.fsi` is the sole declaration of the 103 surface; `AnimationClock.From`, `updateClockForState`, `sampleOnPaint` are `internal` (zero baseline delta). The imported `RetainedRender.fs` carries redundant `internal`/`private` access modifiers, which Principle II discourages when an `.fsi` is present — the same bounded Tier-2 follow-up the 091/092/099/097 plans scoped as **DF-1** (Workstream E1), not a blocker for this backfill. |
| III. Idiomatic simplicity | ✅ Pass | Records + pure functions; the composite is value→value (`Animation.applyAt` over two opacity layers). Mutation appears only on the existing render hot path, disclosed at the use site. No SRTP/reflection/type-providers/custom operators requiring justification. |
| IV. Elmish/MVU boundary | ✅ Pass | The `AnimationClock` (with `From`) is durable Model state in `StateByIdentity`; `updateClockForState`/`sampleOnPaint` are **pure** transitions/projections; the host tick interprets at the edge (mutable-ref loop), injecting the per-frame delta as data. The suite drives the real `ControlRuntime` bridge + host tick seam, not a hand-seeded clock. |
| V. Test evidence mandatory | ✅ Pass | `Feature103CrossFadeTests` proves US1 through the real seam and fails on the no-seam counterfactual (the pre-R6 fade-in lacks the prior colour mid-flight); US2 proves at-rest/settled byte-identity; US3 pins determinism (60 FsCheck cases); US4 the four edge behaviours. Readiness artifacts (`mid-flight-interpolation`, `at-rest-byte-identity`, `final-frame-identity`, `determinism`) are **self-written by the suite**. The readiness honestly declares it does **not** prove pixels/desktop visibility (`DeterministicRenderOnly`, structural scene equality). |
| VI. Observability & safe failure | ✅ Pass | `updateClockForState`/`sampleOnPaint` are total for any input (a settled/absent clock is not composited; `From = []` degenerates to the plain fade-in; a non-positive delta is a no-op; a large delta settles canonically). No silent failure; the composite consults no wall-clock, so a tick can never fail on a missing time source. |

**Gate result**: PASS (two deviations justified and recorded — both inherited verbatim from the
091/092/099/097 backfills; neither is a public-contract or evidence violation). Re-checked post-Phase-1
design below — unchanged: the design artifacts add no public surface, no dependency, and no new behaviour
beyond what the existing suite pins.

## Project Structure

### Documentation (this feature)

```text
specs/103-visual-state-cross-fade/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions recovered from the imported cross-fade composite
├── data-model.md        # Phase 1 — the 103-in-scope cross-fade entities
├── quickstart.md        # Phase 1 — how to run + read the 103 validation
├── contracts/
│   └── visual-state-cross-fade.md   # Phase 1 — the internal cross-fade seam contract the suite pins
├── checklists/
│   └── requirements.md  # Pre-existing authoring checklist (from /speckit-specify)
├── readiness/           # SELF-WRITTEN by the suite: mid-flight-interpolation, at-rest-byte-identity, final-frame-identity, determinism
└── tasks.md             # Phase 2 — created by /speckit-tasks (conformance pass)
```

### Source Code (repository root)

```text
src/Controls/
├── RetainedRender.fsi / RetainedRender.fs   # 099 clock base + 103 cross-fade: AnimationClock.From, updateClockForState (From capture/re-seed), sampleOnPaint (two-layer composite)
├── ControlRuntime.fsi / ControlRuntime.fs   # applyRuntimeVisualState — the visual-state bridge that triggers a transition
├── Style.fs                                  # Style.resolve — the Muted→Accent restyle that creates the colour delta the cross-fade interpolates
└── Control.fsi / Control.fs                 # renderTree measure/paint — the full-rebuild parity oracle (at-rest / settled identity)

src/Controls.Elmish/
└── ControlsElmish.fsi / ControlsElmish.fs   # The live host tick seam: advances each clock by the injected delta, steps, samples (composites)

tests/Controls.Tests/
└── Feature103CrossFadeTests.fs              # US1 genuine cross-fade, US2 byte-identity, US3 determinism, US4 edges — through the real seam; self-writes readiness
```

**Structure Decision**: Single F# solution (`FS.GG.Rendering.slnx`). 103 adds no project and no public
surface; it populates the existing internal `AnimationClock.From` snapshot in `updateClockForState` and
makes `sampleOnPaint` a genuine two-layer composite, and pins the behaviour with one suite in the existing
`Controls.Tests` assembly. All four user stories live in `Feature103CrossFadeTests` because each is a
property of the real `ControlRuntime` bridge + host tick + `step` + `sampleOnPaint` seam (the same seam
`Feature099AnimationSeamTests` drives). Surface baselines under `tests/surface-baselines/`
(`FS.GG.UI.Controls.txt`) must remain byte-unchanged.

## Complexity Tracking

> Recorded deviations (justified above), kept visible rather than silently accepted.

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | The cross-fade composite, the accreted `.fsi`, and the suite were imported wholesale at migration; this spec/plan is authored afterward (task C4). | Re-deriving the composite from a fresh spec would discard working, evidence-backed code and its history. The backfill restores the chain at lower cost and risk. |
| Redundant `internal`/`private` access modifiers in `RetainedRender.fs` | Inherited verbatim from the imported source. | Stripping them is a behavior-neutral Tier-2 cleanup; bundling it into this backfill would mix a documentation pass with a code edit. Already scoped as the shared bounded follow-up **DF-1** (Workstream E1), not done here. |
| One `RetainedRender.fsi` documents many features' fields together (103 alongside 099/121 and 097/110/113/114/116/117/120), and `updateClockForState`/`sampleOnPaint` are SHARED with 099 | The single imported `.fsi` accreted later features in place; 099 and 103 deliberately share the clock seam (103 is the cross-fade realization of the same `sampleOnPaint` 099 used as a plain fade-in). | 103's plan scopes its surface explicitly (Scale/Scope) — the `From` snapshot and the two-layer composite — and defers the live single-channel clock (099) and the no-alloc idle (121) to their owning features, rather than forking the file. |
| SC numbering skips SC-005 | The in-tree `Feature103CrossFadeTests` and its readiness files label outcomes SC-001/002/003/004/006 (and INV-1…INV-6). | The spec mirrors the existing in-tree labels 1:1 so the contract aligns with the proofs; renumbering would desync the spec from the suite's own SC references. |
</content>
