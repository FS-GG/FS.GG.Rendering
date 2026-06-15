namespace FS.GG.UI.Controls

open System
open FS.GG.UI.DesignSystem

type TextInputMode =
    | SingleLine
    | MultiLine

type TextSelection =
    { Start: int
      End: int }

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

type TextInputEffect =
    | RequestClipboardText of ControlId
    | CommitText of ControlId * string
    | ReportTextInputDiagnostic of ControlDiagnostic

module TextInput =
    let clamp low high value =
        value |> max low |> min high

    let normalizeNewlines (mode: TextInputMode) (value: string) =
        match mode with
        | SingleLine -> value.Replace("\r", "").Replace("\n", "")
        | MultiLine -> value.Replace("\r\n", "\n")

    let init controlId mode value : TextInputModel * TextInputEffect list =
        let normalized = normalizeNewlines mode value

        { ControlId = controlId
          Mode = mode
          CommittedText = normalized
          DraftText = normalized
          CaretIndex = normalized.Length
          Selection = None
          Composition = None
          Validation = Valid
          Focused = false },
        []

    let insertAt model value =
        let value = normalizeNewlines model.Mode value
        let startIndex, endIndex =
            model.Selection
            |> Option.map (fun range -> min range.Start range.End, max range.Start range.End)
            |> Option.defaultValue (model.CaretIndex, model.CaretIndex)

        let prefix = model.DraftText.Substring(0, clamp 0 model.DraftText.Length startIndex)
        let suffix = model.DraftText.Substring(clamp 0 model.DraftText.Length endIndex)
        let next = prefix + value + suffix

        { model with
            DraftText = next
            CaretIndex = prefix.Length + value.Length
            Selection = None }

    let update msg model =
        match msg with
        | Focus -> { model with Focused = true }, []
        | Blur -> { model with Focused = false; Selection = None; Composition = None }, []
        | InsertText value -> insertAt model value, []
        | MoveCaret offset ->
            { model with
                CaretIndex = clamp 0 model.DraftText.Length (model.CaretIndex + offset)
                Selection = None },
            []
        | SelectRange(startIndex, endIndex) ->
            { model with
                Selection =
                    Some
                        { Start = clamp 0 model.DraftText.Length startIndex
                          End = clamp 0 model.DraftText.Length endIndex } },
            []
        | RequestClipboardPaste -> model, [ RequestClipboardText model.ControlId ]
        | ClipboardTextReceived value -> insertAt model value, []
        | Commit ->
            let next = { model with CommittedText = model.DraftText; Composition = None }
            next, [ CommitText(model.ControlId, next.CommittedText) ]
        | Cancel -> { model with DraftText = model.CommittedText; CaretIndex = model.CommittedText.Length; Selection = None; Composition = None }, []
        | CompositionStarted value -> { model with Composition = Some value }, []
        | CompositionCommitted value -> { insertAt model value with Composition = None }, []
        | ApplyValidation state -> { model with Validation = state }, []

    let interpretEffect effect : TextInputMsg option =
        match effect with
        | RequestClipboardText _ ->
            None
        | CommitText _ -> None
        | ReportTextInputDiagnostic _ -> None

    let diagnostics model =
        [ if model.Composition.IsSome then
              yield Diagnostics.unsupportedEnvironment "text-input" "platform IME composition host callback" ]
