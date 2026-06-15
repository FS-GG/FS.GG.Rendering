# Implementation Plan: Frame-Rate Pacing & No-Alloc Idle Tick (Feature 121)

**Branch**: `121-paced-noalloc-tick` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/121-paced-noalloc-tick/spec.md`

## Summary

Feature 121 carries two live-host efficiency guards: **(US1) frame-rate pacing** — the pure decision
`GlHost.shouldAdvanceFrame` gates update + present so a consumer `ViewerOptions.FrameRateCap` bounds render
cadence (tighter cap ⇒ strictly fewer advances; non-positive cap rejected at validation as a `ProductDefect`);
and **(US2) no-alloc idle** — `advanceStateClocks` returns the per-identity state map reference-equal when no
clock is active (an idle live tick allocates nothing), active clocks advancing exactly as `advance` (099/103
unchanged).

**This is a backfill plan** (the 121 close, grouped with C3). The implementation, the accreted surface
(internal `advanceStateClocks`; public `shouldAdvanceFrame` + additive `ViewerOptions.FrameRateCap`, both on
already-baselined types), and the two suites already exist; 121 imported with **no `readiness/`**.
`/speckit-tasks`/`/speckit-implement` reduce to a conformance pass.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto (deterministic; no FsCheck); the per-identity `StateByIdentity` + `advance`
(099); the SkiaViewer host loop, `ViewerOptions`, and option validation. No new dependency.
**Storage**: N/A.
**Testing**: `tests/Controls.Tests/Feature121IdleTickTests.fs` (US2 — headless no-alloc core via
`obj.ReferenceEquals`); `tests/SkiaViewer.Tests/Feature121LiveHostPacingTests.fs` (US1 — deterministic-headless,
**not** GL-gated: the pacing tests call the pure `shouldAdvanceFrame`; the validation tests return before GL
init). Reaches the internal via `InternalsVisibleTo`.
**Target Platform**: Linux/dev; headless (the persistent window is not driven; the pure decision + the
validation seam are exercised).
**Project Type**: F# UI framework — internal idle gating in `Controls`, pacing decision/validation in `SkiaViewer`.
**Performance Goals**: No wall-clock target. Goals: paced cadence (tighter cap ⇒ strictly fewer advances)
(SC-001); reference-equal idle tick (no allocation) (SC-003); non-positive cap rejected (SC-005).
**Constraints**: Zero new public-surface delta (FR-005) — `advanceStateClocks` internal; `shouldAdvanceFrame`/
`FrameRateCap` on already-baselined public types. `shouldAdvanceFrame` pure; `advanceStateClocks` total.
**Scale/Scope**: One reference-equality idle gate + one pure pacing decision + option validation. 099/103 unchanged.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (paced cadence; allocation-free idle tick;
cap validation). Public delta is a public `val` + an additive field on an already-baselined type ⇒ zero new
baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; backfill restores the chain. Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | `advanceStateClocks` internal; `shouldAdvanceFrame`/`FrameRateCap` public. Imported `.fs` redundant modifiers — shared DF-1 (E1), not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | A reference-equality short-circuit + a pure boolean decision; no SRTP/reflection. |
| IV. Elmish/MVU boundary | ✅ Pass | `advanceStateClocks` is a pure Model transition; pacing is a pure decision interpreted at the host edge; the suites drive the pure decision + validation seam, not a live window. |
| V. Test evidence mandatory | ✅ Pass | US1 pacing (tighter-cap-fewer-advances + cap-rejection counterfactual), US2 no-alloc (reference-equality via `obj.ReferenceEquals`; active-clock rebuilt). Readiness authored in `/speckit-implement` (121 imported without it). |
| VI. Observability & safe failure | ✅ Pass | `shouldAdvanceFrame` total; a non-positive cap fails fast at validation as a `ProductDefect` (no silent misconfiguration); the idle path is allocation-free. |

**Gate result**: PASS (two deviations justified/recorded, inherited).

## Project Structure

### Documentation (this feature)

```text
specs/121-paced-noalloc-tick/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/paced-noalloc-tick.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (121 imported without evidence)
```

### Source Code (repository root)

```text
src/Controls/RetainedRender.fsi / .fs   # advanceStateClocks (internal): reference-equal when no clock is active
src/SkiaViewer/Host/OpenGl.fsi / .fs    # GlHost.shouldAdvanceFrame (public val): pure pacing decision (gates update + present)
src/SkiaViewer/SkiaViewer.fs            # ViewerOptions.FrameRateCap (default 60) + non-positive-cap ProductDefect validation
tests/Controls.Tests/Feature121IdleTickTests.fs            # US2 no-alloc core (obj.ReferenceEquals)
tests/SkiaViewer.Tests/Feature121LiveHostPacingTests.fs    # US1 pacing decision + validation seam (deterministic-headless)
```

**Structure Decision**: Single F# solution. 121 adds no project; the idle gate is internal, the pacing
decision/validation ride already-baselined public types. Surface baselines remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Idle gate + pacing + suites imported wholesale (121 close). | Re-deriving from a fresh spec discards working, evidence-backed code. |
| No `readiness/` imported | 121 arrived with suites but no captured evidence. | Authoring readiness from the suites is cheaper/honest. |
| Redundant access modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2; shared DF-1 (E1), not bundled. |
| Two user stories in one feature (pacing + no-alloc) | The imported feature 121 bundles both live-host efficiency guards. | They share the "live tick efficiency" theme; the spec scopes each as an independent story rather than splitting the feature number. |
</content>
