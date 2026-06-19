module TestingCapability.Feature165VisualInspectionValidationTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let private size: Size = { Width = 320; Height = 200 }
let private scope: VisualInspectionScope = { ScopeId = "validation"; Title = "Validation"; Required = true }

let private rect x y w h : Rect = { X = x; Y = y; Width = w; Height = h }

let private node id z bounds : VisualInspectionNode =
    { NodeId = id
      ParentId = None
      Kind = VisualInspectionNodeKind.Container
      OwnerId = Some id
      Bounds = Some bounds
      Clip = VisualInspectionClipStatus.None
      ZOrder = z
      PaintRole = VisualInspectionPaintRole.Content
      SurfaceRole = VisualInspectionSurfaceRole.Content
      TextRunIds = []
      Children = []
      Dynamic = false
      UnsupportedFacts = [] }

let private region id bounds required : VisualRegionBoundary =
    { RegionId = id
      Name = id
      Role = VisualInspectionSurfaceRole.Content
      Bounds = Some bounds
      Required = required
      OwnerNodeIds = [ id ]
      AllowedOverlapRoles = [] }

let private paint id : VisualPaintCoverage =
    { CoverageId = id + ":paint"
      TargetId = id
      PaintRole = VisualInspectionPaintRole.Background
      CoverageBounds = Some(rect 0.0 0.0 100.0 100.0)
      CoverageStatus = VisualInspectionCoverageStatus.Complete
      Reason = None }

let private artifact regions textRuns clips nodes : VisualInspectionArtifact =
    { ArtifactId = "artifact"
      Scope = scope
      OutputSize = size
      Presentation = "light"
      ReadinessStatus = VisualInspectionStatus.Accepted
      Nodes = nodes
      Regions = regions
      TextRuns = textRuns
      PaintCoverage = regions |> List.map (fun r -> paint r.RegionId)
      ClipFacts = clips
      Findings = []
      UnsupportedFacts = []
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

let private validate ruleIds artifact =
    VisualInspectionValidation.validate artifact (ruleIds |> List.map VisualInspectionValidation.rule) []

[<Tests>]
let tests =
    testList
        "Feature165 visual inspection validation"
        [ test "required-region-present reports missing required regions" {
              let art = artifact [ region "root" (rect 0.0 0.0 100.0 100.0) true ] [] [] [ node "root" 0 (rect 0.0 0.0 100.0 100.0) ]
              let result =
                  VisualInspectionValidation.validateCheck
                      { Artifact = art
                        Rules = [ VisualInspectionValidation.rule "required-region-present" ]
                        Exceptions = []
                        RequiredRegionIds = [ "root"; "missing" ]
                        PreviousArtifact = None
                        EnvironmentLimitations = [] }

              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "missing required region blocks readiness"
              Expect.exists result.Findings (fun f -> f.RuleId = "required-region-present" && f.AffectedRegionIds = [ "missing" ]) "missing region finding"
          }

          test "ordinary-regions-disjoint reports unclassified sibling overlap" {
              let art =
                  artifact
                      [ region "left" (rect 0.0 0.0 100.0 100.0) false
                        region "right" (rect 50.0 50.0 100.0 100.0) false ]
                      []
                      []
                      [ node "left" 0 (rect 0.0 0.0 100.0 100.0); node "right" 1 (rect 50.0 50.0 100.0 100.0) ]

              let result = validate [ "ordinary-regions-disjoint" ] art
              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "overlap blocks"
              Expect.exists result.Findings (fun f -> f.RuleId = "ordinary-regions-disjoint") "overlap finding"
          }

          test "text-contained-in-owner reports overflow and unsupported required facts" {
              let text: VisualTextInspection =
                  { TextId = "title:text"
                    OwnerNodeId = "title"
                    Text = "overflow"
                    TextBounds = Some(rect 0.0 0.0 220.0 20.0)
                    OwnerBounds = Some(rect 0.0 0.0 100.0 20.0)
                    Baseline = Some 14.0
                    MeasurementMode = VisualInspectionMeasurementMode.Approximate
                    FitStatus = VisualInspectionFitStatus.Overflow
                    Required = true
                    Diagnostics = [] }

              let art = artifact [ region "root" (rect 0.0 0.0 320.0 200.0) true ] [ text ] [] [ node "title" 0 (rect 0.0 0.0 100.0 20.0) ]
              let result = validate [ "text-contained-in-owner" ] art
              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "overflow blocks"
              Expect.exists result.Findings (fun f -> f.RuleId = "text-contained-in-owner" && f.AffectedNodeIds = [ "title" ]) "text finding"
          }

          test "clip-intent-classified reports accidental clipping" {
              let clip: VisualClipFact =
                  { ClipId = "panel:clip"
                    NodeId = "panel"
                    ClipBounds = Some(rect 0.0 0.0 100.0 20.0)
                    ClipStatus = VisualInspectionClipStatus.Accidental
                    Reason = None
                    AffectedTextRunIds = [] }

              let art = artifact [ region "root" (rect 0.0 0.0 320.0 200.0) true ] [] [ clip ] [ node "panel" 0 (rect 0.0 0.0 100.0 20.0) ]
              let result = validate [ "clip-intent-classified" ] art
              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "accidental clip blocks"
              Expect.exists result.Findings (fun f -> f.RuleId = "clip-intent-classified") "clip finding"
          }

          test "identity-stable and visual-order-stable compare repeated artifacts" {
              let previous =
                  artifact [ region "root" (rect 0.0 0.0 320.0 200.0) true ] [] [] [ node "root" 0 (rect 0.0 0.0 10.0 10.0); node "old" 1 (rect 0.0 0.0 10.0 10.0) ]

              let current =
                  artifact [ region "root" (rect 0.0 0.0 320.0 200.0) true ] [] [] [ node "new" 0 (rect 0.0 0.0 10.0 10.0); node "root" 1 (rect 0.0 0.0 10.0 10.0) ]

              let result =
                  VisualInspectionValidation.validateCheck
                      { Artifact = current
                        Rules = [ VisualInspectionValidation.rule "identity-stable"; VisualInspectionValidation.rule "visual-order-stable" ]
                        Exceptions = []
                        RequiredRegionIds = []
                        PreviousArtifact = Some previous
                        EnvironmentLimitations = [] }

              Expect.exists result.Findings (fun f -> f.RuleId = "identity-stable") "identity finding"
              Expect.exists result.Findings (fun f -> f.RuleId = "visual-order-stable") "order finding"
          } ]
