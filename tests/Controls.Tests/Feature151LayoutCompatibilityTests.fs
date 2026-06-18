module Feature151LayoutCompatibilityTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature151LayoutCompatibility" [
        test "default layout around ScrollViewer keeps surrounding bounds stable" {
            let tree =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "before" ] |> Control.withKey "before"
                            Feature151ScrollViewerFixtures.scrollViewer "sv-layout-compat" (Feature151ScrollViewerFixtures.rows 10)
                            TextBlock.create [ TextBlock.text "after" ] |> Control.withKey "after" ] ]

            let first = Feature151ScrollViewerFixtures.render tree
            let second = Feature151ScrollViewerFixtures.render tree

            Expect.equal second.Bounds first.Bounds "surrounding layout stable"
            Expect.isEmpty second.Diagnostics "no new default layout diagnostics"
        }

        test "overlay state remains classified outside ScrollViewer extent calculation" {
            let tree =
                Stack.create
                    [ Stack.children
                          [ Feature151ScrollViewerFixtures.scrollViewer "sv-overlay-compat" (Feature151ScrollViewerFixtures.rows 10)
                            Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "menu" ]); Attr.selected true ] ] ]

            let rendered = Feature151ScrollViewerFixtures.render tree

            Expect.isSome (Control.scrollViewport rendered "sv-overlay-compat") "ScrollViewer extent still resolved"
            Expect.isEmpty rendered.Diagnostics "overlay compatibility diagnostics"
        }
    ]
