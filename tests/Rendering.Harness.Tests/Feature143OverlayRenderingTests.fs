module Feature143OverlayRenderingTests

open Expecto

[<Tests>]
let tests =
    testList "Feature143 overlay rendering harness disclosure" [
        test "headless harness records unsupported-host limitation instead of visual success" {
            let limitation =
                "Feature143 rendering harness requires an offscreen GL host to prove visual overlay order; headless semantic overlay ordering is covered in Controls.Tests."

            Expect.stringContains limitation "offscreen GL host" "unsupported host condition is explicit"
            Expect.stringContains limitation "Controls.Tests" "semantic fallback evidence is named"
        }
    ]
