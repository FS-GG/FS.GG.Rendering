module Feature165ControlInspectionPaintTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene
open FS.GG.UI.Themes.Default

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 360; Height = 220 }
let private scope: VisualInspectionScope = { ScopeId = "paint"; Title = "Paint"; Required = true }

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
        "Feature165 Controls inspection paint"
        [ test "root surface coverage overlay roles popup roles and scroll clipping are emitted" {
              let overlay =
                  Control.create "overlay" [ Attr.children [ TextBlock.create [ TextBlock.text "Overlay" ] ] ]
                  |> Control.withKey "overlay"

              let popup =
                  Control.create "popup" [ Attr.children [ TextBlock.create [ TextBlock.text "Popup" ] ] ]
                  |> Control.withKey "popup"

              let scroll =
                  Control.create "scroll-viewer" [ Attr.children [ Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "row" ] ] ] ] ]
                  |> Control.withKey "scroll"

              let artifact = inspect (Stack.create [ Stack.children [ overlay; popup; scroll ] ])

              Expect.exists artifact.PaintCoverage (fun p -> p.TargetId = "0" && p.CoverageStatus = VisualInspectionCoverageStatus.Complete) "root paint coverage"
              Expect.exists artifact.Regions (fun r -> r.RegionId = "overlay" && r.Role = VisualInspectionSurfaceRole.Overlay) "overlay role"
              Expect.exists artifact.Regions (fun r -> r.RegionId = "popup" && r.Role = VisualInspectionSurfaceRole.Popup) "popup role"
              Expect.exists artifact.ClipFacts (fun c -> c.NodeId = "scroll" && c.ClipStatus = VisualInspectionClipStatus.Intentional) "scroll clipping classified"
          }

          test "unsupported transformed bounds are explicit facts instead of inferred passing facts" {
              let transformed =
                  Control.customControl "transform-card" [ Attr.children [ TextBlock.create [ TextBlock.text "Unsupported transform" ] ] ]
                  |> Control.withKey "transform"

              let artifact = inspect transformed
              Expect.exists artifact.UnsupportedFacts (fun f -> f.Fact = "transform-bounds" && f.OwnerId = Some "transform") "unsupported transform fact"
              Expect.exists artifact.Nodes (fun n -> n.NodeId = "transform" && not n.UnsupportedFacts.IsEmpty) "node carries unsupported fact"
          } ]
