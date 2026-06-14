namespace FS.Skia.UI.Controls.Typed

open FS.Skia.UI.Controls

type TextBoxProps<'msg> =
    { Id: ControlId
      Mode: TextInputMode
      Value: string
      ReadOnly: bool
      Validation: ValidationState
      OnChanged: (string -> 'msg) option }

module TextBox =
    let defaults (controlId: ControlId) : TextBoxProps<'msg> =
        { Id = controlId
          Mode = SingleLine
          Value = ""
          ReadOnly = false
          Validation = Valid
          OnChanged = None }

    let init (props: TextBoxProps<'msg>) : TextInputModel * TextInputEffect list =
        TextInput.init props.Id props.Mode props.Value

    let update (msg: TextInputMsg) (model: TextInputModel) : TextInputModel * TextInputEffect list =
        TextInput.update msg model

    let view (props: TextBoxProps<'msg>) (model: TextInputModel) : Widget<'msg> =
        let attrs =
            [ yield FS.Skia.UI.Controls.TextBox.value model.DraftText
              yield FS.Skia.UI.Controls.TextBox.readOnly props.ReadOnly
              yield FS.Skia.UI.Controls.TextBox.validation model.Validation
              match props.OnChanged with
              | Some map -> yield FS.Skia.UI.Controls.TextBox.onChanged map
              | None -> () ]

        FS.Skia.UI.Controls.TextBox.create attrs
        |> Control.withKey props.Id
        |> Widget.ofControl
