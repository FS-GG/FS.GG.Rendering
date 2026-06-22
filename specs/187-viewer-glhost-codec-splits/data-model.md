# Phase 1 Data Model: Viewer + GlHost + SceneCodec Module Splits

**Feature**: 187-viewer-glhost-codec-splits | **Date**: 2026-06-22

This is a behavior-preserving structural refactor, so the "data model" is the set of **internal**
record/table shapes introduced by the split. All shapes below are **internal** (declared in files
with no `.fsi`); none appear on a public surface (FR-007). No public type is added, removed, or
changed.

---

## US3 — Node codec table (Pattern A) · `src/Scene/SceneWire.fs` (internal)

The single new structural type of the feature.

### `NodeCodec` (internal)

```fsharp
// internal: one entry per SceneNode kind — pairs the write and read so they cannot drift.
// No access modifier: SceneWire.fs carries no .fsi, so this is a file-internal helper by the
// SceneRenderer.fs/Numeric.fs precedent (Constitution Principle II — visibility lives in .fsi,
// never as a `private`/`internal` keyword on a .fs declaration).
type NodeCodec =
    { Tag: byte                                   // wire discriminator, identical to today's tag
      Write: System.IO.BinaryWriter -> SceneNode -> unit
      Read: System.IO.BinaryReader -> SceneNode }
```

**Construction / lookup rules** (preserve current behavior exactly):
- **Write path**: `writeSceneNode` keeps an exhaustive `match node with …` that selects the entry for
  the node kind, writes `entry.Tag`, then `entry.Write writer node`. The exhaustive match preserves
  `FS0025`: a newly added `SceneNode` case with no entry is a **compile error** (FR-005).
- **Read path**: `readSceneNode` reads the tag byte, looks up the entry by tag, and calls
  `entry.Read reader`. An unknown tag **fails loud** with the same diagnostic as today (FR-009) — no
  silent default.
- **Byte format invariant**: each `Write` emits exactly the bytes the current arm emits, in the same
  field order, width, and endianness; each `Read` consumes them in the same order. Verified by
  `Feature146PortableSceneRoundTripTests` (byte-exact) + `Feature183CodecSymmetryTests` (SC-004).

### Responsibility grouping (internal sub-modules within `SceneWire.fs`)

The 25 node arms partition by family (names indicative; finalized while editing):

| Group | Node families (indicative) |
|---|---|
| `Primitives` | rectangle / rounded-rect / circle / line / clear / group container |
| `Paint` | fill / stroke / gradient / shadow / blend attributes |
| `Path` | path commands / path geometry nodes |
| `Text` | text run / glyph run / shaped-text payload |
| `Scene` | image / picture / transform / clip / composite wrappers |

Each group exposes its `NodeCodec` entries; `SceneWire` concatenates them into the lookup tables.
Shared low-level helpers (`writeList`/`readList`, primitive readers/writers) move here too so both
sides share one definition.

**Public surface unchanged**: `SceneCodec.fs` keeps the package types (`ProtocolVersion`,
`PortableScenePackage`, …) and the public functions (`exportScene`/`export`/`importPackage`/
`inspect`/`inspectWith`/`compareScenes`/`packageIdentity`/`formatDiagnostics`); its `writeScene`/
`readScene` now delegate to `SceneWire`.

---

## US1 — Viewer responsibility groups (Pattern E) · `src/SkiaViewer/*.fs` (internal)

No new public types. Bodies relocate; the public `module Viewer` keeps thin delegators with
byte-identical signatures. Indicative internal homes:

| Internal file (no `.fsi`) | Moves these `Viewer` bodies (public delegators remain) |
|---|---|
| `ViewerInputQueue.fs` | `emptyInputQueue`, `inputQueueDepth`, `enqueueInput`, `drainInputQueue`, `dirtyState`, `dirtyStateRequiresRecompose` |
| `ViewerResponsiveness.fs` | `*Token` encoders, `createResponsivenessRunId`, `latencyRecordToJsonLine`, `summarizeResponsivenessRecords`, `responsivenessSummaryToJson`/`…Markdown`, `writeResponsivenessRun`, `RenderLagTrace` + trace seam |
| `ViewerEvidence.fs` | `captureScreenshotEvidence`, `initEvidenceWorkflow`/`updateEvidenceWorkflow`, `runBounded`/`runUntilFirstFrame`/`runForFrames` evidence bodies |
| `ViewerWindow.fs` | the shared window lifecycle scaffold (R2) + the two specialized runners `runPresentedPersistentWindow` / `runPersistentWindow` and the `run*`/`runApp*`/`runInteractive*` entry bodies |

### Window lifecycle scaffold (internal, US1 / FR-002)

Not a new public type — an internal function that captures the shared skeleton both runners call:

```fsharp
// internal: shared persistent-window skeleton; specializations passed as parameters.
// Divergent steps (window construction, input pump, event handlers) are arguments, NOT inlined.
let private runPersistentWindowCore
    (lifecycle: WindowLifecycleHooks)   // windowOpened/framePresented/closeReason refs + diagnostic capture + handler teardown
    (createWindow: unit -> IWindow)      // present-program path  OR  raw Silk path
    (pump: ...)                          // input-queue drain     OR  warmup-FIFO flush
    : Result<ViewerLaunchOutcome, ViewerRunFailure>
```

`runPresentedPersistentWindow` and `runPersistentWindow` become thin specializations supplying
`createWindow`/`pump`. **State-mutation order on the live path is preserved** (Edge Cases): the
scaffold threads the same refs/mutables in the same sequence; only their *location* changes.

> Per R2, if the genuinely-shared surface is small, the scaffold degrades to shared helpers and the
> two runners stay separate — US1 still delivers via the module-group split. SC-002 is read against
> whatever shared-scaffold count results, documented in quickstart.

---

## US2 — GlHost internal units (Pattern E) · `src/SkiaViewer/Host/GlHostRun.fs` (internal)

No new public types (the public `GlHost` decision types/functions are untouched). `GlHost.run`
becomes an orchestrator over internal units; indicative shapes:

| Internal unit | Responsibility (relocated from `run`'s body) |
|---|---|
| render/readback | offscreen render → GPU→CPU readback (~L788+); the screenshot image build/encode |
| `interpretEffect` | the effect interpreter (~L1227): `RenderFrame`, screenshot capture, etc. |
| input dispatch | key/pointer event → program dispatch |
| present/damage loop | `runEventLoop` wiring of the public pure decisions (`planPresent`/`decideScissorRedraw`/`validateDamage`/…) |

`run` keeps its signature `ViewerProgram<'model,'msg> -> Result<unit, RenderDiagnostic>` and the same
GL-resource acquire/release order and fail-loud diagnostics (FR-009, Principle VI).

---

## Validation rules (apply to every shape above)

1. **Surface invariance** — none of these shapes appears in any `.fsi`; `FS.GG.UI.SkiaViewer.txt` and
   `FS.GG.UI.Scene.txt` baselines diff empty (FR-007, SC-006).
2. **Byte/behavior invariance** — frames, traces, screenshots, and package bytes equivalent to
   baseline (FR-006, SC-004/SC-007).
3. **Fail-loud invariance** — unknown node tag, malformed package, GL-context failure,
   screenshot-before-first-frame all keep their diagnostics (FR-009).
4. **Order invariance** — float accumulation / present ordering / wire field order unchanged
   (Edge Cases).
5. **No new dependency/project** — all new files live in the existing `SkiaViewer`/`Scene` projects
   (FR-010).
