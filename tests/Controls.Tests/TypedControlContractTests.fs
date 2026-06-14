module ControlsTypedControlContractTests

open System
open System.IO
open Microsoft.FSharp.Reflection
open Expecto
open FS.Skia.UI.Controls

let repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then
            dir
        else
            (match Directory.GetParent dir |> Option.ofObj with Some p -> find p.FullName | None -> dir)
    find __SOURCE_DIRECTORY__

let read (relativePath: string) =
    File.ReadAllText(Path.Combine(repositoryRoot, relativePath.Replace("/", string Path.DirectorySeparatorChar)))

let attrByName name (control: Control<'msg>) =
    control.Attributes |> List.find (fun attr -> attr.Name = name)

[<Tests>]
let typedControlContractTests =
    testList "Typed standard controls contract" [
        test "Types signature declares typed standard controls events attributes and values" {
            let typesFsi = read "src/Controls/Types.fsi"

            [ "type StandardControlKind"
              "type StandardEventKind"
              "type StandardAttributeName"
              "type StandardAttributeValue"
              "KnownControl"
              "KnownEvent"
              "KnownAttribute" ]
            |> List.iter (fun required ->
                Expect.stringContains typesFsi required $"Types.fsi exposes {required}")
        }

        test "Control and Attributes signatures expose typed standard and visibly custom APIs" {
            let controlFsi = read "src/Controls/Control.fsi"
            let attributesFsi = read "src/Controls/Attributes.fsi"
            let combined = controlFsi + Environment.NewLine + attributesFsi

            [ "standard"
              "standardEvent"
              "standardAttribute"
              "customControl"
              "customEvent"
              "customAttribute"
              "lowerStandard"
              "lowerCustom" ]
            |> List.iter (fun required ->
                Expect.stringContains combined required $"typed control front door exposes {required}")
        }

        test "Catalog and Diagnostics signatures expose schema-backed validation" {
            let catalogFsi = read "src/Controls/Catalog.fsi"
            let diagnosticsFsi = read "src/Controls/Diagnostics.fsi"
            let combined = catalogFsi + Environment.NewLine + diagnosticsFsi

            [ "standardSchema"
              "knownControlKinds"
              "requiredAttributes"
              "supportedAttributes"
              "supportedEvents"
              "validateStandardControl"
              "unsupportedStandardAttribute"
              "unsupportedStandardEvent"
              "missingStandardAttribute"
              "customExtension" ]
            |> List.iter (fun required ->
                Expect.stringContains combined required $"schema diagnostics expose {required}")
        }

        // R4 SKIP (Principle V, FR-009): depends on `specs/028-agent-validation-framework/readiness/
        // fsi-session.txt`, an old-repo feature-workflow/readiness artifact deliberately NOT imported.
        // Pending until a current FSI transcript fixture is added. Not weakened, not marked passing.
        ptest "FSI transcript expectations cover typed front doors and custom escape hatch" {
            let transcriptPath = "specs/028-agent-validation-framework/readiness/fsi-session.txt"
            Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, transcriptPath))) "draft FSI transcript exists"

            let transcript = read transcriptPath

            [ "StandardControlKind"
              "StandardEventKind"
              "Button"
              "DataGrid"
              "LineChart"
              "customControl"
              "customAttribute"
              "validateStandardControl" ]
            |> List.iter (fun expected ->
                Expect.stringContains transcript expected $"FSI transcript exercises {expected}")
        }

        test "known standard kinds events and attributes reject misspelled standard names through typed unions" {
            let unionCaseNames (typ: Type) =
                FSharpType.GetUnionCases typ
                |> Array.map _.Name
                |> Set.ofArray

            let knownControls = unionCaseNames typeof<KnownControl>
            let knownEvents = unionCaseNames typeof<KnownEvent>
            let knownAttributes = unionCaseNames typeof<KnownAttribute>

            [ "Button"; "TextBox"; "LineChart"; "DataGrid"; "GraphView" ]
            |> List.iter (fun name -> Expect.isTrue (knownControls.Contains name) $"KnownControl exposes {name}")

            [ "Click"; "Changed"; "Selected"; "FocusChanged"; "SortChanged" ]
            |> List.iter (fun name -> Expect.isTrue (knownEvents.Contains name) $"KnownEvent exposes {name}")

            [ "Text"; "Value"; "Series"; "Columns"; "Rows"; "VisibleRange"; "SelectedRows"; "FocusedCell" ]
            |> List.iter (fun name -> Expect.isTrue (knownAttributes.Contains name) $"KnownAttribute exposes {name}")

            [ "Buton"; "TxtBox"; "LineChar"; "DataGird" ]
            |> List.iter (fun misspelled -> Expect.isFalse (knownControls.Contains misspelled) $"misspelled control {misspelled} is not a known standard kind")

            [ "Clik"; "OnChanged"; "FocusChange" ]
            |> List.iter (fun misspelled -> Expect.isFalse (knownEvents.Contains misspelled) $"misspelled event {misspelled} is not a known standard event")

            [ "Seriez"; "Colums"; "VisibleRanges" ]
            |> List.iter (fun misspelled -> Expect.isFalse (knownAttributes.Contains misspelled) $"misspelled attribute {misspelled} is not a known standard attribute")
        }

        test "chart and DataGrid typed data front doors lower to stable attribute names and typed payloads" {
            let series =
                [ { Name = "sales"
                    Points = [ { X = 0.0; Y = 4.0; Label = Some "Q1" } ] } ]

            let chart = LineChart.create [ LineChart.series series ]
            let chartSeries = attrByName "series" chart

            match chartSeries.Value with
            | UntypedValue(:? (ChartSeries list) as lowered) ->
                Expect.equal lowered series "LineChart.series preserves typed series payload"
            | other -> failtestf "LineChart.series lowered to unexpected payload %A" other

            let columns =
                [ { Key = "name"; Header = "Name"; Width = 120.0; ColumnType = TextColumn } ]

            let rows =
                [ { Key = "row-1"
                    Cells = [ { RowKey = "row-1"; ColumnKey = "name"; Value = "Ada" } ] } ]

            let visibleRange = { FirstIndex = 0; Count = 1; Total = 1 }
            let focusedCell = Some { RowKey = "row-1"; ColumnKey = "name" }

            let grid =
                DataGrid.create columns [
                    DataGrid.rows rows
                    DataGrid.visibleRange visibleRange
                    DataGrid.selectedRows (Set.singleton "row-1")
                    DataGrid.focusedCell focusedCell
                ]

            match (attrByName "columns" grid).Value with
            | UntypedValue(:? (DataGridColumn list) as lowered) -> Expect.equal lowered columns "DataGrid.columns preserves typed columns"
            | other -> failtestf "DataGrid.columns lowered to unexpected payload %A" other

            match (attrByName "rows" grid).Value with
            | UntypedValue(:? (DataGridRow list) as lowered) -> Expect.equal lowered rows "DataGrid.rows preserves typed rows"
            | other -> failtestf "DataGrid.rows lowered to unexpected payload %A" other

            match (attrByName "visibleRange" grid).Value with
            | UntypedValue(:? VisibleRange as lowered) -> Expect.equal lowered visibleRange "DataGrid.visibleRange preserves typed range"
            | other -> failtestf "DataGrid.visibleRange lowered to unexpected payload %A" other

            match (attrByName "selectedRows" grid).Value with
            | UntypedValue(:? Set<string> as lowered) -> Expect.equal lowered (Set.singleton "row-1") "DataGrid.selectedRows preserves typed selection"
            | other -> failtestf "DataGrid.selectedRows lowered to unexpected payload %A" other

            match (attrByName "focusedCell" grid).Value with
            | UntypedValue(:? (DataGridFocusedCell option) as lowered) -> Expect.equal lowered focusedCell "DataGrid.focusedCell preserves typed focus"
            | other -> failtestf "DataGrid.focusedCell lowered to unexpected payload %A" other
        }

        test "schema-backed diagnostics report missing attributes unsupported attributes unsupported events and custom usage" {
            let missing =
                Control.standard StandardControlKind.DataGrid [
                    Attr.standardAttribute StandardAttributeName.Columns (StandardUntyped [ "Name" ])
                ]
                |> Catalog.validateStandardControl

            Expect.exists missing (fun diagnostic -> diagnostic.Code = MissingRequiredAttribute && diagnostic.Message.Contains "Rows") "schema reports missing required rows"

            let unsupportedAttribute =
                Control.standard StandardControlKind.Button [
                    Attr.standardAttribute StandardAttributeName.Text (StandardText "Save")
                    Attr.standardAttribute StandardAttributeName.Rows (StandardUntyped [])
                ]
                |> Catalog.validateStandardControl

            Expect.exists unsupportedAttribute (fun diagnostic -> diagnostic.Message.Contains "Rows" && diagnostic.Message.Contains "Button") "schema reports unsupported standard attribute"

            let unsupportedEvent =
                Control.standard StandardControlKind.TextBlock [
                    Attr.standardAttribute StandardAttributeName.Text (StandardText "Title")
                    Attr.standardEvent StandardEventKind.Click "clicked"
                ]
                |> Catalog.validateStandardControl

            Expect.exists unsupportedEvent (fun diagnostic -> diagnostic.Message.Contains "Click" && diagnostic.Message.Contains "TextBlock") "schema reports unsupported standard event"

            let custom =
                Control.customControl "vendor-widget" [
                    Attr.customAttribute "vendor-mode" ("compact" :> obj)
                    Attr.customEvent "vendor-activated" "Activated"
                ]
                |> Catalog.validateStandardControl

            Expect.exists custom (fun diagnostic -> diagnostic.Severity = Info && diagnostic.Message.Contains "Custom extension") "custom controls are visibly classified"
        }
    ]

