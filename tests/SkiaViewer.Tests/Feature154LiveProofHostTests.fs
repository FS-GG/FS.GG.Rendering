module Feature154LiveProofHostTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private capableFacts: GlHost.LiveProofHostFacts =
    { Display = Some ":99"
      WaylandDisplay = None
      SessionType = Some "x11"
      Renderer = Some "llvmpipe"
      ReadbackAvailable = true
      PermissionGranted = true
      TimedOut = false }

[<Tests>]
let tests =
    testList "Feature154 live proof host" [
        test "capable host facts produce the proof profile used by acceptance" {
            let profile = GlHost.liveProofHostProfile capableFacts

            Expect.equal (GlHost.classifyLiveProofHost capableFacts) GlHost.LiveProofHostReadiness.Capable "capable host"
            Expect.equal profile.Backend "OpenGL" "backend"
            Expect.equal profile.DisplayEnvironment CompositorProof.HostDisplayEnvironment.X11 "display environment"
            Expect.equal profile.ProofAlgorithmVersion CompositorProof.proofAlgorithmVersion "proof method"
        }

        test "unsupported host facts are non-capable and cannot accept proof" {
            [ { capableFacts with Display = None; WaylandDisplay = None }, GlHost.LiveProofHostReadiness.MissingDisplay
              { capableFacts with Renderer = None }, GlHost.LiveProofHostReadiness.MissingRenderer
              { capableFacts with ReadbackAvailable = false }, GlHost.LiveProofHostReadiness.ReadbackUnavailable
              { capableFacts with PermissionGranted = false }, GlHost.LiveProofHostReadiness.PermissionDenied
              { capableFacts with TimedOut = true }, GlHost.LiveProofHostReadiness.Timeout ]
            |> List.iter (fun (facts, expected) ->
                Expect.equal (GlHost.classifyLiveProofHost facts) expected $"{expected}")
        }
    ]
