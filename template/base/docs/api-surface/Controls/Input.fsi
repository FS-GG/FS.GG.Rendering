namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

/// Immutable, compiler-checked authoring surface for an icon-only command.
/// `OnClick = None` lowers to NO event binding (FR-005), never a default message.
type IconButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      Enabled: bool
      Intent: ButtonIntent
      OnClick: 'msg option }

/// Immutable, compiler-checked authoring surface for a numeric editor.
type NumericInputProps<'msg> =
    { Id: ControlId option
      Value: float
      ReadOnly: bool
      OnChanged: (float -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a radio group. `items` required.
type RadioGroupProps<'msg> =
    { Id: ControlId option
      Items: string list
      SelectedKey: string option
      OnChanged: (string -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a compact boolean switch.
type SwitchProps<'msg> =
    { Id: ControlId option
      Checked: bool
      OnChanged: (bool -> 'msg) option }

/// Immutable, compiler-checked authoring surface for a continuous slider. `value` required.
type SliderProps<'msg> =
    { Id: ControlId option
      Value: float
      OnChanged: (float -> 'msg) option }

/// Typed Props front door for the `IconButton` control.
module IconButton =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: IconButtonProps<'msg>
    /// Lowers structurally equal to the legacy `IconButton.create` attrs;
    /// `OnClick = None` lowers to no event binding.
    val view: props: IconButtonProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `NumericInput` control.
module NumericInput =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: NumericInputProps<'msg>
    /// Lowers structurally equal to the legacy `NumericInput.create` attrs;
    /// `OnChanged = None` lowers to no event binding.
    val view: props: NumericInputProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `RadioGroup` control.
module RadioGroup =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: RadioGroupProps<'msg>
    /// Lowers structurally equal to the legacy `RadioGroup.create` attrs.
    val view: props: RadioGroupProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Switch` control.
module Switch =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: SwitchProps<'msg>
    /// Lowers structurally equal to the legacy `Switch.create` attrs.
    val view: props: SwitchProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Slider` control.
module Slider =
    /// Authoring defaults; optional fields take their value from here.
    val defaults: SliderProps<'msg>
    /// Lowers structurally equal to the legacy `Slider.create` attrs.
    val view: props: SliderProps<'msg> -> Widget<'msg>
