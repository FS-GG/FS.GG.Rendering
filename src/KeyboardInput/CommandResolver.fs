namespace FS.GG.UI.KeyboardInput

type InputSourceId = string
type InputEventId = string

[<RequireQualifiedAccess>]
type InputGesturePhase =
    | Pressed
    | Released

type CommandInputEvent =
    {
        Id: InputEventId
        Source: InputSourceId
        Gesture: InputGesture
        Phase: InputGesturePhase
        IsRepeat: bool
        NativeEditable: bool
        HostReserved: bool
        IsComposing: bool
        AvailableCommands: CommandId list
    }

type InputModalFrame =
    {
        Context: string
        RestoreFocus: string
    }

type PendingInputSequence =
    {
        Steps: (InputKeyIdentity * InputModifiers) list
        DeadlineToken: string
        TerminalCommand: CommandId option
        TerminalContext: string option
    }

type OwnedCommandPress =
    {
        Source: InputSourceId
        Gesture: InputGesture
        Command: CommandId
        Trigger: CommandTriggerPolicy
    }

type CommandResolverState =
    {
        Profile: EffectiveInputProfile
        ActiveContexts: string list
        ModalStack: InputModalFrame list
        CaptureRestoreFocus: string option
        PendingSequence: PendingInputSequence option
        OwnedPresses: OwnedCommandPress list
        IsComposing: bool
        IsDisposed: bool
    }

type CommandInvocation =
    {
        EventId: InputEventId
        Source: InputSourceId
        Command: CommandId
    }

[<RequireQualifiedAccess>]
type CommandResolverEffect =
    | InvokeCommand of CommandInvocation
    | HeldActionChanged of command: CommandId * isHeld: bool
    | RequestDeadline of token: string
    | CancelDeadline of token: string
    | RequestFocus of target: string
    | CapturedGesture of InputGesture
    | PreventDefault of eventId: InputEventId
    | ResolverDiagnostic of code: string

[<RequireQualifiedAccess>]
type CommandResolverObservation =
    | InputEvent of CommandInputEvent
    | DeadlineElapsed of token: string * availableCommands: CommandId list
    | ContextsChanged of string list
    | ProfileChanged of EffectiveInputProfile
    | BeginCapture of restoreFocus: string
    | CancelCapture
    | PushModal of InputModalFrame
    | PopModal
    | Escape of eventId: InputEventId * source: InputSourceId
    | CompositionStarted
    | CompositionEnded
    | FocusLost
    | ModalTakenOver
    | SourceDisconnected of InputSourceId
    | Dispose

