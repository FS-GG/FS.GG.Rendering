---
name: fs-gg-ui-widgets
description: Build Skia-rendered FS.GG.UI Controls, rich text, chart controls, graph controls, DataGrid, custom wrappers, and generated product examples.
---

# Controls

## Scope

Use this skill for user-facing controls built with `FS.GG.UI.Controls`:
forms, buttons, text input, lists, tables, rich text, layout containers, chart
controls, graph controls, DataGrid, custom control wrappers, catalog examples,
and generated product guidance.

For **Ant-styled** controls, the canonical upstream Ant reference is the central hub
[`docs/product/ant-design/reference/ant-llms-sources.md`](../../../docs/product/ant-design/reference/ant-llms-sources.md)
(the three Ant LLM files); apply patterns via the `fs-gg-ant-design` skill and the per-family docs
under `docs/product/ant-design/patterns/`. Ant is a design language only — no React/DOM.

## Public Contract

The supported API lives in `src/Controls/*.fsi`. View functions should build
`Control<'msg>` values with module-per-control `create` functions and
declarative attributes such as `TextBox.value`, `Button.onClick`,
`LineChart.series`, `DataGrid.columns`, `DataGrid.rows`, and `Stack.children`.
Persistent values stay in the product model; controls may keep only keyed
transient interaction state through product-owned `ControlRuntime`.

### `CustomControl` does NOT rasterize its content (feature 122)

`Control.renderTree` (the production paint path the live host and every
screenshot/preview use) paints a **labeled placeholder** for a `custom-control`
— it does **not** invoke the `CustomControlDefinition` `Render`/`Draw`/`Layout`
fields, so authored Skia geometry does not appear in the window or in evidence.
`CustomControl` is a wrapper for product-owned **events/attributes**, not a
draw seam. When geometry must show in the rasterized/screenshot path, build it
from primitive controls (`Border` + `TextBlock` + `Stack`); reserve
`CustomControl` for non-visual extension points. (A null/blank `Id` or a null
effect string is guarded — `validate`/`create` return a diagnostic, never an NRE.)

## Generated Product Pattern

Generated examples should keep product state and messages local:

```fsharp
type Msg =
    | NameChanged of string
    | SaveRequested
    | GridSelectionChanged of string

type Model =
    { Name: string
      Revenue: ChartSeries list
      Columns: DataGridColumn list
      Rows: DataGridRow list }

let view model : Control<Msg> =
    Stack.create [
        Stack.children [
            TextBox.create [
                TextBox.value model.Name
                TextBox.onChanged NameChanged
            ]
            Button.create [
                Button.text "Save"
                Button.onClick SaveRequested
            ]
            LineChart.create [ LineChart.series model.Revenue ]
            GraphView.create [ GraphView.nodes [ "form"; "chart"; "grid" ] ]
            DataGrid.create model.Columns [
                DataGrid.rows model.Rows
                DataGrid.visibleRange {
                    FirstIndex = 0
                    Count = model.Rows.Length
                    Total = model.Rows.Length
                }
            ]
        ]
    ]
```

When Elmish program integration is selected, use the
`FS.GG.UI.Controls.Elmish` adapter for commands, subscriptions, and program
wiring at the product edge.

## Capability surface — E1–E5 (live dispatch → lookless slot composition)

The Controls runtime is a declarative-retained MVU core: you write a single
`view : 'model -> Control<'msg>` (or build `Widget<'msg>` through the typed front
door `FS.GG.UI.Controls.Typed`), and the framework supplies five composable,
**all-shipped** capabilities. None of them is a data binding, `DataContext`, or
lookless `ControlTemplate` — those remain permanent non-goals.

### E1 — live event dispatch

An authored event lowers to a binding keyed by the control's `ControlId`; the host
loop routes a `ControlEvent` to it through `Control.dispatch`, returning the `'msg`
your `update` folds in.

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed

let saveButton =
    Button.view { Button.defaults with Id = Some "save"; Text = "Save"; OnClick = Some SaveRequested }

// host loop: Control.dispatch { Kind = "click"; ControlId = Some "save"; Origin = ControlEventOrigin.Pointer; Payload = None } tree
//            => [ SaveRequested ]
```

### E2 — retained identity (why focus/text survive a re-render)

Retained identity is a property of the **keyed tree**, not a binding. Give a control
a stable `Id`; the keyed reconciler matches it key-first across a sibling-shifting
re-render, so its focus / caret / text / animation state survive even when an
unrelated sibling is inserted above it. Omit the key and a positional shift resets
that transient state.

```fsharp
// the editor stays keyed, so inserting a banner above it does NOT reset its caret:
Stack.view
    { Stack.defaults with
        Children =
            [ banner
              Button.view { Button.defaults with Id = Some "editor"; Text = "Edit" } ] }
