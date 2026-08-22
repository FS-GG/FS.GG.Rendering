# Elmish Fragment

> **Not shipped — this is repo-side fragment documentation.**
> `.template.config/template.json` sources nothing from `template/fragments/elmish/`, so no generated
> product ever receives this file. The guidance a product actually gets is
> [`template/product-skills/fs-gg-elmish/SKILL.md`](../../../template/product-skills/fs-gg-elmish/SKILL.md) — the capability's `supplied-by`.
> Recorded as `materializes: none` on the `elmish` row of `template/capabilities.yml`, and held there
> by R-FRAG in `tests/Package.Tests/SkillPackageReachTests.fs` (#510).

Adds Elmish adapter package references and generated product Elmish guidance.

Generated products that select Elmish and Controls should reference
`FS.GG.UI.Controls.Elmish` for command, subscription, and program adapter
wiring. Base Controls views remain generic over product messages and return
`Control<'msg>`.

Use `AdapterCommand<'msg>` for commands and `AdapterSubscription<'msg>` for
subscriptions in reusable guidance; generated examples may replace `'msg` with
the product `Msg` type.

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

let init () : Model * AdapterCommand<Msg> =
    initialModel, []

let update msg model : Model * AdapterCommand<Msg> =
    model, []

let view model : Control<Msg> =
    controlsExampleView model

let subscriptions model : AdapterSubscription<Msg> list =
    ControlsElmish.subscriptions [] []

let program =
    ControlsElmish.program init update view subscriptions
```

Map keyboard and control runtime effects at the product edge:

```fsharp
let keyboardCommands =
    ControlsElmish.interpretKeyboardEffect KeyboardCommandResolved keyboardEffect

let controlCommands =
    ControlsElmish.interpretControlEffect ControlRuntimeMsg controlEffect
```

## Transition-aware workspace host

`FS.GG.UI.Elmish.TransitionHost` is an additive host transaction for expensive React workspace
presentation. Existing `ElmishAdapter` updates, simulation ticks, and controlled input dispatch stay
synchronous. Opt in only at the React host edge that owns an expensive target such as Editor, Plan,
or Simulate.

The product owns its target and response types. Begin with a typed request, capture the returned
generation token when starting worker or client-feature work, and return every delayed response with
that original generation and target:

```fsharp
open FS.GG.UI.Elmish

type Workspace = Editor | Plan | Simulate
type Prepared = PlanningRows of int | ClientFeatures of string list

let request target =
    { Target = target
      PendingFocus = { ControlId = "workspace-status"; AriaLabel = $"{target} loading" }
      CommittedFocus = { ControlId = $"workspace-{target}"; AriaLabel = $"{target} workspace" } }

let host : TransitionHostModel<Workspace, Prepared> =
    TransitionHost.init TransitionVisibility.Visible

let pending, effects = TransitionHost.beginTransition (request Plan) host
let token = TransitionHost.authoritative pending |> Option.get

let workerResponse =
    TransitionHostMsg.ResponseArrived
        { Generation = token.Generation
          Target = token.Target
          Kind = TransitionResponseKind.PlanningWorker
          Payload = PlanningRows 1200 }
```

Interpret the returned effects at the DOM/React boundary:

- Run every `RequestPresentation presentation` in a fresh React `startTransition`, including each
  delayed response that arrives after an `await`. React does not retain the transition lane across an
  async boundary.
- After that exact presentation DOM commits, dispatch `Presented presentation.Token` from a layout
  effect. Never synthesize a generation-only acknowledgement; the generation, target, and response
  revision must all match.
- Keep controlled text/file setters and normal Elmish dispatch outside `startTransition`. Feed their
  corresponding `ControlledValueChanged`, `ControlledFileChanged`, and `ControlledBlurred` messages
  to the bridge synchronously.
- While `TransitionHost.isPending` is true, implement `ReleasePointerCapture` and `SuppressInput`
  before an obsolete DOM can dispatch global pointer/key/click/file actions. Implement `MoveFocus`
  as the single pending or committed focus/ARIA destination.
- Forward document visibility edges as `VisibilityChanged Hidden|Visible`. Hidden responses remain
  authoritative without presentation; one hidden-to-visible edge requests the newest token once.

The typed ledger is the diagnostic authority. Rejected stale responses and acknowledgements remain
observable and cannot mutate the committed target. Rapid Editor→Plan→Simulate replacement therefore
permits only the latest exact Simulate token to commit.
