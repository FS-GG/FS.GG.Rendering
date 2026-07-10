---
name: fs-gg-elmish
description: Drive a generated FS.GG.UI product through the pure Elmish adapter.
---

# Elmish Capability

## Scope

Use this skill for the Elmish boundary of a generated product: wrapping your pure
user model/messages in the adapter so viewer messages, effects, and scene refreshes
are threaded through it. The adapter is a **viewer bridge**, not your MVU runtime —
it does not fold your product's `update`; a `UserMsg` is forwarded verbatim as a
`DispatchUser` effect, and you compose your own `update` around the adapter (see Usage).

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

## Sources / links

- Fable.Elmish (driven adapter model): https://elmish.github.io/elmish/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
