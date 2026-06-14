module ControlsTypedAdapterTests

open System.IO
open Expecto
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

let repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some parent -> find parent.FullName
            | None -> dir

    find __SOURCE_DIRECTORY__

type Msg = Save

type Model = { Saved: bool }

// A product view authored entirely through the typed front door, terminated with
// `Widget.toControl` so it satisfies the existing `AdapterProgram.View: 'model ->
// Control<'msg>` contract with no adapter edit (FR-009).
let widgetView (model: Model) : Control<Msg> =
    FS.Skia.UI.Controls.Typed.Stack.view
        { FS.Skia.UI.Controls.Typed.Stack.defaults with
            Children =
                [ FS.Skia.UI.Controls.Typed.TextBlock.view
                      { FS.Skia.UI.Controls.Typed.TextBlock.defaults with Text = "Typed" }
                  FS.Skia.UI.Controls.Typed.Button.view
                      { FS.Skia.UI.Controls.Typed.Button.defaults with
                          Id = Some "save"
                          Text = "Save"
                          OnClick = Some Save } ] }
    |> Widget.toControl

// The same view authored end-to-end through the typed front door, returning
// `Widget<'msg>` directly — note the absence of any `Widget.toControl` shim in
// product code (068 US1). `widgetView model = typedWidgetView model |> Widget.toControl`.
let typedWidgetView (model: Model) : Widget<Msg> =
    FS.Skia.UI.Controls.Typed.Stack.view
        { FS.Skia.UI.Controls.Typed.Stack.defaults with
            Children =
                [ FS.Skia.UI.Controls.Typed.TextBlock.view
                      { FS.Skia.UI.Controls.Typed.TextBlock.defaults with Text = "Typed" }
                  FS.Skia.UI.Controls.Typed.Button.view
                      { FS.Skia.UI.Controls.Typed.Button.defaults with
                          Id = Some "save"
                          Text = "Save"
                          OnClick = Some Save } ] }

[<Tests>]
let typedAdapterTests =
    testList "Typed controls Elmish boundary and dependency guard" [
        test "Widget.toControl-terminated view runs through AdapterProgram unchanged" {
            let init () = { Saved = false }, []
            let update msg model =
                match msg with
                | Save -> { model with Saved = true }, []

            let program = ControlsElmish.program init update widgetView (fun _ -> [])

            let model, initCommands = program.Init()
            let updated, _ = program.Update Save model
            let control = program.View updated
            let rendered = Control.render Theme.light control

            Expect.isTrue updated.Saved "adapter update ran"
            Expect.isEmpty initCommands "no startup commands required"
            Expect.isGreaterThan rendered.NodeCount 0 "typed view renders through the adapter"

            let click =
                { Kind = "click"; ControlId = Some "save"; Origin = ControlEventOrigin.Pointer; Payload = None; Nav = None }

            Expect.equal (Control.dispatch click control) [ Save ] "typed event dispatches through the adapter view"
        }

        test "programOfWidget runs a Widget-returning view with no Widget.toControl in product code (US1, SC-001/SC-002/FR-004)" {
            let init () = { Saved = false }, []
            let update msg model =
                match msg with
                | Save -> { model with Saved = true }, []

            // Authored with the typed front door; the view returns Widget<Msg> and the
            // adapter lowers it internally — product code never calls Widget.toControl.
            let program = ControlsElmish.programOfWidget init update typedWidgetView (fun _ -> [])

            let model, initCommands = program.Init()
            let viaWidgetPath = program.View model
            let viaLegacyBoundary = typedWidgetView model |> Widget.toControl

            let renderedWidget = Control.render Theme.light viaWidgetPath
            let renderedLegacy = Control.render Theme.light viaLegacyBoundary

            Expect.isEmpty initCommands "no startup commands required"
            Expect.isGreaterThan renderedWidget.NodeCount 0 "typed Widget view renders through the adapter"
            Expect.equal renderedWidget.NodeCount renderedLegacy.NodeCount "lowering parity: same node count as view >> Widget.toControl"

            let click =
                { Kind = "click"; ControlId = Some "save"; Origin = ControlEventOrigin.Pointer; Payload = None; Nav = None }

            Expect.equal
                (Control.dispatch click viaWidgetPath)
                (Control.dispatch click viaLegacyBoundary)
                "lowering parity: identical dispatch to the hand-written boundary"
            Expect.equal (Control.dispatch click viaWidgetPath) [ Save ] "typed event dispatches through the Widget-view path"
        }

        test "Widget.ofControl lowers identically to the wrapped control; Widget and Control programs coexist (US4, FR-010)" {
            let legacy: Control<Msg> =
                FS.Skia.UI.Controls.Typed.Button.view
                    { FS.Skia.UI.Controls.Typed.Button.defaults with
                        Id = Some "save"
                        Text = "Save"
                        OnClick = Some Save }
                |> Widget.toControl

            // toControl (ofControl c) = c — the bridge is identity on the lowering seam.
            let bridged = Widget.ofControl legacy |> Widget.toControl
            let click =
                { Kind = "click"; ControlId = Some "save"; Origin = ControlEventOrigin.Pointer; Payload = None; Nav = None }

            Expect.equal (Control.dispatch click bridged) (Control.dispatch click legacy) "ofControl >> toControl preserves dispatch"
            Expect.equal
                (Control.render Theme.light bridged).NodeCount
                (Control.render Theme.light legacy).NodeCount
                "ofControl >> toControl preserves render"

            // Coexistence: one program on the Widget-view path, one on the Control-view path.
            let init () = { Saved = false }, []
            let update msg model =
                match msg with
                | Save -> { model with Saved = true }, []

            let widgetProgram = ControlsElmish.programOfWidget init update typedWidgetView (fun _ -> [])
            let controlProgram = ControlsElmish.program init update widgetView (fun _ -> [])
            let widgetModel, _ = widgetProgram.Init()
            let controlModel, _ = controlProgram.Init()

            Expect.isGreaterThan (Control.render Theme.light (widgetProgram.View widgetModel)).NodeCount 0 "widget-path program renders"
            Expect.isGreaterThan (Control.render Theme.light (controlProgram.View controlModel)).NodeCount 0 "control-path program renders"
        }

        test "base Controls package gains no Fable.Elmish dependency (FR-011, SC-004)" {
            let controlsProject = Path.Combine(repositoryRoot, "src", "Controls", "Controls.fsproj")
            let text = File.ReadAllText controlsProject

            Expect.isFalse (text.Contains "Fable.Elmish") "Controls.fsproj does not reference Fable.Elmish"
            Expect.isFalse (text.Contains "PackageReference") "Controls.fsproj adds no NuGet package dependency"

            [ @"..\Scene\Scene.fsproj"; @"..\Layout\Layout.fsproj"; @"..\KeyboardInput\KeyboardInput.fsproj" ]
            |> List.iter (fun reference -> Expect.stringContains text reference $"existing reference {reference} retained")
        }
    ]
