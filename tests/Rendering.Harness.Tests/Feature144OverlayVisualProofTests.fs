module Feature144OverlayVisualProofTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature144 overlay visual proof disclosure" [
        test "unsupported hosts report an explicit limitation" {
            let facts =
                { EffectiveBackend = NoDisplay
                  Display = None
                  GlRenderer = None
                  GlVersion = None
                  GlDirect = false
                  RefreshHz = None
                  Extensions = []
                  SwapControl = None
                  VblankSource = None
                  UinputAvailable = false }

            let limitation = Live.overlayVisualLimitation facts

            Expect.isSome limitation "headless host reports limitation"
            Expect.stringContains limitation.Value "offscreen GL/display host" "limitation names missing proof path"
        }
    ]
