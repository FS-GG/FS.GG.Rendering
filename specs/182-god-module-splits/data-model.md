# Phase 1 Data Model: God-Module Splits (Feature 182)

This is a structural refactor, so the "data model" is (a) the two new internal records the spec names,
(b) the concern-module catalog (which file owns which seam), and (c) the surface-invariance entity that
binds the whole feature. None of these change any **public** package surface (Tier 2, FR-002).

## Entity: Surface-area baseline (the binding oracle)

The per-package `.fsi`-derived public-surface snapshot under `readiness/surface-baselines/*.txt`. It is
the authoritative oracle for "no public surface change" (FR-002, SC-001).

| Field | Value |
|-------|-------|
| Location | `readiness/surface-baselines/FS.GG.UI.<Pkg>.txt` (12 files) |
| Generator | `scripts/refresh-surface-baselines.fsx` (reads built assemblies; single authoritative writer) |
| Live gate | `tests/Package.Tests/SurfaceAreaTests.fs` + `build/Governance/PackageSurface.fs` (both READ the same dir) |
| Touched by this feature | `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Controls`, `FS.GG.UI.Scene`, `FS.GG.UI.Testing`, `FS.GG.UI.Controls.Elmish` |
| Invariant | `git diff --exit-code readiness/surface-baselines/` is **empty** after every story (zero baseline edits) |

A required edit to any of these files means a split overshot into Tier 1 — the offending split is
reverted/re-scoped (FR-009), never baselined forward.

## Entity: Byte-stability baseline

The pre-change capture of regenerated readiness/evidence artifacts (Markdown + JSON), viewer
observations/screenshots, scene hashes/fingerprints, and damage regions, plus the full
`*.Tests.fsproj` red/green set. Diffed after each story (FR-003, FR-008, SC-002/003).

| Field | Value |
|-------|-------|
| Surface snapshot | the 12 `*.txt` baselines (above) |
| Artifact snapshot | regenerated readiness/evidence MD+JSON for touched subsystems |
| Render snapshot | viewer observations/screenshots, scene hashes, fingerprints, damage regions |
| Test snapshot | `dotnet fsi scripts/baseline-tests.fsx --out specs/182-…/readiness/baseline/` |
| Comparison | byte-for-byte diff (`post-change/` vs `baseline/`); red/green set equality |

## Entity: `StepMetrics` (US5 — internal record)

The extracted record replacing `RetainedRender.step`'s ~30 ad-hoc `let mutable` accumulators
(`RetainedRender.fs:1424…`), threaded through named passes. **Internal** to `FS.GG.UI.Controls` — does
NOT appear in `Control.fsi`/`RetainedRender.fsi`; the surface diff confirms this.

| Field group | Captured accumulators (illustrative, from HEAD) |
|-------------|--------------------------------------------------|
| Layout/id | `nextId`, `recomputed`, `changedBound`, `shifted` |
| Memo | `memo`, `memoHits`, `memoMisses` |
| Text cache | `tc`, `textHits`, `textMisses` |
| Metadata | `metadataVisited` |
| Virtualization | `virtualMaterialized`, `virtualTotal` |
| Picture cache | `pcEntries`, `pcClock`, `pictureHits`, `pictureMisses` |
| Replay | `replaySkippedNodes`, `replayNativeBytes` |

