namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

/// Opaque public return type of every typed `view`. Wraps the lowered
/// `Control<'msg>` IR. The internal representation (`{ Lowered: Control<'msg> }`)
/// stays in the implementation — Principle II. Additive-only: nothing in the
/// existing public surface changes.
[<Sealed>]
type Widget<'msg>

/// The typed-tree lowering bridge: `ofControl`/`toControl` convert to and from the legacy IR, and `render` paints a `Widget<'msg>`.
module Widget =
    /// Migration bridge: lift a legacy `Control<'msg>` into the typed tree
    /// (e.g. to drop it into a typed `Stack.Children`). FR-002.
    val ofControl: control: Control<'msg> -> Widget<'msg>
    /// Lowering accessor — the single, explicit seam to the existing IR. Used by
    /// render and the Elmish adapter. FR-002. Invariant: toControl (ofControl c) = c.
    val toControl: widget: Widget<'msg> -> Control<'msg>
    /// Convenience = `Control.render theme (toControl widget)`. FR-009.
    val render: theme: Theme -> widget: Widget<'msg> -> ControlRenderResult<'msg>
    /// Feature 108 (US5, FR-014): `Widget.map f = ofControl ∘ Control.map f ∘ toControl` — change
    /// only the message type of the lowered control, preserving structure / key / focus identity.
    val map: f: ('a -> 'b) -> widget: Widget<'a> -> Widget<'b>
