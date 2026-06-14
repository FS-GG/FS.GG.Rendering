namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

/// A secondary command in a `SplitButton` popup menu. `Key` is the selection
/// identity dispatched by `OnSelected`; `Label` is the displayed text.
type SplitButtonItem = { Key: string; Label: string }

/// Immutable, compiler-checked authoring surface for a stateful on/off button.
/// `IsOn` is the product-owned pressed state (mirroring `CheckBox`); `OnToggle`
/// is dispatched with the NEXT state on activation and lowers to NO binding when
/// `None`.
type ToggleButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      IsOn: bool
      Enabled: bool
      OnToggle: (bool -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a primary action plus a popup
/// menu of secondary commands. `IsOpen` is product-owned popup visibility; empty
/// `Items` lowers to an empty/disabled menu. `OnClick`/`OnSelected = None` lower to
/// no binding.
type SplitButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      Enabled: bool
      IsOpen: bool
      Items: SplitButtonItem list
      OnClick: 'msg option
      OnSelected: (string -> 'msg) option }

/// Typed Props front door for the `ToggleButton` control.
module ToggleButton =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: ToggleButtonProps<'msg>
    /// Lowers to a legacy `Button` carrying the pressed state (`Attr.selected IsOn`)
    /// whose activation maps to `OnToggle (not IsOn)`; `OnToggle = None` lowers to
    /// no event binding.
    val view: props: ToggleButtonProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `SplitButton` control.
module SplitButton =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: SplitButtonProps<'msg>
    /// Lowers to a legacy `Toolbar` of [ primary `Button`; dropdown-trigger
    /// `Button`; an `Overlay` `Menu` of `Items` shown when `IsOpen` ]; `None`
    /// callbacks lower to no binding.
    val view: props: SplitButtonProps<'msg> -> Widget<'msg>
