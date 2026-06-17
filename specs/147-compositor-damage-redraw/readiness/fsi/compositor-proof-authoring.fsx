#r "nuget: FS.GG.UI.SkiaViewer"

open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let profile: CompositorProof.HostProfile =
    { ProfileId = "fsi-proof"
      Backend = "OpenGL"
      Renderer = Some "Mesa"
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FramebufferSize = { Width = 640; Height = 480 }
      Scale = Some 1.0
      DisplayEnvironment = CompositorProof.HostDisplayEnvironment.X11
      ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

let proofReadiness =
    CompositorProof.readiness profile DateTimeOffset.UtcNow (TimeSpan.FromHours 1.0) None

printfn "readiness=%s" (CompositorProof.readinessToken proofReadiness)
