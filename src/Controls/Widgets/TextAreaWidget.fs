namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

type TextAreaProps<'msg> =
    { Id: ControlId
      Value: string
      ReadOnly: bool
      Validation: ValidationState
      OnChanged: (string -> 'msg) option }

module TextArea =
    let defaults (controlId: ControlId) : TextAreaProps<'msg> =
        { Id = controlId
          Value = ""
          ReadOnly = false
          Validation = Valid
          OnChanged = None }

    let init (props: TextAreaProps<'msg>) : TextInputModel * TextInputEffect list =
        TextInput.init props.Id MultiLine props.Value

    let update (msg: TextInputMsg) (model: TextInputModel) : TextInputModel * TextInputEffect list =
        TextInput.update msg model

    let view (props: TextAreaProps<'msg>) (model: TextInputModel) : Widget<'msg> =
        let attrs =
            [ yield FS.GG.UI.Controls.TextArea.value model.DraftText
              yield Attr.readOnly props.ReadOnly
              yield Attr.validation model.Validation
              match props.OnChanged with
              | Some map -> yield FS.GG.UI.Controls.TextArea.onChanged map
              | None -> () ]

        FS.GG.UI.Controls.TextArea.create attrs
        |> Control.withKey props.Id
        |> Widget.ofControl
