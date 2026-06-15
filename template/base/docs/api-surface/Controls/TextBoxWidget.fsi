namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a text box. `Id` is required
/// identity for a stateful control. `OnChanged = None` lowers to no binding.
type TextBoxProps<'msg> =
    { Id: ControlId
      Mode: TextInputMode
      Value: string
      ReadOnly: bool
      Validation: ValidationState
      OnChanged: (string -> 'msg) option }

/// Typed Props front door for the `TextBox` control.
module TextBox =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> TextBoxProps<'msg>
    /// Delegates to `TextInput.init` — initial model + effects equal the existing control.
    val init: props: TextBoxProps<'msg> -> TextInputModel * TextInputEffect list
    /// Delegates to `TextInput.update` — pure transition, no I/O.
    val update: msg: TextInputMsg -> model: TextInputModel -> TextInputModel * TextInputEffect list
    /// Lowers structurally equal to the legacy `TextBox.create` attrs for the current model state.
    val view: props: TextBoxProps<'msg> -> model: TextInputModel -> Widget<'msg>
