namespace FS.GG.UI.Controls

open FS.GG.UI.Layout

[<RequireQualifiedAccess>]
type PointerButton =
    | Primary
    | Secondary
    | Middle

type PointerOrigin = Pointer

[<RequireQualifiedAccess>]
type PointerPhase =
    | Moved
    | Pressed
    | Released
    | Wheel
    | Exited

type PointerSample =
    { Phase: PointerPhase
      X: float
      Y: float
      Button: PointerButton option
      DeltaX: float
      DeltaY: float }

type PressCandidate =
    { Control: ControlId
      StartX: float
      StartY: float
      Dragging: bool }

type PointerState =
    { Hover: ControlId option
      Presses: Map<PointerButton, PressCandidate>
      LastX: float
      LastY: float
      DragThreshold: float }

type PointerDiagnosticCode =
    | HitTestMiss
    | StaleTarget

type PointerDiagnostic =
    { Code: PointerDiagnosticCode
      Message: string
      Control: ControlId option
      X: float
      Y: float }

type PointerInteraction =
    | HoverEnter of control: ControlId * x: float * y: float
    | HoverLeave of control: ControlId
    | PressedDown of control: ControlId * button: PointerButton * x: float * y: float
    | ReleasedUp of control: ControlId * button: PointerButton * x: float * y: float
    | Click of control: ControlId * button: PointerButton * x: float * y: float
    | DragBegin of control: ControlId * button: PointerButton * startX: float * startY: float
    | DragMove of control: ControlId * button: PointerButton * x: float * y: float
    | DragEnd of control: ControlId * button: PointerButton * x: float * y: float
    | DragCancelled of control: ControlId option
    | Scroll of control: ControlId * deltaX: float * deltaY: float * x: float * y: float
    | FocusMovedByPointer of control: ControlId
    | Diagnostic of PointerDiagnostic

type PointerMsg =
    | Move of x: float * y: float
    | Down of button: PointerButton * x: float * y: float
    | Up of button: PointerButton * x: float * y: float
    | WheelMsg of deltaX: float * deltaY: float * x: float * y: float
    | WindowExited
    | FocusLost

