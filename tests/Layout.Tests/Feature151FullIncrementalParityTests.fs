module Feature151FullIncrementalParityTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature151FullIncrementalParity" [
        test "cold and warm incremental runs match full layout for accepted corpus cases" {
            for item in Feature151CorpusFixtures.acceptedCases do
                let full = Layout.evaluate item.Available item.Root
                let cold = Layout.evaluateIncremental full [] item.Available item.Root
                let warm = Layout.evaluateIncremental cold [] item.Available item.Root

                Expect.equal cold.Bounds full.Bounds $"{item.CaseId}: cold bounds"
                Expect.equal cold.Diagnostics full.Diagnostics $"{item.CaseId}: cold diagnostics"
                Expect.equal warm.Bounds full.Bounds $"{item.CaseId}: warm bounds"
                Expect.equal warm.Diagnostics full.Diagnostics $"{item.CaseId}: warm diagnostics"
        }

        test "changed-input incremental runs match a fresh full evaluation and reject stale geometry" {
            let changedCases =
                Feature151CorpusFixtures.acceptedCases
                |> List.choose (fun item -> item.ChangedRoot |> Option.map (fun changed -> item, changed))

            for item, changedRoot in changedCases do
                let previous = Layout.evaluate item.Available item.Root
                let incremental = Layout.evaluateIncremental previous item.ChangedNodeIds item.Available changedRoot
                let fullChanged = Layout.evaluate item.Available changedRoot

                Expect.equal incremental.Bounds fullChanged.Bounds $"{item.CaseId}: changed bounds"
                Expect.equal incremental.Diagnostics fullChanged.Diagnostics $"{item.CaseId}: changed diagnostics"
                Expect.notEqual (Layout.layoutInputKey changedRoot) (Layout.layoutInputKey item.Root) $"{item.CaseId}: changed input key"
        }

        test "changed content produces reviewer-visible invalidation evidence" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "dynamic-content")
            let previous = Layout.evaluate item.Available item.Root
            let changedRoot = item.ChangedRoot |> Option.defaultValue item.Root
            let incremental = Layout.evaluateIncremental previous item.ChangedNodeIds item.Available changedRoot

            Expect.isNonEmpty incremental.Invalidated "changed pass reports invalidated participants"
            Expect.contains (Set.ofList incremental.Invalidated) "dynamic-a" "changed measured participant invalidated"
        }
    ]
