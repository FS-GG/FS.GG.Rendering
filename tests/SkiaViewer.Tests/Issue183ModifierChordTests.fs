module Issue183ModifierChordTests

// Issue 183 — the modifier-chord seam was wired end to end but nothing ever produced a chord.
//
// Silk reports `Ctrl+L` as two independent events (`ControlLeft`, then `L`) and its key callback
// carries no modifier state, so `chordFallthrough`'s premise — "a chord like `Ctrl+L` survives the
// backend as `ViewerKey.Unknown \"Ctrl+L\"`" — was false: `MapKeyChord` could not fire from a real
// window, and every test in the repo stubbed it to `None`, so CI never saw it.
//
// `KeyChord.rawKey` closes that gap by sampling the held modifiers off the firing keyboard. These
// tests drive it with a fake `IKeyboard` (reached via InternalsVisibleTo) and then push its output
// through the exact two calls `chordFallthrough` makes, with a NON-`None` `MapKeyChord`.

open System
open System.Collections.Generic
open Expecto
open Silk.NET.Input
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer.Host

/// A keyboard that reports exactly the keys it was handed as held. Only `IsKeyPressed` is exercised.
type private FakeKeyboard(held: Key list) =
    let held = Set.ofList held
    let keyDown = DelegateEvent<Action<IKeyboard, Key, int>>()
    let keyUp = DelegateEvent<Action<IKeyboard, Key, int>>()
    let keyChar = DelegateEvent<Action<IKeyboard, char>>()

    interface IKeyboard with
        member _.Name = "fake-keyboard"
        member _.Index = 0
        member _.IsConnected = true
        member _.SupportedKeys = List<Key>() :> IReadOnlyList<Key>

        member _.ClipboardText
            with get () = ""
            and set (_: string) = ()

        member _.IsKeyPressed(key: Key) = held.Contains key
        member _.IsScancodePressed(_: int) = false
        member _.BeginInput() = ()
        member _.EndInput() = ()

        [<CLIEvent>]
        member _.KeyDown = keyDown.Publish

        [<CLIEvent>]
        member _.KeyUp = keyUp.Publish

        [<CLIEvent>]
        member _.KeyChar = keyChar.Publish

let private rawKey (held: Key list) (key: Key) =
    KeyChord.rawKeyDown (FakeKeyboard held :> IKeyboard) key

/// The two calls `ControlsElmish.chordFallthrough` makes on a key-down, against a real (non-`None`)
/// `MapKeyChord`. Returns the product message, if any.
type private Msg =
    | Lasso
    | Left

let private mapKeyChord (key: ViewerKey) (mods: KeyModifiers) : Msg option =
    match key, mods.Ctrl, mods.Shift with
    | Letter 'L', true, false -> Some Lasso
    | _ -> None

let private routeThroughSeam (raw: string) : Msg option =
    let baseKey, _, mods =
        ViewerKeyboard.normalizeEventWithModifiers
            { RawKey = raw
              Direction = ViewerKeyDirection.KeyDown }

    mapKeyChord baseKey mods

