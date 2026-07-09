module Issue183ChordFormatTests

// Issue 183 — `formatChord` is the only producer of the `Ctrl+L` raw-key wire format that
// `normalizeEventWithModifiers` parses, and `isModifierKey` is what stops a host decorating a
// modifier key with its own held state. The parser existed and was tested; nothing produced its
// input, so the two halves had never been checked against each other.

open Expecto
open FS.GG.UI.KeyboardInput

let private mods ctrl alt shift meta : KeyModifiers =
    { Ctrl = ctrl
      Alt = alt
      Shift = shift
      Meta = meta }

let private roundTrip (m: KeyModifiers) (baseKey: string) =
    let key, _, parsed =
        ViewerKeyboard.normalizeEventWithModifiers
            { RawKey = ViewerKeyboard.formatChord m baseKey
              Direction = ViewerKeyDirection.KeyDown }

    key, parsed

[<Tests>]
let tests =
    testList "Issue 183 chord wire format (formatChord / isModifierKey)" [
        test "no modifiers leaves the base key byte-identical (SC-012)" {
            Expect.equal (ViewerKeyboard.formatChord ViewerKeyboard.noModifiers "L") "L" "no prefix, no '+'"
            Expect.equal (ViewerKeyboard.formatChord ViewerKeyboard.noModifiers "ArrowLeft") "ArrowLeft" "unchanged"
        }

        test "each modifier prefixes the base key" {
            Expect.equal (ViewerKeyboard.formatChord (mods true false false false) "L") "Ctrl+L" "Ctrl"
            Expect.equal (ViewerKeyboard.formatChord (mods false true false false) "F4") "Alt+F4" "Alt"
            Expect.equal (ViewerKeyboard.formatChord (mods false false true false) "Tab") "Shift+Tab" "Shift"
            Expect.equal (ViewerKeyboard.formatChord (mods false false false true) "S") "Meta+S" "Meta"
        }

        test "modifiers compose in canonical Ctrl+Alt+Shift+Meta order" {
            Expect.equal (ViewerKeyboard.formatChord (mods true true true true) "L") "Ctrl+Alt+Shift+Meta+L" "all four"
        }

        test "formatChord round-trips through normalizeEventWithModifiers for every modifier subset" {
            for ctrl in [ true; false ] do
                for alt in [ true; false ] do
                    for shift in [ true; false ] do
                        for meta in [ true; false ] do
                            let m = mods ctrl alt shift meta
                            let key, parsed = roundTrip m "L"
                            Expect.equal key (Letter 'L') $"base key survives {m}"
                            Expect.equal parsed m $"modifiers survive {m}"
        }

        test "an empty base key is passed through untouched (total, never throws)" {
            Expect.equal (ViewerKeyboard.formatChord (mods true false false false) "") "" "no dangling '+'"
        }

        test "isModifierKey recognises the toolkit spellings a host reports" {
            for raw in [ "ControlLeft"; "ControlRight"; "ShiftLeft"; "ShiftRight"; "AltLeft"; "AltRight"; "SuperLeft"; "SuperRight" ] do
                Expect.isTrue (ViewerKeyboard.isModifierKey raw) $"{raw} is a modifier key"
        }

        test "isModifierKey recognises the bare tokens parseModifiers accepts, case-insensitively" {
            for raw in [ "ctrl"; "Control"; "ALT"; "shift"; "Meta"; "cmd"; "Win"; "super" ] do
                Expect.isTrue (ViewerKeyboard.isModifierKey raw) $"{raw} is a modifier token"
        }

        test "isModifierKey rejects ordinary keys and the empty raw" {
            for raw in [ "L"; "ArrowLeft"; "Tab"; "F4"; "Space"; "" ] do
                Expect.isFalse (ViewerKeyboard.isModifierKey raw) $"{raw} is not a modifier key"
        }
    ]
