# Viewer run-core pipeline-staging plan

**Date:** 2026-07-16
**Scope:** `src/SkiaViewer/ViewerRuntime.fs` — the run-core after the `SkiaViewer.fs` → facade +
internal-module decomposition (PRs #816–#823). Basis for staging the window-driving loops
(Pattern B from `docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`).

## Key finding that reshapes the work

The "loops + `update` knot" is **not** one knot. From a full deep-read of `ViewerRuntime.fs`:

- `update` (`~600`) and `updateRun` (`~772`) are **already pure** `msg -> model -> model * effect list`
  and **already decoupled** from the two persistent loops. The persistent loops drive the *product's*
  `host.Update`, not the viewer's own `update`. `update`/`updateRun` are called **only** by the
  separate raw-GL `runBounded` path (and one **dead**, result-discarded `update Start` call in
  `runGeneratedApp` ~`1344`). ⇒ Do NOT restage `update`/`updateRun`; they are already separated.
- `runPresentedPersistentWindow` (`232–562`) is the shared low-level loop; it is **already internally
  staged** into named closures (`configuration`, `updateLegacy`, `eventMapper`, `effectMapper`,
  `program`) plus a single-sourced input-pump (`drainQueuedInputs` `339`, `pumpScriptInput` `303`).
  Its length is inherent, not tangled.
- The genuine duplication is the **front-end scaffolding** shared, near-verbatim, between
  `runGeneratedApp` (`1207–1395`) and `runInteractiveViewerWithWindowBehaviorCore` (`1433–1705`).

So "pipeline staging" here = **de-duplicating the two front-ends into shared, risk-classified stages**,
not untangling `update`.

## Stages (risk-classified)

| Stage | What | Sites | Risk | Do now? |
|---|---|---|---|---|
| **A** `validateLaunch options behavior : Result<unit, ViewerRunFailure>` | validateOptions + option-failure filter + capability gate | `1218–1236` / `1440–1458` (**byte-identical**) | **ZERO** (pure precondition, no timing/GL) | **YES** |
| **E** `assembleLaunchOutcome` | patch the `runPresentedPersistentWindow` result onto the launch outcome | `1386–1395` / `1697–1705` | LOW (pure patch; message string differs — pass in) | YES (after A) |
| **B** `initProductState host firstView` | `currentModel`/`currentScene`/`inputDispatch`/size state + `reportProductDefect`/`safeView`/`onScene`/`onInputDispatch`/`onDiagnostic`/`evidenceSink` | `1238–1279` / `1460–1505` | MED — **behavioral**: `evidenceSink` size is `InitialSize` (generated-app) vs `currentSize` (interactive); interactive tracks 3 sizes | Gated on an evidence-image-dimension test |
| **C** `buildDispatchCore` | `interpretEffects`/`dispatchHostMsg`/`initialCloseRequested`, incl. the persistence path | `1306–1340` (generated-app `let rec` knot) / `1525–1571` (interactive plain) | **HIGH** — the `let rec … and …` persistence mutual-recursion must be preserved exactly; a stray forward-mutable resurrects the Init-load-dropped bug, a stray persistence-from-outcome emit **stack-overflows uncatchably** | Gated on persistence round-trip + Init-drop regression tests |
| **D** `installInputHandlers` | `handleTick`/`handleKey`(±pointer/fb) | `1356–1381` / `1573–1654` | **HIGH** — `runtimeStateRepaint` placement is the "focus one click behind" class; the generated-app `handleKey` repaints only on the no-message branch (a real asymmetry — do not "unify" away) | Gated on a focus-latency / responsiveness assertion per branch |

## 7 behaviour-observable danger zones (NOT fully covered by the golden-image gate)

The golden-image gate proves **static corpus render output** byte-identical; it does **not** exercise
live loop timing/ordering. These need their own assertions before the stage that touches them lands:

1. `updateLegacy` close-decision order (`405–414`): `pumpScriptInput → drainQueuedInputs → scriptWantsClose → onTick`, `||`-short-circuited — reorder changes *which frame* close lands on.
2. `drainQueuedInputs` order (`353–362`): discrete ++ coalesced-pointer ++ deferred — the responsiveness/lag-trace contract.
3. `scriptedCompletionFrames`/`framePresented` counters (`241`, `303–321`, `415–424`): script-run termination frame counts.
4. Generated-app persistence `let rec` + sticky `outcomeCloseRequested` (`1304–1340`): Init-drop bug + uncatchable stack overflow.
5. `handleKey`/`handlePointer` `runtimeStateRepaint` placement (`1380`, `1601`, `1652`): focus-one-behind.
6. `handleFramebufferResize` conditional re-derive (`1673–1677`) + `setLiveAuthoringSizeOverride`: spurious re-render of fixed-res games.
7. `FrameRateCap`/`TargetFrameRate` default `Option.orElse (Some 60)` (`247`): must survive restaging of `configuration`.

## Sequencing

1. **Stage A** (this pass): extract `validateLaunch`; both front-ends thread it. Zero-risk, verified against golden gate + `SkiaViewer.Tests` + responsiveness lane.
2. **Stage E**, then **B**: each its own PR with the noted test added first.
3. **C** and **D**: surgical, human-reviewed, one danger zone at a time, each with a dedicated behavioral test authored **before** the mechanical change. Do NOT bulk-unify.
4. Delete the dead `update Start` call in `runGeneratedApp` (~`1344`) — behaviour-neutral today.

**Guardrails for every stage:** golden-image gate (render output), `SkiaViewer.Tests` (note the
~1/15 pre-existing timing flake — re-run, don't chase), and the SecondAntShowcase live-responsiveness
lane (input→visible latency). C/D additionally require the danger-zone-specific tests above.

## Coverage revision (2026-07-16 — after auditing existing tests)

The initial "danger zones not covered by gates" framing was too pessimistic. Several danger zones
already have **direct seam tests** (the relevant functions are `internal` *specifically* so a test can
reach the wiring the windowed launch cannot exercise headlessly):

- **Danger zone 4 / Stage C persistence wiring** — `tests/SkiaViewer.Tests/Issue535PersistenceSeamTests.fs`
  drives `Viewer.interpretViewerEffects` and `Viewer.dispatchPersistenceBatch` directly: Persist-batch
  dispatch *order*, fold routing (only Persist reaches the sink), outcome→product-message dispatch,
  drop-not-invent, and close-propagation. Plus `runAppWithPersistence` public-entry behaviour on a
  windowless host. ⇒ The persistence **seams** are well-gated. What is NOT gated is the `let rec … and …`
  **assembly** inside `runGeneratedApp` (a windowed path) — so Stage C's residual risk is the *mutual
  recursion wiring*, not the individual seams.
- **Stage B size divergence** — `Issue246LogicalSizeTests.fs` pins the logical/surface fit arithmetic
  and `captureScreenshotEvidence` at explicit sizes; `Issue396FirstFrameFaultTests.fs` covers the
  `tryFirstProductView` first-view guard. ⇒ Stage B is **better gated than first assessed**; its
  `evidenceSink`-size and first-view concerns have real coverage.

**Revised sequencing:** Stage B is now reasonable to attempt against Issue246/Issue396 + golden gate +
Deterministic — but note it is *not* a byte-identical extraction like A/E: the two front-ends carry
**divergent state models** (generated-app 1 size vs interactive 3 sizes) plus per-loop `firstView`/
`evidenceSize` closures, so it is a genuine unification (parameterize the divergences; do not collapse
them). Stage C stays gated on a test that exercises the **assembly** (not just the seams) — e.g. a
scripted `runAppWithPersistence` round-trip on a host that DOES open (the live lane), asserting an
outcome-driven message both dispatches and can close. Stage D (repaint placement) still needs its
focus-latency assertion.
