namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type IconButtonProps<'msg> =
    { Id: ControlId option
      Text: string
      Enabled: bool
      Intent: ButtonIntent
      OnClick: 'msg option }

type NumericInputProps<'msg> =
    { Id: ControlId option
      Value: float
      ReadOnly: bool
      OnChanged: (float -> 'msg) option }

type RadioGroupProps<'msg> =
    { Id: ControlId option
      Items: string list
      SelectedKey: string option
      OnChanged: (string -> 'msg) option }

type SwitchProps<'msg> =
    { Id: ControlId option
      Checked: bool
      OnChanged: (bool -> 'msg) option }

type SliderProps<'msg> =
    { Id: ControlId option
      Value: float
      OnChanged: (float -> 'msg) option }

// File-private lowering helpers — see Display.fs for the construction-by-parity
// rationale. Hidden from the public surface by absence from Input.fsi.
module InputLowering =
    let intentStyle intent =
        match intent with
        | Primary -> "primary"
        | Secondary -> "secondary"
        | Danger -> "danger"
        | Ghost -> "ghost"

module IconButton =
    let defaults: IconButtonProps<'msg> =
        { Id = None
          Text = ""
          Enabled = true
          Intent = Primary
          OnClick = None }

    let view (props: IconButtonProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.IconButton.icon props.Text
              yield Attr.enabled props.Enabled
              yield Attr.style (InputLowering.intentStyle props.Intent)
              match props.OnClick with
              | Some msg -> yield FS.GG.UI.Controls.IconButton.onClick msg
              | None -> () ]

        FS.GG.UI.Controls.IconButton.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module NumericInput =
    let defaults: NumericInputProps<'msg> =
        { Id = None
          Value = 0.0
          ReadOnly = false
          OnChanged = None }

    let view (props: NumericInputProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.NumericInput.value props.Value
              yield Attr.readOnly props.ReadOnly
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.NumericInput.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.NumericInput.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module RadioGroup =
    let defaults: RadioGroupProps<'msg> =
        { Id = None
          Items = []
          SelectedKey = None
          OnChanged = None }

    let view (props: RadioGroupProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.RadioGroup.items props.Items
              match props.SelectedKey with
              | Some key -> yield FS.GG.UI.Controls.RadioGroup.selected key
              | None -> ()
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.RadioGroup.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.RadioGroup.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Switch =
    let defaults: SwitchProps<'msg> =
        { Id = None; Checked = false; OnChanged = None }

    let view (props: SwitchProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.Switch.checked' props.Checked
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.Switch.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.Switch.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl

module Slider =
    let defaults: SliderProps<'msg> =
        { Id = None; Value = 0.0; OnChanged = None }

    let view (props: SliderProps<'msg>) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.Slider.value props.Value
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.Slider.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.Slider.create attrs
        |> WidgetLowering.withKeyOpt props.Id
        |> Widget.ofControl
