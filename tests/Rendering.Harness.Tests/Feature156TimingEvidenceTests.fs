module Feature156TimingEvidenceTests

open Expecto
open Rendering.Harness

let private distribution path samples =
    Perf.summarizeSamples path samples

[<Tests>]
let tests =
    testList "Feature156 timing evidence" [
        test "policy computes percentiles noise band and positive decision" {
            let full = distribution "raw/full.csv" [ 10.0; 11.0; 12.0; 13.0; 14.0 ]
            let damage = distribution "raw/damage.csv" [ 4.0; 5.0; 5.5; 6.0; 6.5 ]
            let decision = Perf.evaluateScenario 5 full damage

            Expect.equal (Perf.timingVerdictToken decision.Verdict) "positive" "positive verdict"
            Expect.floatClose Accuracy.medium decision.NoiseBandMs 0.6 "5 percent of full p50"
            Expect.equal decision.ConfidenceDecision "positive-outside-noise-band" "confidence"
        }

        test "sparse heavy localized update is positive outside the noise band" {
            let full = distribution "raw/sparse-heavy-full.csv" [ 31.0; 32.0; 32.5; 33.0; 34.0 ]
            let damage = distribution "raw/sparse-heavy-damage.csv" [ 3.8; 4.0; 4.1; 4.2; 4.4 ]
            let decision = Perf.evaluateScenario 5 full damage

            Expect.equal decision.Verdict Perf.Positive "tiny dirty patch should beat a heavy full redraw"
            Expect.floatClose Accuracy.medium decision.NoiseBandMs 1.625 "5 percent of full p50"
            Expect.isEmpty decision.Reasons "positive damage-scoped evidence has no rejection reasons"
        }

        test "noisy non-beneficial and incomplete inputs fail closed" {
            let full = distribution "raw/full.csv" [ 10.0; 10.0; 10.0; 10.0; 10.0 ]
            let noisy = distribution "raw/noisy.csv" [ 9.9; 9.9; 9.9; 9.9; 9.9 ]
            let slower = distribution "raw/slower.csv" [ 12.0; 12.0; 12.0; 12.0; 12.0 ]
            let incomplete = distribution "raw/short.csv" [ 1.0; 2.0 ]

            Expect.equal (Perf.evaluateScenario 5 full noisy).Verdict Perf.Noisy "inside noise band"
            Expect.equal (Perf.evaluateScenario 5 full slower).Verdict Perf.NonBeneficial "slower damage path"
            Expect.equal (Perf.evaluateScenario 5 full incomplete).Verdict Perf.Incomplete "not enough samples"
            Expect.equal (Perf.evaluateScenario 5 full None).Verdict Perf.Incomplete "missing path"
        }

        test "Feature156 constants declare accepted profile policy and required scenarios" {
            Expect.equal Compositor.feature156AcceptedProfileId "probe-08a47c01" "accepted host profile"
            Expect.equal Compositor.feature156PolicyId "same-profile-live-threshold-v2" "policy id"
            Expect.equal Compositor.feature156RequiredScenarioIds.Length 5 "five required scenarios"
            Expect.contains Compositor.feature156RequiredScenarioIds "timing/edge-clipping" "edge clipping scenario"
        }

        test "MVU workflow records host policy scenario and summary publication" {
            let model0, effects0 = Compositor.initFeature156 3 5
            Expect.contains effects0 Compositor.Feature156DetectHostProfile "host detection"
            Expect.contains effects0 (Compositor.Feature156DeclarePolicy Compositor.feature156PolicyId) "policy declaration"

            let profile : Compositor.HostProfile =
                { ProfileId = Compositor.feature156AcceptedProfileId
                  Backend = "OpenGL"
                  Renderer = Some "test-renderer"
                  PresentMode = "DirectToSwapchain"
                  FramebufferSize = "640x480"
                  Scale = Some 1.0
                  DisplayEnvironment = "x11"
                  ProofAlgorithmVersion = "sentinel-damage-v1" }

            let report : Compositor.Feature156ScenarioReport =
                { ScenarioId = "timing/localized-update"
                  FullRedraw = None
                  DamageScoped = None
                  WarmupCount = 3
                  MeasuredRepetitions = 5
                  NoiseBandMs = 0.0
                  Verdict = Compositor.Feature156Incomplete
                  ConfidenceDecision = "incomplete"
                  ArtifactPaths = [ "scenarios/timing-localized-update.md" ]
                  RejectionReasons = [ "missing samples" ]
                  ProofOverheadIncluded = false }

            let model1, _ = Compositor.updateFeature156 (Compositor.Feature156HostProfileDetected profile) model0
            let model2, _ = Compositor.updateFeature156 (Compositor.Feature156PolicyDeclared Compositor.feature156PolicyId) model1
            let model3, _ = Compositor.updateFeature156 (Compositor.Feature156ScenarioEvaluated report) model2
            let model4, _ = Compositor.updateFeature156 (Compositor.Feature156SummaryPublished "timing/summary.md") model3

            Expect.equal model4.ActiveProfile (Some profile) "active profile"
            Expect.equal model4.PolicyId (Some Compositor.feature156PolicyId) "policy"
            Expect.equal model4.Verdict Compositor.Feature156Incomplete "overall incomplete until all required scenarios are positive"
            Expect.contains model4.PublishedArtifacts "timing/summary.md" "published artifact"
        }
    ]