```

### E3 — style class / variant + visual state

Attach an ordered `StyleClass list` (typed `Variant` or free-form `Custom`); the
resolver folds `base < classes-in-order < visual-state` with fixed precedence. No
CSS selectors.

The **runtime visual state** has a public entry point (feature 096): `deriveVisualState
model controlId : VisualState` is the pure, total projection from live interaction state
to a single `VisualState` under the fixed closed precedence tail `Pressed > Selected >
Focused > Hover > Normal` (a control named by no interaction state yields `Normal`). That is
the state the resolver folds in. The host stamps it onto the lowered tree **pre-reconcile**
via the internal `applyRuntimeVisualState model control` (it preserves a consumer-set
non-`Normal` attribute and emits nothing at `Normal`, so a resting control is byte-identical);
consumers read state through the public `deriveVisualState`, not the internal bridge.

```fsharp
Button.view { Button.defaults with Text = "Delete"; Classes = [ Variant StyleVariant.Danger ] }
// the current visual state rides the control through the reconciler:
//   ControlRuntime.deriveVisualState model "delete"  // => Pressed when the pointer is down on it
```

### E4 — focus / keyboard traversal

`Focus.order` derives the deterministic tab order from accessibility metadata and
`Focus.traverse` moves Next/Previous (wrapping). A focusable control inside a
non-focusable container is its own tab stop.

`Focus.route` (as it ships after feature 100) takes `role`, `keyboard`, `navRange`,
`key`, `isTab`, `shift` and returns a closed `KeyRouting` = `Activate` | `Navigate of
NavIntent` | `Traverse of FocusMove` | `Fallthrough`. A focused **navigation** key is
classified by `role` (and the declared `navRange` for a value role) into a closed
`NavIntent` = `ValueStep of delta` (a signed step the host applies to the live value and
clamps) | `SelectionMove of Direction` | `GridMove of rowDelta * colDelta`, carried by
`KeyRouting.Navigate`. Activation and navigation are tested **before** the Tab test, so a
control that lists a traversal key consumes it; only an unconsumed Tab/Shift+Tab yields
`Traverse`.

```fsharp
let order = Focus.order tree
let next  = Focus.traverse order (Some "save") Next   // FocusMove.Next

// route the focused control's key; a slider's Right-arrow becomes a Navigate (ValueStep):
match Focus.route role keyboard navRange "ArrowRight" false false with
| Navigate (ValueStep delta) -> ()   // host adds delta to the live value, clamps
| _ -> ()
```

### E5 — lookless slot composition (typed-closed)

Fill a control's declared, per-kind, **typed** slot regions with your OWN
`Widget<'msg>` to re-skin its **shape** — an icon before a button's label, a custom
panel header/footer. A slot fill is a static `Control<'msg>` your `view` already
computed — **not** a data-bound template, `DataContext`, or binding. The regions are
**closed per kind**: filling a region a kind does not declare is a compile error.
An unfilled slot renders the kind's existing chrome (byte-identical to before).

```fsharp
let icon = TextBlock.view { TextBlock.defaults with Text = "★" }

// Button declares Leading / Trailing; Panel declares Header / Footer:
let starred =
    Button.view { Button.defaults with Text = "Save"; Leading = Some icon }

let framed =
    Panel.view
        { Panel.defaults with
            Header = Some(TextBlock.view { TextBlock.defaults with Text = "Settings" })
            Children = [ body ] }
// Button.view { Button.defaults with Header = ... }  // does NOT compile — Button declares no Header (closed per kind)
```

Slotted content is a first-class sub-tree: it composes with E1 dispatch, E3 styling,
and E4 focus, and keeps its E2 retained identity across a re-render — **free**,
because the fill lands in the control's `Children`, not a parallel channel.

## Build Commands

Run `./fake.sh build -t Dev` for normal development and
`./fake.sh build -t VerifyPreflight` before broad verification. Run
`./fake.sh build -t Verify` before readiness sign-off. Use `./fake.sh build -t
PackLocal` and `./fake.sh build -t PackageSurfaceCheck` when changing `.fsi`
files. If `Verify` or `Ci` reports `environment-failure`, focused gates are
diagnostic only; final readiness needs a later healthy broad pass in a fresh
shell, fresh container, or CI runner.

## Test Commands

Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` for focused
coverage. The governed targets are `./fake.sh build -t ControlsCatalogCheck`,
`./fake.sh build -t ControlsInteractionCheck`, and
`./fake.sh build -t ControlsRenderingCheck`.

## Evidence

Update the active feature readiness reports for control catalog, semantic
tests, interaction tests, layout/rendering, public surface, and generated
product evidence when behavior or public surface changes. Stable public
surface baselines live under `readiness/surface-baselines/`. Supported catalog
rows need purpose, attributes, events, visual states, accessibility metadata,
examples, tests, and evidence.

## Package Boundary

Controls owns ordinary controls, rich text, chart controls, graph controls,
DataGrid, custom wrappers, the catalog, and generated controls guidance. Scene,
SkiaViewer, Elmish, KeyboardInput, Layout, and Testing remain separate
capabilities for lower-level or host-specific work. Layout remains a runtime
package dependency; generated control authoring stays in Controls.

## Generated Product

Generated products with Controls receive this skill. Product examples must be
product-owned and must not copy framework galleries, samples, historical specs,
readiness evidence, docs, or implementation projects.

## Charts migration

Users moving from the legacy Charts package should replace chart declarations
with Controls `LineChart`, `BarChart`, `PieChart`, `ScatterPlot`, `GraphView`,
and `DataGrid` declarations. There is no compatibility shim; generated
products should use `FS.GG.UI.Controls` directly.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-layout]] is the runtime layout engine these controls compose over.
- [[fs-gg-scene]] is the primitive surface controls ultimately render into.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (the driven Skia rendering library): https://github.com/mono/SkiaSharp
