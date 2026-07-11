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
