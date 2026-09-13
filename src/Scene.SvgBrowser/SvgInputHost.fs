namespace FS.GG.UI.Scene.SvgBrowser

open System
open System.Collections.Generic
open Browser.Dom
open Browser.Types
open Fable.Core
open FS.GG.UI.KeyboardInput

type SvgGamepadSnapshot = { Source: InputSourceId; Buttons: bool list }

type SvgInputHostOptions =
    { SequenceTimeoutMilliseconds: int
      PollGamepads: bool
      Gamepads: unit -> SvgGamepadSnapshot list }

type SvgInputHostObservation =
    { OwnedListenerCount: int
      OwnedDeadlineCount: int
      GamepadPollScheduled: bool
      OwnedSourceCount: int
      IsDisposed: bool }

module private InputDom =
    [<Emit("$0.getModifierState && $0.getModifierState('AltGraph')")>]
    let altGraph (_event: KeyboardEvent) : bool = jsNative

    [<Emit("$0.repeat")>]
    let repeat (_event: KeyboardEvent) : bool = jsNative

    [<Emit("$0.isComposing")>]
    let composing (_event: KeyboardEvent) : bool = jsNative

    [<Emit("$0.pointerType || 'mouse'")>]
    let pointerType (_event: PointerEvent) : string = jsNative

    [<Emit("$0.relatedTarget")>]
    let relatedTarget (_event: FocusEvent) : Node = jsNative

    [<Emit("$0 && $1.contains($0)")>]
    let contains (_node: Node) (_root: HTMLElement) : bool = jsNative

    [<Emit("$0 instanceof Element ? $0.closest('button,input,select,textarea,a,[contenteditable=true],[contenteditable=\"\"]') !== null : false")>]
    let nativeInteractive (_target: EventTarget) : bool = jsNative

    [<Emit("$0 instanceof Element ? ($0.closest('[data-fsgg-input-action]')?.getAttribute('data-fsgg-input-action') || 'primary') : 'primary'")>]
    let action (_target: EventTarget) : string = jsNative

    [<Emit("document.hidden")>]
    let documentHidden () : bool = jsNative

    [<Emit("$0.setPointerCapture($1)")>]
    let capturePointer (_root: HTMLElement) (_pointerId: int) : unit = jsNative

    [<Emit("$0.hasPointerCapture($1)")>]
    let hasPointerCapture (_root: HTMLElement) (_pointerId: int) : bool = jsNative

    [<Emit("$0.releasePointerCapture($1)")>]
    let releasePointer (_root: HTMLElement) (_pointerId: int) : unit = jsNative

    [<Emit("requestAnimationFrame($0)")>]
    let requestFrame (_callback: float -> unit) : float = jsNative

    [<Emit("cancelAnimationFrame($0)")>]
    let cancelFrame (_token: float) : unit = jsNative

    [<Emit("Array.from(navigator.getGamepads ? navigator.getGamepads() : []).filter(Boolean).map(p => ({ Source: 'gamepad:' + p.index, Buttons: Array.from(p.buttons, b => !!b.pressed) }))")>]
    let gamepads () : SvgGamepadSnapshot array = jsNative