module Pointer =

    let origin: PointerOrigin = Pointer

    let init () : PointerState =
        { Hover = None
          Presses = Map.empty
          LastX = 0.0
          LastY = 0.0
          DragThreshold = 4.0 }

    let toMsg (sample: PointerSample) : PointerMsg option =
        match sample.Phase with
        | PointerPhase.Moved -> Some(Move(sample.X, sample.Y))
        | PointerPhase.Pressed -> sample.Button |> Option.map (fun button -> Down(button, sample.X, sample.Y))
        | PointerPhase.Released -> sample.Button |> Option.map (fun button -> Up(button, sample.X, sample.Y))
        | PointerPhase.Wheel -> Some(WheelMsg(sample.DeltaX, sample.DeltaY, sample.X, sample.Y))
        | PointerPhase.Exited -> Some WindowExited

    // --- internal helpers ---------------------------------------------------

    let private hitTest policy layout x y : ControlId option = Layout.hitTestComputed policy layout x y

    let private controlExists (layout: LayoutResult) (control: ControlId) =
        layout.Bounds |> List.exists (fun item -> item.NodeId = control)

    let private beyondThreshold (threshold: float) (startX: float) (startY: float) (x: float) (y: float) =
        let dx = x - startX
        let dy = y - startY
        dx * dx + dy * dy > threshold * threshold

    // Drag bookkeeping for a single held button on a Move. Returns the (possibly
    // updated) candidate plus the ordered interactions and runtime messages.
    let private advanceDrag (button: PointerButton) (candidate: PressCandidate) (threshold: float) (x: float) (y: float) =
        if candidate.Dragging then
            { candidate with StartX = candidate.StartX; StartY = candidate.StartY },
            [ DragMove(candidate.Control, button, x, y) ],
            [ MoveDrag(x, y) ]
        elif beyondThreshold threshold candidate.StartX candidate.StartY x y then
            { candidate with Dragging = true },
            [ DragBegin(candidate.Control, button, candidate.StartX, candidate.StartY) ],
            [ StartDrag(candidate.Control, candidate.StartX, candidate.StartY) ]
        else
            candidate, [], []

    // --- the reducer --------------------------------------------------------

    let update
        (policy: PixelSnapPolicy)
        (layout: LayoutResult)
        (msg: PointerMsg)
        (state: PointerState)
        : PointerState * PointerInteraction list * ControlRuntimeMsg list =
        match msg with
        | Move(x, y) when not state.Presses.IsEmpty ->
            // A button is held: this is drag bookkeeping, not hover (hover is
            // suppressed while pressing). Process each held button deterministically.
            let folded =
                state.Presses
                |> Map.toList
                |> List.fold
                    (fun (presses, interactions, runtimeMsgs) (button, candidate) ->
                        let candidate', newInteractions, newRuntime = advanceDrag button candidate state.DragThreshold x y
                        Map.add button candidate' presses, interactions @ newInteractions, runtimeMsgs @ newRuntime)
                    (state.Presses, [], [])

            let presses, interactions, runtimeMsgs = folded
            { state with Presses = presses; LastX = x; LastY = y }, interactions, runtimeMsgs

        | Move(x, y) ->
            let hit = hitTest policy layout x y

            if hit = state.Hover then
                { state with LastX = x; LastY = y }, [], []
            else
                let leave =
                    match state.Hover with
                    | Some prior -> [ HoverLeave prior ]
                    | None -> []

                let enter =
                    match hit with
                    | Some next -> [ HoverEnter(next, x, y) ]
                    | None -> []

                { state with Hover = hit; LastX = x; LastY = y }, leave @ enter, [ HoverControl hit ]

        | Down(button, x, y) ->
            match hitTest policy layout x y with
            | Some control ->
                let candidate = { Control = control; StartX = x; StartY = y; Dragging = false }

                let focusInteractions, focusRuntime =
                    match button with
                    | PointerButton.Primary -> [ FocusMovedByPointer control ], [ FocusControl(Some control) ]
                    | PointerButton.Secondary
                    | PointerButton.Middle -> [], []

                { state with
                    Presses = Map.add button candidate state.Presses
                    LastX = x
                    LastY = y },
                PressedDown(control, button, x, y) :: focusInteractions,
                PressControl control :: focusRuntime

            | None ->
                let diagnostic =
                    { Code = HitTestMiss
                      Message = "Pointer press resolved to no control inside the window."
                      Control = None
                      X = x
                      Y = y }

                { state with LastX = x; LastY = y }, [ Diagnostic diagnostic ], []

        | Up(button, x, y) ->
            match Map.tryFind button state.Presses with
            | Some candidate ->
                let presses = Map.remove button state.Presses
                let nextState = { state with Presses = presses; LastX = x; LastY = y }

                if candidate.Dragging then
                    nextState, [ DragEnd(candidate.Control, button, x, y) ], [ EndDrag; ReleaseControl candidate.Control ]
                elif not (controlExists layout candidate.Control) then
                    let diagnostic =
                        { Code = StaleTarget
                          Message =
                            sprintf "Pointer release targeted stale control '%s'; no click dispatched." candidate.Control
                          Control = Some candidate.Control
                          X = x
                          Y = y }

                    nextState, [ Diagnostic diagnostic ], [ RecoverStaleTarget candidate.Control ]
                else
                    let hit = hitTest policy layout x y
                    let released = [ ReleasedUp(candidate.Control, button, x, y) ]

                    let click =
                        if hit = Some candidate.Control then
                            [ Click(candidate.Control, button, x, y) ]
                        else
                            []

                    nextState, released @ click, [ ReleaseControl candidate.Control ]

            | None ->
                // A release with no matching press is benign and ignored.
                { state with LastX = x; LastY = y }, [], []

        | WheelMsg(deltaX, deltaY, x, y) ->
            match hitTest policy layout x y with
            | Some control -> { state with LastX = x; LastY = y }, [ Scroll(control, deltaX, deltaY, x, y) ], []
            | None ->
                // Wheel over empty space is a silent miss (US5 scenario 2).
                { state with LastX = x; LastY = y }, [], []

        | WindowExited
        | FocusLost ->
            let cancelled =
                state.Presses
                |> Map.toList
                |> List.map (fun (_, candidate) -> DragCancelled(Some candidate.Control))

            let hoverLeave =
                match state.Hover with
                | Some prior -> [ HoverLeave prior ]
                | None -> []

            let runtimeMsgs =
                match msg with
                | FocusLost -> [ ControlRuntimeMsg.FocusLost ]
                | _ -> [ CancelInteraction None; HoverControl None ]

            { state with Presses = Map.empty; Hover = None }, cancelled @ hoverLeave, runtimeMsgs

    let replay
        (policy: PixelSnapPolicy)
        (layout: LayoutResult)
        (messages: PointerMsg list)
        (initial: PointerState)
        : PointerState * PointerInteraction list =
        messages
        |> List.fold
            (fun (state, accumulated) msg ->
                let state', interactions, _ = update policy layout msg state
                state', accumulated @ interactions)
            (initial, [])
