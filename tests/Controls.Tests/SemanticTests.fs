module ControlsSemanticTests

open Expecto
open System.Reflection
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

type FormModel =
    { Count: int
      Name: string
      CanSave: bool
      Validation: ValidationState }

type FormMsg =
    | Increment
    | NameChanged of string
    | SaveRequested

let view model =
    Stack.create [
        Stack.children [
            TextBlock.create [ TextBlock.text $"Count: {model.Count}" ]
            TextBox.create [
                TextBox.value model.Name
                TextBox.validation model.Validation
                TextBox.onChanged NameChanged
            ]
            Button.create [
                Button.text "Save"
                Button.enabled model.CanSave
                Button.onClick SaveRequested
            ]
            LineChart.create [
                LineChart.series [
                    { Name = "trend"
                      Points = [ { X = 0.0; Y = float model.Count; Label = Some "count" } ] }
                ]
            ]
        ]
    ]

[<Tests>]
let semanticTests =
    testList "Controls semantic behavior" [
        test "representative view function renders through public Control surface" {
            let model = { Count = 2; Name = "Ada"; CanSave = true; Validation = Valid }
            let control = view model
            let rendered = Control.render Theme.light control

            Expect.equal rendered.NodeCount 5 "root and children are counted"
            Expect.isEmpty rendered.Diagnostics "representative screen has no diagnostics"
            Expect.contains (Scene.describe rendered.Scene) TextRunElement "render produces fitted text scene output"
            Expect.equal rendered.Layout.Id "stack" "layout node is produced at the public boundary"
        }

        test "model-owned state changes are reflected by re-evaluating the view" {
            let first = view { Count = 1; Name = "Ada"; CanSave = true; Validation = Valid }
            let second = view { Count = 3; Name = "Grace"; CanSave = false; Validation = Invalid "required" }

            Expect.notEqual first.Children[0].Content second.Children[0].Content "control values are model-owned descriptions"
            Expect.isTrue (second.Children[1].Content = Some "Grace") "text box value reflects the current model"
            Expect.isTrue (second.Children[2].Attributes |> List.exists (fun attr -> match attr.Name, attr.Value with "enabled", BoolValue false -> true | _ -> false)) "enabled state reflects current model"
        }

        test "custom controls expose explicit Skia measurement drawing clipping and effect hooks" {
            let properties =
                typeof<CustomControlDefinition<FormMsg>>
                    .GetProperties(BindingFlags.Public ||| BindingFlags.Instance)
                |> Array.map _.Name
                |> Set.ofArray

            [ "Measure"; "Draw"; "Clip"; "Effects"; "Diagnostics" ]
            |> List.iter (fun property ->
                Expect.isTrue (Set.contains property properties) $"CustomControlDefinition exposes {property}")
        }

        test "chart and graph controls render as Controls-owned scene elements" {
            let chart =
                LineChart.create [
                    LineChart.series [
                        { Name = "sales"
                          Points = [ { X = 0.0; Y = 4.0; Label = Some "Q1" }; { X = 1.0; Y = 8.0; Label = Some "Q2" } ] }
                    ]
                ]

            let graph =
                GraphView.create [
                    GraphView.nodes [ "form"; "chart"; "grid" ]
                ]

            let root = Stack.create [ Stack.children [ chart; graph ] ]
            let rendered = Control.render Theme.light root
            let kinds = Scene.describe rendered.Scene

            Expect.equal chart.Accessibility.Value.Role Chart "line chart defaults to chart accessibility role"
            Expect.equal graph.Accessibility.Value.Role Graph "graph view defaults to graph accessibility role"
            // Feature 080 (T012): charts/graphs lower to faithful geometry (Path/Circle/Line),
            // not the opaque off-canvas `Chart` node — so `Scene.describe` no longer reports
            // `ChartElement`; the line chart contributes a polyline `PathElement` and the graph
            // contributes node `CircleElement`s.
            Expect.isFalse (kinds |> List.contains ChartElement) "charts no longer emit the opaque Chart node (feature 080)"
            Expect.isTrue (kinds |> List.contains PathElement) "line chart renders a polyline path within bounds"
            Expect.isTrue (kinds |> List.contains CircleElement) "graph view renders node circles within bounds"
            Expect.isEmpty rendered.Diagnostics "valid chart and graph controls render without diagnostics"
        }
    ]