[<Sealed>]
type SvgInputHost(root: HTMLElement, catalog: InputCatalog, initialState: CommandResolverState, availableCommands: unit -> CommandId list, onEffect: CommandResolverEffect -> unit, options: SvgInputHostOptions) =
    let listeners = ResizeArray<EventTarget * string * (Event -> unit)>()
    let deadlines = Dictionary<string, float>()
    let keys = Dictionary<string, InputGesture>()
    let pointers = Dictionary<int, InputGesture>()
    let mutable pads: Map<InputSourceId, Set<int>> = Map.empty
    let mutable state = initialState
    let mutable disposed = false
    let mutable eventNumber = 0
    let mutable frame: float option = None

    let nextId prefix =
        eventNumber <- eventNumber + 1
        $"{prefix}:{eventNumber}"

    let listen (target: EventTarget) name handler =
        target.addEventListener(name, handler)
        listeners.Add(target, name, handler)

    let cancelDeadline token =
        match deadlines.TryGetValue token with
        | true, handle -> window.clearTimeout handle; deadlines.Remove token |> ignore
        | _ -> ()

    let rec apply observation =
        if disposed then []
        else
            let next, effects = CommandResolver.update catalog observation state
            state <- next
            for effect in effects do
                match effect with
                | CommandResolverEffect.RequestDeadline token ->
                    cancelDeadline token
                    let handle = window.setTimeout((fun () ->
                        deadlines.Remove token |> ignore
                        apply (CommandResolverObservation.DeadlineElapsed(token, availableCommands())) |> ignore), options.SequenceTimeoutMilliseconds)
                    deadlines[token] <- handle
                | CommandResolverEffect.CancelDeadline token -> cancelDeadline token
                | CommandResolverEffect.RequestFocus target ->
                    match document.getElementById target with
                    | null -> ()
                    | element -> element.focus()
                | _ -> ()
                onEffect effect
            effects

    let accepted eventId effects =
        effects |> List.exists (function CommandResolverEffect.PreventDefault id when id = eventId -> true | _ -> false)

    let modifiers (event: KeyboardEvent) =
        { Ctrl = event.ctrlKey
          Meta = event.metaKey
          Alt = event.altKey
          Shift = event.shiftKey
          AltGraph = InputDom.altGraph event }

    let activeContexts () =
        let byId = catalog.Contexts |> List.map (fun value -> value.Id, value) |> Map.ofList
        let projected =
            state.ActiveContexts @ (state.ModalStack |> List.map _.Context)
            |> List.distinct
            |> List.choose byId.TryFind
        let floor =
            projected |> List.filter _.Exclusive |> List.map _.Priority
            |> function [] -> None | values -> Some(List.max values)
        projected
        |> List.filter (fun value -> floor |> Option.forall (fun priority -> value.Priority >= priority))
        |> List.map _.Id
        |> Set.ofList

    let keyboardGesture (event: KeyboardEvent) =
        let mods = modifiers event
        let logical = InputGesture.KeyChord(InputKeyIdentity.LogicalKey event.key, mods)
        let physical = InputGesture.KeyChord(InputKeyIdentity.PhysicalCode event.code, mods)
        let active = activeContexts ()
        let available = availableCommands() |> Set.ofList
        let expectedIndex = state.PendingSequence |> Option.map _.Steps.Length |> Option.defaultValue 0
        let uses gesture (binding: InputBinding) =
            if not (active.Contains binding.Context && available.Contains binding.Command) then false
            else
                match binding.Gesture, gesture with
                | candidate, expected when candidate = expected -> true
                | InputGesture.KeySequence steps, InputGesture.KeyChord(identity, modifiers) when expectedIndex < steps.Length -> steps[expectedIndex] = (identity, modifiers)
                | _ -> false
        if state.Profile.Bindings |> List.exists (uses logical) then logical
        elif state.Profile.Bindings |> List.exists (uses physical) then physical
        else logical

    let dispatch source gesture phase isRepeat nativeEditable hostReserved composing eventId =
        apply (CommandResolverObservation.InputEvent
            { Id = eventId
              Source = source
              Gesture = gesture
              Phase = phase
              IsRepeat = isRepeat
              NativeEditable = nativeEditable
              HostReserved = hostReserved
              IsComposing = composing
              AvailableCommands = availableCommands() })

    let keyDown (event: Event) =
        let keyboard = event :?> KeyboardEvent
        let native = InputDom.nativeInteractive event.target
        if not native then
            let eventId = nextId "keyboard"
            let source = "keyboard:" + keyboard.code
            let gesture = keyboardGesture keyboard
            let effects =
                if keyboard.key = "Escape" && (state.CaptureRestoreFocus.IsSome || not state.ModalStack.IsEmpty || state.PendingSequence.IsSome) then
                    apply (CommandResolverObservation.Escape(eventId, source))
                else
                    keys[keyboard.code] <- gesture
                    dispatch source gesture InputGesturePhase.Pressed (InputDom.repeat keyboard) false (catalog.ReservedGestures |> List.contains gesture) (InputDom.composing keyboard || state.IsComposing) eventId
            if accepted eventId effects then event.preventDefault()

    let keyUp (event: Event) =
        let keyboard = event :?> KeyboardEvent
        match keys.TryGetValue keyboard.code with
        | true, gesture ->
            keys.Remove keyboard.code |> ignore
            let eventId = nextId "keyboard"
            let effects = dispatch ("keyboard:" + keyboard.code) gesture InputGesturePhase.Released false false false (InputDom.composing keyboard || state.IsComposing) eventId
            if accepted eventId effects then event.preventDefault()
        | _ -> ()

    let pointerGesture (pointer: PointerEvent) =
        let value = InputDom.action pointer.target
        if InputDom.pointerType pointer = "touch" then InputGesture.Touch value else InputGesture.Pointer value

    let pointerDown (event: Event) =
        let pointer = event :?> PointerEvent
        if not (InputDom.nativeInteractive event.target) then
            let gesture = pointerGesture pointer
            let pointerId = int pointer.pointerId
            let source = $"{InputDom.pointerType pointer}:{pointerId}"
            let eventId = nextId source
            pointers[pointerId] <- gesture
            let effects = dispatch source gesture InputGesturePhase.Pressed false false (catalog.ReservedGestures |> List.contains gesture) false eventId
            if accepted eventId effects then
                InputDom.capturePointer root pointerId
                event.preventDefault()

    let releasePointer (event: Event) =
        let pointer = event :?> PointerEvent
        let pointerId = int pointer.pointerId
        match pointers.TryGetValue pointerId with
        | true, gesture ->
            pointers.Remove pointerId |> ignore
            let source = $"{InputDom.pointerType pointer}:{pointerId}"
            let eventId = nextId source
            let effects = dispatch source gesture InputGesturePhase.Released false false false false eventId
            if InputDom.hasPointerCapture root pointerId then InputDom.releasePointer root pointerId
            if accepted eventId effects then event.preventDefault()
        | _ -> ()

    let neutralize observation =
        keys.Clear()
        pointers.Clear()
        pads <- Map.empty
        apply observation |> ignore

    let pollPads () =
        if not disposed then
            let current =
                options.Gamepads()
                |> List.map (fun pad -> pad.Source, (pad.Buttons |> List.indexed |> List.choose (fun (index, pressed) -> if pressed then Some index else None) |> Set.ofList))
                |> Map.ofList
            for KeyValue(source, previous) in pads do
                match current |> Map.tryFind source with
                | None -> apply (CommandResolverObservation.SourceDisconnected source) |> ignore
                | Some now ->
                    for index in Set.difference previous now do
                        dispatch source (InputGesture.Gamepad $"button-{index}") InputGesturePhase.Released false false false false (nextId source) |> ignore
            for KeyValue(source, now) in current do
                let previous = pads |> Map.tryFind source |> Option.defaultValue Set.empty
                for index in Set.difference now previous do
                    dispatch source (InputGesture.Gamepad $"button-{index}") InputGesturePhase.Pressed false false false false (nextId source) |> ignore
            pads <- current

    let rec scheduleFrame () =
        if options.PollGamepads && not disposed then
            frame <- Some(InputDom.requestFrame (fun _ -> frame <- None; pollPads (); scheduleFrame ()))

    do
        if options.SequenceTimeoutMilliseconds < 1 || options.SequenceTimeoutMilliseconds > 60000 then
            invalidArg (nameof options) "sequence timeout must be between 1 and 60000 milliseconds"
        listen (root :> EventTarget) "keydown" keyDown
        listen (root :> EventTarget) "keyup" keyUp
        listen (root :> EventTarget) "compositionstart" (fun _ -> apply CommandResolverObservation.CompositionStarted |> ignore)
        listen (root :> EventTarget) "compositionend" (fun _ -> apply CommandResolverObservation.CompositionEnded |> ignore)
        listen (root :> EventTarget) "pointerdown" pointerDown
        listen (root :> EventTarget) "pointerup" releasePointer
        listen (root :> EventTarget) "pointercancel" releasePointer
        listen (root :> EventTarget) "lostpointercapture" releasePointer
        listen (root :> EventTarget) "focusout" (fun event ->
            let related = InputDom.relatedTarget (event :?> FocusEvent)
            if isNull related || not (InputDom.contains related root) then neutralize CommandResolverObservation.FocusLost)
        listen (window :> EventTarget) "blur" (fun _ -> neutralize CommandResolverObservation.FocusLost)
        listen (document :> EventTarget) "visibilitychange" (fun _ -> if InputDom.documentHidden() then neutralize CommandResolverObservation.FocusLost)
        scheduleFrame ()

    member _.State = state
    member _.Update observation = apply observation
    member _.PollGamepadsOnce() = pollPads ()
    member _.Observe() =
        { OwnedListenerCount = if disposed then 0 else listeners.Count
          OwnedDeadlineCount = deadlines.Count
          GamepadPollScheduled = frame.IsSome
          OwnedSourceCount = state.OwnedPresses |> List.map _.Source |> List.distinct |> List.length
          IsDisposed = disposed }

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                neutralize CommandResolverObservation.Dispose
                disposed <- true
                for target, name, handler in listeners do target.removeEventListener(name, handler)
                listeners.Clear()
                deadlines.Values |> Seq.iter window.clearTimeout
                deadlines.Clear()
                frame |> Option.iter InputDom.cancelFrame
                frame <- None

[<RequireQualifiedAccess>]
module SvgInputHost =
    let browserGamepads () = InputDom.gamepads () |> Array.toList
    let defaultOptions =
        { SequenceTimeoutMilliseconds = 750
          PollGamepads = true
          Gamepads = browserGamepads }
