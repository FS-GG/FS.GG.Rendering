module Feature153LiveProofHostTests

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
    testList "Feature153 live proof host classification" [
        test "capable host facts classify as capable and produce a matching profile" {
            Expect.equal (GlHost.classifyLiveProofHost capableFacts) GlHost.LiveProofHostReadiness.Capable "capable host"
            let profile = GlHost.liveProofHostProfile capableFacts
            Expect.equal profile.Backend "OpenGL" "backend"
            Expect.equal profile.DisplayEnvironment CompositorProof.HostDisplayEnvironment.X11 "display environment"
            Expect.equal profile.ProofAlgorithmVersion CompositorProof.proofAlgorithmVersion "proof method"
        }

        test "missing display, renderer, readback, permission, and timeout are non-capable" {
            [ { capableFacts with Display = None; WaylandDisplay = None }, GlHost.LiveProofHostReadiness.MissingDisplay
              { capableFacts with Renderer = None }, GlHost.LiveProofHostReadiness.MissingRenderer
              { capableFacts with ReadbackAvailable = false }, GlHost.LiveProofHostReadiness.ReadbackUnavailable
              { capableFacts with PermissionGranted = false }, GlHost.LiveProofHostReadiness.PermissionDenied
              { capableFacts with TimedOut = true }, GlHost.LiveProofHostReadiness.Timeout ]
            |> List.iter (fun (facts, expected) ->
                Expect.equal (GlHost.classifyLiveProofHost facts) expected $"{expected}")
        }

        test "viewer host exposes whether the program can carry live proof interpreter effects" {
            let program =
                FS.GG.UI.SkiaViewer.Host.Viewer.create
                    (FS.GG.UI.SkiaViewer.Host.Viewer.defaultConfiguration "feature153" { Width = 320; Height = 200 })
                    (fun () -> 0, Elmish.Cmd.none)
                    (fun _ model -> model, Elmish.Cmd.none)
                    (fun _ -> FS.GG.UI.Scene.Scene.empty)

            Expect.isTrue (FS.GG.UI.SkiaViewer.Host.Viewer.liveProofInterpreterSupported program) "positive-sized viewer program can host proof effects"
        }
    ]
