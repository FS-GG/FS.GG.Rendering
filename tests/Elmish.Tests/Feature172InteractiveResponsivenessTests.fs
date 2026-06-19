module Feature172InteractiveResponsivenessTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

[<Tests>]
let tests =
    testList "Feature172 retained interactive responsiveness" [
        test "pointer click applies product state in the discrete input frame" {
            let frames =
                Feature167ResponsivenessFixtures.run
                    [ FrameInput.Pointer(Click("btn", PointerButton.Primary, 12.0, 12.0)) ]

            Expect.equal frames.Length 1 "click is one discrete frame"
            Expect.isTrue frames.[0].ProductModelChanged "click dispatched the product message before catch-up frames"
            Expect.equal frames.[0].FrameCause FrameCause.PointerDiscrete "click is classified as discrete input"
        }

        test "pointer move burst is coalesced and remains retained-routed" {
            let frames =
                Feature167ResponsivenessFixtures.run
                    [ for i in 0 .. 5 ->
                          FrameInput.Pointer(HoverEnter("btn", float (8 + i), 12.0)) ]

            Expect.equal frames.Length 1 "move burst collapses to one frame"
            Expect.equal frames.[0].PointerSamplesReceived 6 "raw samples are still counted"
            Expect.equal frames.[0].PointerMovesProcessed 1 "only the latest move is routed"
            Expect.equal frames.[0].FullRenderFallbackCount 0 "retained route avoids routing full renders"
        }

        test "coalesced move does not swallow adjacent discrete clicks" {
            let frames =
                Feature167ResponsivenessFixtures.run
                    [ FrameInput.Pointer(HoverEnter("btn", 4.0, 4.0))
                      FrameInput.Pointer(HoverEnter("btn", 8.0, 8.0))
                      FrameInput.Pointer(Click("btn", PointerButton.Primary, 12.0, 12.0))
                      FrameInput.Pointer(Click("btn", PointerButton.Primary, 14.0, 12.0)) ]

            Expect.equal frames.Length 3 "one move frame plus two discrete click frames"
            Expect.equal frames.[0].FrameCause FrameCause.PointerMove "coalesced move drains first"
            Expect.equal frames.[1].FrameCause FrameCause.PointerDiscrete "first click survives"
            Expect.equal frames.[2].FrameCause FrameCause.PointerDiscrete "second click survives"
            Expect.isTrue (frames |> List.skip 1 |> List.forall _.ProductModelChanged) "both clicks update state"
        }

        test "outside and re-enter pointer movement remains deterministic" {
            let frames =
                Feature167ResponsivenessFixtures.run
                    [ FrameInput.Pointer(HoverEnter("outside", -10.0, -10.0))
                      FrameInput.Pointer(HoverEnter("btn", 12.0, 12.0))
                      FrameInput.Pointer(Click("btn", PointerButton.Primary, 12.0, 12.0)) ]

            Expect.equal frames.Length 2 "outside/re-enter moves coalesce before click"
            Expect.equal frames.[0].PointerSamplesReceived 2 "both move samples are counted"
            Expect.isTrue frames.[1].ProductModelChanged "re-enter click still dispatches"
        }
    ]
