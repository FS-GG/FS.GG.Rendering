---
name: fs-gg-elmish
description: Drive a generated FS.GG.UI product through the pure Elmish adapter, and drive its UI headlessly in tests — fold click/key/scroll scripts to a final model with Perf.runScriptToModel, guarded by BoundIds so a silent unbound click cannot pass green.
---

# Elmish Capability

## Scope

Use this skill for the Elmish boundary of a generated product: wrapping your pure
user model/messages in the adapter so viewer messages, effects, and scene refreshes
are threaded through it. The adapter is a **viewer bridge**, not your MVU runtime —
it does not fold your product's `update`; a `UserMsg` is forwarded verbatim as a
`DispatchUser` effect, and you compose your own `update` around the adapter (see Usage).

It also owns **driving that boundary headlessly in tests** — folding a click/key/scroll
script through the real retained route to a final model, so you can prove the UI
*responds* and not merely that it renders (see Drive interaction headlessly).

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/Elmish/Elmish.fsi`. `ElmishAdapter.init` and
`ElmishAdapter.update` are pure: they return the next model and a list of
requested effects as plain values, interpreted later at the host boundary.

## The front door — `ControlsElmish.program`

This is where an Elmish product starts, and your scaffold already calls it: `View.fs` builds

```fsharp
open FS.GG.UI.Controls.Elmish

// init -> update -> view -> subscriptions, assembled into one AdapterProgram.
let adapterProgram =
    ControlsElmish.program AppRoot.Model.init AppRoot.Model.update controlsExampleView AppRoot.Model.subscriptions
```

`ControlsElmish.program` takes the four Elmish functions and returns an
`AdapterProgram<'model, 'msg>` — a record carrying exactly them, as `Init` / `Update` / `View` /
`Subscriptions`. The `view` it wants returns a `Control<'msg>`.

**It bundles; it does not run.** Nothing in the shipped surface *consumes* an `AdapterProgram`, so
do not go hunting for a `run(program)` — there isn't one, and that absence is the design. What the
program gives you is the pure four, reachable and callable as plain values:

```fsharp
let model, initCommands = adapterProgram.Init()
let updated, saveCommands = adapterProgram.Update SaveRequested model
let rendered = adapterProgram.View updated
let subs = adapterProgram.Subscriptions updated
```

That is a whole product's MVU, folded in a test with no window, no GL and no host — and it is
precisely what your generated `BehaviorTests.fs` does with it.

The **live window** is a separate record: `InteractiveAppHost`, handed to
`ControlsElmish.runInteractiveApp` (and its `…WithAudio` / `…WithWindowBehavior` variants). Your
scaffold builds that one too, in `EvidenceCommands.fs`. Two records, two jobs — the program is the
pure declarative bundle you assert against, the host is what the viewer drives. Keep them straight
and neither will surprise you.

## Usage

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.Elmish

// init wraps your user model and returns startup effects (values, not I/O).
let adapterModel, startupEffects =
    ElmishAdapter.init viewerOptions initialModel (view initialModel)

// A UserMsg is a pass-through: it forwards `productMsg` as a DispatchUser effect and
// leaves adapterModel/scene unchanged (only a ViewerMsg re-renders via `view`).
let passthrough, effects =
    ElmishAdapter.update view (UserMsg productMsg) adapterModel

// The adapter never folds your update. Compose your own `update` around it: interpret
// DispatchUser by running your update, then reflect the next user model back so the
// following ViewerMsg re-renders the scene from it.
let folded =
    match effects with
    | [ DispatchUser m ] ->
        // `update` here is YOUR product's own MVU update (msg -> model -> model),
        // NOT ElmishAdapter.update — the adapter never calls it for you.
        let userModel' = update m passthrough.UserModel
        { passthrough with UserModel = userModel'; Scene = view userModel' }
    | _ -> passthrough
```

### No-op command / subscription

An `update` branch that issues no command returns `model, Cmd.none`, and a
`subscriptions` with none returns `Sub.none` — the Elmish-convention no-ops, not a bare
`[]`. Both are in `FS.GG.UI.Controls.Elmish.Authoring` (`Cmd.none = ([] :
AdapterCommand<_>)`, `Sub.none = ([] : AdapterSubscription<_> list)`); `open` it in the
product `Model`:

```fsharp
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Controls.Elmish.Authoring

