module Feature151DisabledCacheParityTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature151DisabledCacheParity" [
        test "repeated ScrollViewer renders are observable-parity equivalent" {
            let item = Feature151ScrollViewerFixtures.cases |> List.find (fun item -> item.CaseId = "substantially-overflowing")
            let first = Feature151ScrollViewerFixtures.render item.Control
            let second = Feature151ScrollViewerFixtures.render item.Control

            Expect.equal second.Bounds first.Bounds "bounds parity"
            Expect.equal (Control.scrollViewport second item.ScrollViewerId) (Control.scrollViewport first item.ScrollViewerId) "scroll viewport parity"
        }

        test "layout compatibility render keeps diagnostics and work observable without cache assumptions" {
            let tree =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "header" ]
                            Feature151ScrollViewerFixtures.scrollViewer "sv-disabled-cache" (Feature151ScrollViewerFixtures.rows 12) ] ]

            let first = Feature151ScrollViewerFixtures.render tree
            let second = Feature151ScrollViewerFixtures.render tree

            Expect.equal second.Diagnostics first.Diagnostics "diagnostics parity"
            Expect.equal second.Bounds first.Bounds "default render parity"
            Expect.isSome (Control.scrollViewport second "sv-disabled-cache") "scroll viewport present"
        }
    ]
