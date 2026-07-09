namespace FS.GG.UI.SkiaViewer.Host

open Silk.NET.Input
open FS.GG.UI.KeyboardInput

/// Issue 183: the host's half of the modifier-chord seam.
///
/// Silk delivers a chord as two independent events — `ControlLeft` then `L` — and its key callback
/// carries no modifier state (the `int` argument is the scancode). Modifiers are *held state*, so
/// they have to be sampled from the keyboard at the instant the key fires. `ViewerKeyboard`'s
/// `Ctrl+L` raw-key format is the wire representation the `MapKeyChord` seam already parses; this
/// module is what finally produces it.
///
/// Auto-repeat note: Silk raises `KeyDown` for GLFW's `Repeat` action as well as `Press` (only
/// `Release` is distinguished), so holding a key delivers repeated `KeyDown` with no intervening
/// `KeyUp`. Repeats are passed through, not coalesced, and the callback exposes no repeat flag to
/// surface — a consumer that must not double-fire should track its own key-down set.
module internal KeyChord =

    /// The modifiers held at the moment a key event fires, read straight off the keyboard.
    let heldModifiers (keyboard: IKeyboard) : KeyModifiers =
        { Ctrl = keyboard.IsKeyPressed Key.ControlLeft || keyboard.IsKeyPressed Key.ControlRight
          Alt = keyboard.IsKeyPressed Key.AltLeft || keyboard.IsKeyPressed Key.AltRight
          Shift = keyboard.IsKeyPressed Key.ShiftLeft || keyboard.IsKeyPressed Key.ShiftRight
          Meta = keyboard.IsKeyPressed Key.SuperLeft || keyboard.IsKeyPressed Key.SuperRight }

    /// The raw key a host reports for a key-DOWN: its name, decorated with the modifiers held
    /// alongside it. A modifier key is reported bare — `ControlLeft`, never `Ctrl+ControlLeft`,
    /// which would leave the chord's base key a modifier and never resolve to a real key.
    let rawKeyDown (keyboard: IKeyboard) (key: Key) : string =
        let name = key.ToString()

        if ViewerKeyboard.isModifierKey name then
            name
        else
            ViewerKeyboard.formatChord (heldModifiers keyboard) name

    /// The raw key a host reports for a key-UP — deliberately undecorated.
    ///
    /// The modifiers held at release need not be those held at press: releasing Ctrl before L makes a
    /// decorated key-up (`L`) disagree with its key-down (`Ctrl+L`), so a product tracking pressed keys
    /// by raw id would strand the `Ctrl+L` entry. `MapKeyChord` only ever runs on key-down, so
    /// decorating key-up buys nothing. Key-up therefore reports exactly what it did before issue 183.
    let rawKeyUp (key: Key) : string = key.ToString()