let update msg model : Model * AdapterCommand<Msg> =
    match msg with
    | Tick -> step model, Cmd.none          // no command — reads as a deliberate no-op
    | Save -> model, [ DispatchHostCommand "save" ]

let subscriptions _ : AdapterSubscription<Msg> list = Sub.none
```

The names live in that dedicated sub-namespace so `Cmd`/`Sub` never shadow Fable
`Elmish.Cmd`; a generated product does not `open Elmish`, so `Cmd.none` resolves
unambiguously (qualify only if the product also opens Fable Elmish).

### Two subscription lists, one slot — `ControlsElmish.subscriptions`

`Sub.none` is the whole story only while your product subscribes to nothing. Wire up
keyboard shortcuts and you have **two** lists — the keyboard's and the control runtime's —
and `program` has exactly **one** slot to put them in. `ControlsElmish.subscriptions` is the
merge:

```fsharp
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Controls.Elmish.Authoring

// An AdapterSubscription is a { Id; Subscribe } record: a name, and a thunk yielding commands.
// `ControlsElmish.interpretKeyboardEffect` is what turns a fired KeyboardEffect into those
// commands (see fs-gg-keyboard-input), lifting each CommandId through your own message ctor.
let keyboardSubs : AdapterSubscription<Msg> list =
    [ { Id = "keyboard"
        Subscribe = fun () -> ControlsElmish.interpretKeyboardEffect Activate keyboardEffect } ]

// keyboard first, controls second — one list, in that order, for `program`.
let subscriptions _ : AdapterSubscription<Msg> list =
    ControlsElmish.subscriptions keyboardSubs Sub.none
```

It is an ordered concatenation and nothing more: `subscriptions keyboard controls` is
`keyboard @ controls`. So the reason to call it rather than write `@` yourself is that it
**names which list goes first**, and the order is the contract — the two lists can carry the
same `Id`, and a consumer that folds them in order sees the keyboard's first.

The half you do not have is `Sub.none`, not a bare `[]` — the same convention as above, and
this is the ordinary case: a product with keyboard shortcuts and no control-runtime
subscriptions of its own.

## Drive interaction headlessly — a button that renders is not a button that dispatches

A suite that only exercises `update` proves the part that was never in doubt. It
never asks the question that actually breaks: **when the user clicks this control,
does anything happen?** A control can render perfectly and be bound to nothing, and
no amount of `update` testing will notice.

`ControlsElmish.Perf.runScriptToModel` asks it. It folds an ordered `FrameInput`
script (clicks / keys / scrolls / ticks) through the REAL retained pointer route
and returns the **final model** — pure, headless, no GL, no window, deterministic.
You then assert on the model the interaction actually produced.

Four steps: **locate → guard → drive → assert.**

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

// 1. LOCATE — render the initial frame and read the control's real bounds, so the
//    scripted click lands on it rather than on a coordinate you guessed.
let initial = Control.renderTree host.Theme size (host.View size model)
let _, rect = initial.Bounds |> List.find (fun (id, _) -> id = "btn")
let cx, cy = rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0

// 2. GUARD — assert the id is BOUND before clicking it. Not optional; see below.
Expect.isTrue
    (Set.contains "btn" initial.BoundIds)
    "'btn' must be a bound control — an unbound click is silent"

// 3. DRIVE — fold the script to the final model through the real route.
let script: FrameInput<Msg> list =
    [ FrameInput.Pointer(HoverEnter("btn", cx, cy))
      FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy))
      FrameInput.Idle ]

let finalModel, _metrics = ControlsElmish.Perf.runScriptToModel host size script

// 4. ASSERT — the click actually drove state.
Expect.equal finalModel.Count 1 "the scripted click incremented the counter"
```

`Perf.runScript` is the same fold returning only the per-frame `FrameMetrics` (no
final model) — reach for it when you are asserting frame/coalescing behaviour, and
for `runScriptToModel` whenever you care what the interaction *did*.

For a scene/game host that maps raw `ViewerPointerInput`, test high-rate movement at the retained
viewer boundary with `Viewer.enqueueInputWithPointerPolicy` and launch production through
`Viewer.runInteractiveViewerWithPointerPacing`. A useful acceptance fixture distributes 1,000
`PointerMove` samples over 60 drains and requires at most 60 folded move updates, while a
`PointerDiscrete` press/release/click sequence is observed exactly once and in order. In production,
wire `ViewerPointerPacingOptions.OnMetrics`; totals for raw samples, folded samples, coalesced samples,
model updates, presented frames, repaint causes, and full-render fallbacks are the evidence. An
`OnMetrics = ignore` launch deliberately supplies no pacing evidence.

