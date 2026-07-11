namespace FS.GG.UI.Controls

open System
open FS.GG.UI.DesignSystem

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
    /// DEPRECATED — returns `None` for every `TextInputEffect`, always, and cannot do otherwise.
    /// It claims to map "a host-fulfilled effect" back into a `TextInputMsg`, but no
    /// `TextInputEffect` case carries a host result to map: `RequestClipboardText` is a request
    /// going OUT to the host, and `CommitText` / `ReportTextInputDiagnostic` are notifications
    /// going OUT. There is no fulfilment in the input, so there is no `Msg` in the output. Feed a
    /// fulfilled clipboard read back yourself as `TextInputMsg.ClipboardTextReceived`.
    [<Obsolete("TextInput.interpretEffect ALWAYS returns None, for every case, and no implementation could do better: no TextInputEffect case carries a host result to map back into a TextInputMsg. RequestClipboardText goes OUT to the host; CommitText and ReportTextInputDiagnostic are outward notifications. When your host fulfils a RequestClipboardText, dispatch TextInputMsg.ClipboardTextReceived yourself. This inert no-op is scheduled for removal at the next FS.GG.UI major.")>]
    val interpretEffect: effect: TextInputEffect -> TextInputMsg option
    /// Returns the `ControlDiagnostic` list implied by the current `model` state.
    val diagnostics: model: TextInputModel -> ControlDiagnostic list
