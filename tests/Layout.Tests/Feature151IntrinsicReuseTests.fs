module Feature151IntrinsicReuseTests

open Expecto
open FS.GG.UI.Layout

let private queryFor axis cross (node: LayoutNode) =
    Layout.intrinsicQuery node.Id axis cross (Layout.layoutInputKey node) DiagnosticProbe

[<Tests>]
let tests =
    testList "Feature151IntrinsicReuse" [
        test "equivalent intrinsic queries produce stable identities and accepted results" {
            let node = Feature151CorpusFixtures.dynamicContent 24.0
            let first = queryFor IntrinsicMaxHeight (Some 120.0) node
            let second = queryFor IntrinsicMaxHeight (Some 120.0) node
            let result = Layout.evaluateIntrinsic first node

            Expect.equal second.QueryIdentity first.QueryIdentity "query identity"
            Expect.isTrue result.Accepted "accepted intrinsic result"
            Expect.isNonEmpty result.Dependencies "child dependencies"
        }

        test "axis and cross-axis changes alter intrinsic query identity" {
            let node = Feature151CorpusFixtures.dynamicContent 24.0
            let baseline = queryFor IntrinsicMaxHeight (Some 120.0) node
            let axisChanged = queryFor IntrinsicMaxWidth (Some 120.0) node
            let crossChanged = queryFor IntrinsicMaxHeight (Some 160.0) node

            Expect.notEqual axisChanged.QueryIdentity baseline.QueryIdentity "axis identity"
            Expect.notEqual crossChanged.QueryIdentity baseline.QueryIdentity "cross-axis identity"
        }

        test "dynamic content changes intrinsic dependency identity" {
            let baseline = Feature151CorpusFixtures.dynamicContent 24.0
            let changed = Feature151CorpusFixtures.dynamicContent 48.0
            let baselineQuery = queryFor IntrinsicMaxHeight (Some 120.0) baseline
            let changedQuery = queryFor IntrinsicMaxHeight (Some 120.0) changed

            Expect.notEqual changedQuery.QueryIdentity baselineQuery.QueryIdentity "layout input key participates in query identity"

            let baselineResult = Layout.evaluateIntrinsic baselineQuery baseline
            let changedResult = Layout.evaluateIntrinsic changedQuery changed

            Expect.notEqual changedResult.Dependencies baselineResult.Dependencies "intrinsic dependency result identities"
        }

        test "cacheEntry records intrinsic dependency keys and revision" {
            let node = Feature151CorpusFixtures.dynamicContent 24.0
            let query = queryFor IntrinsicMaxHeight (Some 120.0) node
            let result = Layout.evaluateIntrinsic query node
            let entry = Layout.cacheEntry IntrinsicLayoutEntry node.Id query.QueryIdentity query.LayoutInputKey (result.Dependencies |> List.map _.ResultIdentity) $"{result.Size:R}"

            Expect.equal entry.EntryKind IntrinsicLayoutEntry "kind"
            Expect.equal entry.Revision 150 "revision"
            Expect.stringContains entry.EntryId query.QueryIdentity "query participates in cache identity"
        }
    ]
