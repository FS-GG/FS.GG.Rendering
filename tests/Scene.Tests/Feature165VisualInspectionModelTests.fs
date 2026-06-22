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

let private retainedArtifact findings : RetainedInspectionArtifact =
    { ArtifactId = "artifact"
      RunId = "run"
      Scope = scope
      OutputSize = size
      Presentation = "light"
      Transition = None
      FinalVisualArtifact = None
      RetainedNodes = []
      Damage = None
      Findings = findings
      UnsupportedFacts = []
      RelatedVisualEvidence = []
      ReadinessStatus = RetainedInspectionStatus.Accepted
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
          }

          test "normalizeArtifact collapses duplicate visual finding ids keeping first occurrence (FR-006)" {
              // Two findings share rule + affected ids => identical FindingId => duplicates; a third is unique.
              let dupFirst = VisualInspection.finding "overlap" VisualInspectionSeverity.Blocking [ "a" ] [] "first" "expected" "actual"
              let dupSecond = VisualInspection.finding "overlap" VisualInspectionSeverity.Warning [ "a" ] [] "second" "expected" "actual"
              let unique = VisualInspection.finding "contrast" VisualInspectionSeverity.Info [ "b" ] [] "u" "expected" "actual"
              Expect.equal dupFirst.FindingId dupSecond.FindingId "precondition: the duplicates share a FindingId"
              let input = { artifact [] [] with Findings = [ dupFirst; dupSecond; unique ] }
              let normalized = VisualInspection.normalizeArtifact input
              Expect.equal (normalized.Findings |> List.map _.FindingId) [ unique.FindingId; dupFirst.FindingId ] "one per FindingId, sorted, unique preserved"
              Expect.equal (normalized.Findings |> List.map _.Message) [ "u"; "first" ] "first occurrence of the duplicate is kept"
          }

          test "retained normalizeArtifact collapses duplicate finding ids uniformly (FR-006/SC-003)" {
              // Same collapse rule on the retained path (its identity scope keeps transitionId).
              let dupFirst = RetainedInspection.finding "damage-localized" VisualInspectionSeverity.Blocking "t1" [ "a" ] [] "first" "expected" "actual"
              let dupSecond = RetainedInspection.finding "damage-localized" VisualInspectionSeverity.Warning "t1" [ "a" ] [] "second" "expected" "actual"
              let unique = RetainedInspection.finding "damage-broad" VisualInspectionSeverity.Info "t1" [ "b" ] [] "u" "expected" "actual"
              Expect.equal dupFirst.FindingId dupSecond.FindingId "precondition: the duplicates share a FindingId"
              let normalized = RetainedInspection.normalizeArtifact (retainedArtifact [ dupFirst; dupSecond; unique ])
              Expect.equal (normalized.Findings |> List.length) 2 "collapsed to one per FindingId"
              Expect.equal (normalized.Findings |> List.map _.Message) [ "u"; "first" ] "first occurrence kept, unique preserved"
          } ]
