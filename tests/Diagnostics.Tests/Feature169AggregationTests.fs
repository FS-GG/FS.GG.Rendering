module Feature169AggregationTests

open Expecto

[<Tests>]
let tests =
    testList "Feature169 aggregation" [
        test "Synthetic repeated backend cost diagnostics aggregate to one occurrence group" {
            let summary = Feature169Fixtures.repeatedBackendCost 100 |> Feature169Fixtures.summarize

            Expect.equal summary.Groups.Length 1 "one repeated group"
            Expect.equal summary.Groups.Head.OccurrenceCount 100 "occurrence count"
            Expect.contains summary.Groups.Head.FirstOccurrence.Details ("frame", "1") "first context retained"
            Expect.contains summary.Groups.Head.LastOccurrence.Details ("frame", "100") "last context retained"
        }
    ]
