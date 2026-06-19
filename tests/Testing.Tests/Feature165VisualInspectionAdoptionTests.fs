module TestingCapability.Feature165VisualInspectionAdoptionTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let private size: Size = { Width = 320; Height = 200 }
let private rect: Rect = { X = 0.0; Y = 0.0; Width = 100.0; Height = 40.0 }

let private artifact id status unsupportedFacts : VisualInspectionArtifact =
    { ArtifactId = id
      Scope = { ScopeId = id; Title = id; Required = true }
      OutputSize = size
      Presentation = "light"
      ReadinessStatus = status
      Nodes = []
      Regions = []
      TextRuns = []
      PaintCoverage = []
      ClipFacts = []
      Findings = []
      UnsupportedFacts = unsupportedFacts
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

let private result id status : VisualInspectionValidationResult =
    { ArtifactId = id
      ReadinessStatus = status
      Findings = []
      AppliedExceptions = []
      InvalidExceptions = []
      UnusedExceptions = []
      Diagnostics = [] }

[<Tests>]
let tests =
    testList
        "Feature165 visual inspection adoption"
        [ test "partial adoption distinguishes inspected not-inspected not-run unsupported and environment-limited scopes" {
              let unsupported = VisualInspection.unsupportedFact "text-fit" (Some "title") true "font unavailable" "unsupported fact" false
              let env = VisualInspection.unsupportedFact "paint-readback" (Some "root") true "headless host" "environment limitation" true

              let summary =
                  VisualInspectionReadiness.aggregate
                      "run"
                      [ artifact "accepted" VisualInspectionStatus.Accepted []
                        artifact "future" VisualInspectionStatus.NotInspected []
                        artifact "not-run" VisualInspectionStatus.NotRun []
                        artifact "unsupported" VisualInspectionStatus.Accepted [ unsupported ]
                        artifact "env" VisualInspectionStatus.Accepted [ env ] ]
                      [ result "accepted" VisualInspectionStatus.Accepted
                        result "unsupported" VisualInspectionStatus.Unsupported
                        result "env" VisualInspectionStatus.EnvironmentLimited ]
                      []
                      []

              Expect.contains summary.InspectedScopes "accepted" "inspected scope"
              Expect.contains summary.NotInspectedScopes "future" "not-inspected scope"
              Expect.contains summary.NotRunScopes "not-run" "not-run scope"
              Expect.contains summary.StatusCounts ("unsupported", 1) "unsupported counted"
              Expect.contains summary.StatusCounts ("environment-limited", 1) "environment limited counted"
              Expect.equal summary.UnsupportedFacts.Length 2 "unsupported facts remain visible"
          }

          test "legacy GeneratedLayoutValidation remains accepted for readable layout evidence" {
              let report: LayoutEvidenceReport =
                  { Scene = Scene.empty
                    OutputSize = size
                    ProofLevel = ReadableLayout
                    HudRegion = Some { Name = "hud"; Bounds = rect }
                    GameplayRegion = Some { Name = "gameplay"; Bounds = { rect with X = 120.0; Width = 80.0 } }
                    TextBounds = [ { Name = "score"; Text = "Score"; Bounds = rect; MeasurementMode = ExactTextBounds } ]
                    GameplayBounds = [ { Name = "gameplay"; Bounds = { rect with X = 120.0; Width = 80.0 } } ]
                    OverlapStatus = NoLayoutOverlap
                    MeasurementMode = ExactTextBounds
                    UnsupportedReasons = []
                    Diagnostics = []
                    RenderEvidence = None }

              let validation = GeneratedLayoutValidation.validate { Report = report; RequireReadableLayout = true }
              Expect.isTrue validation.Accepted "legacy layout evidence still validates"
              Expect.isNone validation.FailureClass "no legacy failure class"
          } ]
