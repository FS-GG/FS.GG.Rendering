---
name: fs-gg-skiaviewer
description: Wire a generated FS.GG.UI product to the desktop viewer host.
---

# SkiaViewer Capability

## Scope

Use this skill for the host boundary of a generated product: opening the native
window, rendering scenes, routing keyboard input, advancing time, and
interpreting `ViewerEffect` values returned by your pure `update`.

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/SkiaViewer/SkiaViewer.fsi`. `Viewer.runApp` is the canonical
entry point and the only place that performs host-boundary I/O. See
`docs/effects-boundary.md` for the full effect-category description.

## Usage

```fsharp
open FS.GG.UI.SkiaViewer

// Bundle your pure pieces into the host record.
let generatedHost =
    { Init = fun () -> initialModel, []   // initial model + startup effects
      Update = update                     // pure Msg -> Model -> Model * ViewerEffect list
      View = view                         // Model -> SceneNode
      MapKey = mapKey                     // ViewerKey -> bool -> Msg option
      Tick = tick                         // TimeSpan -> Msg option
      Diagnostics = Viewer.defaultDiagnostics }

match Viewer.runApp viewerOptions generatedHost with
| Ok _ -> 0          // window opened, scenes rendered, effects interpreted
| Error _ -> 1       // classified host/launch/verification failure
```

## Host-boundary wiring: the one-page map

Everything that connects your pure Elmish core to the desktop host passes through **one record**
(`GeneratedAppHost`) and **one launch call**. Wiring milestones turn entirely on this seam, so here it
is in one place — the map you would otherwise assemble by reading `EvidenceCommands.fs`, `AudioCues.fs`,
the `SkiaViewer.fsi` contract, and `GovernanceTests` side by side.

### `MapKey` is stateless — a model-aware router cannot live here

The scaffold ships exactly one keyboard seam:

```fsharp
// EvidenceCommands.fs
let mapKey key isDown = Some(ViewerInput(key, isDown))
```

`MapKey : ViewerKey -> bool -> Msg option` sees **only** the key and its up/down edge — never the
`Model`. It is a pure key→message wrapper, so **any input handling that depends on model state** (an
active menu, a modal, a rebindable action map, "space does different things in different screens")
**cannot go here.** Wrap the raw key into a message — the starter's `ViewerInput` — and decide what it
*means* inside `update`, where the model is in scope. The host feeds you raw events; interpretation is
`update`'s job, the same discipline as the audio cue seam below.

### `Init` / `Update` compute the `ViewerEffect` batch

The host renders and plays nothing directly — it interprets the `ViewerEffect list` your `Init` and
`Update` return. `Update` folds `msg` through your pure `update`, turns the resulting model into a
`RenderScene(view next)` effect, and — on the audio profile — appends `PlayAudio cues`, where
`cues = AudioCues.forTransition msg previous next` is the (possibly empty) cue batch:

```fsharp
Update = fun msg model ->
    let next, _ = update msg model               // your pure update
    let effects = [ RenderScene(view next) ]     // the frame to draw
    match AudioCues.forTransition msg model next with
    | []   -> next, effects
    | cues -> next, effects @ [ PlayAudio cues ] // cues recovered by model-diff
