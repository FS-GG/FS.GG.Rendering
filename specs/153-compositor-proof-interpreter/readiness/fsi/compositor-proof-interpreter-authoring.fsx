#r "nuget: FS.GG.UI.SkiaViewer, 0.1.15-preview.1"
#r "nuget: FS.GG.UI.Testing, 0.1.15-preview.1"

open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host
open FS.GG.UI.Testing

let profile: CompositorProof.HostProfile =
    { ProfileId = "feature153-host"
      Backend = "OpenGL"
      Renderer = Some "fsi"
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
    { ProofId = "proof-153"
      HostProfile = profile
      ScenarioId = "proof/live-sentinel-damage-v1"
      Verdict = CompositorProof.PresentProofPassed
      ObservedUntouchedRegions = []
      ObservedDamagedRegion = None
      EvidenceArtifacts = [ "sentinel-frame.png"; "damage-frame.png" ]
      CreatedAt = DateTimeOffset.UtcNow
      Diagnostics = [] }

let attempt: CompositorProof.LiveProofAttempt =
    { AttemptId = "run-1"
      Proof = proof
      ProofMethod = CompositorProof.proofAlgorithmVersion
      ArtifactQuality = quality }

let readiness =
    CompositorProof.evaluateProofSet profile DateTimeOffset.UtcNow (TimeSpan.FromHours 24.0) [ attempt; attempt; attempt ]

let proofSetReadiness: CompositorProof.ProofSetReadiness = readiness

let proofSetToken = CompositorProof.proofSetReadinessToken readiness

let acceptedShape (proofSet: CompositorProof.AcceptedProofSet) =
    proofSet.SelectedAttemptIds, proofSet.FreshnessWindow

let hostFacts: GlHost.LiveProofHostFacts =
    { Display = Some ":99"
      WaylandDisplay = None
      SessionType = Some "x11"
      Renderer = Some "llvmpipe"
      ReadbackAvailable = true
      PermissionGranted = true
      TimedOut = false }

let hostStatus = GlHost.classifyLiveProofHost hostFacts

let program =
    FS.GG.UI.SkiaViewer.Host.Viewer.create
        (FS.GG.UI.SkiaViewer.Host.Viewer.defaultConfiguration "feature153" { Width = 320; Height = 200 })
        (fun () -> 0, Elmish.Cmd.none)
        (fun _ model -> model, Elmish.Cmd.none)
        (fun _ -> Scene.empty)

let viewerSupportsProof = FS.GG.UI.SkiaViewer.Host.Viewer.liveProofInterpreterSupported program

let report: CompositorReadinessReport =
    { Feature = "153-compositor-proof-interpreter"
      ProofStatus = CompositorReadinessAccepted
      ParityStatus = CompositorReadinessAccepted
      TimingStatus = CompositorReadinessEnvironmentLimited
      CompatibilityStatus = CompositorReadinessAccepted
      RegressionStatus = CompositorReadinessAccepted
      Evidence = []
      Limitations = [] }

let validation = CompositorReadiness.validate report

printfn "%s %A %b %b" proofSetToken hostStatus viewerSupportsProof validation.Accepted
