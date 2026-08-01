---
name: fs-gg-ui-widgets
description: Generated product guidance for Skia-rendered FS.GG.UI Controls, rich text, chart controls, graph controls, DataGrid, and custom wrappers.
# This skill MANDATES a rule whose instrument lives in another skill. Declaring it holds the two
# `materializes-when` sets to each other (R-INST, FS.GG.Rendering#624): if `fs-gg-elmish` ever stops
# materializing where this skill does, a product would be handed the rule and never the instrument.
# See template/product-skills/README.md.
instruments:
  - rule: responsiveness evidence (respondsProofOf / captureRespondsProof, OnFrameMetrics)
    skill: fs-gg-elmish
---
<!-- skill-refs: closed-ok FS.GG.Rendering#624 — cited as the issue that ESTABLISHED the R-INST rule, not as somewhere to go. Closed is correct; it stays closed. The ref it excuses is in the YAML frontmatter above, which cannot host an HTML comment without breaking the parse; closed-ok is file-scoped, so it is honoured from here. -->

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
use) paints a **labeled placeholder** for a `custom-control`. The catalog calls it a
"product-owned wrapper", which is for routing custom **events/attributes**, not for drawing.
`CustomControlDefinition` carries only `Id`/`Effects`/`Accessibility`/`Diagnostics` — it no
longer advertises `Render`/`Draw`/`Layout`/`Measure`/`HitTest` callbacks, because nothing ever
invoked them.

So: when geometry must actually show in the rasterized/screenshot path, use the **`canvas`
kind** — `Canvas.create [ Canvas.scene myScene ]` carries an immutable `Scene` through the
render path, clipped and translated to the laid-out box. For must-show chrome, **build it from
primitive controls** (`Border` + `TextBlock` + `Stack`); a reusable recipe is a fixed-cell grid
composed of framed cells/rows that `renderTree` paints reliably. Reserve `CustomControl` for
non-visual extension seams.

## Check the control you authored — `validate`

Authoring errors in a control tree are **not** type errors: a custom control with a missing accessible
label, a definition that declares an event nothing routes, a control that fails its accessibility
contract — all of these compile, render, and ship. `validate` is the entry point that says so, and it
is the same name in each authoring module:

```fsharp
open FS.GG.UI.Controls

Accessibility.validate control       // ControlDiagnostic list — the a11y contract this control breaks
CustomControl.validate definition    // ControlDiagnostic list — authoring errors in a CustomControlDefinition
Catalog.validate ()                  // ControlDiagnostic list — the catalog's own self-check
```

Each returns a `ControlDiagnostic` list — **empty means clean**, so the assertion is
`Expect.isEmpty`, and it costs one line in the suite you already have. Validate a control you authored
by hand, and *every* `CustomControlDefinition` you write: it is the only check standing between a
mislabeled extension seam and a screen reader that says nothing.

`Control.diagnostics` is the tree-wide companion — it collects what a whole `Control<'msg>` reports,
without rendering it. See [[fs-gg-elmish]] for it and the runtime/adapter `diagnostics` beside it.

**Pointer lives next door.** `docs/api-surface/Controls/Pointer.fsi` names this skill, but the pointer
route is taught in [[fs-gg-elmish]] — `Pointer.replay` (the pure fold SC-005's determinism rests on),
`routeInteractivePointer`, and the `Perf.runScript*` drivers built on them. Go there for anything that
drives a click rather than authors one.

**Logical-canvas ownership also lives next door.** On an `InteractiveAppHost`, SkiaViewer supplies
Controls with the selected logical size and a pointer sample already mapped through the inverse
letterbox fit. Seed `ViewerOptions.LogicalSize` and emit `ApplyLogicalCanvas` for runtime changes;
never scale the control tree or remap the pointer a second time. See [[fs-gg-skiaviewer]].

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

## Charts whose series colors carry meaning

`ChartSeries.Name` is a label, not a color contract. When the distinction between series is part of
the product meaning, author the identity, color, and points together and render that exact value. A
product-owned `Canvas` scene is the direct route when each line needs an authored color:

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Scene

type DamageSeriesId = Dealt | Taken

module DamageSeriesId =
    let all = [ Dealt; Taken ]

type DamageSeries =
    { Id: DamageSeriesId
      Color: Color
      Points: Point list }

let private dealtColor = { Red = 42uy; Green = 120uy; Blue = 214uy; Alpha = 255uy }
let private takenColor = { Red = 27uy; Green = 175uy; Blue = 122uy; Alpha = 255uy }

let damageSeries model =
    [ { Id = Dealt; Color = dealtColor; Points = dealtPoints model }
      { Id = Taken; Color = takenColor; Points = takenPoints model } ]

let private seriesScene series =
    series.Points
    |> List.pairwise
    |> List.map (fun (startPoint, endPoint) ->
        Scene.line startPoint endPoint (Paint.stroke series.Color 3.0))
    |> Scene.group

let statsChartScene model =
    damageSeries model |> List.map seriesScene |> Scene.group

let statsChart model =
    Canvas.create [ Canvas.scene (statsChartScene model) ]
```

The production view and supplemental evidence both call `damageSeries` and `statsChartScene`. The
evidence compares `damageSeries model |> List.map _.Id` with `DamageSeriesId.all`, requires the colors
to be pairwise distinct, and rasterizes `statsChartScene model`. Do not duplicate `"dealt"`/`"taken"`
lists, accept `series.Length = 2`, or infer color from a label such as `"Dealt #2a78d6"`; those checks
can stay green while both traces render identically.

Keep the resulting reference PNG content-addressed and current: its filename and receipt identity
must match the SHA-256 of its bytes, exactly one referenced PNG remains for the subject, and the visual
or pixel check confirms both authored colors occur on the chart. This is deterministic reference-raster
evidence, not native compositor or usability evidence. The complete exact-identity, two-output HUD,
KPI, component-only classification, and raster-receipt test recipe lives in `fs-gg-testing`.

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

**The instruments for the responsiveness rule are in [[fs-gg-elmish]]**, which is
where they are explained — `ControlsElmish.respondsProofOf` / `captureRespondsProof`
for the activation half (their `RespondsVerdict` is the only evidence class that
tells *renders* from *responds*), and the per-frame projection
(`compositorDiagnostics`, `layoutMetrics`, `responsivenessTimingContribution`,
`InteractiveAppHost.OnFrameMetrics`) for the latency half, which separates routing
from update, render and present. A screenshot proves neither.

They live in that skill rather than this one because `Perf`, `BoundIds` and
`ControlsElmish` come from the Controls packages, and a single copy of the recipe
is the one that cannot rot — not because a widget product lacks them: every
profile this skill ships to (`app`, `game`) also receives [[fs-gg-elmish]].

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
- [[fs-gg-elmish]] — wire control messages through the pure adapter at the edge,
  and the instruments the responsiveness Evidence Rule above requires
  (`captureRespondsProof`, the `OnFrameMetrics` projection).
- [[fs-gg-scene]] — the primitive layer controls ultimately render into.

## Sources / links

- Yoga (Flexbox layout engine behind control layout): https://www.yogalayout.dev/
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
