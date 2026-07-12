# KeyboardInput Fragment

> **Not shipped — this is repo-side fragment documentation.**
> `.template.config/template.json` sources nothing from `template/fragments/keyboard-input/`, so no generated
> product ever receives this file. The guidance a product actually gets is
> [`template/product-skills/fs-gg-keyboard-input/SKILL.md`](../../../template/product-skills/fs-gg-keyboard-input/SKILL.md) — the capability's `supplied-by`.
> Recorded as `materializes: none` on the `keyboard-input` row of `template/capabilities.yml`, and held there
> by R-FRAG in `tests/Package.Tests/SkillPackageReachTests.fs` (#510).

> This README is deliberately kept in **parity** with the product-skill above (feature 251, FR-004): the
> capability-boundary note appears in both. Parity is the point — it is not a second source of truth.

Adds keyboard input package references and reducer guidance.

Use `FS.GG.UI.KeyboardInput` for product-owned keyboard runtime state,
messages, pure updates, and emitted effects. Generated Controls screens should
consume keyboard state through Controls or the
`FS.GG.UI.Controls.Elmish` adapter when Elmish program integration is
selected.

Keep chart controls, graph controls, DataGrid, and rich text guidance in the
Controls fragment.

```fsharp
open FS.GG.UI.KeyboardInput

let bindings = [ { Key = "ArrowLeft"; Command = "move-left" }
                 { Key = "Space"; Command = "primary-action" } ]

let model, startupEffects = Keyboard.init bindings

let mapKey (key: ViewerKey) (isDown: bool) : Msg option =
    match key, isDown with
    | ArrowLeft, true -> Some MoveLeft
    | Space, true -> Some PrimaryAction
    | _ -> None
```

## Capability boundary — the default host is keyboard-only

The game family's governed default host (`Viewer.runApp` over `GeneratedAppHost`) is
**keyboard-only**: its only input seam is `MapKey: ViewerKey -> bool -> 'msg option`, and
`ViewerKey` has **no mouse/pointer case** (input arrives as `DispatchInput of ViewerKey * isDown`).
A mouse-aimed control scheme cannot be wired through `MapKey`. Reading the mouse requires the
pointer-aware interactive host — `InteractiveAppHost` via `Controls.Elmish.runInteractiveApp`, which
adds a `MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list` seam — a durable,
governance-scanned host-wiring change in `Program.fs`, not an edit at the input-mapping site. Decide
your control scheme with this boundary in mind.
