namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for rich text. `runs` required.
type RichTextProps<'msg> =
    { Id: ControlId option
      Runs: RichTextRun list }

/// Immutable, compiler-checked authoring surface for a short-form label.
type LabelProps<'msg> =
    { Id: ControlId option
      Text: string }

/// Immutable, compiler-checked authoring surface for an image. `value` required.
type ImageProps<'msg> =
    { Id: ControlId option
      Value: string }

/// Immutable, compiler-checked authoring surface for an icon glyph. `text` required.
type IconProps<'msg> =
    { Id: ControlId option
      Text: string }

/// Immutable, compiler-checked authoring surface for a visual separator.
type SeparatorProps<'msg> =
    { Id: ControlId option }

/// Immutable, compiler-checked authoring surface for a compact status badge.
type BadgeProps<'msg> =
    { Id: ControlId option
      Text: string }

/// Immutable, compiler-checked authoring surface for a determinate progress bar.
type ProgressBarProps<'msg> =
    { Id: ControlId option
      Value: float }

/// Immutable, compiler-checked authoring surface for an indeterminate spinner.
type SpinnerProps<'msg> =
    { Id: ControlId option }

/// Immutable, compiler-checked authoring surface for a validation message.
type ValidationMessageProps<'msg> =
    { Id: ControlId option
      Text: string
      Severity: ValidationState }

/// Typed Props front door for the `RichText` control.
module RichText =
    /// Authoring defaults for `RichTextProps` — optional fields take their value from here.
    val defaults: RichTextProps<'msg>
    /// Lowers structurally equal to `RichText.create (RichText.block props.Runs) []`.
    val view: props: RichTextProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Label` control.
module Label =
    /// Authoring defaults for `LabelProps` — optional fields take their value from here.
    val defaults: LabelProps<'msg>
    /// Lowers structurally equal to `Label.create [ Label.text props.Text ]`.
    val view: props: LabelProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Image` control.
module Image =
    /// Authoring defaults for `ImageProps` — optional fields take their value from here.
    val defaults: ImageProps<'msg>
    /// Lowers structurally equal to `Image.create [ Image.source props.Value ]`.
    val view: props: ImageProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Icon` control.
module Icon =
    /// Authoring defaults for `IconProps` — optional fields take their value from here.
    val defaults: IconProps<'msg>
    /// Lowers structurally equal to `Icon.create [ Icon.name props.Text ]`.
    val view: props: IconProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Separator` control.
module Separator =
    /// Authoring defaults for `SeparatorProps` — optional fields take their value from here.
    val defaults: SeparatorProps<'msg>
    /// Lowers structurally equal to `Separator.create []`.
    val view: props: SeparatorProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Badge` control.
module Badge =
    /// Authoring defaults for `BadgeProps` — optional fields take their value from here.
    val defaults: BadgeProps<'msg>
    /// Lowers structurally equal to `Badge.create [ Badge.text props.Text ]`.
    val view: props: BadgeProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `ProgressBar` control.
module ProgressBar =
    /// Authoring defaults for `ProgressBarProps` — optional fields take their value from here.
    val defaults: ProgressBarProps<'msg>
    /// Lowers structurally equal to `ProgressBar.create [ ProgressBar.value props.Value ]`.
    val view: props: ProgressBarProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `Spinner` control.
module Spinner =
    /// Authoring defaults for `SpinnerProps` — optional fields take their value from here.
    val defaults: SpinnerProps<'msg>
    /// Lowers structurally equal to `Spinner.create []`.
    val view: props: SpinnerProps<'msg> -> Widget<'msg>

/// Typed Props front door for the `ValidationMessage` control.
module ValidationMessage =
    /// Authoring defaults for `ValidationMessageProps` — optional fields take their value from here.
    val defaults: ValidationMessageProps<'msg>
    /// Lowers structurally equal to `ValidationMessage.create` with the validation severity.
    val view: props: ValidationMessageProps<'msg> -> Widget<'msg>