[<Tests>]
let typedControlsFrontDoorContractTests =
    testList "Typed controls front door (065) contract" [
        test "Widget seam exists and round-trips a Control" {
            let control = TextBlock.create [ TextBlock.text "hi" ]
            let widget = Widget.ofControl control
            Expect.equal (sprintf "%A" (Widget.toControl widget)) (sprintf "%A" control) "Widget.toControl (ofControl c) = c"
        }

        test "six typed modules under FS.Skia.UI.Controls.Typed lower to Control" {
            // Existence of the typed modules is proven by these calls compiling;
            // each view must lower through the Widget seam to a legacy Control kind.
            let lowered =
                [ Widget.toControl (FS.Skia.UI.Controls.Typed.TextBlock.view FS.Skia.UI.Controls.Typed.TextBlock.defaults)
                  Widget.toControl (FS.Skia.UI.Controls.Typed.Button.view FS.Skia.UI.Controls.Typed.Button.defaults)
                  Widget.toControl (FS.Skia.UI.Controls.Typed.CheckBox.view FS.Skia.UI.Controls.Typed.CheckBox.defaults)
                  Widget.toControl (FS.Skia.UI.Controls.Typed.Stack.view FS.Skia.UI.Controls.Typed.Stack.defaults) ]
                |> List.map (fun control -> control.Kind)

            Expect.equal lowered [ "text-block"; "button"; "check-box"; "stack" ] "primitive typed views lower to legacy kinds"

            let textBoxProps = FS.Skia.UI.Controls.Typed.TextBox.defaults "id"
            let textBoxModel, _ = FS.Skia.UI.Controls.Typed.TextBox.init textBoxProps
            let textBox = Widget.toControl (FS.Skia.UI.Controls.Typed.TextBox.view textBoxProps textBoxModel)
            Expect.equal textBox.Kind "text-box" "typed TextBox lowers to legacy text-box"

            let gridProps = FS.Skia.UI.Controls.Typed.DataGrid.defaults "grid"
            let gridModel, _ = FS.Skia.UI.Controls.Typed.DataGrid.init gridProps
            let grid = Widget.toControl (FS.Skia.UI.Controls.Typed.DataGrid.view gridProps gridModel)
            Expect.equal grid.Kind "data-grid" "typed DataGrid lowers to legacy data-grid"
        }

        test "typed .fsi surface declares no obj field and no string-named event (FR-005)" {
            let signatures =
                [ "src/Controls/Widget.fsi"
                  "src/Controls/Widgets/Primitives.fsi"
                  "src/Controls/Widgets/TextBoxWidget.fsi"
                  "src/Controls/Widgets/DataGridWidget.fsi" ]
                |> List.map (fun path -> path, read path)

            for path, text in signatures do
                Expect.isFalse (text.Contains ": obj") $"{path} has no obj-typed field"
                Expect.isFalse (text.Contains "UntypedValue") $"{path} exposes no untyped payload"

            let primitives = signatures |> List.find (fun (path, _) -> path.EndsWith "Primitives.fsi") |> snd
            Expect.stringContains primitives "OnClick: 'msg option" "Button event is a typed message option"
            Expect.stringContains primitives "OnChanged: (bool -> 'msg) option" "CheckBox event is a typed function option"
        }

        test "Widget and typed signature files exist on disk" {
            [ "src/Controls/Widget.fsi"
              "src/Controls/Widgets/Primitives.fsi"
              "src/Controls/Widgets/TextBoxWidget.fsi"
              "src/Controls/Widgets/DataGridWidget.fsi" ]
            |> List.iter (fun path ->
                Expect.isTrue (File.Exists(Path.Combine(repositoryRoot, path.Replace("/", string Path.DirectorySeparatorChar)))) $"{path} exists")
        }
    ]
