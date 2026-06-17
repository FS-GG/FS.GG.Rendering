module Feature147ScissorRedrawTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

[<Tests>]
let tests =
    testList "Feature147 scissor redraw decision" [
        test "damage rectangles are clipped and empty regions discarded" {
            let rects =
                GlHost.normalizeScissorRects
                    100
                    80
                    [ { GlHost.ScissorRect.X = -10; Y = -10; Width = 20; Height = 20 }
                      { GlHost.ScissorRect.X = 90; Y = 70; Width = 20; Height = 20 }
                      { GlHost.ScissorRect.X = 200; Y = 0; Width = 10; Height = 10 } ]

            Expect.equal rects.Length 2 "two visible clipped rects"
            Expect.equal (GlHost.scissorArea rects) 200 "area after clipping"
        }

        test "ready proof allows scissored redraw for localized damage" {
            let decision =
                GlHost.decideScissorRedraw
                    CompositorProof.ProofReadiness.Ready
                    false
                    [ { GlHost.ScissorRect.X = 1; Y = 2; Width = 10; Height = 12 } ]
                    100
                    100

            match decision with
            | GlHost.ScissorDecision.Scissored rects -> Expect.equal rects.Length 1 "scissored"
            | other -> failtestf "expected scissored, got %A" other
        }

        test "unsafe proof states and full-frame invalidations fall back to full redraw" {
            Expect.equal
                (GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Missing false [] 100 100)
                (GlHost.ScissorDecision.FullRedraw "missing present-path proof")
                "missing proof"

            Expect.equal
                (GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready true [ { GlHost.ScissorRect.X = 1; Y = 1; Width = 10; Height = 10 } ] 100 100)
                (GlHost.ScissorDecision.FullRedraw "full-frame invalidation")
                "full-frame invalidation"
        }
    ]
