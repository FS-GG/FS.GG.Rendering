---
name: fs-skia-generated-controls-guidance
description: Generated product guidance for Skia-rendered FS.Skia.UI Controls, rich text, chart controls, graph controls, DataGrid, and custom wrappers.
---

# Generated Controls

## Scope

Use this skill for generated product screens that compose controls in an
Elmish-style view function. Controls is the generated authoring path for
ordinary controls, rich text, chart controls, graph controls, DataGrid, and
custom wrappers.

## Public Contract

Reference `FS.Skia.UI.Controls` and build `Control<'msg>` values with
module-per-control `create` functions and declarative attributes.
DataGrid is a data control with product-owned rows, columns, selection, focus,
and viewport state.
Use typed standard front doors for known controls, events, attributes, chart
data, and DataGrid data. Only use `Control.customControl`,
`Attr.customAttribute`, or `Attr.customEvent` for deliberate product-owned or
vendor extension points; custom usage must be visibly named as custom rather
than masquerading as a misspelled standard control.

## Generic Message Flow

Keep product state and messages in the generated product:

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

Use `GraphView.create`, `BarChart.create`, `PieChart.create`, and
`ScatterPlot.create` from the same Controls package when the product needs
graph or chart variants.

When the generated product also selects Elmish program integration, use the
`FS.Skia.UI.Controls.Elmish` adapter at the product edge for commands and
subscriptions.

## Capability surface — E1–E5 (live dispatch → lookless slot composition)

The Controls runtime is a declarative-retained MVU core: your generated product
writes a single `view : 'model -> Control<'msg>` (or builds `Widget<'msg>` through
the typed front door `FS.Skia.UI.Controls.Typed`), and the framework supplies five
composable, **all-shipped** capabilities. None of them is a data binding,
`DataContext`, or lookless `ControlTemplate` — those remain permanent non-goals.

### E1 — live event dispatch

An authored event lowers to a binding keyed by the control's `ControlId`; the host
loop routes a `ControlEvent` to it through `Control.dispatch`, returning the `'msg`
your `update` folds in.

```fsharp
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Typed

let saveButton =
    Button.view { Button.defaults with Id = Some "save"; Text = "Save"; OnClick = Some SaveRequested }
// host: Control.dispatch { Kind = "click"; ControlId = Some "save"; Origin = ControlEventOrigin.Pointer; Payload = None } tree => [ SaveRequested ]
```

### E2 — retained identity (why focus/text survive a re-render)

Retained identity is a property of the **keyed tree**, not a binding. Give a control
a stable `Id`; the keyed reconciler matches it key-first across a sibling-shifting
re-render, so its focus / caret / text / animation survive even when an unrelated
sibling is inserted above it. Omit the key and a positional shift resets that state.

```fsharp
Stack.view
    { Stack.defaults with
        Children =
            [ banner
              Button.view { Button.defaults with Id = Some "editor"; Text = "Edit" } ] }
```

### E3 — style class / variant + visual state

Attach an ordered `StyleClass list` (typed `Variant` or free-form `Custom`); the
resolver folds `base < classes-in-order < visual-state` with fixed precedence. No CSS selectors.

```fsharp
Button.view { Button.defaults with Text = "Delete"; Classes = [ Variant StyleVariant.Danger ] }
```

### E4 — focus / keyboard traversal

`Focus.order` derives the deterministic tab order, `Focus.traverse` moves
Next/Previous (wrapping), and `Focus.route` classifies a delivered key against the
focused control. A focusable control inside a non-focusable container is its own stop.

```fsharp
let order = Focus.order tree
let next  = Focus.traverse order (Some "save") Focus.Next
```

### E5 — lookless slot composition (typed-closed)

Fill a control's declared, per-kind, **typed** slot regions with your OWN
`Widget<'msg>` to re-skin its **shape** — an icon before a button's label, a custom
panel header/footer. A slot fill is a static `Control<'msg>` your `view` already
computed — **not** a data-bound template, `DataContext`, or binding. The regions are
**closed per kind**: filling a region a kind does not declare is a compile error. An
unfilled slot renders the kind's existing chrome (byte-identical to before).

```fsharp
let icon = TextBlock.view { TextBlock.defaults with Text = "★" }

// Button declares Leading / Trailing; Panel declares Header / Footer:
let starred = Button.view { Button.defaults with Text = "Save"; Leading = Some icon }

let framed =
    Panel.view
        { Panel.defaults with
            Header = Some(TextBlock.view { TextBlock.defaults with Text = "Settings" })
            Children = [ body ] }
// Button.view { Button.defaults with Header = ... }  // does NOT compile — Button declares no Header
```

Slotted content is a first-class sub-tree: it composes with E1 dispatch, E3 styling,
and E4 focus, and keeps its E2 retained identity across a re-render — **free**,
because the fill lands in the control's `Children`, not a parallel channel.

## Build Commands

Run `./fake.sh build -t Dev` and `./fake.sh build -t Verify` in the generated
product.

## Test Commands

Run `./fake.sh build -t Test` for product-owned control examples.

## Evidence

Product evidence belongs in the generated product readiness folder. Do not copy
framework readiness reports.

## Package Boundary

Controls owns ordinary controls, rich text, chart controls, graph controls,
DataGrid, and custom wrappers. Layout remains a runtime package dependency;
generated control authoring stays in Controls.

## Generated Product

Keep examples small and product-owned. Do not copy framework galleries,
framework samples, framework readiness evidence, historical specs, framework
docs, or framework implementation projects.

## Charts migration

Users moving from the legacy Charts package should replace chart declarations
with Controls `LineChart`, `BarChart`, `PieChart`, `ScatterPlot`, `GraphView`,
and `DataGrid` declarations. There is no compatibility shim; generated
products should use `FS.Skia.UI.Controls` directly.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-skia-ui-widgets]] — the product-skills counterpart for generated controls.
- [[fs-skia-elmish]] — wire control messages through the pure adapter at the edge.

## Sources / links

- Yoga (Flexbox layout engine behind control layout): https://www.yogalayout.dev/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
