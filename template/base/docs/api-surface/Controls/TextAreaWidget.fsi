namespace FS.GG.UI.Controls.Typed

open FS.GG.UI.Controls

/// Immutable, compiler-checked authoring surface for a multi-line text area. `Id`
/// is required identity for a stateful control. Reuses the existing `TextInput`
/// model — no parallel state type (FR-004/SC-003). `OnChanged = None` lowers to no binding.
type TextAreaProps<'msg> =
    { Id: ControlId
      Value: string
      ReadOnly: bool
      Validation: ValidationState
      OnChanged: (string -> 'msg) option }

/// Typed Props front door for the `TextArea` control.
module TextArea =
    /// Authoring defaults for the given required `Id`.
    val defaults: controlId: ControlId -> TextAreaProps<'msg>
    /// Delegates to `TextInput.init` (multi-line) — initial model + effects equal the existing control.
    val init: props: TextAreaProps<'msg> -> TextInputModel * TextInputEffect list
    /// Delegates to `TextInput.update` — pure transition, no I/O.
    val update: msg: TextInputMsg -> model: TextInputModel -> TextInputModel * TextInputEffect list
    /// Lowers structurally equal to the legacy `TextArea.create` attrs for the current model state.
    val view: props: TextAreaProps<'msg> -> model: TextInputModel -> Widget<'msg>
