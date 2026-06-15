namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

/// The font weight applied to a `RichTextRun`: `Regular`, `Medium`, or `Bold`.
type RichTextWeight =
    | Regular
    | Medium
    | Bold

/// The visual styling of a `RichTextRun`: font family/size, `Weight`, foreground/background colors, underline, and italic.
type RichTextStyle =
    { FontFamily: string option
      FontSize: float
      Weight: RichTextWeight
      Foreground: Color
      Background: Color option
      Underline: bool
      Italic: bool }

/// A single span of `Text` carrying one `RichTextStyle`, the atomic unit composed into a `RichTextBlock`.
type RichTextRun =
    { Text: string
      Style: RichTextStyle
      Diagnostics: ControlDiagnostic list }

/// An ordered sequence of `Runs` with optional `MaxWidth`, clipping, effects, and accessibility metadata, forming a layout unit.
type RichTextBlock =
    { Runs: RichTextRun list
      MaxWidth: float option
      Clip: bool
      Effects: string list
      Accessibility: AccessibilityMetadata option }

/// The measured layout of a `RichTextBlock`: `Width`, `Height`, `LineCount`, and any measurement diagnostics.
type RichTextMeasurement =
    { Width: float
      Height: float
      LineCount: int
      Diagnostics: ControlDiagnostic list }

/// Builders for styled rich-text `RichTextRun`/`RichTextBlock` values and their lowering to a `Control`.
module RichText =
    /// Returns the baseline `RichTextStyle` derived from the supplied `Theme`.
    val defaultStyle: Theme -> RichTextStyle
    /// Builds a `RichTextRun` pairing `text` with a `style`.
    val run: text: string -> style: RichTextStyle -> RichTextRun
    /// Assembles a `RichTextBlock` from an ordered list of `runs`.
    val block: runs: RichTextRun list -> RichTextBlock
    /// Computes the `RichTextMeasurement` (width, height, line count) for `block`.
    val measure: block: RichTextBlock -> RichTextMeasurement
    /// Lowers a `RichTextBlock` and its `Attr` list into a renderable `Control`.
    val create: block: RichTextBlock -> Attr<'msg> list -> Control<'msg>