### When the model cannot answer, ask what the script REQUESTED

`Perf.runScriptToEffects` is the same fold once more, returning the final model, **every
`ViewerEffect` the script's `Init` and `Update` asked for** (in dispatch order, `Init`
first), and the metrics:

```fsharp
let finalModel, effects, _metrics = ControlsElmish.Perf.runScriptToEffects host size script
```

Reach for it when the thing you need to prove is a **request**, not a state change —
because for a whole class of bug the model simply cannot testify. Sound is the sharp
case: a volume the product restored into its model but never told the mixer about is
*indistinguishable, from inside the model*, from one that was applied. A model-level
test passes on the silent product. Asking what the frame requested separates them:

```fsharp
let _, effects, _ = ControlsElmish.Perf.runScriptToEffects host size script

effects
|> ControlsElmish.audioRequests    // ViewerEffect list -> AudioEffect list
|> Audio.interpret                 // AudioEvidence
```

The stream is the one the *live* loop would hand its sink for this script — not a
re-fold of `host.Update` written in your test. That distinction is the point: a
hand-rolled fold asserts what your test does, and the bug you are hunting is the
product loop doing something else.

### Guard every click with `BoundIds`

`ControlRenderResult.BoundIds` is the set of canonical ids of every node carrying at
least one event binding. **Assert your id is in it before you drive a click.**

