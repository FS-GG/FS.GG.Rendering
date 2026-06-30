---
name: fs-gg-ui-widgets
description: Generated product guidance for Skia-rendered FS.GG.UI Controls, rich text, chart controls, graph controls, DataGrid, and custom wrappers.
---

# Generated Controls

## Scope

Use this skill for generated product screens that compose controls in an
Elmish-style view function. Controls is the generated authoring path for
ordinary controls, rich text, chart controls, graph controls, DataGrid, and
custom wrappers.

## Public Contract

Reference `FS.GG.UI.Controls` and build `Control<'msg>` values with
module-per-control `create` functions and declarative attributes.
DataGrid is a data control with product-owned rows, columns, selection, focus,
and viewport state.
Use typed standard front doors for known controls, events, attributes, chart
data, and DataGrid data. Only use `Control.customControl`,
`Attr.customAttribute`, or `Attr.customEvent` for deliberate product-owned or
vendor extension points; custom usage must be visibly named as custom rather
than masquerading as a misspelled standard control.

## `CustomControl` does NOT rasterize its content

`Control.renderTree` (the production paint path the live host and every screenshot/preview
use) paints a **labeled placeholder** for a `custom-control` — it does **not** invoke the
`CustomControlDefinition` `Render`/`Draw`/`Layout` fields, so authored Skia geometry does
**not** appear in the window or in evidence. The catalog calls it a "product-owned wrapper",
which is for routing custom **events/attributes**, not for drawing.

So: when geometry must actually show in the rasterized/screenshot path, **build it from
primitive controls** (`Border` + `TextBlock` + `Stack`), not from one big `CustomControl`.
A reusable recipe is a fixed-cell grid composed of framed cells/rows that `renderTree` paints
reliably. Reserve `CustomControl` for non-visual extension seams.

## No-new-dependency property tests

When the product test project ships no FsCheck reference and the governance decision is
"no dependency change," you can still get property-style coverage: drive a **deterministic
generative loop** (a fixed-seed sequence of inputs) through the **real** engine/function and
assert the invariant each iteration. Disclose the pattern in the test file header so it reads
as intentional, not as a missing dependency.

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
`FS.GG.UI.Controls.Elmish` adapter at the product edge for commands and
subscriptions.

## Build Commands

Run `./fake.sh build -t Dev` and `./fake.sh build -t Verify` in the generated
product.

## Test Commands

Run `./fake.sh build -t Test` for product-owned control examples.

## Evidence

Product evidence belongs in the generated product readiness folder. Do not copy
framework readiness reports.

## Control Evidence Rules

- Compare your product's current `FS.GG.UI.` package pins against the versions you
  intend to ship against; when you validate controls against a locally built
  package, record it as a caveat so a stale pin never passes silently.
- Prefer real screenshot evidence for controls; disclose degraded captures,
  require reviewer accepted readiness, and keep manual caveats outside generated
  summary or managed section rewrites.
- Responsiveness evidence must validate pointer and keyboard activation
  separately from screenshot readiness and distinguish input routing from update,
  render, and present latency.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited checks remain visibly caveated.

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
products should use `FS.GG.UI.Controls` directly.

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

- [[fs-gg-styling]] — to theme and style the controls above (pick a theme, set a
  control's style variant and class, consume the resolved style), see the
  `fs-gg-styling` skill.
- [[fs-gg-elmish]] — wire control messages through the pure adapter at the edge.
- [[fs-gg-scene]] — the primitive layer controls ultimately render into.

## Sources / links

- Yoga (Flexbox layout engine behind control layout): https://www.yogalayout.dev/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
