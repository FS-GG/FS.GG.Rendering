namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type RichTextProps<'msg> =
    { Id: ControlId option
      Runs: RichTextRun list }

type LabelProps<'msg> =
    { Id: ControlId option
      Text: string }

type ImageProps<'msg> =
    { Id: ControlId option
      Value: string }

type IconProps<'msg> =
    { Id: ControlId option
      Text: string }

type SeparatorProps<'msg> =
    { Id: ControlId option }

type BadgeProps<'msg> =
    { Id: ControlId option
      Text: string }

type ProgressBarProps<'msg> =
    { Id: ControlId option
      Value: float }

type SpinnerProps<'msg> =
    { Id: ControlId option }

type ValidationMessageProps<'msg> =
    { Id: ControlId option
      Text: string
      Severity: ValidationState }

// The typed `view` calls the exact same legacy string-keyed builders, so the lowered IR is
// structurally equal to the legacy authoring call by construction (FR-002, SC-002). Key
// application lives once in the internal WidgetLowering module.

module RichText =
    let defaults: RichTextProps<'msg> = { Id = None; Runs = [] }

    let view (props: RichTextProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.RichText.create (FS.GG.UI.Controls.RichText.block props.Runs) []
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Label =
    let defaults: LabelProps<'msg> = { Id = None; Text = "" }

    let view (props: LabelProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Label.create [ FS.GG.UI.Controls.Label.text props.Text ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Image =
    let defaults: ImageProps<'msg> = { Id = None; Value = "" }

    let view (props: ImageProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Image.create [ FS.GG.UI.Controls.Image.source props.Value ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Icon =
    let defaults: IconProps<'msg> = { Id = None; Text = "" }

    let view (props: IconProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Icon.create [ FS.GG.UI.Controls.Icon.name props.Text ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Separator =
    let defaults: SeparatorProps<'msg> = { Id = None }

    let view (props: SeparatorProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Separator.create []
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Badge =
    let defaults: BadgeProps<'msg> = { Id = None; Text = "" }

    let view (props: BadgeProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Badge.create [ FS.GG.UI.Controls.Badge.text props.Text ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module ProgressBar =
    let defaults: ProgressBarProps<'msg> = { Id = None; Value = 0.0 }

    let view (props: ProgressBarProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.ProgressBar.create [ FS.GG.UI.Controls.ProgressBar.value props.Value ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Spinner =
    let defaults: SpinnerProps<'msg> = { Id = None }

    let view (props: SpinnerProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.Spinner.create []
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module ValidationMessage =
    let defaults: ValidationMessageProps<'msg> =
        { Id = None; Text = ""; Severity = Valid }

    let view (props: ValidationMessageProps<'msg>) : Widget<'msg> =
        FS.GG.UI.Controls.ValidationMessage.create
            [ FS.GG.UI.Controls.ValidationMessage.text props.Text
              Attr.validation props.Severity ]
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
