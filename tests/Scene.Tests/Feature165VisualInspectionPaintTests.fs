module SceneCapability.Feature165VisualInspectionPaintTests

open Expecto
open FS.GG.UI.Scene

[<Tests>]
let tests =
    testList
        "Feature165 Scene paint and clip facts"
        [ test "paint coverage and clip facts carry stable required inspection vocabulary" {
              let bounds: Rect = { X = 0.0; Y = 0.0; Width = 200.0; Height = 100.0 }

              let paint: VisualPaintCoverage =
                  { CoverageId = "root:paint"
                    TargetId = "root"
                    PaintRole = VisualInspectionPaintRole.Background
                    CoverageBounds = Some bounds
                    CoverageStatus = VisualInspectionCoverageStatus.Complete
                    Reason = None }

              let clip: VisualClipFact =
                  { ClipId = "scroll:clip"
                    NodeId = "scroll"
                    ClipBounds = Some bounds
                    ClipStatus = VisualInspectionClipStatus.Intentional
                    Reason = Some "scroll viewport"
                    AffectedTextRunIds = [ "scroll:text" ] }

              Expect.equal (VisualInspection.paintRoleText paint.PaintRole) "background" "paint role is stable"
              Expect.equal (VisualInspection.coverageStatusText paint.CoverageStatus) "complete" "coverage status is stable"
              Expect.equal (VisualInspection.clipStatusText clip.ClipStatus) "intentional" "clip status is stable"
              Expect.equal clip.AffectedTextRunIds [ "scroll:text" ] "clip carries affected text ids"
          }

          test "unsupported paint facts are explicit and can be marked environment-limited" {
              let fact =
                  VisualInspection.unsupportedFact
                      "paint-coverage"
                      (Some "root")
                      true
                      "headless environment did not expose paint readback"
                      "required root coverage is environment-limited"
                      true

              Expect.equal fact.Fact "paint-coverage" "fact name"
              Expect.equal fact.OwnerId (Some "root") "owner"
              Expect.isTrue fact.Required "required fact"
              Expect.isTrue fact.EnvironmentLimited "environment limitation is explicit"
          } ]
