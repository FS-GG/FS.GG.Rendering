module Feature108ModifierTests

// Feature 108 (US5) — `ViewerKeyboard.normalizeEventWithModifiers` strips `Ctrl+/Alt+/Shift+/Meta+`
// prefixes (any order, case-insensitive) into a base `ViewerKey` + `KeyModifiers`, while an
// unmodified key is byte-identical to `normalizeEvent` (zero silent loss, SC-009, FR-016). Red on
// the pre-108 build (no `KeyModifiers`).

open Expecto
open FS.Skia.UI.KeyboardInput

let private ev raw : ViewerKeyEvent =
    { RawKey = raw
      Direction = ViewerKeyDirection.KeyDown }

[<Tests>]
let tests =
    testList "Feature 108 modifier-aware key boundary (US5, FR-016, SC-009)" [
        test "an unmodified key parses to noModifiers and the same ViewerKey as normalizeEvent" {
            let key, down, mods = ViewerKeyboard.normalizeEventWithModifiers (ev "L")
            let baseKey, baseDown = ViewerKeyboard.normalizeEvent (ev "L")
            Expect.equal key baseKey "same base key as normalizeEvent (byte-identical routing)"
            Expect.equal down baseDown "same down/up flag"
            Expect.equal mods ViewerKeyboard.noModifiers "no modifiers for a plain key"
        }

        test "Ctrl+L recovers Ctrl + base Letter L (FR-016)" {
            let key, down, mods = ViewerKeyboard.normalizeEventWithModifiers (ev "Ctrl+L")
            Expect.equal key (Letter 'L') "base key normalized off the prefix"
            Expect.isTrue down "key-down flag preserved"
            Expect.isTrue mods.Ctrl "Ctrl recovered"
            Expect.isFalse mods.Alt "Alt not held"
            Expect.isFalse mods.Shift "Shift not held"
            Expect.isFalse mods.Meta "Meta not held"
        }

        test "modifiers parse in any order, case-insensitive, with the base key intact (SC-009)" {
            let key, _, mods = ViewerKeyboard.normalizeEventWithModifiers (ev "shift+CTRL+alt+meta+ArrowLeft")
            Expect.isTrue (mods.Ctrl && mods.Alt && mods.Shift && mods.Meta) "all four modifiers recovered"
            Expect.equal key ArrowLeft "base ArrowLeft recovered after stripping every modifier"
        }

        test "Cmd / Win / Super alias to Meta" {
            let _, _, m1 = ViewerKeyboard.normalizeEventWithModifiers (ev "Cmd+S")
            let _, _, m2 = ViewerKeyboard.normalizeEventWithModifiers (ev "Win+S")
            let _, _, m3 = ViewerKeyboard.normalizeEventWithModifiers (ev "Super+S")
            Expect.isTrue (m1.Meta && m2.Meta && m3.Meta) "Cmd/Win/Super all recover Meta"
        }
    ]
