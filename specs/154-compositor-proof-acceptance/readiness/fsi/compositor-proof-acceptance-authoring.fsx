#r "nuget: FS.GG.UI.SkiaViewer"
#r "nuget: FS.GG.UI.Testing"

open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Testing

let profile: CompositorProof.HostProfile =
    { ProfileId = "feature154-fsi-host"
      Backend = "OpenGL"
      Renderer = Some "fsi-renderer"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let quality: CompositorProof.ProofArtifactQuality =
    { Present = true; Decodable = true; NonBlank = true; Fresh = true; Synthetic = false }

let proof id =
    { ProofId = id
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = []
      ObservedDamagedRegion = None
      EvidenceArtifacts = [ $"{id}/proof.md" ]
      CreatedAt = DateTimeOffset.UtcNow
      Diagnostics = [ "Feature154 FSI authoring" ] }

let attempt id: CompositorProof.LiveProofAttempt =
    { AttemptId = id
      Proof = proof id
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

let readiness =
    CompositorProof.evaluateProofSet
        profile
        DateTimeOffset.UtcNow
        (TimeSpan.FromHours 1.0)
        [ attempt "run-1"; attempt "run-2"; attempt "run-3" ]

let proofSetStatus =
    match readiness with
    | CompositorProof.ProofSetReadiness.Accepted _ -> CompositorReadinessAccepted
    | CompositorProof.ProofSetReadiness.FallbackGated _ -> CompositorReadinessFallbackGated
    | CompositorProof.ProofSetReadiness.Failed _ -> CompositorReadinessFailed
    | CompositorProof.ProofSetReadiness.EnvironmentLimited _ -> CompositorReadinessEnvironmentLimited

let readinessReport =
    { Feature = "154-compositor-proof-acceptance"
      ProofStatus = proofSetStatus
      ParityStatus = CompositorReadinessFallbackGated
      TimingStatus = CompositorReadinessEnvironmentLimited
      CompatibilityStatus = CompositorReadinessAccepted
      RegressionStatus = CompositorReadinessAccepted
      Evidence = []
      Limitations = [] }

let validation = CompositorReadiness.validate readinessReport

printfn "Feature154 proof-set status = %s" (CompositorProof.proofSetReadinessToken readiness)
printfn "Feature154 helper status = %s" (CompositorReadiness.statusText validation.Status)
