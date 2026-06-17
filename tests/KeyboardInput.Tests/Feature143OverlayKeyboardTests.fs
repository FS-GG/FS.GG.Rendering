module Feature143OverlayKeyboardTests

open Expecto
open FS.GG.UI.KeyboardInput

[<Tests>]
let tests =
    testList "Feature143 overlay keyboard handoff" [
        test "Escape normalizes to the overlay dismissal key id" {
            let key, isDown = ViewerKeyboard.normalizeEvent { RawKey = "Escape"; Direction = ViewerKeyDirection.KeyDown }

            Expect.equal key Escape "Escape key normalized"
            Expect.isTrue isDown "key-down state preserved"
            Expect.equal (ViewerKeyboard.toKeyId key) "Escape" "overlay can consume Escape by stable key id"
        }

        test "Shift+Tab preserves the traversal modifier for overlay focus cycling" {
            let key, isDown, modifiers =
                ViewerKeyboard.normalizeEventWithModifiers { RawKey = "Shift+Tab"; Direction = ViewerKeyDirection.KeyDown }

            Expect.equal key (Unknown "Tab") "Tab remains an explicit host key until a Tab DU exists"
            Expect.isTrue isDown "key-down state preserved"
            Expect.isTrue modifiers.Shift "Shift modifier preserved for reverse traversal"
        }
    ]
