# Controls Fragment

Adds the `FS.GG.UI.Controls` package reference, Skia-rendered Controls
guidance, product-owned example views, product test coverage, and generated
controls guidance. Generated app skill installation receives the consumer-facing
`fs-gg-ui-widgets` skill authored in this fragment (`skill/SKILL.md`).

Generated products use one Elmish-style Controls path for ordinary controls,
rich text, chart controls, graph controls, and DataGrid. Product models own
business data and messages; Controls declarations stay generic over
`Control<'msg>`.

When Controls are authored beside Scene primitives, generated examples should
fully qualify collision-prone names. Use `FS.GG.UI.Scene.Rect`,
`FS.GG.UI.Scene.Paint`, and `FS.GG.UI.Scene.TextRun` for scene records, and
use Controls front doors such as `FS.GG.UI.Controls.TextBlock.create`,
`FS.GG.UI.Controls.TextBox.onChanged`, and
`FS.GG.UI.Controls.Stack.children` for controls. Do not rely on namespace
open order to choose between overlapping names.

```fsharp
open FS.GG.UI.Controls

let view model : Control<Msg> =
    Stack.create
        [ Stack.children
            [ TextBox.create [ TextBox.value model.Name; TextBox.onChanged NameChanged ]
              Button.create [ Button.text "Save"; Button.onClick SaveRequested ] ] ]
```

## Pointer / mouse interaction

Mouse input is consumer-driven through the host-independent pointer front door in
`FS.GG.UI.Controls` (`Pointer.init`/`toMsg`/`update`/`replay`) and the MVU bridge
`FS.GG.UI.Controls.Elmish.interpretPointerOutcome`. A product translates host
`ViewerEvent.Pointer*` events into a neutral `PointerSample`, runs the pure
`Pointer.update` against the current `LayoutResult` (the framework hit-tests; the
product writes no coordinate math), and routes the resulting `PointerInteraction`
values — `HoverEnter`/`HoverLeave`, `Click`, `DragBegin`/`DragMove`/`DragEnd`,
`Scroll` — to `ControlId`-level product messages. The per-button `Click` carries
`PointerButton.Primary`/`Secondary`/`Middle`, so right-click context actions are a
plain match. Pointer support is additive and opt-in: a keyboard-only product that
never builds a `PointerState` behaves unchanged.

Generated products must not copy framework galleries, framework samples,
framework readiness evidence, historical specs, or framework implementation
projects.
