module SceneCapability.Feature165VisualInspectionModelTests

open Expecto
open FS.GG.UI.Scene

let private size: Size = { Width = 320; Height = 200 }

let private scope: VisualInspectionScope =
    { ScopeId = "sample"
      Title = "Sample"
      Required = true }

let private rect: Rect =
    { X = 0.0
      Y = 0.0
      Width = 100.0
      Height = 40.0 }

let private node nodeId parent zOrder : VisualInspectionNode =
    { NodeId = nodeId
      ParentId = parent
      Kind = VisualInspectionNodeKind.Container
      OwnerId = Some nodeId
      Bounds = Some rect
      Clip = VisualInspectionClipStatus.None
      ZOrder = zOrder
      PaintRole = VisualInspectionPaintRole.Content
      SurfaceRole = VisualInspectionSurfaceRole.Content
      TextRunIds = []
      Children = []
      Dynamic = false
      UnsupportedFacts = [] }

let private artifact nodes facts : VisualInspectionArtifact =
    { ArtifactId = "artifact"
      Scope = scope
      OutputSize = size
      Presentation = "light"
      ReadinessStatus = VisualInspectionStatus.Accepted
      Nodes = nodes
      Regions = []
      TextRuns = []
      PaintCoverage = []
      ClipFacts = []
      Findings = []
      UnsupportedFacts = facts
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

[<Tests>]
let tests =
    testList
        "Feature165 Scene visual inspection model"
        [ test "status severity and fact tokens are stable lowercase contract strings" {
              Expect.equal (VisualInspection.statusText VisualInspectionStatus.EnvironmentLimited) "environment-limited" "status token"
              Expect.equal (VisualInspection.severityText VisualInspectionSeverity.Blocking) "blocking" "severity token"
              Expect.equal (VisualInspection.measurementModeText VisualInspectionMeasurementMode.Approximate) "approximate" "measurement token"
              Expect.equal (VisualInspection.fitStatusText VisualInspectionFitStatus.Overflow) "overflow" "fit token"
              Expect.equal (VisualInspection.nodeKindText (VisualInspectionNodeKind.Custom "Fancy Panel")) "fancy-panel" "custom kind token"
              Expect.equal (VisualInspection.paintRoleText VisualInspectionPaintRole.Background) "background" "paint token"
              Expect.equal (VisualInspection.surfaceRoleText VisualInspectionSurfaceRole.Popup) "popup" "surface token"
              Expect.equal (VisualInspection.clipStatusText VisualInspectionClipStatus.Intentional) "intentional" "clip token"
              Expect.equal (VisualInspection.coverageStatusText VisualInspectionCoverageStatus.Complete) "complete" "coverage token"
          }

          test "stable finding ids are deterministic across affected id order" {
              let first = VisualInspection.stableFindingId "text-contained-in-owner" [ "body"; "title" ]
              let second = VisualInspection.stableFindingId "text-contained-in-owner" [ "title"; "body" ]
              Expect.equal first second "affected ids are sorted before id generation"
              Expect.equal first "text-contained-in-owner:body+title" "finding id includes rule and ids"
          }

          test "artifact diagnostics report duplicate ids invalid parents and unsupported facts" {
              let unsupported =
                  VisualInspection.unsupportedFact "transform-bounds" (Some "panel") true "" "" false

              let diagnostics =
                  artifact [ node "root" None 0; node "root" (Some "missing") 1 ] [ unsupported ]
                  |> VisualInspection.artifactDiagnostics

              Expect.exists diagnostics (fun d -> d.Contains("duplicate visual inspection node id")) "duplicate node"
              Expect.exists diagnostics (fun d -> d.Contains("references missing parent")) "missing parent"
              Expect.exists diagnostics (fun d -> d.Contains("unsupported visual inspection fact")) "unsupported fact disclosure"
          }

          test "normalizeArtifact orders nodes regions findings and unsupported facts deterministically" {
              let findingA = VisualInspection.finding "rule-b" VisualInspectionSeverity.Blocking [ "b" ] [] "b" "expected" "actual"
              let findingB = VisualInspection.finding "rule-a" VisualInspectionSeverity.Blocking [ "a" ] [] "a" "expected" "actual"
              let factA = VisualInspection.unsupportedFact "zeta" None true "reason" "diagnostic" false
              let factB = VisualInspection.unsupportedFact "alpha" None true "reason" "diagnostic" false
              let input =
                  { artifact [ node "second" None 2; node "first" None 1 ] [ factA; factB ] with
                      Findings = [ findingA; findingB ] }

              let normalized = VisualInspection.normalizeArtifact input
              Expect.equal (normalized.Nodes |> List.map _.NodeId) [ "first"; "second" ] "nodes sorted by order and id"
              Expect.equal (normalized.Findings |> List.map _.RuleId) [ "rule-a"; "rule-b" ] "findings sorted"
              Expect.equal (normalized.UnsupportedFacts |> List.map _.Fact) [ "alpha"; "zeta" ] "facts sorted"
          } ]
