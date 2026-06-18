#r "nuget: FS.GG.UI.SkiaViewer, 0.1.14-preview.1"
#r "nuget: FS.GG.UI.Testing, 0.1.14-preview.1"

open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Testing

let profile: CompositorProof.HostProfile =
    { ProfileId = "feature152-authoring"
      Backend = "OpenGL"
      Renderer = Some "authoring"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let quality: CompositorProof.ProofArtifactQuality =
    { Present = true
      Decodable = true
      NonBlank = true
      Fresh = true
      Synthetic = false }

let proof: CompositorProof.PresentProof =
    { ProofId = "authoring-proof"
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = []
      ObservedDamagedRegion = None
      EvidenceArtifacts = [ "sentinel-frame.png"; "damage-frame.png"; "proof.md" ]
      CreatedAt = DateTimeOffset.UnixEpoch
      Diagnostics = [ "authoring=true" ] }

let attempt: CompositorProof.LiveProofAttempt =
    { AttemptId = "run-1"
      Proof = proof
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

let proofSetStatus: CompositorProof.ProofSetReadiness =
    CompositorProof.evaluateProofSet profile DateTimeOffset.UnixEpoch (TimeSpan.FromHours 1.0) [ attempt; { attempt with AttemptId = "run-2" }; { attempt with AttemptId = "run-3" } ]

let readinessReport: CompositorReadinessReport =
    { Feature = "152-compositor-live-proof"
      ProofStatus = CompositorReadinessEnvironmentLimited
      ParityStatus = CompositorReadinessFallbackGated
      TimingStatus = CompositorReadinessEnvironmentLimited
      CompatibilityStatus = CompositorReadinessAccepted
      RegressionStatus = CompositorReadinessAccepted
      Evidence = []
      Limitations = [] }

let readiness = CompositorReadiness.validate readinessReport

printfn "%s" (CompositorProof.proofSetReadinessToken proofSetStatus)
printfn "%s" (CompositorReadiness.statusText readiness.Status)
