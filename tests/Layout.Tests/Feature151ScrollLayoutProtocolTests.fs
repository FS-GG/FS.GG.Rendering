module Feature151ScrollLayoutProtocolTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature151ScrollLayoutProtocol" [
        test "ScrollViewer content extent comes from intrinsic content evidence" {
            let content = Feature151CorpusFixtures.dynamicContent 72.0
            let extent = Layout.contentExtent 120.0 40.0 (Some content)

            Expect.equal extent.ExtentSource IntrinsicResult "intrinsic extent"
            Expect.isGreaterThan extent.ContentHeight 40.0 "natural content height exceeds viewport"
            Expect.isGreaterThan extent.MaxVerticalOffset 0.0 "vertical max offset"
            Expect.hasLength extent.DependencyKeys 2 "width and height intrinsic query identities"
        }

        test "content extent dependency keys change when content changes" {
            let baseline = Layout.contentExtent 120.0 40.0 (Some(Feature151CorpusFixtures.dynamicContent 24.0))
            let changed = Layout.contentExtent 120.0 40.0 (Some(Feature151CorpusFixtures.dynamicContent 72.0))

            Expect.notEqual changed.DependencyKeys baseline.DependencyKeys "dynamic content changes intrinsic dependency identity"
            Expect.isGreaterThanOrEqual changed.ContentHeight baseline.ContentHeight "changed content keeps or increases extent"
        }

        test "empty content records an explicit empty source and zero offset" {
            let extent = Layout.contentExtent 120.0 40.0 None

            Expect.equal extent.ExtentSource EmptyContent "empty source"
            Expect.equal extent.ContentWidth 120.0 "viewport width"
            Expect.equal extent.ContentHeight 40.0 "viewport height"
            Expect.equal extent.MaxHorizontalOffset 0.0 "horizontal max"
            Expect.equal extent.MaxVerticalOffset 0.0 "vertical max"
        }
    ]
