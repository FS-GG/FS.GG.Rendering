module SecondAntShowcase.Tests.ReviewFindingTests

open Expecto
open SecondAntShowcase.Core

[<Tests>]
let reviewFindingTests =
    testList "ReviewFindings" [
        test "finding lifecycle moves open -> fixed -> reviewed -> closed" {
            let opened =
                ReviewFindings.create
                    "F-001"
                    [ "buttons-antLight-preferred" ]
                    ReviewFindings.Spacing
                    ReviewFindings.Blocking
                    "Button spacing drifts from Ant rhythm"
                    "8px rhythm"
                    "uneven gaps"
            Expect.equal opened.Status ReviewFindings.Open "new finding is open"

            let fixed' = opened |> ReviewFindings.markFixed "commit:abc123"
            Expect.equal fixed'.Status ReviewFindings.Fixed "fixed waits for review"

            let reviewed = fixed' |> ReviewFindings.markReviewed "run-1"
            Expect.equal reviewed.Status ReviewFindings.Reviewed "reviewed waits for close"

            let closed = reviewed |> ReviewFindings.close
            Expect.equal closed.Status ReviewFindings.Closed "reviewed finding can close"
        }

        test "unresolved gate blocks open, fixed, and reviewed blocking findings" {
            let openFinding = ReviewFindings.create "F-open" [ "a" ] ReviewFindings.Palette ReviewFindings.Warning "open" "expected" "actual"
            let fixedFinding = ReviewFindings.create "F-fixed" [ "b" ] ReviewFindings.Overlap ReviewFindings.Warning "fixed" "expected" "actual" |> ReviewFindings.markFixed "commit"
            let reviewedBlocking = ReviewFindings.create "F-reviewed" [ "c" ] ReviewFindings.Clipping ReviewFindings.Blocking "reviewed" "expected" "actual" |> ReviewFindings.markFixed "commit" |> ReviewFindings.markReviewed "run"
            let closed = ReviewFindings.create "F-closed" [ "d" ] ReviewFindings.Alignment ReviewFindings.Warning "closed" "expected" "actual" |> ReviewFindings.markFixed "commit" |> ReviewFindings.markReviewed "run" |> ReviewFindings.close

            let unresolvedIds =
                ReviewFindings.unresolved [ openFinding; fixedFinding; reviewedBlocking; closed ]
                |> List.map _.FindingId

            Expect.equal unresolvedIds [ "F-open"; "F-fixed"; "F-reviewed" ] "only unresolved lifecycle states remain blocking"
        }

        test "validation reports malformed findings, missing target classifications, and unresolved ids" {
            let malformed = ReviewFindings.create "" [] ReviewFindings.State ReviewFindings.Warning "bad" "expected" "actual"
            let openFinding = ReviewFindings.create "F-002" [ "target-a" ] ReviewFindings.AntConformance ReviewFindings.Warning "open" "expected" "actual"
            let result = ReviewFindings.validate [ "target-a"; "target-b" ] [ malformed; openFinding ]

            Expect.equal result.MalformedFindingIds [ "" ] "malformed finding id reported"
            Expect.equal result.MissingClassificationTargetIds [ "target-b" ] "missing target classification reported"
            Expect.equal result.UnresolvedFindingIds [ ""; "F-002" ] "unresolved findings reported"
        }
    ]
