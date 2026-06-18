module Feature156TimingHelperTests

open Expecto
open FS.GG.UI.Testing

let private scenario id verdict =
    { CompositorTimingScenario.ScenarioId = id
      FullRedrawSampleCount = 5
      DamageScopedSampleCount = 5
      Verdict = verdict
      ArtifactPaths = [ $"timing/scenarios/{id}.md"; $"timing/raw/{id}.csv" ]
      RejectionReasons = [] }

let private required =
    [ "timing/localized-update"
      "timing/no-change"
      "timing/movement-old-new"
      "timing/overlap"
      "timing/edge-clipping" ]

let private check scenarios =
    { CompositorTimingSummaryCheck.Feature = "156-same-profile-timing"
      ExpectedProfileId = "probe-08a47c01"
      ActualProfileId = "probe-08a47c01"
      PolicyId = "same-profile-live-threshold-v2"
      WarmupCount = 3
      MeasuredRepetitions = 5
      RequiredScenarioIds = required
      Scenarios = scenarios
      ShippedPerformanceClaim = "performance-not-accepted" }

[<Tests>]
let tests =
    testList "Feature156 timing helper" [
        test "positive same-profile summaries validate without accepting shipped performance claim" {
            let scenarios = required |> List.map (fun id -> scenario id CompositorTimingPositive)
            let result = CompositorTimingAssertions.validateSummary (check scenarios)

            Expect.isTrue result.Accepted "summary positive for measured profile"
            Expect.equal result.Verdict CompositorTimingPositive "verdict"
            Expect.equal (CompositorTimingAssertions.verdictText result.Verdict) "positive" "token"
        }

        test "noisy incomplete limited and environment-limited summaries fail closed" {
            // SYNTHETIC: rejection-only verdict fixtures exercise helper policy without live timing artifacts.
            [ CompositorTimingNoisy
              CompositorTimingIncomplete
              CompositorTimingLimited
              CompositorTimingEnvironmentLimited
              CompositorTimingNonBeneficial
              CompositorTimingRejected ]
            |> List.iter (fun verdict ->
                let scenarios =
                    required
                    |> List.map (fun id ->
                        if id = "timing/no-change" then
                            { scenario id verdict with RejectionReasons = [ CompositorTimingAssertions.verdictText verdict ] }
                        else
                            scenario id CompositorTimingPositive)

                let result = CompositorTimingAssertions.validateSummary (check scenarios)
                Expect.isFalse result.Accepted $"verdict {verdict} fails closed")
        }

        test "cross-profile missing scenario and overclaiming shipped status are rejected" {
            let invalid =
                { check [ scenario "timing/localized-update" CompositorTimingPositive ] with
                    ActualProfileId = "other-profile"
                    ShippedPerformanceClaim = "accepted" }

            let result = CompositorTimingAssertions.validateSummary invalid

            Expect.equal result.Verdict CompositorTimingIncomplete "missing scenarios dominate package completeness"
            Expect.isNonEmpty result.MissingScenarios "missing scenarios"
            Expect.exists result.Diagnostics (fun item -> item.Contains("profile mismatch")) "profile mismatch"
            Expect.exists result.Diagnostics (fun item -> item.Contains("cannot accept the shipped P7 performance claim")) "overclaim"
        }
    ]
