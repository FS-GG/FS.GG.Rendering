module Feature170CompatibilityTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

[<Tests>]
let tests =
    testList
        "Feature170 compatibility"
        [ test "VisualInspectionArtifact shape remains usable without retained fields" {
              let artifact: VisualInspectionArtifact =
                  { ArtifactId = "visual"
                    Scope = { ScopeId = "visual"; Title = "Visual"; Required = true }
                    OutputSize = { Width = 100; Height = 100 }
                    Presentation = "light"
                    ReadinessStatus = VisualInspectionStatus.Accepted
                    Nodes = []
                    Regions = []
                    TextRuns = []
                    PaintCoverage = []
                    ClipFacts = []
                    Findings = []
                    UnsupportedFacts = []
                    Diagnostics = []
                    GeneratedAtUtc = "2026-06-19T00:00:00Z" }

              let result = VisualInspectionValidation.validate artifact VisualInspectionValidation.defaultRules []
              Expect.equal result.ArtifactId "visual" "existing artifact validates through existing API"
          }

          test "CompositorDamageReadiness remains source-compatible beside retained inspection" {
              let check: CompositorDamageReadinessCheck =
                  { Feature = "compat"
                    RequiredScenarioIds = [ "localized" ]
                    Scenarios =
                      [ { ScenarioId = "localized"
                          Status = CompositorDamageAccepted
                          AcceptedAttemptCount = 3
                          ArtifactPaths = [ "readiness/localized.json" ]
                          FallbackReason = None } ]
                    AcceptedAttemptCount = 3
                    UnsupportedHostStatus = CompositorDamageEnvironmentLimited
                    AcceptedPartialRedrawArtifacts = 0
                    CompatibilityAccepted = true
                    PackageAccepted = true
                    RegressionAccepted = true
                    PerformanceClaim = "performance-not-accepted"
                    Limitations = [] }

              let result = CompositorDamageReadiness.validate check
              Expect.isTrue result.Accepted "existing compositor damage readiness still accepted"
              Expect.equal (CompositorDamageReadiness.statusText result.Status) "accepted" "existing status token"
          } ]