This is not ceremony. A click at an id that is not bound dispatches nothing and
raises nothing — it is a **silent no-op**. So a typo'd `ControlId` in a test drives
*nothing*, and if the assertion is negative ("the screen did not change", "no error
appeared") the test **passes**. An entire headless UI suite can be green and
pressing nothing.

```fsharp
let bound = (Control.renderTree theme size (host.View size model)).BoundIds
Expect.isTrue (Set.contains id bound) $"'{id}' must be a bound control — an unbound click is silent"
```

One line, and it converts the whole failure class from silent to loud. Put it inside
whatever click helper you build, so it cannot be forgotten per-test.

### Capture the frame the interaction produced

Because the fold returns the final model, you can render and offscreen-capture the
**post-interaction** frame — closing the "drive interaction → see the resulting
frame" loop without a live window:

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer   // Viewer.captureScreenshotEvidence, ViewerPresentMode

// `request` is a ScreenshotEvidenceRequest and `options` a ViewerOptions — build them
// as in [[fs-gg-skiaviewer]]; only PresentMode has to change for an offscreen capture.
let scene = SceneNode.Group [ (Control.renderTree host.Theme size (host.View size finalModel)).Scene ]
let result = Viewer.captureScreenshotEvidence request { options with PresentMode = ViewerPresentMode.OffscreenReadback } scene
// result.ProvesScreenshot = a real PNG was written. Otherwise the capture is
// environment-limited — disclose it; never report an unproven capture as green.
```

The state half is deterministic and needs no GL. Only the PNG readback does, and it
degrades-and-discloses on a no-GL host.

### The keyboard sibling — a key that is bound is a key that dispatches

The recipe above is the **click** route (the `app` family's pointer-driven Controls
host). The **game** family drives input through a different host — the keyboard
**scene-host** `generatedHost` (`GeneratedAppHost`), launched by `Viewer.runAppWithAudio`
— so it has its own sibling of the same toolkit, from the same package. Use it for
exactly the same reason: a suite that only exercises `update` never asks whether a
*pressed key* reaches gameplay, and that gap is where a keyboard game ships
green-but-unplayable (issue FS.GG.Rendering#912, the Rougue1 defect — 108 green tests, nothing
playable, because every test injected a `Msg` straight into `update`).
<!-- skill-refs: closed-ok FS.GG.Rendering#912 — cited as the green-but-unplayable defect that NAMED the scene-host test-altitude gap this section guards against, history not a pointer. File-scoped, so it also excuses the "crux of #912" citation below. Closed is correct; it stays closed. -->

| click route (`app`) | keyboard scene-host (`game` / `sample-pack`) |
|---|---|
| `ControlsElmish.Perf.runScriptToModel` | `GeneratedAppHost.runKeyScriptToModel` |
| `ControlRenderResult.BoundIds` (an unbound click is silent) | `GeneratedAppHost.auditKeyWiring` → `.Dead` (a bound key that dispatches nothing) |
| — | `GeneratedAppHost.reachableMessages` (a handled `Msg` no source produces) |

**Drive → assert**, end to end through the host's own `dispatchKey` fold (each raw
key normalized, then `MapKey`, then `Update`) — the SAME fold the live runtime
performs, so what you drive is the product's real routing, not a test-local
re-derivation:

```fsharp
open FS.GG.UI.KeyboardInput   // ViewerKeyEvent, ViewerKeyDirection
open FS.GG.UI.SkiaViewer      // GeneratedAppHost

let down raw : ViewerKeyEvent = { RawKey = raw; Direction = ViewerKeyDirection.KeyDown }

// DRIVE — a raw key SCRIPT, folded from Init's model to the final one. No window, no GL.
let played, _effects = GeneratedAppHost.runKeyScriptToModel host [ down "w"; down "w" ]

// ASSERT — the pressed key actually drove gameplay (here: it moved a paddle), not just a message.
Expect.isTrue (played.LeftPaddleY < restedY) "'w' reaches gameplay through key -> mapKey -> update"
```

**The guard depends on how your `mapKey` is shaped, and this is the crux of FS.GG.Rendering#912.**
The scaffold's default `mapKey` wraps EVERY key as one `ViewerInput(key, isDown)`
key-state snapshot and routes to gameplay *inside* `update`. Under that shape
`auditKeyWiring.Dead` is **structurally empty** — every key "binds" to the snapshot
message — so it cannot see a key that reaches no gameplay, and the **played-through
assertion above is the load-bearing guard**. The moment your `mapKey` returns
<!-- skill-refs: closed-ok FS.GG.Rendering#911 — cited as the framework issue that BUILT the auditKeyWiring primitive, history not a pointer. Closed is correct; it stays closed. -->
gameplay **intent** messages directly (the shape FS.GG.Rendering#911 was built for), `Dead` becomes
a real check — a declared key your `mapKey` does not handle shows up in it:

```fsharp
let wiring = GeneratedAppHost.auditKeyWiring host [ down "Enter"; down "Space"; down "x" ]
Expect.equal (wiring.Wired |> List.map snd) [ StartRun; TogglePause ] "each live key names its intent"
Expect.equal wiring.Dead [ down "x" ] "the declared-but-unbound key is dead — a bound key that dispatches nothing shows up here"
```

And the **handled-but-unwired** check catches the other half — a `Msg` case `update`
handles that no runtime source produces. `reachableMessages` returns what the host
SOURCES (`mapKey` over the probe, plus `Tick` when you pass a sample); the product
asserts its handled intents are covered:

```fsharp
let reachable = GeneratedAppHost.reachableMessages host probe (Some(TimeSpan.FromMilliseconds 16.0))
let unwired = Set.difference (Set.ofList handledIntents) (reachable |> List.map caseName |> Set.ofList)
Expect.isEmpty unwired "every intent your update handles is dispatched by some source"
```

Pass `Some dt` when a message is reachable only through `Tick`, or the clock message
reads as a false positive; the package uses no reflection, so you name the handled
universe (`handledIntents` / `caseName`) and it returns what IS reachable to subtract.

### Responsiveness evidence: prove it *responds*, not that it *renders*

Your Evidence Rules (and [[fs-gg-testing]]'s, and [[fs-gg-ui-widgets]]') require
responsiveness evidence that **validates pointer and keyboard activation separately
from screenshot readiness**, and separates routing from update/render/present
latency. These are the instruments for it. They ship in the package you already
have, and until now no skill named them — so the rules demanded evidence and
withheld every means of producing it (FS.GG.Rendering#507).
<!-- skill-refs: closed-ok FS.GG.Rendering#507 — cited as the issue that NAMED the gap and closed it, not as somewhere to go. Closed is correct; it stays closed. -->

**`ControlsElmish.respondsProofOf` / `captureRespondsProof`** are the only evidence
class that tells *renders* from *responds*:

```fsharp
let proof = ControlsElmish.captureRespondsProof host size model script
// proof.Verdict : RespondsVerdict — Responded | Inert
```

The point is the `Inert` verdict. An app whose authored binding was **dropped**
produces identical before/after frames, so a screenshot diff calls it fine. This
does not: no state moved, so it reports `Inert`, and *"renders" cannot be passed off
as "responds"*. Assert on `Verdict`, not on a pixel diff.

**The per-frame projection** answers the latency half — `compositorDiagnostics`,
`layoutMetrics`, `responsivenessTimingContribution`, and the `FrameMetrics` /
`FrameCause` records they yield. A live product subscribes through
`InteractiveAppHost.OnFrameMetrics`; a headless test reads the projection directly.
Routing, update, render and present are separate contributions — which is exactly the
separation the rule asks for, and it cannot be produced from a screenshot.

**`routeInteractivePointer`** is the primitive `Perf.runScript*` and
`captureRespondsProof` are built on, *"exposed so a headless test exercises the real
adapter path without opening a window"*. Reach for it when you need one raw pointer
route rather than a scripted fold; prefer the derivatives otherwise.

### `Pointer.replay` — the fold determinism rests on

One level below the app: `Pointer.replay` folds a recorded `PointerMsg` sequence to a final
`PointerState` plus the ordered interactions it produced. Identical input yields identical output —
that is SC-005, and this is the surface it is asserted on.

```fsharp
open FS.GG.UI.Controls

// `policy` (a PixelSnapPolicy) and `layout` (a LayoutResult) are the SAME inputs your live route
// already threads. Pointer.replay never fetches them — that is exactly what keeps it pure and
// replayable, so a recorded sequence folds to a byte-identical result on any machine.
let finalState, interactions =
    Pointer.replay policy layout recorded (Pointer.init ())
```

Use it to pin a recorded pointer trace against regression, or to reason about pointer state without
standing up a host. It is *pointer-level*: it returns interactions, not your model. When the question
is "what did the interaction do to my product", stay with `Perf.runScriptToModel` above, which drives
the whole route and hands you the model back.

The rule to carry away, and it is the same one [[fs-gg-testing]] states at the sink:
**a frame proves the renderer ran. Only a verdict proves the app answered.**

## Ask what went wrong — `diagnostics`

The `Diagnostics` *constructors* build a report. `diagnostics` is the **query that hands you the
list** — how a product asks what is wrong with the thing it just built. It is the same name, and the
same question, at three levels:

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

Control.diagnostics rendered          // authoring issues in a Control<'msg> tree, without rendering it
ControlRuntime.diagnostics runtime    // what the focus/hover/press/drag runtime has accumulated
AdapterCmd.diagnostics command        // what an interpreter REPORTED while producing this command
```

Mind the module on the third: it is **`AdapterCmd`**, beside `AdapterCmd.productMessages` — not
`ControlsElmish`, which is the *runner* module next door.

<!-- skill-refs: closed-ok FS.GG.Rendering#457 — cited as the issue where this gap was FOUND, not as somewhere to go. Closed is correct; it stays closed. -->
That third one is the one that bites. **`AdapterCmd.productMessages` extracts messages and nothing
else** — so a routing site that calls it alone *silently drops every diagnostic the interpreter
raised*: a pointer hit-test miss, a stale target, an unresolved control id (issue
FS.GG.Rendering#457). Route both, as the shipped pointer routing sites now do:

```fsharp
let messages = AdapterCmd.productMessages command
let reported = AdapterCmd.diagnostics command   // AdapterDiagnostic list, in order

// Hand `reported` to your observer/log. Dropping it is how a mis-routed click
// becomes "nothing happened" with nothing anywhere saying why.
```

A command built from a plain product message carries none, so this is free on the happy path and
loud on the unhappy one — which is the whole point of asking.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to assert adapter transitions and effects.

## Evidence

Record transition and effect evidence under this product's `readiness/` paths. Do
not copy framework readiness reports into the product.

## Package Boundary

Keep `Model`, `Msg`, `Effect`, `init`, and `update` pure. Native viewer I/O
belongs to `fs-gg-skiaviewer` interpreter code, not the adapter.

## Generated Product

Products that select Elmish also receive Scene and SkiaViewer; wire the adapter
between your pure `update` and `Viewer.runApp`.

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

- [[fs-gg-skiaviewer]] — interpret the adapter's requested effects at the host.
- [[fs-gg-scene]] — produce the `SceneNode` your `view` returns.
- [[fs-gg-testing]] — product expectations and evidence; it defers to the interaction
  driver above for "does the UI actually respond".

## Sources / links

- Fable.Elmish (driven adapter model): https://elmish.github.io/elmish/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
