module Feature156CompositorTimingTests

open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private scissor x y width height : GlHost.ScissorRect =
    { X = x; Y = y; Width = width; Height = height }

[<Tests>]
let tests =
    testList "Feature156 compositor timing support" [
        test "viewer timing path tokens are stable" {
            Expect.equal (Viewer.timingPathToken ViewerTimingPath.FullRedraw) "full-redraw" "full"
            Expect.equal (Viewer.timingPathToken ViewerTimingPath.DamageScoped) "damage-scoped" "damage"
            Expect.isTrue (Viewer.timingPathCanSupportClaim ViewerTimingPath.DamageScoped false false) "isolated path"
            Expect.isFalse (Viewer.timingPathCanSupportClaim ViewerTimingPath.DamageScoped true false) "proof readback limits claim"
        }

        test "CompositorProof timing overhead disclosure classifies limited measurements" {
            let disclosure =
                { CompositorProof.TimingOverheadDisclosure.Path = CompositorProof.TimingPath.DamageScoped
                  CompositorProof.TimingOverheadDisclosure.ProofReadbackIncluded = true
                  CompositorProof.TimingOverheadDisclosure.ValidationReadbackIncluded = false
                  CompositorProof.TimingOverheadDisclosure.ReviewerNote = "readback dominated" }

            Expect.equal (CompositorProof.timingPathToken CompositorProof.TimingPath.DamageScoped) "damage-scoped" "path"
            Expect.equal (CompositorProof.timingOverheadVerdict disclosure) "limited" "limited by proof readback"
        }

        test "damage-scoped timing path still preserves full-redraw fallback boundary" {
            let damage = [ scissor 10 10 20 20 ]
            let ready = GlHost.decideScissorRedraw CompositorProof.ProofReadiness.Ready false damage 100 100
            let fallback = GlHost.decideScissorRedraw (CompositorProof.ProofReadiness.EnvironmentLimited "missing display") false damage 100 100

            match ready with
            | GlHost.ScissorDecision.Scissored rects -> Expect.equal rects.Length 1 "scissored when proof-ready"
            | other -> failtestf "expected scissored decision, got %A" other

            match fallback with
            | GlHost.ScissorDecision.FullRedraw reason -> Expect.stringContains reason "missing display" "fallback reason"
            | other -> failtestf "expected full redraw fallback, got %A" other
        }
    ]
