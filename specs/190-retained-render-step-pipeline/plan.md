# Implementation Plan: `RetainedRender.step` Pipeline Decomposition (Pattern B + C)

**Branch**: `190-retained-render-step-pipeline` | **Date**: 2026-06-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/190-retained-render-step-pipeline/spec.md`

## Summary

`RetainedRender.step` (`src/Controls/RetainedRender.fs`, the per-frame frame function spanning
~lines 1500–2115, ≈615 lines) is the single largest and riskiest function in the repo: it diffs
the prev/next control tree, derives the layout dirty set and runs incremental layout, walks the
keyed reconciler (`build`/`carry`/`buildFresh` with Keep/Replace/Update + child ops), makes
fragment-reuse / memo / picture-cache / text-cache decisions, tallies virtualization and damage,
collects offscreen/state diagnostics, samples animation clocks, and assembles a 40-field
`WorkReductionRecord` plus the render result. `init` (~lines 1313–1426, ≈113 lines) is a parallel
cold-start copy of the build/paint/seed scaffolding.

Phase 2 of the campaign (feature 186) already replaced the loose `let mutable` accumulators with the
`FrameState` record (`RetainedRender.fs:1291`), threaded through both `step` and `init`. This phase
(the campaign's **final** Phase 6, Pattern B) builds on that record to re-express `step` as a short
composition of four named, independently unit-testable internal stages — **diff → layout → paint →
assembly** — that thread `FrameState` and an explicit per-frame context, and (US2, conditional)
converges `init` onto the shared paint/assembly stage bodies in their cold-start configuration.

**Technical approach** (recommended; the compile-probe task in Phase 0 confirms or substitutes it):
extract the four stages as named `let internal` functions **inside** `module internal RetainedRender`
(this intentionally departs from the campaign report's §4.1 sketch, which drew the stages as separate
`module DiffStage/LayoutStage/…`; separate modules compiled before `RetainedRender.fs` would create a
producer→consumer back-edge into the in-module helper graph, so in-file functions are the back-edge-free
realization of the same Pattern B — see research R3)
(so the dense web of in-module helpers — `assembleRetainedNode`, `retainedFragment`,
`retainedMetadata`, `memoize`, `pictureKeyOf`, `updateClockForState`, `sampleOnPaint`,
`renderFromRetainedMetadata`, … — stays in scope and **no producer→consumer back-edge** is created,
FR-009). The current nested closures (`mint`/`paintOwn`/`paintFresh`/`buildFresh`/`carry`/`build`,
`measureCached`, the post-build walks) capture `st`/`theme`/`boundsById`/`prev`/`themeChanged`; the
stage functions take those as explicit parameters instead, so the operation sequence — and therefore
the float/integer accumulation order and allocation profile — is byte-identical **by construction**.
Stage entry points + `FrameState` + the new `FrameContext` input record are surfaced as
`val internal`/`type internal` in `RetainedRender.fsi` so `Controls.Tests` can exercise each stage in
isolation via `InternalsVisibleTo` (FR-003) — all `internal`, so the **public** package surface
(`readiness/surface-baselines/FS.GG.UI.Controls.txt`) is unchanged (FR-004/FR-014, no bump).

The SC-001 size goal (no resulting file > ≈1,500 lines; `RetainedRender.fs` is 2,173 today) is met by
relocating the **step-independent** policy/diagnostic cluster — the feature-159 reuse/promotion family,
the feature-147 damage/compositor helpers, `snapshotVerdict`, `promotionDecision` (≈lines 600–1290,
already pure `val internal`, **no** dependency on the step pipeline) — into one `Internal/` file
compiled before `RetainedRender.fs` (Pattern E; no back-edge because the cluster does not reference the
stages or the retained walk). This is a name-qualifier change for internal test call sites only; the
public surface and all rendered output stay unchanged.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is a structural refactor, not a defect fix, so there is no root-cause hypothesis to confirm.
> The analogue here is the **byte-identity hypothesis**: the claim that threading `FrameState` through
> named stages preserves frame bytes/hashes is *provisional* until the regression gate (US3) runs the
> real scene corpus through the decomposed `step` and the existing perf/responsiveness lanes. `/speckit-tasks`
> MUST schedule the **baseline capture + injected-regression gate proof** in the Foundational phase
> (before any stage edit) so US1 is built on a verified gate, not on the assumption alone.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (single library project `src/Controls`, `FS.GG.UI.Controls`).

**Primary Dependencies**: SkiaSharp over **OpenGL (GL)**; the internal `Reconcile` keyed reconciler
(feature 067), `ControlInternals` (layout/paint), `FS.GG.UI.Scene`, `FS.GG.UI.Layout`,
`FS.GG.UI.DesignSystem`. No new project or package reference is introduced (FR-013).

**Storage**: N/A (in-memory retained render structures; no persistence).

**Testing**: Expecto + FsCheck via `tests/Controls.Tests` (reaches internals through
`InternalsVisibleTo("Controls.Tests")`), `tests/Elmish.Tests` (the per-frame metrics/responsiveness/
work lanes), `tests/Package.Tests/SurfaceAreaTests.fs` (public-surface drift). GL-dependent suites run
under `DISPLAY=:1` (X11) locally.

**Target Platform**: Linux desktop (X11/GL) for validation; library is platform-neutral.

**Project Type**: Single F# class library + its test assemblies (Option 1, single project).

**Performance Goals**: Per-frame render hot path. No regression in **per-frame allocation count** or
**frame time** beyond the agreed budget margin on the existing perf/responsiveness lanes
(features 160/161/167/173) — FR-006/SC-004. Byte-identical emitted scenes and `hashScene` fingerprints
over the baseline corpus (FR-005/SC-002).

**Constraints**: No new circular module dependency / no producer→consumer back-edge in `src/Controls`
(FR-009). Fail-loud (`KeyCollision`, swallow-free seams) preserved (FR-010). All
`RetainedRenderTrace.time "retained-step-*"` spans preserved with equivalent coverage (FR-008). Public
`RetainedRender` surface (`step`/`init`/result types) unchanged (FR-004). No existing test deleted,
skipped, or weakened (FR-011, Constitution V). Mutation permitted on the threaded `FrameState` hot path
(Constitution III, `// mutable: hot path` already disclosed).

