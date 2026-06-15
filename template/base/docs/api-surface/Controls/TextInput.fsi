namespace FS.GG.UI.Controls

/// Whether a `TextInputModel` accepts a `SingleLine` or `MultiLine` of text.
type TextInputMode =
    | SingleLine
    | MultiLine

/// A selected character range (`Start`..`End`) within a `TextInputModel`.
type TextSelection =
    { Start: int
      End: int }

/// The MVU state of a text field: committed vs. draft text, `CaretIndex`, `Selection`, in-flight `Composition`, `Validation`, and focus.
type TextInputModel =
    { ControlId: ControlId
      Mode: TextInputMode
      CommittedText: string
      DraftText: string
      CaretIndex: int
      Selection: TextSelection option
      Composition: string option
      Validation: ValidationState
      Focused: bool }

/// An input message driving `TextInput.update`, e.g. `Focus`, `InsertText`, `MoveCaret`, `Commit`, `Cancel`, or composition events.
type TextInputMsg =
    | Focus
    | Blur
    | InsertText of string
    | MoveCaret of int
    | SelectRange of int * int
    | RequestClipboardPaste
    | ClipboardTextReceived of string
    | Commit
    | Cancel
    | CompositionStarted of string
    | CompositionCommitted of string
    | ApplyValidation of ValidationState

/// A side effect raised by `TextInput.update`: a clipboard read request, a committed-text notification, or a reported diagnostic.
type TextInputEffect =
    | RequestClipboardText of ControlId
    | CommitText of ControlId * string
    | ReportTextInputDiagnostic of ControlDiagnostic

/// MVU text-field component covering caret, selection, IME composition, clipboard, and validation.
module TextInput =
    /// Seeds a `TextInputModel` for `controlId` in the given `mode` with an initial `value`, plus any startup effects.
    val init: controlId: ControlId -> mode: TextInputMode -> value: string -> TextInputModel * TextInputEffect list
    /// Pure transition applying `msg` to `model`, returning the next model and the `TextInputEffect` list it raises.
    val update: msg: TextInputMsg -> model: TextInputModel -> TextInputModel * TextInputEffect list
    /// Maps a host-fulfilled `effect` back into the `TextInputMsg` that feeds it into `update`, if any.
    val interpretEffect: effect: TextInputEffect -> TextInputMsg option
    /// Returns the `ControlDiagnostic` list implied by the current `model` state.
    val diagnostics: model: TextInputModel -> ControlDiagnostic list
