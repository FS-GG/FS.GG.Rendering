module Feature156ReadinessPackageTests

open Expecto
open Rendering.Harness

let private profile : Compositor.Types.HostProfile =
    { ProfileId = Compositor.Config.feature156AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "test-renderer"
      PresentMode = "DirectToSwapchain"
      FramebufferSize = "640x480"
      Scale = Some 1.0
      DisplayEnvironment = "x11"
      ProofAlgorithmVersion = "sentinel-damage-v1" }

let private dist path : Compositor.Types.Feature156PathDistribution =
    { SampleCount = 5
      P50Ms = 10.0
      P95Ms = 12.0
      P99Ms = 13.0
      MinMs = 9.0
      MaxMs = 13.0
      RawSamplePath = path }

let private report scenario verdict : Compositor.Types.Feature156ScenarioReport =
    { ScenarioId = scenario
      FullRedraw = Some(dist $"raw/{Compositor.FeatureState.feature156ScenarioFileName scenario}-full.csv")
      DamageScoped = Some { dist $"raw/{Compositor.FeatureState.feature156ScenarioFileName scenario}-damage.csv" with P50Ms = 6.0; P95Ms = 7.0; P99Ms = 8.0 }
      WarmupCount = 3
      MeasuredRepetitions = 5
      NoiseBandMs = 0.5
      Verdict = verdict
      ConfidenceDecision = Compositor.FeatureState.feature156VerdictToken verdict
      ArtifactPaths = [ $"scenarios/{Compositor.FeatureState.feature156ScenarioFileName scenario}" ]
      RejectionReasons = []
      ProofOverheadIncluded = false }

[<Tests>]
let tests =
    testList "Feature156 readiness package" [
        test "summary includes scenario table policy host claim boundary and remaining gates" {
            let reports = Compositor.Config.feature156RequiredScenarioIds |> List.map (fun scenario -> report scenario Compositor.Types.Feature156Positive)
            let summary : Compositor.Types.Feature156TimingSummary =
                { RunId = "feature156-test"
                  HostProfile = profile
                  PolicyId = Compositor.Config.feature156PolicyId
                  WarmupCount = 3
                  MeasuredRepetitions = 5
                  ScenarioReports = reports
                  OverallVerdict = Compositor.FeatureState.feature156OverallVerdict reports
                  ShippedPerformanceClaim = "performance-not-accepted"
                  Diagnostics = [] }

            let rendered = Compositor.Render3.emitFeature156TimingSummary summary

            [ "Feature 156 timing verdict: `positive`"
              "Shipped P7 performance claim: `performance-not-accepted`"
              "Policy id: `same-profile-live-threshold-v2`"
              "Accepted profile id: `probe-08a47c01`"
              "Feature 155 Baseline"
              "Feature 157 damage-scissored no-clear renderer"
              "Feature 160 validation throughput follow-up"
              "Feature 161 host performance lane ledger" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "scenario report exposes distributions artifacts rejection reasons and overhead disclosure" {
            let rendered =
                { report "timing/no-change" Compositor.Types.Feature156Noisy with
                    RejectionReasons = [ "inside noise band" ]
                    ProofOverheadIncluded = true }
                |> Compositor.Render3.emitFeature156ScenarioReport

            [ "Verdict: `noisy`"
              "Warmup count: `3`"
              "| full-redraw |"
              "| damage-scoped |"
              "inside noise band"
              "proof-readback-or-validation-overhead-included" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "validation and compatibility summaries keep correctness and performance boundaries separate" {
            let reports = [ report "timing/localized-update" Compositor.Types.Feature156Incomplete ]
            let summary : Compositor.Types.Feature156TimingSummary =
                { RunId = "feature156-test"
                  HostProfile = profile
                  PolicyId = Compositor.Config.feature156PolicyId
                  WarmupCount = 3
                  MeasuredRepetitions = 5
                  ScenarioReports = reports
                  OverallVerdict = Compositor.FeatureState.feature156OverallVerdict reports
                  ShippedPerformanceClaim = "performance-not-accepted"
                  Diagnostics = [] }

            let validation = Compositor.Render3.emitFeature156ValidationSummary summary
            let ledger = Compositor.Render3.emitFeature156CompatibilityLedger ()

            Expect.stringContains validation "Correctness status: `accepted-via-feature-155`" "correctness baseline"
            Expect.stringContains validation "Performance claim: `performance-not-accepted`" "performance boundary"
            Expect.stringContains ledger "CompositorTimingAssertions" "package helper"
            Expect.stringContains ledger "Feature 155 proof, parity, fallback, and correctness vocabulary remains authoritative" "Feature155 boundary"
        }
    ]
