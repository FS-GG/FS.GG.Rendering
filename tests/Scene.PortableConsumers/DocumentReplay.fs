module DocumentTraceReplay

open System
open FS.GG.UI.Scene

type private Expected =
    { Trace: int
      Step: int
      Action: string
      Args: int array
      Revision: int
      Selected: string option
      Focused: string option
      Camera: SvgAffine
      Captured: int option
      ObjectAAvailable: bool
      ObjectBAvailable: bool
      Outcome: int }

let private identity = function
    | 0 -> None
    | 1 -> Some "a"
    | 2 -> Some "b"
    | value -> failwith $"model identity is outside the bounded domain: {value}"

let private optionalPointer = function -1 -> None | value -> Some value

let private element id =
    { Id = id
      SemanticId = Some id
      Visible = true
      Transform = SvgAffine.identity
      ClipId = None
      MaskId = None
      Presentation = None
      Content = SvgElementContent.SceneLeaf { Nodes = [] } }

let private document objectAAvailable objectBAvailable =
    { Schema = SvgDocument.schema
      Id = "trace-document"
      ViewBox = { X = 0.0; Y = 0.0; Width = 100.0; Height = 100.0 }
      Definitions = []
      Children =
        [ if objectAAvailable then element "a"
          if objectBAvailable then element "b" ] }

let private invalidDocument () =
    let unresolved = { element "a" with ClipId = Some "missing-clip" }
    { document true true with Children = [ unresolved ] }

let private initialState () =
    match SvgDocumentInteraction.tryCreate 0 SvgAffine.identity (document true true) with
    | Ok state -> state
    | Error error -> failwith $"document trace initialization failed: {error}"

let private parseLine (line: string) =
    let fields = line.Split('\t')
    let number index = Int32.Parse fields[index]
    let boolean index = Boolean.Parse fields[index]
    { Trace = number 0
      Step = number 1
      Action = fields[2]
      Args = [| for index in 3 .. 8 -> number index |]
      Revision = number 9
      Selected = identity (number 10)
      Focused = identity (number 11)
      Camera = { A = float (number 14); B = 0.0; C = 0.0; D = float (number 14); E = float (number 12); F = float (number 13) }
      Captured = optionalPointer (number 15)
      ObjectAAvailable = boolean 16
      ObjectBAvailable = boolean 17
      Outcome = number 18 }

let private errorCode = function
    | None -> 0
    | Some(SvgDocumentInteractionError.StaleRevision _) -> 1
    | Some(SvgDocumentInteractionError.UnknownSemanticIdentity _) -> 2
    | Some(SvgDocumentInteractionError.NonIncreasingRevision _) -> 3
    | Some SvgDocumentInteractionError.InvalidCamera -> 4
    | Some(SvgDocumentInteractionError.PointerNotCaptured _) -> 5
    | Some(SvgDocumentInteractionError.InvalidDocument _) -> 6
    | Some error -> failwith $"trace produced an unmodeled document reducer error: {error}"

let private dispatch (expected: Expected) state =
    let a = expected.Args
    let message =
        match expected.Action with
        | "Select" -> SvgDocumentInteractionMessage.SelectSemantic(a[0], identity a[1] |> Option.defaultValue "missing")
        | "ReplaceDocument" ->
            let candidate = if a[4] = 1 then document (a[2] = 1) (a[3] = 1) else invalidDocument ()
            SvgDocumentInteractionMessage.ReplaceDocument(a[0], a[1], candidate)
        | "ClearSelection" -> SvgDocumentInteractionMessage.ClearSelection a[0]
        | "FocusNext" -> SvgDocumentInteractionMessage.FocusNext a[0]
        | "FocusPrevious" -> SvgDocumentInteractionMessage.FocusPrevious a[0]
        | "SetCamera" ->
            SvgDocumentInteractionMessage.SetCamera(a[0], { A = float a[3]; B = 0.0; C = 0.0; D = float a[3]; E = float a[1]; F = float a[2] })
        | "CapturePointer" -> SvgDocumentInteractionMessage.CapturePointer(a[0], a[1])
        | "ReleasePointer" -> SvgDocumentInteractionMessage.ReleasePointer(a[0], a[1])
        | action -> failwith $"document trace action has no public reducer binding: {action}"
    SvgDocumentInteraction.update message state

