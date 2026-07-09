module Issue184PointerButtonTests

open System
open Expecto
open Silk.NET.Input
open FS.GG.UI.SkiaViewer.Host

// Issue #184 — `toViewerButton` sent every button past the first three to `PrimaryButton`, so a
// back/forward/thumb press fired whatever the product bound to a left click. The mapping is now
// total over Silk's `MouseButton`: the three the contract carries map onto it, and every other
// button — including Silk's `Unknown` — is dropped with a diagnostic rather than coerced.
//
// `mapPointerButton` takes the raw underlying code, so these tests pass `int button` exactly as
// the live handler in `attachInputEventMapping` does.

let private allSilkButtons =
    Enum.GetValues(typeof<MouseButton>) |> Seq.cast<MouseButton> |> Seq.toList

let private carried =
    [ MouseButton.Left, PrimaryButton
      MouseButton.Right, SecondaryButton
      MouseButton.Middle, MiddleButton ]

[<Tests>]
let issue184PointerButtonTests =
    testList "Issue 184 pointer button mapping" [

        test "the three contract buttons map onto their host identity" {
            for button, expected in carried do
                Expect.equal
                    (GlHost.mapPointerButton (int button))
                    (Some expected)
                    $"{button} maps to {expected}"
        }

        // The acceptance criterion, stated directly: a Button4 press must not reach the product as
        // a primary click. Before the fix this returned `PrimaryButton`.
        test "Button4 does not produce a primary click" {
            Expect.equal (GlHost.mapPointerButton (int MouseButton.Button4)) None "Button4 is dropped, not coerced"
        }

        // Exhaustive over the whole enum, so a Silk upgrade that adds a button cannot quietly
        // reintroduce the coercion: anything not explicitly carried must be `None`.
        test "every button outside the contract is dropped, never coerced" {
            let carriedButtons = carried |> List.map fst

            for button in allSilkButtons do
                if not (List.contains button carriedButtons) then
                    Expect.equal (GlHost.mapPointerButton (int button)) None $"{button} has no host representation"
        }

        // Silk's sentinel is -1, which the old wildcard also read as a left click.
        test "Silk's Unknown button is dropped" {
            Expect.equal (GlHost.mapPointerButton (int MouseButton.Unknown)) None "Unknown is dropped"
        }

        // A code Silk never emits still must not coerce — the mapping is closed by construction,
        // not by matching a fixed set of known-bad values.
        test "an unrecognized raw code is dropped" {
            Expect.equal (GlHost.mapPointerButton 4242) None "an out-of-range code is dropped"
        }

        // Dropping silently would be the other half of the bug: the product must be told.
        test "the dropped button is reported as a non-blocking Input diagnostic" {
            let diagnostic = Diagnostics.unmappedPointerButton (string MouseButton.Button4)

            Expect.equal diagnostic.Stage DiagnosticStage.Input "an input event carries the Input stage"
            Expect.equal diagnostic.Severity DiagnosticSeverity.Warning "a dropped button never blocks the run"
            Expect.stringContains diagnostic.Message "Button4" "the diagnostic names the button that was dropped"
        }
    ]
