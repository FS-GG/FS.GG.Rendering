# Implementation Plan: Retained Pointer Routing → Authored Control ID (Feature 110)

**Branch**: `110-retained-authored-routing` | **Date**: 2026-06-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/110-retained-authored-routing/spec.md`

## Summary

Feature 110 routes a pointer interaction entirely from the **retained** frame with no `host.View` and no
`Control.renderTree` rebuild: `authoredControlIds` reproduces the keyed-OR-in-`BoundIds` authored-id climb
(098) from retained identity, and `routeRetainedInteraction`/`routeRetainedPointer` dispatch the **same**
message list the preserved full-render oracle would. The oracle survives only as a parity reference and a
**counted escape hatch** — `FullRenderFallbackCount` increments by exactly one on an unresolvable bindable
hit, `0` on every normal path.

**This is a backfill plan** (task **C5**). The implementation, the accreted surface (internal `authoredControlIds`
/ `routeRetained*` + the public additive `FrameMetrics.FullRenderFallbackCount`), and the three `Elmish.Tests`
suites already exist; 110 imported with **no `readiness/`**. The plan documents the design the code embodies,
confirms the constitution gates, and records the import-before-spec deviation. `/speckit-tasks` and
`/speckit-implement` reduce to a conformance pass.

## Technical Context

**Language/Version**: F# on .NET (`net10.0`), `LangVersion=latest`.
**Primary Dependencies**: Expecto (deterministic example-based tests; no FsCheck); the `Controls`
`retainedHitTest` (092), the keyed-OR-in-`BoundIds` authored-id scheme + `Control.nearestAuthored`/`MapPointer`
oracle (098), the move-coalescing infrastructure; `ControlsElmish.Perf`/host pointer seam. No new dependency.
**Storage**: N/A. Routing reads the retained frame carried in the host loop's mutable-ref state.
**Testing**: Default-tier headless across three `tests/Elmish.Tests/` suites reaching the internal routing via
`InternalsVisibleTo`: `Feature110RetainedRoutingTests` (US1 zero-render + coalescing), `Feature110RetainedRoutingParityTests`
(US2 dispatch parity incl. focus + MapPointer), `Feature110FallbackTests` (US3 counted fallback). Deterministic;
no GL.
**Target Platform**: Linux/dev; headless (scripted pointer interactions + message-list comparison).
**Project Type**: F# UI framework — internal routing in the `Controls`/`Controls.Elmish` runtime, exercised by `Elmish.Tests`.
**Performance Goals**: No wall-clock target. Goals are work-count/parity invariants: zero full renders per routed
event (SC-001/SC-002); dispatch-identical to the oracle (SC-003/SC-004); `FullRenderFallbackCount = 0` normal,
+1 on the unroutable case (SC-005/SC-006); ≤ 1 processed move per burst (SC-009).
**Constraints**: Zero new public-surface delta (FR-013) — routing internal; `FullRenderFallbackCount` additive on
the already-baselined public `FrameMetrics`. Routing total/deterministic; the oracle is the parity authority.
**Scale/Scope**: One internal routing path across `Controls` (`authoredControlIds`) and `Controls.Elmish`
(`routeRetained*`) + one public metric field. Reuses 092 hit-test, 098 authored-id, the coalescing infra.

## Constitution Check

*GATE: evaluated against `.specify/memory/constitution.md` v1.0.0.*

**Change classification**: **Tier 1** — alters observable behaviour (pointer events route without a full
render; a new fallback metric is surfaced). Public delta is the additive `FullRenderFallbackCount` on an
already-baselined type ⇒ zero new baseline lines.

| Principle | Status | Evidence / Justification |
|---|---|---|
| I. Spec → FSI → Tests → Impl | ⚠️ Justified deviation | Order inverted by import; this backfill restores the chain (spec/plan + confirm the `.fsi`/`.fsi` and the real-seam suites). Recorded in Complexity Tracking. |
| II. Visibility in `.fsi` | ⚠️ Pass with noted drift | Routing declared `internal` in `RetainedRender.fsi`/`ControlsElmish.fsi`; `FullRenderFallbackCount` on the public `FrameMetrics`. Imported `.fs` carries redundant access modifiers — shared DF-1 (Workstream E1), not fixed here. |
| III. Idiomatic simplicity | ✅ Pass | Pure map-building + dispatch; no SRTP/reflection/custom operators. |
| IV. Elmish/MVU boundary | ✅ Pass | Routing is a pure projection over the retained Model; dispatch is the existing MVU update. The suites drive the real host pointer seam. |
| V. Test evidence mandatory | ✅ Pass | US1 zero-render, US2 oracle parity (the counterfactual is the oracle itself), US3 counted fallback. Readiness authored in `/speckit-implement` (110 imported without it). |
| VI. Observability & safe failure | ✅ Pass | `authoredControlIds` is total (a node with no authored ancestor has no entry); the unresolvable case degrades to the counted oracle fallback, never a silent miss. |

**Gate result**: PASS (two deviations justified/recorded, inherited from the prior backfills).

## Project Structure

### Documentation (this feature)

```text
specs/110-retained-authored-routing/
├── plan.md · research.md · data-model.md · quickstart.md · spec.md · tasks.md
├── contracts/retained-authored-routing.md
├── checklists/requirements.md
└── readiness/            # AUTHORED in /speckit-implement (110 imported without evidence)
```

### Source Code (repository root)

```text
src/Controls/RetainedRender.fsi / .fs   # authoredControlIds (internal): retained-id -> authored ControlId
src/Controls.Elmish/ControlsElmish.fsi / .fs   # routeRetainedInteraction / routeRetainedPointer (internal); FrameMetrics.FullRenderFallbackCount (public, additive)
tests/Elmish.Tests/Feature110RetainedRoutingTests.fs        # US1 zero-render + coalescing
tests/Elmish.Tests/Feature110RetainedRoutingParityTests.fs  # US2 dispatch parity (incl. focus, MapPointer)
tests/Elmish.Tests/Feature110FallbackTests.fs               # US3 counted fallback
```

**Structure Decision**: Single F# solution. 110 adds no project; routing is internal, the metric additive.
Surface baselines under `tests/surface-baselines/` (`FS.GG.UI.Controls.txt`, `FS.GG.UI.Controls.Elmish.txt`)
remain byte-unchanged.

## Complexity Tracking

| Deviation | Why it exists | Why not the simpler/orthodox path |
|---|---|---|
| Contract-first order inverted (code before spec) | Routing + suites imported wholesale at migration (task C5). | Re-deriving from a fresh spec would discard working, evidence-backed code. |
| No `readiness/` imported | 110 arrived with suites but no captured evidence. | Authoring readiness in `/speckit-implement` from the existing suites is cheaper and more honest than fabricating it. |
| Redundant `internal`/`private` modifiers in `.fs` | Inherited from import. | Behavior-neutral Tier-2 cleanup; shared DF-1 (Workstream E1), not bundled here. |
| One public field added to `FrameMetrics` | The metric must be observable to consumers. | The baseline is type-granular, so the additive field is zero new baseline delta; introducing a new type would be heavier and unnecessary. |
</content>
