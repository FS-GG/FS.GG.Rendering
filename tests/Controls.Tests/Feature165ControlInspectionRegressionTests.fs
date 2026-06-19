module Feature165ControlInspectionRegressionTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default

type private Msg = Clicked

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 180 }
let private scope: VisualInspectionScope = { ScopeId = "regression"; Title = "Regression"; Required = true }

[<Tests>]
let tests =
    testList
        "Feature165 Controls inspection regression"
        [ test "inspection does not change renderTree scene bounds diagnostics event bindings bound ids or node count" {
              let tree: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ Button.create [ Button.text "Save"; Button.onClick Clicked ] |> Control.withKey "save"
                              TextBlock.create [ TextBlock.text "Status" ] |> Control.withKey "status" ] ]

              let before = Control.renderTree theme size tree

              let _ =
                  ControlInspection.inspect
                      { Scope = scope
                        Theme = theme
                        OutputSize = size
                        Control = tree
                        Presentation = "light"
                        RunId = Some "run"
                        RelatedVisualEvidence = [] }

              let after = Control.renderTree theme size tree

              Expect.equal after.Scene before.Scene "scene output unchanged"
              Expect.equal after.Bounds before.Bounds "bounds unchanged"
              Expect.equal after.Diagnostics before.Diagnostics "diagnostics unchanged"
              Expect.equal (after.EventBindings |> List.map (fun b -> b.ControlId, b.EventKind)) (before.EventBindings |> List.map (fun b -> b.ControlId, b.EventKind)) "event binding keys unchanged"
              Expect.equal after.BoundIds before.BoundIds "bound ids unchanged"
              Expect.equal after.NodeCount before.NodeCount "node count unchanged"
          } ]
