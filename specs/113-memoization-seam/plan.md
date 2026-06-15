# Implementation Plan: Memoization Seam (DataGrid) (Feature 113)

**Branch**: `113-memoization-seam` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/113-memoization-seam/spec.md`

## Summary

Feature 113 adds a control-internal `memoize` seam: keyed by stable `ControlId` + a structural dependency, a
`Hit` returns the same stored subtree instance (thunk not run), a cold/changed dependency `Miss`es
(recompute + store). The sole memoized site is the DataGrid row/column projection (a childless `data-grid`
leaf, dependency `(theme, evaluated box, dataGridCells)`). Equality is structural; reuse is observable as
`MemoHits`/`MemoMisses` → public `MemoHitCount`/`MemoMissCount`; an always-miss oracle proves memo-on ≡
memo-off byte-identical. An advisory `Diagnostics.stabilityReport` flags reuse-breaking inputs.

**This is a backfill plan** (task **C6**). The implementation, the accreted surface (internal seam + additive
public `FrameMetrics` fields + `stabilityReport`), and the four suites already exist; 113 imported with **no
`readiness/`**. `/speckit-tasks`/`/speckit-implement` reduce to a conformance pass.

**Recorded finding (E2, not fixed here):** the `MemoEnabled` doc-comment overstates the disabled path — it is
a 0/0 bypass (the `&&` short-circuits `memoize`), not "every node a miss". Routed to Workstream E2 to keep
this doc-only backfill uniform (behaviour-neutral, like DF-1).

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto (deterministic; no FsCheck); the retained render structure + stable
`ControlId`; the DataGrid projection (`Control.fs` `gridGeom`); `ControlsElmish.Perf.runScript`; public
`FrameMetrics`. No new dependency.
**Storage**: N/A. `MemoCache` rides the retained record carried frame-to-frame.
**Testing**: Headless across `tests/Controls.Tests/` (`Feature113MemoSeamTests` US1, `Feature113MemoParityTests`
US2, `Feature113StabilityDiagTests` US4) and `tests/Elmish.Tests/Feature113MemoMetricsTests` (US3), reaching
internals via `InternalsVisibleTo`. Deterministic; no GL.
**Target Platform**: Linux/dev; headless.
**Project Type**: F# UI framework — internal seam in `Controls`, metrics in `Controls.Elmish`.
**Performance Goals**: No wall-clock target. Goals: Hit reuses the same instance / no thunk (FR-004); memo-on ≡
memo-off byte-identical (SC-002); steady-state `MemoHitCount > 0`/`MemoMissCount = 0` (SC-004); idle/no-memoizable
0/0; stability findings exact (FR-011/FR-012).
**Constraints**: Zero new public-surface delta (FR-013) — seam internal; `MemoHitCount`/`MemoMissCount` additive
on the already-baselined public `FrameMetrics`. Pure/total/deterministic `memoize`.
**Scale/Scope**: One internal seam (`memoize` + `MemoCache`/`MemoEntry`/`MemoOutcome` + `MemoEnabled`) at one
site (data-grid leaf) + two public metric fields + the advisory `stabilityReport`.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (reuse + new metrics + diagnostic). Public
delta is two additive fields on an already-baselined type ⇒ zero new baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; backfill restores the chain. Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | Seam `internal`; metrics on public `FrameMetrics`. Imported `.fs` redundant modifiers — shared DF-1 (E1), not fixed here. **Plus** the `MemoEnabled` doc-comment narrative nit — recorded, routed to E2, not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | Map + pure function; structural equality; no SRTP/reflection. |
| IV. Elmish/MVU boundary | ✅ Pass | `MemoCache` is durable Model state; `memoize` is a pure projection; the host script drives the real seam. |
| V. Test evidence mandatory | ✅ Pass | US1 Hit/Miss, US2 parity (oracle counterfactual = always-miss), US3 metrics, US4 stability diagnostic. Readiness authored in `/speckit-implement` (113 imported without it). |
| VI. Observability & safe failure | ✅ Pass | `memoize` total; an absent key is a cold miss; `stabilityReport` is advisory (no failure). The disabled path is a safe 0/0 bypass. |

**Gate result**: PASS (deviations justified/recorded — DF-1 + the E2 narrative finding + import-before-spec).

## Project Structure

### Documentation (this feature)

```text
specs/113-memoization-seam/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/memoization-seam.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (113 imported without evidence)
```

### Source Code (repository root)

```text
src/Controls/RetainedRender.fsi / .fs   # MemoOutcome/MemoEntry/MemoCache/memoize, Memo/MemoEnabled fields, MemoHits/MemoMisses (internal)
src/Controls/Diagnostics(.fs)           # stabilityReport (advisory)
src/Controls.Elmish/ControlsElmish.fsi  # FrameMetrics.MemoHitCount / MemoMissCount (public, additive)
tests/Controls.Tests/Feature113MemoSeamTests.fs        # US1 Hit/Miss
tests/Controls.Tests/Feature113MemoParityTests.fs      # US2 memo-on ≡ memo-off + no staleness
tests/Controls.Tests/Feature113StabilityDiagTests.fs   # US4 stability diagnostic
tests/Elmish.Tests/Feature113MemoMetricsTests.fs       # US3 metrics over Perf.runScript
```

**Structure Decision**: Single F# solution. 113 adds no project; seam internal, metrics additive. Surface
baselines remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Seam + suites imported wholesale (task C6). | Re-deriving from a fresh spec discards working, evidence-backed code. |
| No `readiness/` imported | 113 arrived with suites but no captured evidence. | Authoring readiness from the suites is cheaper/honest. |
| Redundant access modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2; shared DF-1 (E1), not bundled. |
| `MemoEnabled` doc-comment narrative nit | Inherited from import; overstates the disabled path (0/0 bypass, not "every node a miss"). | A 1-line comment fix is behavior-neutral but a *source* edit; bundling it would break the uniform doc-only nature of these seven backfills. Recorded and routed to **Workstream E2** (its named home), not done here. |
| Two public fields added to `FrameMetrics` | Reuse must be observable. | Baseline is type-granular ⇒ additive fields are zero new delta; a new type would be heavier. |
</content>
