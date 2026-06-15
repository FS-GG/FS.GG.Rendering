namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

type RichTextWeight =
    | Regular
    | Medium
    | Bold

type RichTextStyle =
    { FontFamily: string option
      FontSize: float
      Weight: RichTextWeight
      Foreground: Color
      Background: Color option
      Underline: bool
      Italic: bool }

type RichTextRun =
    { Text: string
      Style: RichTextStyle
      Diagnostics: ControlDiagnostic list }

type RichTextBlock =
    { Runs: RichTextRun list
      MaxWidth: float option
      Clip: bool
      Effects: string list
      Accessibility: AccessibilityMetadata option }

type RichTextMeasurement =
    { Width: float
      Height: float
      LineCount: int
      Diagnostics: ControlDiagnostic list }

module RichText =
    let defaultStyle (theme: Theme) =
        { FontFamily = theme.FontFamily
          FontSize = theme.FontSize
          Weight = Regular
          Foreground = theme.Foreground
          Background = None
          Underline = false
          Italic = false }

    let run text style =
        { Text = text
          Style = style
          Diagnostics = [] }

    let block runs =
        { Runs = runs
          MaxWidth = None
          Clip = false
          Effects = []
          Accessibility = None }

    let measure (block: RichTextBlock) =
        let maxFont =
            block.Runs
            |> List.map (fun run -> run.Style.FontSize)
            |> List.fold max 0.0

        let charCount =
            block.Runs
            |> List.sumBy (fun run -> run.Text.Length)

        let width =
            float charCount * max 1.0 maxFont * 0.55
            |> fun value ->
                block.MaxWidth
                |> Option.map (min value)
                |> Option.defaultValue value

        let effectDiagnostics =
            block.Effects
            |> List.map (fun effect ->
                Diagnostics.unsupportedEnvironment "rich-text" effect)

        { Width = width
          Height = max 1.0 (maxFont * 1.35)
          LineCount = if charCount = 0 then 0 else 1
          Diagnostics = (block.Runs |> List.collect _.Diagnostics) @ effectDiagnostics }

    let create (block: RichTextBlock) attrs =
        // Stash a field-name-free projection of the styled runs (text, colour, size, weight) that
        // the preview renderer reads to draw per-run colour/weight instead of the kind-id label.
        // Control.fs compiles before this file and so cannot see `RichTextBlock`; a tuple carries
        // the data across without re-declaring the type there (and a record would clash with the
        // many overlapping `Theme`/`Control` field names). Both the typed and legacy authoring
        // paths call this `create`, so the added attr keeps lowering parity by construction.
        let weightInt =
            function
            | Bold -> 700
            | Medium -> 600
            | Regular -> 400

        let runViews: (string * Color * float * int) list =
            block.Runs
            |> List.map (fun run -> run.Text, run.Style.Foreground, run.Style.FontSize, weightInt run.Style.Weight)

        Control.create
            "rich-text"
            (Attr.create "richText" Content (UntypedValue block)
             :: Attr.create "richTextRuns" Data (UntypedValue runViews)
             :: attrs)
