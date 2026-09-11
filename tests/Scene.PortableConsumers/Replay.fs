module RetainedTraceReplay

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
      Camera: SvgCamera
      Captured: int option
      ObjectAAvailable: bool
      ObjectBAvailable: bool
      Outcome: int }

let private identity = function
    | 0 -> None
    | 1 -> Some "a"
    | 2 -> Some "b"
    | value -> failwith $"model identity is outside the bounded domain: {value}"

let private optionalPointer = function
    | -1 -> None
    | value -> Some value

let private sceneObject id =
    { Id = id
      Selectable = true
      AccessibleLabel = id
      Content = { Nodes = [] } }

let private scene revision camera objectAAvailable objectBAvailable =
    { RootId = "trace-root"
      Revision = revision
      Camera = camera
      Layers =
        [ { Id = "world"
            Visible = true
            Objects =
              [ if objectAAvailable then sceneObject "a"
                if objectBAvailable then sceneObject "b" ] } ] }

let private initialState () =
    match SvgRetained.tryCreateInteraction (scene 0 { PanX = 0.0; PanY = 0.0; Zoom = 1.0 } true true) with
    | Ok state -> state
    | Error error -> failwith $"trace fixture initialization failed: {error}"

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
      Camera = { PanX = float (number 12); PanY = float (number 13); Zoom = float (number 14) }
      Captured = optionalPointer (number 15)
      ObjectAAvailable = boolean 16
      ObjectBAvailable = boolean 17
      Outcome = number 18 }

let private errorCode = function
    | None -> 0
    | Some(RetainedInteractionError.StaleRevision _) -> 1
    | Some(RetainedInteractionError.UnknownObject _) -> 2
    | Some(RetainedInteractionError.NonIncreasingRevision _) -> 3
    | Some RetainedInteractionError.InvalidCamera -> 4
    | Some(RetainedInteractionError.PointerNotCaptured _) -> 5
    | Some(RetainedInteractionError.InvalidScene _) -> 6
    | Some error -> failwith $"trace produced an unmodeled reducer error: {error}"

let private dispatch mappedAction (expected: Expected) state =
    let a = expected.Args
    let message =
        match mappedAction with
        | "Select" -> RetainedInteractionMessage.Select(a[0], identity a[1] |> Option.defaultValue "missing")
        | "ReplaceScene" ->
            RetainedInteractionMessage.ReplaceScene(
                scene a[0] { PanX = float a[3]; PanY = float a[4]; Zoom = float a[5] } (a[1] = 1) (a[2] = 1))
        | "ClearSelection" -> RetainedInteractionMessage.ClearSelection a[0]
        | "FocusNext" -> RetainedInteractionMessage.FocusNext a[0]
        | "FocusPrevious" -> RetainedInteractionMessage.FocusPrevious a[0]
        | "SetCamera" -> RetainedInteractionMessage.SetCamera(a[0], { PanX = float a[1]; PanY = float a[2]; Zoom = float a[3] })
        | "CapturePointer" -> RetainedInteractionMessage.CapturePointer(a[0], a[1])
        | "ReleasePointer" -> RetainedInteractionMessage.ReleasePointer(a[0], a[1])
        | action -> failwith $"trace action has no public reducer binding: {action}"
    SvgRetained.update message state

let private projection (result: RetainedInteractionResult) =
    let objects = result.State.Scene.Layers |> List.collect _.Objects |> List.map _.Id
    result.State.Scene.Revision,
    result.State.SelectedObjectId,
    result.State.FocusedObjectId,
    result.State.Scene.Camera,
    result.State.CapturedPointerId,
    List.contains "a" objects,
    List.contains "b" objects,
    errorCode result.Error

type ReplayResult =
    { Count: int
      Canonical: string }

let private optionNumber = function
    | None -> "none"
    | Some value -> string value

let private boolText value = if value then "true" else "false"

let replay mapAction mapOutcome (text: string) =
    let rows = text.Replace("\r\n", "\n").Split('\n') |> Array.filter (String.IsNullOrWhiteSpace >> not) |> Array.skip 1
    let mutable activeTrace = -1
    let mutable state = initialState ()
    let mutable firstDivergence = None
    let mutable count = 0
    let canonical = ResizeArray<string>()
    for line in rows do
        if firstDivergence.IsNone then
            let expected = parseLine line
            if expected.Trace <> activeTrace then
                activeTrace <- expected.Trace
                state <- initialState ()
            let result = dispatch (mapAction expected.Action) expected state
            let revision, selected, focused, camera, captured, objectAAvailable, objectBAvailable, outcome = projection result
            let actual =
                revision,
                selected,
                focused,
                camera,
                captured,
                objectAAvailable,
                objectBAvailable,
                mapOutcome outcome
            let expectedProjection =
                expected.Revision,
                expected.Selected,
                expected.Focused,
                expected.Camera,
                expected.Captured,
                expected.ObjectAAvailable,
                expected.ObjectBAvailable,
                expected.Outcome
            if actual <> expectedProjection then
                firstDivergence <-
                    Some $"TRACE-DIVERGENCE trace={expected.Trace} step={expected.Step} action={expected.Action} expected={expectedProjection} actual={actual}"
            else
                state <- result.State
                count <- count + 1
                canonical.Add(
                    String.concat "\t"
                        [ string expected.Trace
                          string expected.Step
                          string revision
                          optionNumber selected
                          optionNumber focused
                          string (int camera.PanX)
                          string (int camera.PanY)
                          string (int camera.Zoom)
                          optionNumber captured
                          boolText objectAAvailable
                          boolText objectBAvailable
                          string outcome ])
    match firstDivergence with
    | Some divergence -> Error divergence
    | None -> Ok { Count = count; Canonical = String.concat "\n" canonical + "\n" }