**Scale/Scope**: One ~615-line function + one ~113-line function decomposed; `RetainedRender.fs` is
2,173 lines today (target: ≤≈1,500 after relocation); no individual stage body > ≈250 lines (SC-001).
Stage boundaries map onto the existing nine `retained-step-*` trace seams (diff, layout-dirty-set,
layout-incremental, build, count-virtual, damage-reduce, picture-walk, offscreen-diagnostics,
index-prior-own, state-collect, scene-assembly, render-result, work-node-count).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

This is a **Tier 2 (internal change)** refactor: no public API surface change, no new dependency, no
observable behavior change (byte-identical output is the explicit target).

| Principle | Status | Notes |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | ✅ | `.fsi` (`RetainedRender.fsi`) is updated FIRST to add the `val internal` stage entry points + `type internal FrameState`/`FrameContext`; stage unit tests are authored against that signature before the `.fs` bodies move (FR-003). Existing FSI-level semantic suites stay green. |
| **II. Visibility Lives in `.fsi`, Not in `.fs`** | ✅ | No `private`/`internal`/`public` modifier added to top-level `.fs` bindings. Visibility is declared by presence in `RetainedRender.fsi`: stage entry points become `val internal`; `FrameState` is promoted from `type private` to a `type internal` listed in the `.fsi` (needed for FR-003). All additions are `internal` ⇒ **public** surface baseline unchanged. |
| **III. Idiomatic Simplicity Is the Default** | ✅ | Plain named functions + record threading; no new operators/SRTP/reflection/CEs/type-providers/active-patterns. Existing hot-path `mutable` on `FrameState` is retained and already disclosed (`// mutable: hot path`). The change *reduces* cleverness (named stages replace a 615-line closure nest). |
| **IV. Elmish/MVU boundary** | ✅ (N/A) | `step`/`init` already sit at the interpreter edge of the host loop; this refactor does not move the MVU boundary or add I/O. No `Model`/`Msg`/`Cmd` change. |
| **V. Test Evidence Is Mandatory** | ✅ | Baseline captured before any edit (FR-012); new per-stage unit tests fail-before/pass-after (FR-003/SC-003); the regression gate must demonstrably catch an injected regression (FR-015/SC-008). No assertion weakened; no test skipped. Any golden-hash delta is reviewed+recorded, never silent (FR-005). |
| **VI. Observability and Safe Failure** | ✅ | All `retained-step-*` trace spans preserved (FR-008); `KeyCollision` and other fail-loud diagnostics keep firing at every seam (FR-010); no seam swallows an exception. |

**Gate result: PASS** — no violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/190-retained-render-step-pipeline/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — stage-carving decisions + compile-probe protocol
├── data-model.md        # Phase 1 — FrameState / FrameContext / stage I/O entities
├── quickstart.md        # Phase 1 — baseline capture + regression-gate run guide
├── contracts/
│   └── stage-contracts.md   # Phase 1 — the `val internal` stage signatures (the .fsi additions)
├── checklists/          # (pre-existing)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/Controls/
├── Controls.fsproj                 # compile-order owner; InternalsVisibleTo("Controls.Tests")
├── RetainedRender.fsi              # ADD: val internal diffStage/layoutStage/paintStage/assemblyStage,
│                                   #      type internal FrameState, type internal FrameContext
├── RetainedRender.fs               # step → 4-stage composition; init → shared stages (US2);
│                                   #      target ≤≈1,500 lines after the cluster relocation
└── Internal/
    ├── AttrKeys.fs  Hashing.fs  ControlPrimitives.fs  ChartGeometry.fs
    ├── WidgetGeometry.fs  SceneHash.fs  ContentRender.fs  LayoutEval.fs  NodeAssembly.fs
    └── CompositorPolicy.fs         # NEW (size relocation): the step-INDEPENDENT feature-159 reuse/
                                    #      promotion + feature-147 damage/snapshot policy cluster
                                    #      (Pattern E; compiled before RetainedRender.fs; no back-edge)

tests/Controls.Tests/
└── Feature190StagePipelineTests.fs # NEW: per-stage isolation unit tests (FR-003/SC-003) +
                                    #      injected-regression gate proof (FR-015/SC-008)

tests/Elmish.Tests/                 # existing per-frame metrics/responsiveness/work lanes (160/161/167/173)
tests/Package.Tests/SurfaceAreaTests.fs   # public-surface drift gate (expect empty diff → no bump)
readiness/surface-baselines/FS.GG.UI.Controls.txt   # 486-line baseline; expected UNCHANGED
```

**Structure Decision**: Single-project (Option 1). All production edits stay within `src/Controls`
(FR-013). New internal stage code follows the **188/189 precedent**: `module internal` bodies either
remain inside `module internal RetainedRender` (the four stages) or live under `src/Controls/Internal/`
(the relocated policy cluster), reached by tests via `InternalsVisibleTo`, never on the public surface.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
