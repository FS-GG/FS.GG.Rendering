# Implementation Plan: Virtualization Counts & Overscan (Feature 114)

**Branch**: `114-virtualization-counts` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/114-virtualization-counts/spec.md`

## Summary

Feature 114 makes virtualized-DataGrid materialization observable and tunable: a read-only `countVirtual`
walk tallies materialized rows + logical total per frame (→ public `FrameMetrics.VirtualItemsMaterialized`/
`VirtualItemsTotal`); an opt-in `Overscan` widens the realized window by `2 × N` real edge-clamped rows
(default 0 = byte-identical to the historic slice); offscreen rows are logically addressable
(select/toggle/focus/relocate without materializing); accessibility reports the logical total + focused index.
The materialized count is bounded (`visible + 2 × overscan`) and does not scale with the total.

**This is a backfill plan** (task **C7**). The implementation, the accreted surface (internal `countVirtual`
carrier + already-baselined public field/param additions), and the five suites already exist; 114 imported
with **no `readiness/`**. `/speckit-tasks`/`/speckit-implement` reduce to a conformance pass.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto (deterministic; no FsCheck); the DataGrid model + `VisibleRange` windowing;
public `FrameMetrics`, `AccessibilityMetadata`, `Collections`; `ControlsElmish.Perf.runScript`. No new dependency.
**Storage**: N/A.
**Testing**: Headless across `tests/Controls.Tests/` (`Feature114OverscanTests` US1, `Feature114OverscanParityTests`
US2, `Feature114OffscreenTests` + `Feature114AccessibilityTests` US3) and `tests/Elmish.Tests/Feature114VirtualMetricsTests`
(US4). Deterministic; **no GL** (offscreen = logically off-window, not GL-offscreen).
**Target Platform**: Linux/dev; headless.
**Project Type**: F# UI framework — internal counting in `Controls`, metrics in `Controls.Elmish`, model/a11y in `Controls`.
**Performance Goals**: No wall-clock target. Goals: bounded non-scaling materialization (SC-001/SC-006);
overscan-0 byte-identical (SC-002); opt-in real adjacent rows (SC-003); offscreen addressability (SC-004);
logical a11y + boundary nav (SC-005).
**Constraints**: Zero new public-surface delta (FR-017) — counting carrier internal; every public type touched
already baselined, additions field/param-level. Pure/total/deterministic `countVirtual` (read-only walk).
**Scale/Scope**: One read-only walk + two public metric fields + an overscan parameter + a11y position fields.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (overscan, offscreen addressability, new
metrics, a11y position). Public delta is field/param additions on already-baselined types ⇒ zero new baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; backfill restores the chain. Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | `countVirtual`/`VirtualMaterialized`/`VirtualTotal` internal; public additions on already-baselined types. Imported `.fs` redundant modifiers — shared DF-1 (E1), not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | Read-only walk + edge-clamped range arithmetic; no SRTP/reflection. |
| IV. Elmish/MVU boundary | ✅ Pass | Offscreen addressing updates the logical Model; the count walk is a pure projection; the host script drives the real seam. |
| V. Test evidence mandatory | ✅ Pass | US1 bounded/non-scaling, US2 overscan parity (overscan-0 counterfactual), US3 offscreen + a11y, US4 metrics. Readiness authored in `/speckit-implement` (114 imported without it). |
| VI. Observability & safe failure | ✅ Pass | `countVirtual` is read-only/total (0/0 with no virtualized control); overscan is edge-clamped (no fabricated/out-of-range rows); negative overscan clamps to 0. |

**Gate result**: PASS (two deviations justified/recorded, inherited).

## Project Structure

### Documentation (this feature)

```text
specs/114-virtualization-counts/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/virtualization-counts.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (114 imported without evidence)
```

### Source Code (repository root)

```text
src/Controls/RetainedRender.fsi / .fs   # countVirtual walk; WorkReductionRecord.VirtualMaterialized / VirtualTotal (internal)
src/Controls/Collections.fsi / .fs      # CollectionModel.Overscan; visibleRange (+overscan param) (public, additive)
src/Controls/Types.fsi                  # CollectionPosition; AccessibilityMetadata.Collection (public, additive)
src/Controls.Elmish/ControlsElmish.fsi  # FrameMetrics.VirtualItemsMaterialized / VirtualItemsTotal (public, additive)
tests/Controls.Tests/Feature114OverscanTests.fs · Feature114OverscanParityTests.fs · Feature114OffscreenTests.fs · Feature114AccessibilityTests.fs
tests/Elmish.Tests/Feature114VirtualMetricsTests.fs
```

**Structure Decision**: Single F# solution. 114 adds no project; counting internal, public touches additive on
baselined types. Surface baselines remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Counting + overscan + suites imported wholesale (task C7). | Re-deriving from a fresh spec discards working, evidence-backed code. |
| No `readiness/` imported | 114 arrived with suites but no captured evidence. | Authoring readiness from the suites is cheaper/honest. |
| Redundant access modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2; shared DF-1 (E1), not bundled. |
| Several public field/param additions across `FrameMetrics`/`CollectionModel`/`Collections`/`Types` | The counts, overscan, and a11y position must be consumer-visible. | The baseline is type-granular, so additive members are zero new delta; new types would be heavier and unnecessary. |
</content>
