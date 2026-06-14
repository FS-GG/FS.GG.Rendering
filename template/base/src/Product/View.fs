module Product.View

open FS.Skia.UI.Scene
open Product.Model
//#if (profile == "governed" || profile == "headless-scene")

let view model =
    let textColor = { Red = 240uy; Green = 240uy; Blue = 240uy; Alpha = 255uy }

    Group(
        [ { Nodes = [ Rectangle((16.0, 16.0, 288.0, 128.0), { Red = 24uy; Green = 32uy; Blue = 44uy; Alpha = 255uy }) ] }
          { Nodes = [ Text((32.0, 56.0), $"Governed headless scene: {model.Name}", textColor) ] }
          { Nodes = [ Text((32.0, 88.0), $"renders: {model.RenderCount}", textColor) ] } ]
    )

//#else
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.Scene

// The typed Props front door (the `Controls.Typed` namespace) is the recommended,
// compiler-guided authoring path: each control is built from an immutable `Props<'msg>` record
// whose field names + types tell you exactly what it accepts. The typed modules are bound by
// EXPLICIT abbreviation below (not by namespace-open order — do not rely on open order) so the
// names below are the typed front door here; the legacy stringly builders stay available
// fully-qualified and remain documented for consumers who want them. Discover a control's
// contract from IntelliSense, the bundled `docs/api-surface/Controls/` signatures,
// `docs/controls-catalog.md`, or the `Catalog` discovery API — never by reflection.
module TextBlock = FS.Skia.UI.Controls.Typed.TextBlock
module RichText = FS.Skia.UI.Controls.Typed.RichText
module TextBox = FS.Skia.UI.Controls.Typed.TextBox
module Button = FS.Skia.UI.Controls.Typed.Button
module LineChart = FS.Skia.UI.Controls.Typed.LineChart
module GraphView = FS.Skia.UI.Controls.Typed.GraphView
module DataGrid = FS.Skia.UI.Controls.Typed.DataGrid
module Stack = FS.Skia.UI.Controls.Typed.Stack

let visibleRows model =
    { FirstIndex = 0
      Count = model.GridRows.Length
      Total = model.GridRows.Length }

// Author the example UI through the typed Props front door. Every control is a
// `{ Module.defaults with Field = ... } |> Module.view` expression: start from `defaults`,
// override only the fields you need (IntelliSense enumerates them), and `view` returns an
// opaque `Widget<'msg>`. To add a control kind not shown here, type `SomeControl.defaults`
// and let the compiler list its `Props` fields — no attribute-name guessing, no reflection.
let controlsWidgetView (model: Model) : Widget<Msg> =
    Stack.view
        { Stack.defaults with
            Children =
                [
                  // display control
                  TextBlock.view { TextBlock.defaults with Text = "Product controls" }
                  RichText.view { RichText.defaults with Runs = model.RichIntro.Runs }

                  // interactive input: a stateful control. Its per-identity `TextInputModel` is
                  // seeded from the props via `TextBox.init`; the live host then RETAINS edits
                  // across frames keyed by control identity, so typing is preserved. `OnChanged`
                  // binds a message; `OnChanged = None` would bind nothing.
                  (let nameProps =
                      { TextBox.defaults "name" with
                          Value = model.Name
                          OnChanged = Some NameChanged }

                   TextBox.view nameProps (fst (TextBox.init nameProps)))

                  // button with an event handler. `OnClick = Some msg` dispatches `msg` on click
                  // (here a pointer click on the "save"-keyed control); `OnClick = None` binds
                  // nothing. `Id = Some "save"` gives the control a stable key for the host.
                  Button.view
                      { Button.defaults with
                          Id = Some "save"
                          Text = "Save"
                          Enabled = model.CanSave
                          OnClick = Some SaveRequested }

                  LineChart.view { LineChart.defaults with Series = model.Revenue }
                  GraphView.view { GraphView.defaults with Nodes = [ "form"; "chart"; "grid" ] }

                  // another stateful control: the data grid model is seeded from props the same
                  // way as the text box (`DataGrid.init`), and retained by the host across frames.
                  (let gridProps =
                      { DataGrid.defaults "grid" with
                          Columns = model.GridColumns
                          Rows = model.GridRows
                          RowHeight = 24.0
                          ViewportHeight = 132.0 }

                   DataGrid.view gridProps (fst (DataGrid.init gridProps))) ] }

// `Widget.toControl` is the single documented seam that lowers the typed tree to the existing
// `Control<'msg>` IR the render path + Elmish adapter consume. The typed front door lowers
// structurally to the same controls (proven by the framework's TypedLoweringTests parity
// suite), so the rendered output is unchanged from the legacy starter (FR-003).
let controlsExampleView (model: Model) : Control<Msg> =
    controlsWidgetView model |> Widget.toControl

// `ControlsElmish.program` takes a `Control<'msg>`-returning view. To wire a purely typed
// (`Widget<'msg>`-returning) view directly, use `ControlsElmish.programOfWidget` (or
// `ControlsElmish.widgetView`), which lowers via `Widget.toControl` for you.
let adapterProgram =
    ControlsElmish.program Product.Model.init Product.Model.update controlsExampleView Product.Model.subscriptions

// The default scaffold `view` rasterizes the REAL example control tree through the
// production tree-render path (`Control.renderTree`) at the output extent, so the
// unmodified generated app shows actual styled controls — form, rich text, chart, graph,
// and DataGrid — laid out by the framework, not hand-drawn placeholder geometry (FR-003).
let contentArea: FS.Skia.UI.Scene.Size = { Width = 640; Height = 480 }

let view (model: Model) : SceneNode =
    let rendered = Control.renderTree Theme.light contentArea (controlsExampleView model)
    Group [ rendered.Scene ]

//#endif
