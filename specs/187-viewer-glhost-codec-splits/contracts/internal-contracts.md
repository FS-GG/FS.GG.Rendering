# Phase 1 Contracts: Internal-Helper Seams

**Feature**: 187-viewer-glhost-codec-splits | **Date**: 2026-06-22

This feature introduces **no public interface contract** — the deliverable is that the three public
contracts stay **byte-identical**. The "contracts" here are therefore (A) the frozen public
surfaces this refactor must not perturb, and (B) the internal seams the split introduces, expressed
as the invariants they must satisfy.

---

## A. Frozen public contracts (must diff empty)

| Surface | File | Baseline |
|---|---|---|
| `Viewer` / `GeneratedAppHost` / `Text` | `src/SkiaViewer/SkiaViewer.fsi` (191 lines) | `readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` |
| `GlResources` / `GlStartup` / `GlHost` | `src/SkiaViewer/Host/OpenGl.fsi` (324 lines) | (same `FS.GG.UI.SkiaViewer.txt` — one assembly) |
| package types + `SceneCodec` fns | `src/Scene/SceneCodec.fsi` (177 lines) | `readiness/surface-baselines/FS.GG.UI.Scene.txt` |

**Contract test**: `tests/Package.Tests/SurfaceAreaTests.fs` + a manual
`dotnet fsi scripts/refresh-surface-baselines.fsx` whose resulting `git diff` on the two baseline
files is **empty**. Any non-empty diff is a feature failure (FR-007), not a baseline update.

**Permitted exception** (precedent: feature 186): an additive `module internal …` line in a `.fsi`
that is *not* part of the consumed public surface. Avoid if achievable; if used, it must be
internal-only and must not change any existing public signature.

---

## B. Internal seam contracts

### B1. `NodeCodec` table (US3) — `src/Scene/SceneWire.fs`

| Invariant | Statement | Oracle |
|---|---|---|
| Round-trip identity | `import (export s) ≈ s` for every scene in the corpus | `Feature146PortableSceneRoundTripTests` |
| Byte exactness | `export s` produces identical bytes pre/post refactor | round-trip + `Feature146` canonical-bytes assertions |
| Symmetry | every `Write` tag has a matching `Read`; no orphan | `Feature183CodecSymmetryTests` + the table is one value (structural) |
| Exhaustiveness | a new `SceneNode` case with no entry is a compile error | `FS0025` on the write `match` |
| Fail-loud | unknown tag / truncated input → same diagnostic as today | existing import-failure tests; manual truncated-bytes probe |

### B2. Window lifecycle scaffold (US1) — `src/SkiaViewer/ViewerWindow.fs`

| Invariant | Statement | Oracle |
|---|---|---|
| Presented-path behavior | input-queue drain, pointer/resize/scripted inputs, present-mode honored | `Feature118PresentModeTests`, `Feature122PresentPathTests`, `Feature167*` |
| Legacy-path behavior | warmup-FIFO ordering (cap 64, drop-oldest + diagnostic), key-only | `Feature085InteractiveHostTests`, `NativeStartupCleanupTests` |
| State order | lifecycle refs/mutables threaded in the same sequence | host/live-proof suites; frame/trace equivalence vs baseline |
| Close handling | same `ViewerCloseReason` classification + handler teardown | host suites |

### B3. Viewer body groups (US1) — `ViewerInputQueue.fs` / `ViewerResponsiveness.fs` / `ViewerEvidence.fs`

| Invariant | Statement | Oracle |
|---|---|---|
| Pure-function identity | `enqueueInput`/`drainInputQueue`/`dirtyState`/summary encoders return identical values | `Feature167InputQueueTests`, `Feature167SchedulerDrainTests`, `Feature167ResponsivenessSummaryTests` |
| Evidence equivalence | screenshots / evidence-workflow artifacts equivalent (byte or semantic) | `Feature14x`/`Feature15x` proof + harness evidence suites |
| Trace seam | `traceStartCapture`/`traceDrainCapture`/`traceEmit` behavior unchanged | `Feature175TraceReadbackTests` |

### B4. GlHost units (US2) — `src/SkiaViewer/Host/GlHostRun.fs`

| Invariant | Statement | Oracle |
|---|---|---|
| `run` signature | `ViewerProgram<'model,'msg> -> Result<unit, RenderDiagnostic>` unchanged | `OpenGl.fsi` diff empty |
| Present/damage decisions | the public pure decisions still drive the loop with identical outcomes | `Feature11x`/`Feature147`/`Feature148`/`Feature157` damage + present suites |
| GL resource order | acquire/release order + ledger unchanged | `Feature119OpenGlHostTests`, `NativeStartupCleanupTests` |
| Fail-loud | GL-context failure distinguishes defect vs missing window-system; screenshot-before-first-frame errors | `Feature142FallbackDiagnosticsTests`, smoke suite |

---

## C. Out of contract (explicitly)

- No golden-image / golden-hash / perf-budget gate is stood up here (§7 — deferred to Phases 5–6;
  this phase is behavior-preserving, spec Assumptions).
- No change to `Scene.fs`, `Control.fs`, `RetainedRender.step` (later phases).
- No version bump, no `.nuspec`/package-id change, no `.fsproj` reference change (FR-010) — only
  `<Compile Include=…>` additions for the new internal files.
