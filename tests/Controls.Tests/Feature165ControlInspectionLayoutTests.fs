module Feature165ControlInspectionLayoutTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 360; Height = 220 }
let private scope: VisualInspectionScope = { ScopeId = "controls"; Title = "Controls"; Required = true }

let private inspect control =
    ControlInspection.inspect
        { Scope = scope
          Theme = theme
          OutputSize = size
          Control = control
          Presentation = "light"
          RunId = Some "run"
          RelatedVisualEvidence = [] }

[<Tests>]
let tests =
    testList
        "Feature165 Controls inspection layout"
        [ test "inspect emits stable ids final bounds ownership ordering text and clip facts from renderTree" {
              let tree: Control<Msg> =
                  Stack.create
                      [ Stack.children
                            [ TextBlock.create [ TextBlock.text "Inspection title" ] |> Control.withKey "title"
                              Button.create [ Button.text "Save" ] |> Control.withKey "save" ] ]

              let artifact = inspect tree

              Expect.equal artifact.Scope.ScopeId "controls" "scope is preserved"
              Expect.exists artifact.Nodes (fun n -> n.NodeId = "0" && n.Kind = VisualInspectionNodeKind.Root) "root node id"
              Expect.exists artifact.Nodes (fun n -> n.NodeId = "title" && n.OwnerId = Some "title") "authored key is owner"
              Expect.exists artifact.Nodes (fun n -> n.NodeId = "save" && n.Bounds.IsSome) "final bounds are present"
              Expect.equal (artifact.Nodes |> List.map _.ZOrder) (artifact.Nodes |> List.map _.ZOrder |> List.sort) "visual order is deterministic"
              Expect.exists artifact.TextRuns (fun t -> t.OwnerNodeId = "title" && t.Text = "Inspection title") "text facts are emitted"
              Expect.exists artifact.ClipFacts (fun c -> c.NodeId = "0" && c.ClipStatus = VisualInspectionClipStatus.Intentional) "container clip fact"
              Expect.isEmpty artifact.Findings "Controls extraction does not validate findings itself"
          }

          test "long text gets a deterministic fit classification against owner bounds" {
              let tree: Control<Msg> =
                  TextBlock.create [ Attr.width 80.0; TextBlock.text "A very very very long deterministic inspection label" ]
                  |> Control.withKey "title"

              let artifact = inspect tree
              let text = artifact.TextRuns |> List.find (fun run -> run.OwnerNodeId = "title")
              Expect.equal text.MeasurementMode VisualInspectionMeasurementMode.Approximate "adapter discloses approximate text measurement"
              Expect.equal text.FitStatus VisualInspectionFitStatus.Overflow "overflow is classified"
              Expect.exists text.Diagnostics (fun d -> d.Contains("exceed")) "diagnostic explains overflow"
          } ]
