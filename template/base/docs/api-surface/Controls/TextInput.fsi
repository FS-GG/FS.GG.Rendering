// See skill: fs-gg-ui-widgets
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
    ///
    /// The `TextInputEffect` values it raises all point OUT at the host: `RequestClipboardText` asks for
    /// the clipboard, `CommitText` and `ReportTextInputDiagnostic` notify. When the host fulfils a
    /// `RequestClipboardText`, feed the text back in as the `TextInputMsg.ClipboardTextReceived` message —
    /// there is no framework function that turns a raised effect back into a `Msg`, and there cannot be
    /// one: no `TextInputEffect` case carries a host result to map. (0.9.0 shipped an `interpretEffect`
    /// that only ever returned `None` for exactly that reason. It was retired at the 0.10.0 major, #537.)
    val update: msg: TextInputMsg -> model: TextInputModel -> TextInputModel * TextInputEffect list
    /// Returns the `ControlDiagnostic` list implied by the current `model` state.
    val diagnostics: model: TextInputModel -> ControlDiagnostic list
