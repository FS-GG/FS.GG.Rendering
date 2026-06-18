module Feature148DamageScopedRedrawTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private scissor x y width height : GlHost.ScissorRect =
    { X = x; Y = y; Width = width; Height = height }

[<Tests>]
let tests =
    testList "Feature148 damage-scoped redraw host decisions" [
        test "proof-gated damage union uses scissor when ready and localized" {
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

        test "stale proof, host mismatch, disabled mode, and full-frame invalidation fall back" {
            let rects = [ scissor 1 1 10 10 ]

            [ CompositorProof.ProofReadiness.Stale
              CompositorProof.ProofReadiness.HostMismatch
              CompositorProof.ProofReadiness.EnvironmentLimited "unsupported" ]
            |> List.iter (fun readiness ->
                match GlHost.decideScissorRedraw readiness false rects 100 100 with
                | GlHost.ScissorDecision.FullRedraw reason -> Expect.isFalse (System.String.IsNullOrWhiteSpace reason) "fallback reason"
                | other -> failtestf "expected fallback, got %A" other)

            Expect.equal
                (GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready true rects 100 100)
                (GlHost.ScissorDecision.FullRedraw "full-frame invalidation")
                "full-frame fallback"
        }
    ]
