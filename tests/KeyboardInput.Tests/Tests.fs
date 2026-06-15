module KeyboardInputCapabilityTests

open Microsoft.FSharp.Reflection
open Expecto
open FS.GG.UI.KeyboardInput

let recordFields<'T> =
    FSharpType.GetRecordFields(typeof<'T>)
    |> Array.map _.Name
    |> Set.ofArray

let unionCases<'T> =
    FSharpType.GetUnionCases(typeof<'T>)
    |> Array.map _.Name
    |> Set.ofArray

[<Tests>]
let tests =
    testList "Keyboard input MVU contract" [
        test "key down emits command and key-state effects" {
            let model, _ = Keyboard.init [ { Key = "K"; Command = "open" } ]
            let next, effects = Keyboard.update (KeyDown "K") model
            Expect.equal next.LastCommand (Some "open") "last command is stored"
            Expect.equal effects [ KeyStateChanged [ "K" ]; CommandResolved "open" ] "effects are emitted"
        }

        test "runtime model exposes layout modes pending sequence diagnostics and state display state" {
            let fields = recordFields<KeyboardModel>

            [ "PressedKeys"
              "ActiveLayout"
              "ActiveModeStack"
              "PersistentModeState"
              "PendingSequence"
              "Diagnostics"
              "RecentEffects"
              "StateDisplay" ]
            |> List.iter (fun field ->
                Expect.isTrue (Set.contains field fields) $"KeyboardModel exposes {field}")
        }

        test "messages and effects cover focus recovery mode behavior and interpreter data" {
            let messages = unionCases<KeyboardMsg>
            let effects = unionCases<KeyboardEffect>

            [ "KeyDown"
              "KeyUp"
              "FocusLost"
              "Reset"
              "SetActiveLayout"
              "PushTemporaryMode"
              "PopTemporaryMode"
              "SetPersistentMode"
              "ResolvePendingSequence" ]
            |> List.iter (fun caseName ->
                Expect.isTrue (Set.contains caseName messages) $"KeyboardMsg exposes {caseName}")

            [ "CommandResolved"
              "KeyStateChanged"
              "LayoutChanged"
              "ModeChanged"
              "PendingSequenceChanged"
              "StateDisplayChanged"
              "ReportKeyboardDiagnostic"
              "RequestHostKeyCapture" ]
            |> List.iter (fun caseName ->
                Expect.isTrue (Set.contains caseName effects) $"KeyboardEffect exposes {caseName}")
        }

        test "init emits inspectable state-display evidence for interpreters" {
            let _, effects = Keyboard.init []

            Expect.exists
                (effects |> List.map string)
                (fun effect -> effect.Contains("StateDisplay"))
                "initialization publishes state-display data through an effect"
        }

        test "focus loss clears temporary state preserves persistent modes and reports diagnostics" {
            let model, _ = Keyboard.init []
            let withMode, _ = Keyboard.update (SetPersistentMode("layout", "qwerty")) model
            let withTemporary, _ = Keyboard.update (PushTemporaryMode "symbols") withMode
            let withKey, _ = Keyboard.update (KeyDown "K") withTemporary
            let recovered, effects = Keyboard.update FocusLost withKey

            Expect.isEmpty recovered.PressedKeys "focus loss clears pressed keys"
            Expect.isEmpty recovered.ActiveModeStack "focus loss clears temporary modes"
            Expect.equal recovered.PersistentModeState["layout"] "qwerty" "persistent mode state survives focus loss"
            Expect.exists effects (function ReportKeyboardDiagnostic diagnostic when diagnostic.Code = "FocusLostRecovered" -> true | _ -> false) "focus loss reports recovery diagnostic"
        }

        test "viewer keyboard normalization exposes public stable key values" {
            Expect.equal (ViewerKeyboard.normalize "Left") ArrowLeft "left normalizes"
            Expect.equal (ViewerKeyboard.normalize "ArrowLeft") ArrowLeft "alternate left normalizes"
            Expect.equal (ViewerKeyboard.normalize "Right") ArrowRight "right normalizes"
            Expect.equal (ViewerKeyboard.normalize "ArrowRight") ArrowRight "alternate right normalizes"
            Expect.equal (ViewerKeyboard.normalize "Up") ArrowUp "up normalizes"
            Expect.equal (ViewerKeyboard.normalize "ArrowUp") ArrowUp "alternate up normalizes"
            Expect.equal (ViewerKeyboard.normalize "Down") ArrowDown "down normalizes"
            Expect.equal (ViewerKeyboard.normalize "ArrowDown") ArrowDown "alternate down normalizes"
            Expect.equal (ViewerKeyboard.normalize "Return") Enter "return normalizes to enter"
            Expect.equal (ViewerKeyboard.normalize "Enter") Enter "enter normalizes"
            Expect.equal (ViewerKeyboard.normalize " ") Space "space character normalizes"
            Expect.equal (ViewerKeyboard.normalize "Spacebar") Space "spacebar normalizes"
            Expect.equal (ViewerKeyboard.normalize "Esc") Escape "escape alternate normalizes"
            Expect.equal (ViewerKeyboard.normalize "Back") Backspace "backspace alternate normalizes"
            Expect.equal (ViewerKeyboard.normalize "a") (Letter 'A') "letters normalize to uppercase"
            Expect.equal (ViewerKeyboard.normalize "7") (Digit 7) "digits normalize"
            Expect.equal (ViewerKeyboard.normalize "F12") (Function 12) "function keys normalize"
            Expect.equal (ViewerKeyboard.normalize "VendorKey") (Unknown "VendorKey") "unknown raw key is preserved"
        }

        test "viewer keyboard events preserve down and up direction for interpreters" {
            let downKey, isDown =
                ViewerKeyboard.normalizeEvent { RawKey = "Escape"; Direction = ViewerKeyDirection.KeyDown }

            let upKey, isStillDown =
                ViewerKeyboard.normalizeEvent { RawKey = "Escape"; Direction = ViewerKeyDirection.KeyUp }

            Expect.equal downKey Escape "key-down event normalizes key"
            Expect.isTrue isDown "key-down event is marked down"
            Expect.equal upKey Escape "key-up event normalizes key"
            Expect.isFalse isStillDown "key-up event is marked not down"
        }
    ]

[<Tests>]
let feature085NormalizeFamilies =
    // Feature 085 (US3, SC-003, FR-007/FR-008) — toolkit key-name families map onto the existing
    // Digit/Letter cases; unrecognized names stay Unknown raw (totality, no regression).
    testList "Feature 085 normalize key-name families (US3)" [
        test "Number5/Digit5/Keypad5/Key5 all normalize to Digit 5 (FR-007)" {
            for spelling in [ "Number5"; "Digit5"; "Keypad5"; "Key5" ] do
                Expect.equal (ViewerKeyboard.normalize spelling) (Digit 5) (sprintf "%s -> Digit 5" spelling)
        }
        test "the families are case-insensitive" {
            for spelling in [ "number5"; "DIGIT5"; "KeyPad5"; "kEy5" ] do
                Expect.equal (ViewerKeyboard.normalize spelling) (Digit 5) (sprintf "%s -> Digit 5 (case-insensitive)" spelling)
        }
        test "KeyL normalizes to Letter 'L' (case-insensitive, FR-007)" {
            Expect.equal (ViewerKeyboard.normalize "KeyL") (Letter 'L') "KeyL -> Letter 'L'"
            Expect.equal (ViewerKeyboard.normalize "keyl") (Letter 'L') "keyl -> Letter 'L' (case-insensitive)"
        }
        test "unrecognized names still normalize to Unknown raw (totality, FR-008)" {
            Expect.equal (ViewerKeyboard.normalize "Totally-Unknown") (Unknown "Totally-Unknown") "unknown stays Unknown raw"
            Expect.equal (ViewerKeyboard.normalize "Number") (Unknown "Number") "prefix-only is not a digit family"
            Expect.equal (ViewerKeyboard.normalize "KeyLong") (Unknown "KeyLong") "multi-char Key suffix is not a single key"
        }
        test "existing recognized names are unchanged (no regression)" {
            Expect.equal (ViewerKeyboard.normalize "Left") ArrowLeft "arrows unchanged"
            Expect.equal (ViewerKeyboard.normalize "F5") (Function 5) "function keys unchanged"
            Expect.equal (ViewerKeyboard.normalize "L") (Letter 'L') "bare single letter unchanged"
            Expect.equal (ViewerKeyboard.normalize "5") (Digit 5) "bare single digit unchanged"
        }
    ]
