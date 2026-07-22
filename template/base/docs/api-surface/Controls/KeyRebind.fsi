// See skill: fs-gg-keyboard-input
namespace FS.GG.UI.Controls

open FS.GG.UI.KeyboardInput

/// One stable player-facing action row. `Command` is the durable runtime id; `Label` is what the
/// player sees; `Order` is explicit rather than inherited from key ordering. `Binding = None` keeps
/// an unbound/displaced action visible, and `DefaultBinding` supplies reset-to-default state.
type KeyRebindAction =
    { Command: CommandId
      Label: string
      Order: int
      Binding: KeyId option
      DefaultBinding: KeyId option }

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

    /// Supply an action catalog. Rows are rendered by `Order` then stable `Command`; an unbound action
    /// is rendered as `<label> — Unbound`, so displacement never makes an action disappear.
    val actions: catalog: KeyRebindAction list -> Attr<'msg>

    /// Compatibility event mapper for binding-only rows whose payload already is a command id. The
    /// product responds by capturing a raw key and calling `Keymap.replaceCommandBinding`.
    val onRebind: map: (CommandId -> 'msg) -> Attr<'msg>

    /// Catalog-aware event mapper. A rendered player label such as `Move Up — Unbound` is translated
    /// back to its stable `Command` id before `map` runs (a direct command-id payload also works).
    val onActionRebind: catalog: KeyRebindAction list -> map: (CommandId -> 'msg) -> Attr<'msg>

    /// Dispatch a fixed message from the catalog's reset-to-default affordance.
    val onReset: msg: 'msg -> Attr<'msg>

    /// Project current bindings onto stable catalog metadata. Commands absent from `keymap` remain in
    /// the result with `Binding = None`; labels, order, and defaults are preserved.
    val withBindings: keymap: Keymap -> catalog: KeyRebindAction list -> KeyRebindAction list

    /// Restore the catalog defaults using `Keymap.replaceCommandBinding`'s single-binding policy.
    /// Actions whose `DefaultBinding` is `None` remain unbound.
    val restoreDefaults: catalog: KeyRebindAction list -> Keymap

    /// Build the player-facing config screen from stable action rows. A visible reset row is included
    /// whenever the catalog contains at least one default; `extra` carries `onRebind`/`onReset` etc.
    val ofActions: catalog: KeyRebindAction list -> extra: Attr<'msg> list -> Control<'msg>

    /// Compatibility projection from runtime lookup state. It cannot invent player-facing labels,
    /// unbound actions, order, or defaults; new settings screens should use `ofActions`.
    val ofKeymap: keymap: Keymap -> extra: Attr<'msg> list -> Control<'msg>
