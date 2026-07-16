# Run-core staging — execution plan & status

**Date:** 2026-07-16 07:50 (+0200)
**Scope:** `src/SkiaViewer/ViewerRuntime.fs` — completing the Pattern-B pipeline staging of the viewer
run-core, after the `SkiaViewer.fs` god-module decomposition (F-CORE-1).
**Status:** Safe stages landed; the delicate remainder (B/C/D) scoped for a focused, test-first pass.
Companion analysis: [`2026-07-16-viewer-run-core-staging-plan.md`](./2026-07-16-viewer-run-core-staging-plan.md).

---

## 1. Where this stands (done & merged)

The F-CORE-1 architecture-review finding ("`SkiaViewer.fs` is a 3,126-line god module") is resolved by
dissolving the file behind a facade and decomposing the implementation, all behind a §7 golden-image
regression gate. **`SkiaViewer.fs`: 3,126 → 185 lines** (a thin public facade); the implementation lives
in `module internal ViewerRuntime` (2,029 lines) plus focused sibling modules.

| PR | What landed |
|---|---|
| #816 | §7 **golden-image gate** (`Rendering.Harness.GoldenImage` + `GoldenImageGateTests`) — per-pixel corpus comparison, fail-closed, byte-identical in-repo |
| #817 | `DiagnosticsFiltering`/`WindowBehaviorValidation`/`HostCapability` → own files; evidence-writer cluster → `ViewerEvidence.fs` |
| #819 | launch/window helper cluster (incl. `makeFailure` hub) → `ViewerLaunchSupport.fs` |
| #820 | **facade split**: `module Viewer` → 68 delegating facades over `module internal ViewerRuntime`; `RenderLagTrace` → own file. Isolates the `.fsi` surface, unblocking free internal decomposition |
| #821 | window-classification cluster → `ViewerWindowClassify.fs` |
| #823 | responsiveness-reporting cluster → `ViewerResponsivenessReport.fs` |
| #824 | run-core **Stage A** (`validateLaunch`) + this plan's companion analysis |
| #825 | run-core **Stage E** (`assembleLaunchOutcome`) + delete dead `update Start` call |
| #826 | coverage revision (existing seam-tests gate the danger zones better than first assessed) |

Verified on every axis: SkiaViewer.Tests 362, golden-image gate 314 (byte-identical each cut), full CI
green (API-compat, Deterministic, Generated-product, Packaged-consumer, Lifecycle) on every PR, and the
live run path exercised end-to-end (`offscreen` → `runBounded` → `update`/`updateRun` → `SceneRenderer`,
status `passed`, real non-blank frames).

### Reframing that came out of the analysis

The "loops + `update` knot" is **not one knot**. `update`/`updateRun` are already pure and already
decoupled from the two persistent loops (which drive the *product's* `host.Update`, not the viewer's own
`update` — that pair is driven only by the separate `runBounded` path). So the remaining "staging" is
**de-duplicating the two near-clone front-ends** (`runGeneratedApp`, `runInteractiveViewerWithWindowBehaviorCore`)
into shared, risk-classified stages — not restaging `update`.

---

## 2. Remaining work — Stages B, C, D

Line numbers below are approximate (they drift as stages land) — **re-locate by name before editing**.
Both front-ends already share the bookends (A `validateLaunch`, E `assembleLaunchOutcome`); B/C/D are the
middle.

### Stage B — `initProductState` (MEDIUM, do first of the three)

**Extract:** the per-loop product-state bootstrap — `currentModel`/`currentScene`/`inputDispatch`/size
state + the `reportProductDefect`/`safeView`/`onScene`/`onInputDispatch`/`onDiagnostic`/`evidenceSink`
closures — into a shared `initProductState`, parameterized by the divergences.

**Divergences that MUST stay parameterized (do not collapse):**
- **Size model.** generated-app tracks ONE size (`currentSurfaceSize` = `InitialSize`); interactive
  tracks THREE (`currentSurfaceSize` physical, `currentWindowSize` logical, `currentSize` = the space
  `View`/pointer speak). Model the state record with the fields both need; interactive threads all three,
  generated-app threads the one. Do not force generated-app onto the 3-size model or vice versa.
- **`firstView` closure.** `host.View currentModel` (generated-app) vs `host.View currentSize currentModel`
  (interactive) — pass as an argument.
- **`evidenceSink` size.** rasterizes at `InitialSize` (generated-app) vs `currentSize` (interactive).
  This is behaviour-observable (evidence image dimensions) — pass the size function in.

**Gating tests (already exist — this is the coverage-revision finding):**
- `tests/SkiaViewer.Tests/Issue246LogicalSizeTests.fs` — logical/surface fit arithmetic +
  `captureScreenshotEvidence` at explicit sizes.
- `tests/SkiaViewer.Tests/Issue396FirstFrameFaultTests.fs` — the `tryFirstProductView` first-view guard.
- Add one focused assertion that the evidence image dimensions differ per loop as today (InitialSize vs
  currentSize) so a misparameterized size reds.

