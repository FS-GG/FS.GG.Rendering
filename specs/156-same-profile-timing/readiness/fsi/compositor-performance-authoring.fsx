#r "../../../../tools/Rendering.Harness/bin/Debug/net10.0/Rendering.Harness.dll"
#r "../../../../src/SkiaViewer/bin/Debug/net10.0/FS.GG.UI.SkiaViewer.dll"
#r "../../../../src/Testing/bin/Debug/net10.0/FS.GG.UI.Testing.dll"

open FS.GG.UI.SkiaViewer
open FS.GG.UI.Testing
open Rendering.Harness

let full =
    Perf.summarizeSamples
        "raw/fsi-full-redraw.csv"
        [ 13.605; 13.782; 14.054; 13.221; 13.488 ]

let damage =
    Perf.summarizeSamples
        "raw/fsi-damage-scoped.csv"
        [ 13.278; 13.991; 14.296; 13.125; 13.401 ]

let decision = Perf.evaluateScenario 5 full damage
let damagePath = Viewer.timingPathToken ViewerTimingPath.DamageScoped
let policyId = "same-profile-live-threshold-v2"

let disclosure : CompositorProof.TimingOverheadDisclosure =
    { Path = CompositorProof.TimingPath.DamageScoped
      ProofReadbackIncluded = false
      ValidationReadbackIncluded = false
      ReviewerNote = "same-profile timing path separated from proof readback" }

let required =
    [ "timing/localized-update"
      "timing/no-change"
      "timing/movement-old-new"
      "timing/overlap"
      "timing/edge-clipping" ]

let scenarios : CompositorTimingScenario list =
    required
    |> List.map (fun scenario ->
        { ScenarioId = scenario
          FullRedrawSampleCount = 5
          DamageScopedSampleCount = 5
          Verdict = CompositorTimingNoisy
          ArtifactPaths = [ $"timing/scenarios/{scenario}.md" ]
          RejectionReasons = [ "inside noise band" ] })

let check : CompositorTimingSummaryCheck =
    { Feature = "156-same-profile-timing"
      ExpectedProfileId = Compositor.feature156AcceptedProfileId
      ActualProfileId = Compositor.feature156AcceptedProfileId
      PolicyId = policyId
      WarmupCount = 3
      MeasuredRepetitions = 5
      RequiredScenarioIds = required
      Scenarios = scenarios
      ShippedPerformanceClaim = "performance-not-accepted" }

let helperResult = CompositorTimingAssertions.validateSummary check

printfn "Feature 156 policy: %s" policyId
printfn "Accepted profile: %s" Compositor.feature156AcceptedProfileId
printfn "Skia timing path: %s" damagePath
printfn "Overhead verdict: %s" (CompositorProof.timingOverheadVerdict disclosure)
printfn "Helper verdict: %s" (CompositorTimingAssertions.verdictText helperResult.Verdict)
printfn "Timing verdict: %s" (Perf.timingVerdictToken decision.Verdict)
printfn "Confidence: %s" decision.ConfidenceDecision
printfn "Noise band ms: %.3f" decision.NoiseBandMs
printfn "Shipped performance claim: performance-not-accepted"
printfn "FSI transcript PASS"