let private projection (result: SvgDocumentInteractionResult) =
    let identities = result.State.Document.Children |> List.choose _.SemanticId
    result.State.Revision,
    result.State.SelectedSemanticId,
    result.State.FocusedSemanticId,
    result.State.Camera,
    result.State.CapturedPointerId,
    List.contains "a" identities,
    List.contains "b" identities,
    errorCode result.Error

type ReplayResult = { Count: int; Canonical: string }

[<RequireQualifiedAccess>]
type ReplayMutation =
    | WrongOrder
    | AcceptStaleRevision
    | AcceptInvalidReference
    | PreserveReleasedCapture
    | ApplyInvalidEdit

let private optionNumber = function None -> "none" | Some value -> string value
let private boolText value = if value then "true" else "false"

let private replayCore mutation (text: string) =
    let rows = text.Replace("\r\n", "\n").Split('\n') |> Array.filter (String.IsNullOrWhiteSpace >> not) |> Array.skip 1
    let mutable activeTrace = -1
    let mutable state = initialState ()
    let mutable firstDivergence = None
    let mutable count = 0
    let mutable traceStepCount = 0
    let mutable mutationApplied = false
    let canonical = ResizeArray<string>()
    for line in rows do
        if firstDivergence.IsNone then
            let expected = parseLine line
            if expected.Trace <> activeTrace then
                activeTrace <- expected.Trace
                state <- initialState ()
                traceStepCount <- 0
            let stateBefore = state
            let dispatchState =
                match mutation with
                | Some ReplayMutation.WrongOrder
                    when not mutationApplied && traceStepCount > 0 && state <> initialState () ->
                    mutationApplied <- true
                    initialState ()
                | _ -> state
            let actualResult = dispatch expected dispatchState
            let result =
                match mutation with
                | Some ReplayMutation.AcceptStaleRevision when not mutationApplied && errorCode actualResult.Error = 1 ->
                    mutationApplied <- true
                    { actualResult with Error = None }
                | Some ReplayMutation.AcceptInvalidReference when not mutationApplied && errorCode actualResult.Error = 6 ->
                    mutationApplied <- true
                    { actualResult with Error = None }
                | Some ReplayMutation.PreserveReleasedCapture
                    when not mutationApplied && expected.Action = "ReleasePointer" && actualResult.Error.IsNone ->
                    mutationApplied <- true
                    { actualResult with State = { actualResult.State with CapturedPointerId = stateBefore.CapturedPointerId } }
                | Some ReplayMutation.ApplyInvalidEdit when not mutationApplied && errorCode actualResult.Error = 6 ->
                    mutationApplied <- true
                    { actualResult with State = { actualResult.State with Document = invalidDocument () } }
                | _ -> actualResult
            let revision, selected, focused, camera, captured, objectAAvailable, objectBAvailable, outcome = projection result
            let actual = revision, selected, focused, camera, captured, objectAAvailable, objectBAvailable, outcome
            let expectedProjection = expected.Revision, expected.Selected, expected.Focused, expected.Camera, expected.Captured, expected.ObjectAAvailable, expected.ObjectBAvailable, expected.Outcome
            if actual <> expectedProjection then
                firstDivergence <- Some $"DOCUMENT-TRACE-DIVERGENCE trace={expected.Trace} step={expected.Step} action={expected.Action} expected={expectedProjection} actual={actual}"
            else
                state <- result.State
                count <- count + 1
                traceStepCount <- traceStepCount + 1
                canonical.Add(String.concat "\t" [ string expected.Trace; string expected.Step; string revision; optionNumber selected; optionNumber focused; string (int camera.E); string (int camera.F); string (int camera.A); optionNumber captured; boolText objectAAvailable; boolText objectBAvailable; string outcome ])
    let result =
        match firstDivergence with
        | Some divergence -> Error divergence
        | None -> Ok { Count = count; Canonical = String.concat "\n" canonical + "\n" }
    result, mutationApplied

let replay text = replayCore None text |> fst

let replayMutant mutation text =
    match replayCore (Some mutation) text with
    | Error divergence, true -> Error divergence
    | Error divergence, false -> Error $"DOCUMENT-MUTANT-NOT-EXERCISED mutation={mutation}; replay={divergence}"
    | Ok _, true -> Ok()
    | Ok _, false -> Error $"DOCUMENT-MUTANT-NOT-EXERCISED mutation={mutation}"