```

`Init` routes the *initial* model through the same cue seam with a synthetic `Started` transition
(`AudioCues.forTransition Started initialModel initialModel`), so startup sounds fire through the
identical path — closing the `Init` hole that a loaded state would otherwise slip through. Most
gameplay cues carry no `Msg` and are recovered by **diffing `previous` against `next`** inside
`forTransition`; see [[fs-gg-game:fs-gg-audio]] for that pattern and its net-diff coverage boundary.

> **There is no `SaveCues` seam.** Persistence is *not* a cue diff — it is a `ViewerEffect.Persist`
> value your `update` emits explicitly, realized only by a persistence-capable launcher (below). The
> audio seam is the only cue-diff seam the scaffold wires.

### Which launcher realizes which effect

`ViewerEffect` has many cases, but two of them — `PlayAudio` and `Persist` — are interpreted **only** by
a launcher that was handed the matching sink. Every other launcher **drops** them:

| Launcher | `PlayAudio` | `Persist` |
|---|---|---|
| `runApp` · `runAppWithWindowBehavior` | dropped (silent) | dropped, **diagnosed** |
| `runAppWithAudio` · `runAppWithWindowBehaviorAndAudio` | **played** | dropped, **diagnosed** |
| `runAppWithPersistence` | dropped (silent) | **persisted** |
| `runAppWithAudioAndPersistence` | **played** | **persisted** |
| `runAppWithWindowBehaviorAndAudioAndPersistence` | **played** | **persisted** |

A dropped `Persist` is **not silent** — the runtime emits a diagnostic naming the launcher and the one
to switch to. A dropped `PlayAudio` is discarded quietly (its sink is `ignore`). So the rule is:
**the effect your `update` emits must match the launcher, or nothing happens** — and only persistence
tells you when it didn't.

The scaffold's default game / sample-pack launch is **`runAppWithAudio`** — audio is wired out of the
box; persistence is the opt-in swap. See *Saving and loading* below for the `persistenceSink` +
`mapOutcome` a persistence launcher needs.

### `GovernanceTests` pins the launch line — the real wiring boundary

`template/base/tests/Product.Tests/GovernanceTests.fs` hard-pins the host boundary as **literal string
matches** on the generated source: it requires `let generatedHost`, `MapKey = mapKey`, `Tick = tick`,
and the exact terminal launch expression
`Viewer.runAppWithAudio viewerOptions audioSink generatedHost`.

Because these are text-literal `Expect.stringContains` scans, **the launch line is a governance
boundary, not a free implementation detail.** Adopting persistence is therefore two coordinated edits,
not one:

1. change the launch call in `Program.fs` to `runAppWithAudioAndPersistence`, supplying the extra
   `persistenceSink` and `mapOutcome` arguments; **and**
2. update the governance pins that literally require the `runAppWithAudio … generatedHost` substring —
   until they change, the swap fails the governance scan.

`MapKey = mapKey` and `Tick = tick` survive a persistence swap unchanged; only the launch line moves.
Reading this before wiring turns "discover the constraint by a failing governance test" into "state it
up front".

## Wiring a game onto the pointer-aware host (mouse / gamepad)

The default game host — `GeneratedAppHost` launched through `Viewer.runAppWithAudio` — has **no pointer
seam.** Its only input fields are `MapKey` (keyboard) and `Tick` (time). A twin-stick game (WASD to move,
**mouse to aim**) or any product that reads a gamepad stick therefore cannot reach the cursor from the
default host: there is nowhere to put the pointer wiring. Moving onto a **pointer-aware host** is a
durable, governance-scanned `Program.fs` change, and this is the worked recipe for it.

### The pointer seam is `MapPointer`, and unlike `MapKey` it sees the model

There are two pointer-aware hosts, and you pick by what your `View` draws:

| Your `View` returns | Host record | Launch call (audio-capable) | `MapPointer` receives |
|---|---|---|---|
| a `SceneNode` (a game — scene, sprites, HUD you draw yourself) | `InteractiveViewerHost` | `Viewer.runInteractiveViewerWithAudio` | `ViewerPointerInput -> Size -> 'model -> 'msg list` |
| a `Control<'msg>` tree (a widget UI — buttons, panels, text) | `InteractiveAppHost` | `ControlsElmish.runInteractiveAppWithAudio` | `PointerInteraction -> 'msg option` |

A game is the first row: it draws a `SceneNode`, so it wants **`InteractiveViewerHost`** and the raw
`ViewerPointerInput` seam. (The Controls adapter routes each sample through your authored widget
`EventBindings` *first* and only falls back to `MapPointer` for interactions no control consumed — the
right model for a UI, the wrong one for reading a bare aim cursor. Reach for it only when your product
is a control tree.)

**`MapPointer` is model-aware, and that is the whole reason it is a different seam from `MapKey`.**
`MapKey : ViewerKey -> bool -> 'msg list` sees only the key edge — it is a stateless key→message wrapper.
`MapPointer : ViewerPointerInput -> Size -> 'model -> 'msg list` also gets the current viewport `Size` and
the `'model`, because a pointer sample is meaningless without them: an aim vector is *the cursor minus the
player's screen position*, and the player's position is in the model. Keep the same discipline as `MapKey`
anyway — wrap the raw sample into one of *your* messages and compute what it **means** inside `update`,
where you already own the model. `MapPointer` returns a `'msg list` (`[]` = "this sample is not for me"),
so one move can dispatch several messages in order, exactly like the interactive `MapKey`.

### The raw sample: `ViewerPointerInput`

The host hands `MapPointer` a framework-neutral sample. `X`/`Y` are already in scene/swapchain
coordinates (the same space your `View` draws in — no manual DPI or window-origin math):

```fsharp
type ViewerPointerInput =
    { Phase: ViewerPointerPhaseKind          // Moved | Pressed | Released | Wheel | Exited
      X: float
      Y: float
      Button: ViewerPointerButtonKind option // Primary | Secondary | Middle (Some only on Pressed/Released)
      DeltaX: float                          // wheel/scroll delta on a Wheel sample
      DeltaY: float }
```