[<RequireQualifiedAccess>]
module CommandResolver =
    let init (activeContexts: string list) (profile: EffectiveInputProfile) : CommandResolverState =
        {
            Profile = profile
            ActiveContexts = activeContexts
            ModalStack = []
            CaptureRestoreFocus = None
            PendingSequence = None
            OwnedPresses = []
            IsComposing = false
            IsDisposed = false
        }

    let private keySteps (gesture: InputGesture) : (InputKeyIdentity * InputModifiers) list option =
        match gesture with
        | InputGesture.KeyChord(identity, modifiers) -> Some [ identity, modifiers ]
        | InputGesture.KeySequence steps -> Some steps
        | _ -> None

    let private isPrefix
        (prefix: (InputKeyIdentity * InputModifiers) list)
        (values: (InputKeyIdentity * InputModifiers) list)
        =
        prefix.Length <= values.Length
        && (prefix, values |> List.take prefix.Length) ||> List.forall2 (=)

    let private contexts (catalog: InputCatalog) =
        catalog.Contexts |> List.map (fun value -> value.Id, value) |> Map.ofList

    let private commands (catalog: InputCatalog) =
        catalog.Commands |> List.map (fun value -> value.Id, value) |> Map.ofList

    let private activeContextIds (catalog: InputCatalog) (state: CommandResolverState) =
        let byId = contexts catalog

        if
            state.ModalStack
            |> List.exists (fun frame -> byId.ContainsKey frame.Context |> not)
        then
            Set.empty
        else
            let projected =
                state.ActiveContexts @ (state.ModalStack |> List.map _.Context)
                |> List.distinct
                |> List.choose byId.TryFind

            let floor =
                projected
                |> List.filter _.Exclusive
                |> List.map _.Priority
                |> function
                    | [] -> None
                    | priorities -> Some(List.max priorities)

            projected
            |> List.filter (fun value -> floor |> Option.forall (fun priority -> value.Priority >= priority))
            |> List.map _.Id
            |> Set.ofList

    let private chooseBinding
        (catalog: InputCatalog)
        (available: Set<CommandId>)
        (active: Set<string>)
        (bindings: InputBinding list)
        =
        let byId = contexts catalog

        bindings
        |> List.filter (fun binding ->
            active |> Set.contains binding.Context
            && available |> Set.contains binding.Command)
        |> List.sortByDescending (fun binding -> byId.[binding.Context].Priority)
        |> List.tryHead

    let private trigger (catalog: InputCatalog) (command: CommandId) =
        commands catalog
        |> Map.tryFind command
        |> Option.map _.Trigger
        |> Option.defaultValue CommandTriggerPolicy.OncePerPress

    let private cancelPending (state: CommandResolverState) : CommandResolverState * CommandResolverEffect list =
        match state.PendingSequence with
        | None -> state, []
        | Some pending ->
            { state with PendingSequence = None }, [ CommandResolverEffect.CancelDeadline pending.DeadlineToken ]

    let private neutralizeWhere
        (predicate: OwnedCommandPress -> bool)
        (state: CommandResolverState)
        : CommandResolverState * CommandResolverEffect list =
        let removed, retained = state.OwnedPresses |> List.partition predicate
        let stillHeld = retained |> List.map _.Command |> Set.ofList

        let effects =
            removed
            |> List.filter (fun press -> press.Trigger = CommandTriggerPolicy.Continuous)
            |> List.map _.Command
            |> List.distinct
            |> List.filter (stillHeld.Contains >> not)
            |> List.map (fun command -> CommandResolverEffect.HeldActionChanged(command, false))

        { state with OwnedPresses = retained }, effects

    let private resetTransient (code: string) (state: CommandResolverState) =
        let withoutPrefix, deadline = cancelPending state
        let neutral, held = neutralizeWhere (fun _ -> true) withoutPrefix
        neutral, deadline @ held @ [ CommandResolverEffect.ResolverDiagnostic code ]

    let private dispatch
        (catalog: InputCatalog)
        (event: CommandInputEvent)
        (binding: InputBinding)
        (state: CommandResolverState)
        =
        let alreadyOwned =
            state.OwnedPresses
            |> List.exists (fun p -> p.Source = event.Source && p.Gesture = event.Gesture)

        let policy = trigger catalog binding.Command

        let claimed effects =
            effects @ [ CommandResolverEffect.PreventDefault event.Id ]

        match policy, event.IsRepeat, alreadyOwned with
        | CommandTriggerPolicy.OncePerPress, true, _
        | CommandTriggerPolicy.OncePerPress, _, true
        | CommandTriggerPolicy.Continuous, true, _ -> state, []
        | CommandTriggerPolicy.RepeatWhileHeld, true, true ->
            state,
            claimed
                [
                    CommandResolverEffect.InvokeCommand
                        {
                            EventId = event.Id
                            Source = event.Source
                            Command = binding.Command
                        }
                ]
        | _, _, true -> state, []
        | _ ->
            let press: OwnedCommandPress =
                {
                    Source = event.Source
                    Gesture = event.Gesture
                    Command = binding.Command
                    Trigger = policy
                }

            let next =
                { state with
                    OwnedPresses = press :: state.OwnedPresses
                }

            match policy with
            | CommandTriggerPolicy.Continuous ->
                let wasHeld =
                    state.OwnedPresses |> List.exists (fun value -> value.Command = binding.Command)

                next,
                claimed (
                    if wasHeld then
                        []
                    else
                        [ CommandResolverEffect.HeldActionChanged(binding.Command, true) ]
                )
            | _ ->
                next,
                claimed
                    [
                        CommandResolverEffect.InvokeCommand
                            {
                                EventId = event.Id
                                Source = event.Source
                                Command = binding.Command
                            }
                    ]

    let private direct (gesture: InputGesture) (state: CommandResolverState) =
        state.Profile.Bindings |> List.filter (fun binding -> binding.Gesture = gesture)

    let private sequences (prefix: (InputKeyIdentity * InputModifiers) list) (state: CommandResolverState) =
        state.Profile.Bindings
        |> List.choose (fun binding ->
            match keySteps binding.Gesture with
            | Some steps when steps.Length > 1 && isPrefix prefix steps -> Some(binding, steps)
            | _ -> None)

    let private await
        (event: CommandInputEvent)
        (terminal: CommandId option)
        (context: string option)
        (steps: (InputKeyIdentity * InputModifiers) list)
        (state: CommandResolverState)
        (prior: CommandResolverEffect list)
        =
        let token = "sequence:" + event.Id

        { state with
            PendingSequence =
                Some
                    {
                        Steps = steps
                        DeadlineToken = token
                        TerminalCommand = terminal
                        TerminalContext = context
                    }
        },
        prior
        @ [
            CommandResolverEffect.RequestDeadline token
            CommandResolverEffect.PreventDefault event.Id
        ]

    let rec private pressRoot (catalog: InputCatalog) (event: CommandInputEvent) (state: CommandResolverState) =
        let active, available =
            activeContextIds catalog state, Set.ofList event.AvailableCommands

        match keySteps event.Gesture with
        | Some [ step ] ->
            let next =
                sequences [ step ] state
                |> List.filter (fun (binding, _) -> active.Contains binding.Context)

            let terminal = chooseBinding catalog available active (direct event.Gesture state)

            if List.isEmpty next then
                terminal
                |> Option.map (fun value -> dispatch catalog event value state)
                |> Option.defaultValue (state, [])
            else
                match terminal with
                | Some value when catalog.AllowTerminalPrefixes ->
                    await event (Some value.Command) (Some value.Context) [ step ] state []
                | _ -> await event None None [ step ] state []
        | _ ->
            chooseBinding catalog available active (direct event.Gesture state)
            |> Option.map (fun value -> dispatch catalog event value state)
            |> Option.defaultValue (state, [])

    and private pressPending
        (catalog: InputCatalog)
        (event: CommandInputEvent)
        (pending: PendingInputSequence)
        (state: CommandResolverState)
        =
        let cleared = { state with PendingSequence = None }
        let cancelled = [ CommandResolverEffect.CancelDeadline pending.DeadlineToken ]

        match keySteps event.Gesture with
        | Some [ step ] ->
            let prefix = pending.Steps @ [ step ]

            let active, available =
                activeContextIds catalog state, Set.ofList event.AvailableCommands

            let candidates =
                sequences prefix state
                |> List.filter (fun (binding, _) -> active.Contains binding.Context)

            let exact =
                candidates
                |> List.choose (fun (binding, steps) -> if steps = prefix then Some binding else None)

            let longer =
                candidates |> List.exists (fun (_, steps) -> steps.Length > prefix.Length)

            match chooseBinding catalog available active exact, longer with
            | Some terminal, true when catalog.AllowTerminalPrefixes ->
                await event (Some terminal.Command) (Some terminal.Context) prefix cleared cancelled
            | Some terminal, _ ->
                let next, effects = dispatch catalog event terminal cleared in next, cancelled @ effects
            | None, true -> await event None None prefix cleared cancelled
            | None, false -> let next, effects = pressRoot catalog event cleared in next, cancelled @ effects
        | _ -> let next, effects = pressRoot catalog event cleared in next, cancelled @ effects

    let private sameReleasedControl pressed released =
        match pressed, released with
        | InputGesture.KeyChord(pressedKey, _), InputGesture.KeyChord(releasedKey, _) -> pressedKey = releasedKey
        | _ -> pressed = released

    let private release (event: CommandInputEvent) (state: CommandResolverState) =
        let owns (press: OwnedCommandPress) =
            press.Source = event.Source && sameReleasedControl press.Gesture event.Gesture

        if state.OwnedPresses |> List.exists owns then
            let next, effects = neutralizeWhere owns state
            next, effects @ [ CommandResolverEffect.PreventDefault event.Id ]
        else
            state, []

    let private input (catalog: InputCatalog) (event: CommandInputEvent) (state: CommandResolverState) =
        if event.Phase = InputGesturePhase.Released then
            release event state
        elif
            event.HostReserved
            || event.NativeEditable
            || event.IsComposing
            || state.IsComposing
        then
            state, []
        else
            match state.CaptureRestoreFocus, state.PendingSequence with
            | Some target, _ ->
                { state with
                    CaptureRestoreFocus = None
                },
                [
                    CommandResolverEffect.CapturedGesture event.Gesture
                    CommandResolverEffect.RequestFocus target
                    CommandResolverEffect.PreventDefault event.Id
                ]
            | None, Some pending -> pressPending catalog event pending state
            | None, None -> pressRoot catalog event state

    let update (catalog: InputCatalog) (observation: CommandResolverObservation) (state: CommandResolverState) =
        if state.IsDisposed then
            state, []
        else
            match observation with
            | CommandResolverObservation.InputEvent event -> input catalog event state
            | CommandResolverObservation.DeadlineElapsed(token, available) ->
                match state.PendingSequence with
                | Some pending when pending.DeadlineToken = token ->
                    let cleared = { state with PendingSequence = None }

                    match pending.TerminalCommand with
                    | Some command when List.contains command available ->
                        cleared,
                        [
                            CommandResolverEffect.CancelDeadline token
                            CommandResolverEffect.InvokeCommand
                                {
                                    EventId = token
                                    Source = "deadline"
                                    Command = command
                                }
                        ]
                    | _ -> cleared, [ CommandResolverEffect.CancelDeadline token ]
                | _ -> state, [ CommandResolverEffect.ResolverDiagnostic "StaleDeadline" ]
            | CommandResolverObservation.ContextsChanged value ->
                let reset, effects = resetTransient "ContextsChanged" state in
                { reset with ActiveContexts = value }, effects
            | CommandResolverObservation.ProfileChanged value ->
                let reset, effects = resetTransient "ProfileChanged" state in { reset with Profile = value }, effects
            | CommandResolverObservation.BeginCapture target ->
                let reset, effects = resetTransient "CaptureStarted" state in

                { reset with
                    CaptureRestoreFocus = Some target
                },
                effects
            | CommandResolverObservation.CancelCapture ->
                match state.CaptureRestoreFocus with
                | Some target ->
                    { state with
                        CaptureRestoreFocus = None
                    },
                    [ CommandResolverEffect.RequestFocus target ]
                | None -> state, []
            | CommandResolverObservation.PushModal frame ->
                let reset, effects = resetTransient "ModalPushed" state in

                { reset with
                    ModalStack = reset.ModalStack @ [ frame ]
                },
                effects
            | CommandResolverObservation.PopModal ->
                match List.rev state.ModalStack with
                | frame :: rest ->
                    let reset, effects = resetTransient "ModalPopped" state

                    { reset with
                        ModalStack = List.rev rest
                    },
                    effects @ [ CommandResolverEffect.RequestFocus frame.RestoreFocus ]
                | [] -> state, []
            | CommandResolverObservation.Escape(eventId, _) ->
                match state.CaptureRestoreFocus, List.rev state.ModalStack, state.PendingSequence with
                | Some target, _, _ ->
                    { state with
                        CaptureRestoreFocus = None
                    },
                    [
                        CommandResolverEffect.RequestFocus target
                        CommandResolverEffect.PreventDefault eventId
                    ]
                | None, frame :: rest, _ ->
                    let reset, effects = resetTransient "ModalEscaped" state

                    { reset with
                        ModalStack = List.rev rest
                    },
                    effects
                    @ [
                        CommandResolverEffect.RequestFocus frame.RestoreFocus
                        CommandResolverEffect.PreventDefault eventId
                    ]
                | None, [], Some pending ->
                    { state with PendingSequence = None },
                    [
                        CommandResolverEffect.CancelDeadline pending.DeadlineToken
                        CommandResolverEffect.PreventDefault eventId
                    ]
                | _ -> state, []
            | CommandResolverObservation.CompositionStarted ->
                let reset, effects = resetTransient "CompositionStarted" state in
                { reset with IsComposing = true }, effects
            | CommandResolverObservation.CompositionEnded -> { state with IsComposing = false }, []
            | CommandResolverObservation.FocusLost -> resetTransient "FocusLost" state
            | CommandResolverObservation.ModalTakenOver -> resetTransient "ModalTakenOver" state
            | CommandResolverObservation.SourceDisconnected source ->
                let withoutPrefix, deadline = cancelPending state

                let next, effects =
                    neutralizeWhere (fun press -> press.Source = source) withoutPrefix

                next,
                deadline
                @ effects
                @ [ CommandResolverEffect.ResolverDiagnostic "SourceDisconnected" ]
            | CommandResolverObservation.Dispose ->
                let reset, effects = resetTransient "Disposed" state

                { reset with
                    IsDisposed = true
                    ModalStack = []
                    CaptureRestoreFocus = None
                },
                effects
