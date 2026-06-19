module TestingCapability.Feature165VisualInspectionExceptionTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let private size: Size = { Width = 320; Height = 200 }
let private scope: VisualInspectionScope = { ScopeId = "exceptions"; Title = "Exceptions"; Required = true }
let private rect x y w h : Rect = { X = x; Y = y; Width = w; Height = h }

let private region id role bounds required : VisualRegionBoundary =
    { RegionId = id
      Name = id
      Role = role
      Bounds = Some bounds
      Required = required
      OwnerNodeIds = [ id ]
      AllowedOverlapRoles = [ VisualInspectionSurfaceRole.Overlay; VisualInspectionSurfaceRole.Popup ] }

let private node id : VisualInspectionNode =
    { NodeId = id
      ParentId = None
      Kind = VisualInspectionNodeKind.Container
      OwnerId = Some id
      Bounds = Some(rect 0.0 0.0 100.0 100.0)
      Clip = VisualInspectionClipStatus.None
      ZOrder = 0
      PaintRole = VisualInspectionPaintRole.Content
      SurfaceRole = VisualInspectionSurfaceRole.Content
      TextRunIds = []
      Children = []
      Dynamic = false
      UnsupportedFacts = [] }

let private artifact regions paint clips : VisualInspectionArtifact =
    { ArtifactId = "artifact"
      Scope = scope
      OutputSize = size
      Presentation = "light"
      ReadinessStatus = VisualInspectionStatus.Accepted
      Nodes = regions |> List.map (fun r -> node r.RegionId)
      Regions = regions
      TextRuns = []
      PaintCoverage = paint
      ClipFacts = clips
      Findings = []
      UnsupportedFacts = []
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

let private ex id rule affected reason : VisualInspectionException =
    { ExceptionId = id
      RuleId = rule
      OwnerId = "owner"
      AffectedIds = affected
      Reason = reason
      ExpiresWith = None }

[<Tests>]
let tests =
    testList
        "Feature165 visual inspection exceptions"
        [ test "required-region-painted blocks missing root coverage" {
              let art = artifact [ region "root" VisualInspectionSurfaceRole.Root (rect 0.0 0.0 320.0 200.0) true ] [] []
              let result = VisualInspectionValidation.validate art [ VisualInspectionValidation.rule "required-region-painted" ] []
              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "missing paint coverage blocks"
              Expect.exists result.Findings (fun f -> f.RuleId = "required-region-painted") "paint finding"
          }

          test "overlay-overlap-classified is accepted only with a valid matching exception" {
              let art =
                  artifact
                      [ region "content" VisualInspectionSurfaceRole.Content (rect 0.0 0.0 200.0 200.0) false
                        region "overlay" VisualInspectionSurfaceRole.Overlay (rect 40.0 40.0 120.0 120.0) false ]
                      []
                      []

              let blocked = VisualInspectionValidation.validate art [ VisualInspectionValidation.rule "overlay-overlap-classified" ] []
              Expect.equal blocked.ReadinessStatus VisualInspectionStatus.Blocked "unclassified overlay overlap blocks"

              let accepted =
                  VisualInspectionValidation.validate
                      art
                      [ VisualInspectionValidation.rule "overlay-overlap-classified" ]
                      [ ex "overlay-reviewed" "overlay-overlap-classified" [ "content"; "overlay" ] "intentional popover" ]

              Expect.equal accepted.ReadinessStatus VisualInspectionStatus.Accepted "valid exception accepts the overlap finding"
              Expect.equal accepted.AppliedExceptions [ "overlay-reviewed" ] "exception applied"
          }

          test "invalid and unused exceptions are visible diagnostics" {
              let art = artifact [ region "root" VisualInspectionSurfaceRole.Root (rect 0.0 0.0 320.0 200.0) true ] [] []
              let invalid: VisualInspectionException =
                  { ExceptionId = ""
                    RuleId = "overlay-overlap-classified"
                    OwnerId = ""
                    AffectedIds = []
                    Reason = ""
                    ExpiresWith = None }

              let result =
                  VisualInspectionValidation.validate
                      art
                      [ VisualInspectionValidation.rule "overlay-overlap-classified" ]
                      [ invalid; ex "unused" "overlay-overlap-classified" [ "a"; "b" ] "stale allowance" ]

              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "invalid exception blocks accepted readiness"
              Expect.contains result.InvalidExceptions "" "invalid exception reported"
              Expect.contains result.UnusedExceptions "unused" "unused exception reported"
          }

          test "accidental clipping remains blocking without a matching exception" {
              let clip: VisualClipFact =
                  { ClipId = "content:clip"
                    NodeId = "content"
                    ClipBounds = Some(rect 0.0 0.0 100.0 20.0)
                    ClipStatus = VisualInspectionClipStatus.Accidental
                    Reason = None
                    AffectedTextRunIds = [] }

              let art = artifact [ region "content" VisualInspectionSurfaceRole.Content (rect 0.0 0.0 100.0 20.0) false ] [] [ clip ]
              let result = VisualInspectionValidation.validate art [ VisualInspectionValidation.rule "clip-intent-classified" ] []
              Expect.equal result.ReadinessStatus VisualInspectionStatus.Blocked "accidental clipping blocks"
          } ]