`ViewerPointerPhaseKind` and `ViewerPointerButtonKind` are `RequireQualifiedAccess`, so match them
qualified (`ViewerPointerPhaseKind.Pressed`, `ViewerPointerButtonKind.Primary`).

### Frame-paced continuous pointer input

Use `Viewer.runInteractiveViewerWithPointerPacing` when aim/hover must see continuous movement without
letting a high-polling-rate mouse drive one model update and repaint per native sample. Start from
`Viewer.defaultPointerPacingOptions`, set `ContinuousPolicy =
ViewerContinuousPointerPolicy.CoalesceLatestPerFrame`, and replace `OnMetrics` with your production
metrics sink. The host keeps the newest `Moved` sample at each presentation boundary; press, release,
wheel, exit, and the click sequence derived from them remain ordered and lossless.

Assert the receipt, not just visual smoothness: under 1,000 synthetic moves across 60 presented-frame
boundaries, `RawSamplesReceived` totals 1,000, `FoldedSamplesApplied` is at most 60, and the product's
move-driven update count is at most 60. Also inject one press/release/click sequence and require it
exactly once. Record `CoalescedSamples`, `ModelUpdates`, `PresentedFrames`, `RepaintCause`, and
`FullRenderFallbacks`; a launch that replaces `OnMetrics` with `ignore` is not performance evidence.
Use `Viewer.runInteractiveViewerWithPointerPacingAndAudio` for audio, `Viewer.runInteractiveViewerWithWindowBehaviorAndPointerPacing` for explicit
window behavior, and `Viewer.runInteractiveViewerWithWindowBehaviorAndPointerPacingAndAudio` for both. For a deterministic headless fold, use
`Viewer.runInteractiveViewerScriptWithPointerPacing`. Do not wrap the native host and duplicate logical coordinate inversion.

Keep the synthetic stream separate from normal movement+aiming evidence. The normal case must report
p95 below 16.67 ms, p99 below 25 ms, and no sustained catch-up; the 1,000-sample case proves bounded
folding, not ordinary-play latency.

### Logical canvas: the viewer owns both directions

For an interactive fixed-resolution product, seed `ViewerOptions.LogicalSize`. To switch at
runtime, return `ViewerEffect.ApplyLogicalCanvas nextSize` from `Update`. SkiaViewer then performs
both directions of the same fit: logical scene/Controls coordinates to the physical framebuffer for
presentation, and native window coordinates through framebuffer scaling plus the inverse fit for
pointer routing. `View` and `MapPointer` therefore receive the selected logical coordinate space.

Do not fit the scene, scale Controls bounds, or invert the pointer in product code. Those are second
transforms. `ApplyWindowOptions` is independent: windowed, borderless, and fullscreen presentation
all retain the same logical-canvas policy.

### The swap, field by field: `GeneratedAppHost` -> `InteractiveViewerHost`

`InteractiveViewerHost` mirrors `GeneratedAppHost` field-for-field, plus the pointer seam, with two
signatures widened:

| Field | `GeneratedAppHost` | `InteractiveViewerHost` | migration |
|---|---|---|---|
| `Init` | `unit -> 'model * ViewerEffect list` | *same* | none |
| `Update` | `'msg -> 'model -> 'model * ViewerEffect list` | *same* | none |
| `View` | `'model -> SceneNode` | `Size -> 'model -> SceneNode` | add the leading `Size` (ignore it with `_` if your scene is not size-aware yet) |
| `MapKey` | `ViewerKey -> bool -> 'msg option` | `ViewerKey -> bool -> 'msg list` | `Some m` -> `[ m ]`, `None` -> `[]` |
| `MapPointer` | *(absent)* | `ViewerPointerInput -> Size -> 'model -> 'msg list` | **new** — the pointer wiring |
| `Tick` | `TimeSpan -> 'msg option` | *same* | none |
| `Diagnostics` | `ViewerDiagnosticsOptions` | *same* | none |

### Worked `Program.fs`

