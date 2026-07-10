namespace FS.GG.UI.Controls

open FS.GG.UI.KeyboardInput

/// Issue 335 (epic 330): the key-rebind config-screen control. Lists the rebindable commands and their
/// current key bindings, surfaces keymap conflicts (issue 332 `Keymap.validate`), and offers a per-command
/// rebind affordance whose activation a product turns into a host key capture (`RequestHostKeyCapture`,
/// `KeyboardInput.fsi`). Generic and theme-agnostic: pure render + attributes + events; the parent owns the
/// `Keymap` and the capture flow (no internal mutable state). Renders neutrally under `Themes.Default` and
/// Ant-styled under `Themes.AntDesign` through the shared resolver, like the other net-new controls.
module KeyRebind =
    /// Build a `key-rebind` config screen from its attributes; pair with `commands`/`onRebind`, or use
    /// `ofKeymap` to derive the rows (and conflicts) from a `Keymap` directly.
    val create: attrs: Attr<'msg> list -> Control<'msg>

    /// The command->key rows the screen lists, as `command, key` pairs. Rendered one text row per binding,
    /// in the order given (`command — key`).
    val commands: rows: (CommandId * KeyId) list -> Attr<'msg>

    /// Dispatch `map command` when a command's rebind affordance is activated — the product responds by
    /// requesting a host key capture (`RequestHostKeyCapture`) and, on capture, `Keymap.rebind`s the command.
    val onRebind: map: (CommandId -> 'msg) -> Attr<'msg>

    /// Derive a config screen from a `Keymap`: one row per binding (`Keymap.toBindings`, deterministic
    /// key order) followed by one `conflict: <message>` row per `Keymap.validate` diagnostic (issue 332).
    /// `extra` carries any additional attributes (e.g. `onRebind`, `width`).
    val ofKeymap: keymap: Keymap -> extra: Attr<'msg> list -> Control<'msg>