[<Tests>]
let tests =
    testList "Issue 183 — modifier chords fire from a real window" [
        test "an unmodified key is reported bare (routing unchanged, SC-012)" {
            Expect.equal (rawKey [] Key.L) "L" "no modifier held, no prefix"
        }

        test "a key pressed with Ctrl held is reported as the Ctrl+L wire format" {
            Expect.equal (rawKey [ Key.ControlLeft ] Key.L) "Ctrl+L" "held Ctrl decorates the base key"
        }

        test "the right-hand modifier keys count too" {
            Expect.equal (rawKey [ Key.ControlRight ] Key.L) "Ctrl+L" "ControlRight is Ctrl"
            Expect.equal (rawKey [ Key.ShiftRight ] Key.Tab) "Shift+Tab" "ShiftRight is Shift"
            Expect.equal (rawKey [ Key.AltRight ] Key.F4) "Alt+F4" "AltRight is Alt"
            Expect.equal (rawKey [ Key.SuperRight ] Key.S) "Meta+S" "SuperRight is Meta"
        }

        test "several held modifiers compose in canonical order" {
            Expect.equal
                (rawKey [ Key.ShiftLeft; Key.ControlLeft ] Key.L)
                "Ctrl+Shift+L"
                "Ctrl before Shift regardless of press order"
        }

        test "a modifier key is never decorated with its own held state" {
            // The bug this guards: `ControlLeft` arriving as `Ctrl+ControlLeft`, whose base key is
            // itself a modifier — the chord would then never resolve to a real key.
            Expect.equal (rawKey [ Key.ControlLeft ] Key.ControlLeft) "ControlLeft" "reported bare"
            Expect.equal (rawKey [ Key.ControlLeft; Key.ShiftLeft ] Key.ShiftLeft) "ShiftLeft" "reported bare"
        }

        test "the host's raw key round-trips back through the seam's parser" {
            let baseKey, isDown, mods =
                ViewerKeyboard.normalizeEventWithModifiers
                    { RawKey = rawKey [ Key.ControlLeft ] Key.L
                      Direction = ViewerKeyDirection.KeyDown }

            Expect.equal baseKey (Letter 'L') "base key recovered"
            Expect.isTrue isDown "direction preserved"
            Expect.isTrue mods.Ctrl "Ctrl recovered"
            Expect.isFalse mods.Shift "Shift not invented"
        }

        test "normalizeEvent leaves the chord intact for chordFallthrough to recover" {
            // chordFallthrough only reaches its recovery branch for `ViewerKey.Unknown raw`. If the
            // viewer's `normalizeEvent` stripped the prefix, the chord would dissolve before the seam.
            let key, _ =
                ViewerKeyboard.normalizeEvent
                    { RawKey = rawKey [ Key.ControlLeft ] Key.L
                      Direction = ViewerKeyDirection.KeyDown }

            Expect.equal key (ViewerKey.Unknown "Ctrl+L") "the raw chord survives normalizeEvent"
        }

        test "a real Ctrl+L dispatches the product Msg through a non-None MapKeyChord" {
            // The end-to-end assertion the issue says CI has never made.
            Expect.equal (routeThroughSeam (rawKey [ Key.ControlLeft ] Key.L)) (Some Lasso) "Ctrl+L fires the chord"
        }

        test "a bare L does not fire the Ctrl+L chord" {
            Expect.equal (routeThroughSeam (rawKey [] Key.L)) None "no modifier, no chord"
        }

        test "Ctrl+Shift+L does not fire the Ctrl-only chord" {
            Expect.equal (routeThroughSeam (rawKey [ Key.ControlLeft; Key.ShiftLeft ] Key.L)) None "extra modifier is not ignored"
        }

        test "key-up is never decorated, so a down/up pair cannot disagree on release order" {
            // Press Ctrl+L, release Ctrl, then release L: a decorated key-up would report `L` against a
            // `Ctrl+L` key-down and strand the pressed-key entry. Key-up is bare, whatever is held.
            Expect.equal (KeyChord.rawKeyUp Key.L) "L" "bare with nothing held"
            Expect.equal (KeyChord.rawKeyUp Key.ControlLeft) "ControlLeft" "the modifier's own release"
        }

        test "key-up reports exactly what it reported before issue 183" {
            for key in [ Key.L; Key.Tab; Key.Left; Key.Space; Key.F4 ] do
                Expect.equal (KeyChord.rawKeyUp key) (key.ToString()) $"{key} key-up unchanged"
        }

        test "pre-183 behaviour is what regressed: two bare events never form a chord" {
            // Reconstructs the old host: `ControlLeft` then `L`, each undecorated. This is exactly
            // what a real window used to deliver, and why MapKeyChord could not fire.
            Expect.equal (routeThroughSeam "ControlLeft") None "the modifier alone is not a chord"
            Expect.equal (routeThroughSeam "L") None "and the letter alone has lost its modifier"
        }
    ]
