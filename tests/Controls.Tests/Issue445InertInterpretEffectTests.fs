module Controls.Tests.Issue445InertInterpretEffectTests

// Issue #445 (epic .github#416, the silent no-op family): `TextInput.interpretEffect` is a total
// no-op — every arm returns None — and its `[<Obsolete>]` message says so outright. That message is
// a factual claim about behaviour, so it needs a test: if someone later "implements" one arm and
// leaves the deprecation text standing, the surface starts lying again in exactly the way this issue
// exists to stop.
//
// The claim is structural, not accidental: `TextInputEffect` has no case that carries a host RESULT.
// RequestClipboardText asks the host for text; CommitText and ReportTextInputDiagnostic notify it.
// All three point OUT. Nothing comes back in, so no TextInputMsg can come out — which is why the
// function is being retired rather than fixed, and why a product must feed a fulfilled clipboard
// read back itself as `ClipboardTextReceived`.

// FS0044 (obsolete member) is an ERROR here (TreatWarningsAsErrors). Pinning a deprecated function's
// behaviour necessarily calls it, so the suppression is the point of this file, not an oversight.
#nowarn "44"

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Issue 445 TextInput.interpretEffect is inert, and says so" [

        // Every case, exhaustively — this is the whole DU, so "always None" is proved, not sampled.
        test "interpretEffect returns None for every TextInputEffect case (#445)" {
            let diagnostic = Diagnostics.unsupportedEnvironment "text-input" "platform IME composition host callback"

            let everyEffect =
                [ RequestClipboardText "field-1"
                  CommitText("field-1", "committed text")
                  ReportTextInputDiagnostic diagnostic ]

            for effect in everyEffect do
                Expect.isNone
                    (TextInput.interpretEffect effect)
                    (sprintf
                        "interpretEffect %A must return None — no TextInputEffect carries a host result to map back into a Msg"
                        effect)
        }

        // The honest route the deprecation message points a product at: the host fulfils the
        // clipboard request out-of-band and dispatches the text back as a Msg. `update` handles it;
        // `interpretEffect` never had any part to play.
        test "a fulfilled clipboard read reaches the model via ClipboardTextReceived, not interpretEffect (#445)" {
            let model, _ = TextInput.init "field-1" SingleLine ""

            // update raises the outbound request...
            let requesting, effects = TextInput.update RequestClipboardPaste model
            Expect.equal effects [ RequestClipboardText "field-1" ] "paste raises an outbound clipboard request"

            // ...and interpretEffect cannot turn it back into anything.
            Expect.isNone (TextInput.interpretEffect effects.Head) "the request maps to no Msg — the host has not answered yet"

            // The host answers by dispatching a Msg the product owns. THAT is what lands the text.
            let pasted, _ = TextInput.update (ClipboardTextReceived "pasted") requesting
            Expect.equal pasted.DraftText "pasted" "the fulfilled read reaches the model as ClipboardTextReceived"
        }
    ]