```fsharp
open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Wrap raw pointer samples into product messages; DECIDE what they mean in `update`.
let mapPointer (input: ViewerPointerInput) (_size: Size) (_model: Model) : Msg list =
    match input.Phase with
    | ViewerPointerPhaseKind.Moved ->
        [ AimAt(input.X, input.Y) ]                    // update turns cursor + player pos into an aim vector
    | ViewerPointerPhaseKind.Pressed when input.Button = Some ViewerPointerButtonKind.Primary ->
        [ Fire ]
    | _ -> []                                          // Released / Wheel / Exited / secondary: not for us

let interactiveHost =
    { Init = fun () -> initialModel, []
      Update = update                                  // pure Msg -> Model -> Model * ViewerEffect list
      View = fun _size model -> view model             // Size-aware View; ignore size if the scene ignores it
      MapKey = fun key isDown ->                        // widened option -> list
          match mapKey key isDown with
          | Some m -> [ m ]
          | None -> []
      MapPointer = mapPointer
      Tick = tick
      Diagnostics = Viewer.defaultDiagnostics }

// Audio-capable pointer launch: pointer AND sound compose here (see the audio note below).
match Viewer.runInteractiveViewerWithAudio viewerOptions audioSink interactiveHost with
| Ok _ -> 0
| Error _ -> 1
```

Use `Viewer.runInteractiveViewer` (no sink) if the product is silent; the audio-capable
`Viewer.runInteractiveViewerWithAudio` is the one a game wants (below).

### Audio: the pointer host is where pointer AND sound finally compose

This is the reason the audio-capable launcher exists. The old pairing forced a choice: `runAppWithAudio`
gave you sound but its `GeneratedAppHost` has **no pointer**, so a product that needed *both* got silence
on one of them. `Viewer.runInteractiveViewerWithAudio` closes that gap — it drives the same
`InteractiveViewerHost` (so `MapPointer` is live) **and** hands every `ViewerEffect.PlayAudio` batch your
`Update`/`Init` emit to the `audioSink`, exactly as `runAppWithAudio` does. The `AudioCues.forTransition`
seam and the launcher-vs-effect rules from *Which launcher realizes which effect* apply unchanged; the
interactive family simply adds the `-WithWindowBehavior`, `-Script`, and `-WithAudio` variants alongside
the `runApp` ones. A game — which needs pointer *and* audio — uses `Viewer.runInteractiveViewerWithAudio`.

### The launch line is a governance boundary — swap it as one coordinated edit

Just like the persistence swap below, moving off the default game host is **not** a one-line change,
because `GovernanceTests` pins the launch expression and the host binding as literal-string scans of the
generated source. Expect to change together:

1. the launch call in `Program.fs` — `Viewer.runAppWithAudio viewerOptions audioSink generatedHost`
   becomes `Viewer.runInteractiveViewerWithAudio viewerOptions audioSink interactiveHost`; **and**
2. the governance pins that literally require the old `runAppWithAudio … generatedHost` substring and the
   `let generatedHost` / `MapKey = mapKey` / `Tick = tick` bindings — until they name the interactive host,
   the swap fails the governance scan.

Stating this up front turns "discover the constraint by a failing governance test" into a planned edit.

## Saving and loading: `runAppWithPersistence`

`Viewer.runApp` **discards** `ViewerEffect.Persist`. Your `update` can request a save all day and
nothing will happen — so if your product saves, launch it with `Viewer.runAppWithPersistence`, which
takes the two things the framework deliberately does not own:

- `persistenceSink` — performs the real I/O. `SaveSlot` is an opaque, product-owned name, and
  resolving it to a real path is your job. The framework owns no save location.
- `mapOutcome` — turns each `PersistenceOutcome` the sink returns back into one of *your* messages.

That return path is the point. Without it a `Load` is unanswerable: `Persistence.interpretRecordOnly`
records what you asked for and drops it, which is what its name says and what its evidence's
`Backend` field says again.

```fsharp
match Viewer.runAppWithPersistence viewerOptions saveToDisk PersistenceAnswered generatedHost with
| Ok _ -> 0
| Error _ -> 1
```

Use `Viewer.runAppWithAudioAndPersistence` for sound *and* saves — adopting persistence should not
cost you audio.

When the product also supplies non-default window behavior, use
`Viewer.runAppWithWindowBehaviorAndAudioAndPersistence`; it preserves the same audio and persistence
sinks while applying the requested launch mode, size, and placement.

