module SampleApps.Tests.ValidationTests

open Expecto
open FS.GG.UI.Controls
open SampleApps.Core.Productivity

/// FR-004 / SC-007: invalid form input is rejected without committing; inline edit commits to
/// both the displayed value and the data model; the empty model renders its empty-state.
/// Asserted on the pure reducer + the pure `validate` — no GL, no I/O.
[<Tests>]
let validationTests =
    testList "Validation" [
        test "validate rejects blank and over-long titles, accepts a trimmed one" {
            Expect.isError (Todo.validate "") "blank is rejected"
            Expect.isError (Todo.validate "   ") "whitespace-only is rejected"
            Expect.isError (Todo.validate (String.replicate 41 "x")) "over-long is rejected"
            Expect.equal (Todo.validate "  buy milk  ") (Result.Ok "buy milk") "valid input is trimmed and accepted"
        }

        test "an invalid draft is NOT committed and surfaces an error (FR-004)" {
            // type nothing, press Enter -> rejected.
            let model = Todo.update Todo.KeyEnter Todo.init
            Expect.isEmpty model.Items "no item committed on invalid input"
            Expect.equal model.Rejected 1 "the rejection is counted"
            Expect.isNonEmpty model.Errors "an error is surfaced"
        }

        test "a valid draft commits one item" {
            let typed = "abc" |> Seq.fold (fun m c -> Todo.update (Todo.KeyChar c) m) Todo.init
            let committed = Todo.update Todo.KeyEnter typed
            Expect.equal (List.length committed.Items) 1 "one item committed"
            Expect.equal committed.Items.[0].Title "abc" "the committed title matches the draft"
            Expect.equal committed.Draft "" "the draft is cleared after commit"
        }

        test "an inline edit commits to both the displayed value and the data model" {
            let withItem =
                Todo.init
                |> fun m -> "old" |> Seq.fold (fun m c -> Todo.update (Todo.KeyChar c) m) m
                |> Todo.update Todo.KeyEnter
            // begin edit, type a new char, commit.
            let edited =
                withItem
                |> Todo.update Todo.KeyEdit
                |> Todo.update (Todo.KeyChar 'X')
                |> Todo.update Todo.KeyEnter
            Expect.equal edited.Items.[0].Title "oldX" "the data model reflects the inline edit"
            Expect.isNone edited.Edit "the edit session closed on commit"
        }

        test "the empty model renders a non-crashing empty-state" {
            Expect.isEmpty Todo.init.Items "the seed model is empty"
            let control = Todo.view { Width = 800; Height = 600 } Todo.init
            Expect.isGreaterThan (Control.count control) 0 "the empty-state view renders nodes (no crash)"
        }
    ]
