module Feature150LayoutCompatibilityTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature150LayoutCompatibility" [
        test "renderTree still returns stable viewport bounds around scroll content" {
            let rendered = Feature150ScrollFixtures.render (Feature150ScrollFixtures.scrollViewer "sv" (Feature150ScrollFixtures.rows 8))
            let viewport = rendered.Bounds |> List.find (fun (id, _) -> id = "sv") |> snd

            Expect.equal viewport.Width 240.0 "viewport keeps parent width"
            Expect.equal viewport.Height 120.0 "viewport keeps parent height"
        }
    ]

