module SecondAntShowcase.Tests.FeedbackTests

open Expecto
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

/// Feedback capture: a page-tagged entry is saved on submit (pure), blank drafts are
/// no-ops, and entries round-trip through the persistence encoding the App edge writes.
[<Tests>]
let feedbackTests =
    testList "Feedback" [
        test "submitting a non-blank draft saves a page-tagged entry and clears the draft" {
            let m0 = { Host.initModel with CurrentPage = "buttons"; FeedbackDraft = "  the button needs a loading state  " }
            let m1 = Model.update FeedbackSubmitted m0
            Expect.equal m1.FeedbackDraft "" "draft cleared after submit"
            Expect.equal (List.length m1.Feedback) 1 "one entry saved"
            let e = List.head m1.Feedback
            Expect.equal e.PageId "buttons" "entry tagged with the current page"
            Expect.equal e.Text "the button needs a loading state" "entry text trimmed"
        }

        test "a blank / whitespace-only draft is a no-op" {
            let m0 = { Host.initModel with FeedbackDraft = "   " }
            let m1 = Model.update FeedbackSubmitted m0
            Expect.isEmpty m1.Feedback "nothing saved for a blank draft"
        }

        test "feedback accumulates newest-first and stays tagged per page" {
            let steps =
                [ NavigateTo "buttons"; FeedbackChanged "a"; FeedbackSubmitted
                  NavigateTo "overlays"; FeedbackChanged "b"; FeedbackSubmitted ]
            let m = List.fold (fun acc msg -> Model.update msg acc) Host.initModel steps
            Expect.equal (m.Feedback |> List.map (fun e -> e.PageId, e.Text)) [ "overlays", "b"; "buttons", "a" ] "newest-first, per-page tags"
        }

        test "feedback lines round-trip through the persistence encoding (incl. tab/newline)" {
            let entry = { PageId = "feedback-status"; Text = "line1\nline2\tcol" }
            let decoded = Model.decodeFeedbackLine (Model.encodeFeedbackLine entry)
            Expect.equal decoded (Some entry) "encode→decode is identity even with tab/newline in the text"
        }

        test "a malformed stored line decodes to None" {
            Expect.isNone (Model.decodeFeedbackLine "no-tab-here") "no tab ⇒ malformed"
            Expect.isNone (Model.decodeFeedbackLine "\ttext") "empty page id ⇒ malformed"
        }
    ]
