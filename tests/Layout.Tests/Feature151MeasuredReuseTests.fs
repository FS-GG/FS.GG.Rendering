module Feature151MeasuredReuseTests

open Expecto
open FS.GG.UI.Layout

let private measuredEntry item =
    let measured = Layout.measureProtocol (Feature151CorpusFixtures.constraintsFor item) item.Root
    measured.CacheEntryId

[<Tests>]
let tests =
    testList "Feature151MeasuredReuse" [
        test "equivalent measured runs produce the same dependency-backed cache identity" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "measured-leaves")
            let first = measuredEntry item
            let second = measuredEntry item

            Expect.equal second first "warm equivalent cache entry"
        }

        test "constraint changes alter measured cache identity" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "measured-leaves")
            let baseline = measuredEntry item
            let changed = measuredEntry { item with Available = Feature151CorpusFixtures.available 220.0 160.0 }

            Expect.notEqual changed baseline "constraint identity changes"
        }

        test "content and measurement behavior changes alter measured cache identity" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "measured-leaves")
            let baseline = measuredEntry item
            let changedRoot = item.ChangedRoot |> Option.defaultValue item.Root
            let changed = measuredEntry { item with Root = changedRoot }

            Expect.notEqual changed baseline "layout input key changes"
        }

        test "child reorder changes ordered dependency keys" {
            let baseline = Feature151CorpusFixtures.childOrder false
            let reordered = Feature151CorpusFixtures.childOrder true

            Expect.notEqual (Layout.layoutInputKey reordered) (Layout.layoutInputKey baseline) "ordered child keys"

            let constraints = Layout.constraintsFromAvailable Viewport (Feature151CorpusFixtures.available 180.0 60.0)
            let first = Layout.measureProtocol constraints baseline
            let second = Layout.measureProtocol constraints reordered

            Expect.notEqual second.CacheEntryId first.CacheEntryId "ordered child dependencies alter cache entry"
        }
    ]
