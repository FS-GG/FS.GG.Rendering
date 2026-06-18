#r "../../../../tests/Rendering.Harness/bin/Debug/net10.0/Rendering.Harness.dll"
#r "../../../../src/Testing/bin/Debug/net10.0/FS.GG.UI.Testing.dll"

open FS.GG.UI.Testing
open Rendering.Harness

let profile : Compositor.HostProfile =
    { ProfileId = Compositor.feature156AcceptedProfileId
      Backend = "OpenGL"
      Renderer = Some "FSI authoring"
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
      Verdict = Compositor.Feature156Noisy
      ConfidenceDecision = "inside-noise-band"
      ArtifactPaths = [ "timing/scenarios/timing-localized-update.md" ]
      RejectionReasons = [ "p50 or p95 difference is inside the declared noise band" ]
      ProofOverheadIncluded = false }

let summary : Compositor.Feature156TimingSummary =
    { RunId = "feature156-fsi-authoring"
      HostProfile = profile
      PolicyId = Compositor.feature156PolicyId
      WarmupCount = 3
      MeasuredRepetitions = 5
      ScenarioReports = [ report ]
      OverallVerdict = Compositor.Feature156Noisy
      ShippedPerformanceClaim = "performance-not-accepted"
      Diagnostics = [ "FSI authoring transcript" ] }

let rendered = Compositor.renderFeature156ValidationSummary summary

let readinessReport : CompositorReadinessReport =
    { Feature = "156-same-profile-timing"
      ProofStatus = CompositorReadinessAccepted
      ParityStatus = CompositorReadinessAccepted
      TimingStatus = CompositorReadinessFallbackGated
      CompatibilityStatus = CompositorReadinessAccepted
      RegressionStatus = CompositorReadinessAccepted
      Evidence =
        [ { EvidenceName = "timing-summary"
            EvidencePath = Some "specs/156-same-profile-timing/readiness/timing/summary.md"
            EvidenceStatus = CompositorReadinessFallbackGated
            EvidenceRequired = true
            EvidenceDiagnostics = [ "performance-not-accepted" ] } ]
      Limitations = [ "Feature 156 timing verdict is noisy" ] }

let readinessValidation = CompositorReadiness.validate readinessReport

printfn "%s" rendered
printfn "Readiness status: %s" (CompositorReadiness.statusText readinessValidation.Status)
printfn "Timing verdict text: %s" (CompositorTimingAssertions.verdictText CompositorTimingNoisy)
printfn "FSI transcript PASS"
