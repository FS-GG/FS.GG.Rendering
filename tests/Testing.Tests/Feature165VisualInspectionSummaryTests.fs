module TestingCapability.Feature165VisualInspectionSummaryTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing

let private size: Size = { Width = 320; Height = 200 }

let private artifact id status : VisualInspectionArtifact =
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
      UnsupportedFacts = []
      Diagnostics = []
      GeneratedAtUtc = "2026-06-19T00:00:00Z" }

let private result id status findings : VisualInspectionValidationResult =
    { ArtifactId = id
      ReadinessStatus = status
      Findings = findings
      AppliedExceptions = []
      InvalidExceptions = []
      UnusedExceptions = []
      Diagnostics = [] }

[<Tests>]
let tests =
    testList
        "Feature165 visual inspection summaries"
        [ test "readiness summary groups status severity rule and blocking findings" {
              let blocking =
                  VisualInspection.finding "text-contained-in-owner" VisualInspectionSeverity.Blocking [ "title" ] [] "overflow" "inside" "overflow"

              let summary =
                  VisualInspectionReadiness.aggregate
                      "run"
                      [ artifact "accepted" VisualInspectionStatus.Accepted; artifact "blocked" VisualInspectionStatus.Accepted ]
                      [ result "accepted" VisualInspectionStatus.Accepted []; result "blocked" VisualInspectionStatus.Blocked [ blocking ] ]
                      [ "screens/light/accepted.png" ]
                      [ "representative sample only" ]

              Expect.equal summary.OverallStatus VisualInspectionStatus.Blocked "blocking result wins overall status"
              Expect.contains summary.StatusCounts ("blocked", 1) "blocked count"
              Expect.contains summary.FindingCounts ("blocking", 1) "severity count"
              Expect.equal (summary.BlockingFindings |> List.map _.RuleId) [ "text-contained-in-owner" ] "blocking finding retained"
              Expect.equal summary.RelatedVisualEvidence [ "screens/light/accepted.png" ] "visual evidence retained"
          }

          test "summary keeps not-inspected and not-run scopes visible" {
              let summary =
                  VisualInspectionReadiness.aggregate
                      "run"
                      [ artifact "inspected" VisualInspectionStatus.Accepted
                        artifact "future" VisualInspectionStatus.NotInspected
                        artifact "not-run" VisualInspectionStatus.NotRun ]
                      []
                      []
                      []

              Expect.contains summary.NotInspectedScopes "future" "not inspected visible"
              Expect.contains summary.NotRunScopes "not-run" "not run visible"
              Expect.contains summary.StatusCounts ("not-inspected", 1) "not inspected counted"
              Expect.contains summary.StatusCounts ("not-run", 1) "not run counted"
          } ]
