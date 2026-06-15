module ControlsTextInputTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let textInputTests =
    testList "Controls text input MVU boundary" [
        test "text input init and update keep committed value model-owned" {
            let model, effects = TextInput.init "name" SingleLine "Ada"
            Expect.isEmpty effects "init has no I/O effects"
            Expect.equal model.CommittedText "Ada" "committed text starts from model value"

            let edited, editEffects = TextInput.update (InsertText " Lovelace") model
            Expect.isEmpty editEffects "editing is pure"
            Expect.equal edited.DraftText "Ada Lovelace" "draft text changes"
            Expect.equal edited.CommittedText "Ada" "committed value is unchanged until commit"

            let committed, commitEffects = TextInput.update Commit edited
            Expect.equal committed.CommittedText "Ada Lovelace" "commit updates committed text"
            Expect.equal commitEffects [ CommitText("name", "Ada Lovelace") ] "commit emits a host effect"
        }

        test "clipboard and composition are represented as effects or diagnostics" {
            let model, _ = TextInput.init "notes" MultiLine "line 1"
            let _, effects = TextInput.update RequestClipboardPaste model
            Expect.equal effects [ RequestClipboardText "notes" ] "clipboard request is an explicit effect"

            let composing, _ = TextInput.update (CompositionStarted "a") model
            let diagnostics = TextInput.diagnostics composing
            Expect.exists diagnostics (fun item -> item.Code = UnsupportedEnvironment) "IME composition without host support reports environment diagnostic"
        }
    ]
