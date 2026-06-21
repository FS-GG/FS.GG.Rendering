module Feature150ScrollViewerExtentTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature150ScrollViewerExtent" [
        test "overflowing content reports intrinsic content height and vertical max offset" {
            let rendered = Feature150ScrollFixtures.render (Feature150ScrollFixtures.scrollViewer "sv" (Feature150ScrollFixtures.rows 20))

            match Control.scrollViewport rendered "sv" with
            | Some viewport ->
                Expect.equal viewport.ExtentSource IntrinsicContentExtent "extent source is intrinsic"
                Expect.isTrue (viewport.ContentHeight > viewport.Viewport.Height) "content overflows vertically"
                Expect.isTrue (viewport.MaxVerticalOffset > 0.0) "vertical scroll range is positive"
            | None -> failtest "scroll viewport not found"
        }

        test "small content normalizes extent to viewport and reports no unnecessary overflow" {
            let small =
                TextBlock.create
                    [ Attr.width 40.0
                      Attr.height 20.0
                      TextBlock.text "small" ]

            let rendered = Feature150ScrollFixtures.render (Control.create "scroll-viewer" [ Attr.children [ small ] ] |> Control.withKey "sv")

            match Control.scrollViewport rendered "sv" with
            | Some viewport ->
                Expect.equal viewport.ContentHeight viewport.Viewport.Height "small content height normalized"
                Expect.equal viewport.ContentWidth viewport.Viewport.Width "small content width normalized"
                Expect.equal viewport.MaxVerticalOffset 0.0 "no vertical overflow"
                Expect.equal viewport.MaxHorizontalOffset 0.0 "no horizontal overflow"
            | None -> failtest "scroll viewport not found"
        }
    ]