**Do not emit a persistence effect from the handler for a persistence outcome.** That dispatch is
synchronous recursion, not the Elmish queue: it recurses on one stack and dies in a
`StackOverflowException`, which .NET cannot catch and which prints no diagnostic.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` for product host-wiring coverage.

## Evidence

Record window-visibility and screenshot evidence under this product's
`readiness/` paths. Do not copy framework readiness reports into the product.

## Viewer Evidence Rules

- Compare your product's current `FS.GG.UI.` package pins against the versions you
  intend to ship against; when a locally built package or a local restore affects
  your viewer evidence, record it as an explicit caveat so a stale pin never
  passes silently.
- Prefer real screenshot evidence; disclose degraded capture, require reviewer
  accepted readiness, and preserve manual caveats outside generated summary or
  managed section rewrites.
- Responsiveness evidence is separate from screenshot readiness: validate
  pointer and keyboard activation, then distinguish input routing from update,
  render, and present latency.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited checks remain visibly caveated.

## Package Boundary

## Performance evidence boundary

Define the expected host-facing routes **before feature implementation**, then run
`./fake.sh build -t PerformanceEvidence`. Every required row starts as `Placeholder`; replace it with
product state/messages through the real `update` + scene route, review its `definitionDigest`, and mark
it `Authored`. Placeholder and stale-digest rows fail `Test`/`Verify`. A linked blocking
performance-debt issue permits baseline capture but never acceptance. The Release measurement records
zero bounded presents honestly. `./fake.sh build -t PerformanceIntent` emits the same Contracts 7.x
declaration consumed by SDD and evidence; set `liveCompositorRequired` there and keep its workload
separate from normal-play, stress, and throughput routes. Do not relabel that bounded headless result as live compositor,
swapchain/vblank, or vsync evidence. Use a separate live-compositor workload on an actual presentation
host for those claims.

Separate declared input from observed routing receipts. A workload claiming 120 pointer events with
one observed raw input is red and names the missing seam. Bounded headless evidence serializes
present/drop/swapchain/vsync as **unsupported** with a reason, never measured zero. Run the machine gate
before `PerformanceCriticRequest`; its sidecar binds the exact evidence artifact bytes. The
fresh-context critic may narrow a claim but cannot promote
unsupported host capability, synthetic provenance, missing cost coverage, or a red budget.
Its verdict belongs in an attributable external review system at the exact landing commit; an in-repo
receipt or author-entered identity/mode string cannot establish independence.

Keep window, render, and screenshot I/O inside the `Viewer.runApp` interpreter.
Your `update` and `View` stay pure; never perform host I/O inside them.

## Generated Product

The app profile wires `Viewer.runApp viewerOptions generatedHost` as the default
launch path. Use `Viewer.runAppEvidence` with the **evidence** options for bounded
evidence runs.

## Present mode: live vs evidence — never reuse the evidence options

`ViewerOptions.PresentMode` picks the present mechanism; choose it by launch context:

| Launch context | `PresentMode` | Why |
|----------------|---------------|-----|
| Persistent interactive window | `DirectToSwapchain` | zero-readback live present; unchanged frames skip paint |
| Evidence / screenshot capture | `OffscreenReadback` | small readback surface for deterministic pixel capture |

This product ships **two** option records (`EvidenceCommands.fs`): `viewerOptions`
(`DirectToSwapchain`, the persistent launch) and `evidenceViewerOptions`
(`OffscreenReadback`, the bounded evidence commands). **Do NOT** launch the
persistent window from the evidence options — `OffscreenReadback` renders off-screen
and shows a **blank** window. Keep the live launch on `viewerOptions`
(`DirectToSwapchain`) and the readback evidence on `evidenceViewerOptions`.

A consumer without a blocking compositor/vsync can bound the live loop with
`ViewerOptions.FrameRateCap = Some n` (default `None` = 60); a headless host with no
compositor free-runs toward the cap — an environment limitation, not a defect. To
exit gracefully, return `[ ViewerEffect.CloseWindow ]` from your `update` (no extra
host effect is needed).

## Launch noise: GTK `Failed to load module` is cosmetic

On a Linux/GTK host the viewer can print `Failed to load module "…"` (e.g.
`canberra-gtk-module`, `appmenu-gtk-module`) as the window opens. That is the
platform GTK loader probing for optional desktop modules that are simply absent in a
headless/sandbox host — the lines look alarming in the log but are **harmless**: they
do not affect rendering, and they never change the `Ok`/`Error` outcome of
`Viewer.runApp`. Do not read them as a failed launch or a missing dependency to
install — the classified `Error` result is the real launch-failure signal, not stderr
noise.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). If your product uses Spec Kit, record the findings
and resolving links under the feature's `specs/<feature>/feedback/` folder; otherwise record
them in this skill's **Sources** / durable-lessons line (and any product-local `docs/`
location). Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] — build the pure `SceneNode` values this host renders.
- [[fs-gg-keyboard-input]] — feed normalized `ViewerKey` events into `MapKey`.
- [[fs-gg-game:fs-gg-audio]] — the `AudioCues.forTransition` cue seam the host's `Update`/`Init` drive, and the
  model-diff cue pattern behind `PlayAudio`.
- [[fs-gg-game:fs-gg-persistence]] — the `persistenceSink` / `mapOutcome` a persistence launcher needs, and the
  `ViewerEffect.Persist` seam the launcher table above realizes.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
