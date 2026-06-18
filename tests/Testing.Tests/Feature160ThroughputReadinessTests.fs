module Feature160ThroughputReadinessTests

open Expecto
open FS.GG.UI.Testing

let private required =
    [ "timing/localized-update"
      "timing/no-change"
      "timing/movement-old-new"
      "timing/overlap"
      "timing/edge-clipping" ]

let private scenario id =
    { ScenarioId = id
      Covered = true
      WarmupCount = 3
      MeasuredRepetitions = 5
      SamplePolicy = "readback-free"
      ArtifactPaths = [ $"throughput/iterations/{id}.md" ]
      PrimaryReason = None }

let private check fullValidationStatus =
    { Feature = "160-performance-validation-throughput"
      RequiredScenarioIds = required
      Scenarios = required |> List.map scenario
      AcceptedIterationCount = 3
      RequiredIterationCount = 3
      UnsupportedHostStatus = Feature160EnvironmentLimited
      AcceptedUnsupportedHostArtifacts = 0
      FullValidationStatus = fullValidationStatus
      CompatibilityAccepted = true
      PackageAccepted = true
      RegressionAccepted = true
      PerformanceClaim = "performance-not-accepted"
      Limitations = [] }

[<Tests>]
let tests =
    testList "Feature160 throughput readiness helper" [
        test "accepts complete throughput with current full validation without accepting shipped performance" {
            let result = Feature160ThroughputReadiness.validate (check "passed")
            Expect.isTrue result.Accepted "accepted"
            Expect.equal result.Status Feature160Accepted "status"
            Expect.equal (Feature160ThroughputReadiness.statusText result.Status) "accepted" "token"
        }

        test "blocks otherwise accepted throughput when full validation is missing failing interrupted or stale" {
            [ "missing"; "failed"; "interrupted"; "stale"; "undocumented" ]
            |> List.iter (fun status ->
                let result = Feature160ThroughputReadiness.validate (check status)
                Expect.isFalse result.Accepted $"not accepted {status}"
                Expect.equal result.Status Feature160Blocked $"blocked {status}")
        }

        test "Synthetic helper fixture: missing scenarios sample policy mismatch and overclaim fail closed" {
            // SYNTHETIC: in-memory throughput rows exercise package helper rejection policy without live GL artifacts.
            let invalid =
                { check "passed" with
                    RequiredScenarioIds = required @ [ "timing/missing" ]
                    Scenarios = { scenario "timing/localized-update" with SamplePolicy = "probe-readback-included" } :: (required |> List.tail |> List.map scenario)
                    PerformanceClaim = "performance-accepted"
                    Limitations = [ "overclaim attempted" ] }

            let result = Feature160ThroughputReadiness.validate invalid
            Expect.isFalse result.Accepted "invalid rejected"
            Expect.equal result.Status Feature160Rejected "rejected"
            Expect.contains result.MissingScenarios "timing/missing" "missing scenario"
            Expect.exists result.Diagnostics (fun item -> item.Contains("sample policy")) "sample policy diagnostic"
            Expect.exists result.Diagnostics (fun item -> item.Contains("performance claim")) "claim boundary"
        }

        test "environment-limited package preserves zero accepted unsupported-host artifacts" {
            let environmentLimited =
                { check "missing" with
                    AcceptedIterationCount = 0
                    UnsupportedHostStatus = Feature160EnvironmentLimited }

            let result = Feature160ThroughputReadiness.validate environmentLimited
            Expect.isFalse result.Accepted "environment-limited is not accepted"
            Expect.equal result.Status Feature160EnvironmentLimited "status"
        }
    ]
