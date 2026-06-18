module Feature149DamageScopedRedrawTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private scissor x y width height : GlHost.ScissorRect =
    { X = x; Y = y; Width = width; Height = height }

[<Tests>]
let tests =
    testList "Feature149 damage-scoped redraw host decisions" [
        test "accepted proof gates scissored redraw and records area" {
            let decision =
                GlHost.decideScissorRedraw
                    CompositorProof.ProofReadiness.Ready
                    false
                    [ scissor 10 12 40 30
                      scissor 20 20 10 10 ]
                    100
                    100

            match decision with
            | GlHost.ScissorDecision.Scissored rects ->
                Expect.equal rects.Length 2 "two normalized scissor rects"
                Expect.isGreaterThan (GlHost.scissorArea rects) 0 "non-zero area"
            | other -> failtestf "expected scissored redraw, got %A" other
        }

        test "environment-limited proof, disabled mode, and full-frame invalidation fall back" {
            let rects = [ scissor 1 1 10 10 ]

            match GlHost.decideScissorRedraw (CompositorProof.ProofReadiness.EnvironmentLimited "unsupported") false rects 100 100 with
            | GlHost.ScissorDecision.FullRedraw reason -> Expect.stringContains reason "unsupported" "reason"
            | other -> failtestf "expected fallback, got %A" other

            Expect.equal
                (GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready true rects 100 100)
                (GlHost.ScissorDecision.FullRedraw "full-frame invalidation")
                "full-frame fallback"
        }
    ]
