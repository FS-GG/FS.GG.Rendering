module Feature152DamageScopedRedrawTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private scissor x y width height : GlHost.ScissorRect =
    { X = x; Y = y; Width = width; Height = height }

[<Tests>]
let tests =
    testList "Feature152 damage-scoped redraw host decisions" [
        test "accepted proof is still required before damage-scoped redraw" {
            let damage = [ scissor 8 8 32 24 ]

            match GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready false damage 100 100 with
            | GlHost.ScissorDecision.Scissored rects -> Expect.equal rects.Length 1 "scissored"
            | other -> failtestf "expected scissored redraw, got %A" other

            match GlHost.decideScissorRedraw (CompositorProof.ProofReadiness.HostMismatch) false damage 100 100 with
            | GlHost.ScissorDecision.FullRedraw reason -> Expect.stringContains reason "host-mismatched" "host drift fallback"
            | other -> failtestf "expected fallback, got %A" other
        }

        test "invalid and frame-wide damage remain full-redraw fallbacks" {
            Expect.equal
                (GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready false [ scissor -10 -10 0 0 ] 100 100)
                (GlHost.ScissorDecision.FullRedraw "empty damage")
                "invalid empty damage"

            Expect.equal
                (GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready true [ scissor 0 0 100 100 ] 100 100)
                (GlHost.ScissorDecision.FullRedraw "full-frame invalidation")
                "full-frame fallback"
        }
    ]