**Approach:** define a small mutable state record; move the shared closures into a factory returning it +
the guarded initial scene; each front-end calls `initProductState host firstView evidenceSize …`. Verify:
golden gate + Issue246/396 + Deterministic + a fresh dimension assertion.

### Stage C — `buildDispatchCore` (HIGH — the persistence assembly)

**Extract:** `interpretEffects`/`dispatchHostMsg`/`initialCloseRequested`, accommodating both the
generated-app `let rec interpretEffects … and persistenceBatchSink … and dispatchHostMsg` mutual-recursion
knot AND the interactive plain path (a degenerate case where `persistenceBatchSink` never re-enters).

**What is already gated vs what is not:**
- **Gated (seams):** `Issue535PersistenceSeamTests.fs` drives `Viewer.interpretViewerEffects` and
  `Viewer.dispatchPersistenceBatch` directly — dispatch order, fold routing (only `Persist` reaches the
  sink), outcome→product-message dispatch, drop-not-invent, close propagation. These functions are
  `internal` precisely so the test reaches wiring the windowed launch can't.
- **NOT gated (the risk):** the `let rec … and …` **assembly** inside `runGeneratedApp`, and the sticky
  `outcomeCloseRequested`. The code's own comments warn: a stray forward-mutable resurrects the
  Init-load-dropped bug; a stray persistence-from-outcome emit **stack-overflows uncatchably**.

**Author BEFORE touching C:** an assembly-level test — a scripted `runAppWithPersistence` round-trip on a
host that DOES open a window (the SecondAntShowcase live lane, `DISPLAY=:1`), asserting an outcome-driven
message both dispatches back as a product message AND can request close. This is the missing gate; write
it first, watch it pass on today's code, THEN extract.

**Approach:** parameterize on a `persistenceBatchSink` closure and a `traceHooks` record (no-op for
generated-app, real for interactive), keeping the `let rec … and …` form for both. Do not flatten the
recursion.

### Stage D — `installInputHandlers` (HIGH — repaint placement)

**Extract:** `handleTick`/`handleKey` (+ interactive-only `handlePointer`/`handleFramebufferResize`).
`handleTick` and `inputVerified` are byte-identical today. `handleKey` unifies the option-vs-list shape by
treating generated-app's `msg option` as a 0/1-element list.

**Danger:** `runtimeStateRepaint` placement (the no-message repaint must run on THIS input, after the
dispatch fold) is the "focus one click behind" class. The generated-app `handleKey` repaints only on the
**no-message** branch — a real asymmetry with interactive; do NOT "unify" it away.

**Author BEFORE touching D:** a focus-latency assertion (the SecondAntShowcase responsiveness lane already
measures input→visible latency; extend it to pin that a keyboard focus change is visible on the SAME
frame, per branch). Then extract, keeping the asymmetric repaint explicit.

---

## 3. Danger zones — coverage status

| # | Danger zone | Location | Gated by |
|---|---|---|---|
| 1 | `updateLegacy` close-decision order | `~405–414` | scripted-run tests (partial); keep the `||` order + `pumpScript → drain → scriptWantsClose → onTick` |
| 2 | `drainQueuedInputs` order (discrete ++ coalesced-pointer ++ deferred) | `~353–362` | responsiveness/lag-trace lane |
| 3 | `scriptedCompletionFrames`/`framePresented` counters | `~241, 303–321, 415–424` | script-run frame-count tests |
| 4 | persistence `let rec` + sticky `outcomeCloseRequested` | `~1304–1340` | **seams** gated (Issue535); **assembly** NOT — needs the Stage-C test above |
| 5 | `runtimeStateRepaint` placement | `~1380, 1601, 1652` | needs the Stage-D focus-latency test |
| 6 | `handleFramebufferResize` conditional re-derive + `setLiveAuthoringSizeOverride` | `~1673–1677` | Issue246 (size) partial; interactive-only |
| 7 | `FrameRateCap`/`TargetFrameRate` default `Option.orElse (Some 60)` | `~247` | Feature118 present-mode tests (partial) |

---

## 4. Guardrails & rules for every stage

- **Public surface stays byte-identical.** `SkiaViewer.fsi` is unchanged: both front-ends are `private`;
  the 68 public facades in `module Viewer` are the surface. When a moved member is used by a facade,
  repoint the facade (`ViewerRuntime.X` → `NewModule.X`); never change the `.fsi`. The api-compat gate
  enforces this.
- **Verify every stage against:** the golden-image gate (`Rendering.Harness.Tests`, render output),
  `SkiaViewer.Tests` (note the ~1/15 pre-existing timing flake — re-run, don't chase), and the stage's
  named gating test(s). C/D additionally require the new assembly / focus-latency tests, authored FIRST.
- **One stage per PR.** B before C before D. Do NOT bulk-unify — each danger zone gets its test before its
  mechanical change.
- **Internal splits need no facade dance** for private-only members — move to a new `module internal X`
  (compiled before `ViewerRuntime`), `open X` in `ViewerRuntime`. Only public (facaded) members need the
  facade repoint.