- **Validation rule**: rendered scene, damage regions, metrics, and promotion decisions are
  byte-identical to baseline (FR-007, SC-002, US5 acceptance #1).
- **State transition**: passes mutate fields in place where that is the simpler/faster code (hot path,
  disclosed with `// mutable: hot path`, Constitution III); the record is the single unaliased
  accumulator the passes thread.
- **Shared scaffold**: the build/paint scaffolding duplicated between `init` (`:1254`) and `step`
  (`:1424`) is unified so neither duplicates the other (FR-007, US5 acceptance #2).

## Entity: `FrameLoopState` (US6 — internal record)

The extracted record replacing `runInteractiveAppWithLauncher`'s ~20 `ref` cells of ad-hoc frame state
(`ControlsElmish.fs:1186…`). **Internal** to `FS.GG.UI.Controls.Elmish` — does NOT appear in
`ControlsElmish.fsi`; the surface diff confirms this. It is interpreter-edge state, not the Elmish
`Model` (Constitution IV — the boundary is preserved, not crossed).

| Field (from HEAD `ref` cells) | Type |
|-------------------------------|------|
| `pointerState` | `Pointer.State` |
| `focused` | `RetainedId option` |
| `retained` | `RetainedRender<'msg> option` |
| `lastRender` | `ControlRenderResult<'msg> option` |
| `lastView` | `(Size * 'model * Control<'msg>) option` |
| `lastRuntimeModel` | `ControlRuntimeModel option` |
| `scrollOffsets` | `Map<ControlId, ScrollState>` |
| `surfacedDiagnostics` | `Set<string>` |
| `pendingMove` | `ViewerPointerInput option` |
| `pointerSampleCount` | `int` |
| `lastWorkReduction` | `WorkReductionRecord option` |
| `lastPresentTiming` | `TimeSpan * TimeSpan` |

- **Validation rule**: frame-loop transitions, emitted commands, and render-lag traces match baseline
  (FR-007, US6 acceptance #1).
- **State transition**: module-level functions over `FrameLoopState` replace the ad-hoc closures;
  mutation retained where simpler/faster per-frame (disclosed, Constitution III).

## Entity: Concern-scoped module (the carve catalog)

A newly-extracted file carved from a god-module along an existing seam, contributing the **same**
public surface to its package as before (`module internal` or new internal `.fsi`; no external surface
change). The planned catalog (binding requirement = surface union + byte-stability, not exact filenames):

| Story | Target | Extracted concern modules (planned) | Residual |
|-------|--------|-------------------------------------|----------|
| US1 | `SkiaViewer.fs` | `Viewer.Types`, `ViewerResponsiveness`, `ViewerWindowBehavior`, `ViewerRunLoops` (+ unified scaffold), `ViewerEvidence` | `SkiaViewer.fs` = app/interactive runners + public `module Viewer` |
| US2 | `Control.fs` `ControlInternals` | `ChartGeometry` (+ `withPoints` + bar-layout), `WidgetGeometry`, `SceneFingerprint`, `LayoutEval`, `NodeAssembly` | `Control.fs` = assembly glue + public `module Control` |
| US3 | `Scene.fs` | `SceneTypes` (~767-line block), `VisualInspection`, `RetainedInspection` (shared dedup), `LayoutEvidence`, `SceneEvidence` | `Scene.fs` = root primitives + isolated `realTextMeasurer` |
| US4 | `Testing.fs` | `TestingVisual`, `TestingRetainedInspection`, `TestingEvidence`, `TestingCompositor`, `TestingFeatureReadiness` | `Testing.fs` = residual glue / re-exports |
| US5 | `RetainedRender.fs` | `StepMetrics` + named passes (internal) | `RetainedRender.fs` = `step`/`init` over shared scaffold |
| US6 | `ControlsElmish.fs` | `FrameLoopState` + module functions (internal) | `ControlsElmish.fs` = `runInteractiveAppWithLauncher` over typed state |

- **Validation rule (all rows)**: each touched package's `.fsi` + `*.txt` baseline unchanged; per-file
  size toward SC-005 (≤ ~1,500 lines / ≤ ~150 lines per function), except FR-009 retentions recorded.
- **Constraint (all rows)**: new files inserted *before* the residual file in `.fsproj` compile order;
  no back-edge, no new cycle, no new project/dependency/inter-project reference (FR-010, D-002).

## Relationships

```
Surface-area baseline ──gates──> every Concern-scoped module split (FR-002, SC-001)
Byte-stability baseline ──gates──> StepMetrics, FrameLoopState, every split (FR-003, SC-002/003)
StepMetrics ──replaces──> step's ~30 let-mutable accumulators (US5)
FrameLoopState ──replaces──> runInteractiveAppWithLauncher's ~20 ref cells (US6)
Concern-scoped module ──preserves──> package public-surface union (Constitution II)
```
