namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

/// Immutable, compiler-checked authoring surface for a tooltip. `text` required.
type TooltipProps<'msg> =
    { Id: ControlId option
      Text: string }

/// Immutable, compiler-checked authoring surface for a modal dialog. `children`
/// required. `OnSelected = None` lowers to no binding.
type DialogProps<'msg> =
    { Id: ControlId option
      Title: string option
      IsOpen: bool
      Children: Widget<'msg> list
      OnSelected: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a transient toast. `text` required.
type ToastProps<'msg> =
    { Id: ControlId option
      Text: string
      Severity: ValidationState }

/// Immutable, compiler-checked authoring surface for a layered overlay. `child` required.
type OverlayProps<'msg> =
    { Id: ControlId option
      IsOpen: bool
      Child: Widget<'msg> }

/// Typed Props front door for the `Tooltip` control.
module Tooltip =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: TooltipProps<'msg>
    /// Lowers structurally equal to `Tooltip.create [ Tooltip.text props.Text ]`.
    val view: props: TooltipProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Dialog` control.
module Dialog =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: DialogProps<'msg>
    /// Lowers children via `Widget.toControl` into `Dialog.children`, order preserved.
    val view: props: DialogProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Toast` control.
module Toast =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: ToastProps<'msg>
    /// Lowers structurally equal to `Toast.create` with the validation severity.
    val view: props: ToastProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Overlay` control.
module Overlay =
    /// Authoring defaults for the given required child.
    val defaults: child: Widget<'msg> -> OverlayProps<'msg>
    /// Lowers its single child via `Widget.toControl` into `Overlay.child`.
    val view: props: OverlayProps<'msg> -> Widget<'msg>
