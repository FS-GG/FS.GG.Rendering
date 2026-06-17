module Feature144OverlayKeyboardEvidenceTests

open Expecto
open FS.GG.UI.KeyboardInput

[<Tests>]
let tests =
    testList "Feature144 overlay keyboard evidence" [
        test "overlay control keys normalize to stable ids" {
            let cases =
                [ "Escape", Escape, "Escape"
                  "Enter", Enter, "Enter"
                  "Space", Space, "Space"
                  "ArrowLeft", ArrowLeft, "ArrowLeft"
                  "ArrowRight", ArrowRight, "ArrowRight"
                  "ArrowUp", ArrowUp, "ArrowUp"
                  "ArrowDown", ArrowDown, "ArrowDown" ]

            for raw, expected, keyId in cases do
                let key, isDown = ViewerKeyboard.normalizeEvent { RawKey = raw; Direction = ViewerKeyDirection.KeyDown }

                Expect.equal key expected $"{raw} normalizes"
                Expect.isTrue isDown $"{raw} preserves key-down"
                Expect.equal (ViewerKeyboard.toKeyId key) keyId $"{raw} key id stable"
        }

        test "Shift+Tab preserves traversal modifier" {
            let key, isDown, modifiers =
                ViewerKeyboard.normalizeEventWithModifiers { RawKey = "Shift+Tab"; Direction = ViewerKeyDirection.KeyDown }

            Expect.equal key (Unknown "Tab") "Tab base key is preserved"
            Expect.isTrue isDown "event is key-down"
            Expect.isTrue modifiers.Shift "shift modifier is preserved"
        }
    ]
