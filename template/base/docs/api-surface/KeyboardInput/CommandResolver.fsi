// See skill: fs-gg-keyboard-input
namespace FS.GG.UI.KeyboardInput

/// <summary>One normalized event delivered by a host.</summary>
type CommandInputEvent =
    { /// <summary>Host-supplied event identity used for deterministic effects.</summary>
      Id: InputEventId
      /// <summary>The device or pointer that produced the event.</summary>
      Source: InputSourceId
      /// <summary>The typed gesture identity.</summary>
      Gesture: InputGesture
      /// <summary>Press or release phase.</summary>
      Phase: InputGesturePhase
      /// <summary>Whether this is an operating-system repeat observation.</summary>
      IsRepeat: bool
      /// <summary>Whether native editing owns the event.</summary>
      NativeEditable: bool
      /// <summary>Whether the host or operating system reserves the event.</summary>
      HostReserved: bool
      /// <summary>Whether the event belongs to an active composition.</summary>
      IsComposing: bool
      /// <summary>Commands whose current product guards permit invocation.</summary>
      AvailableCommands: CommandId list }

/// <summary>A semantic invocation admitted by the resolver.</summary>
type CommandInvocation =
    { /// <summary>Event that caused admission.</summary>
      EventId: InputEventId
      /// <summary>Source that caused admission.</summary>
      Source: InputSourceId
      /// <summary>Admitted command identity.</summary>
      Command: CommandId }

/// <summary>Pure effects interpreted by the host or product.</summary>
[<RequireQualifiedAccess>]
type CommandResolverEffect =
    /// <summary>Invoke one currently available semantic command.</summary>
    | InvokeCommand of CommandInvocation
    /// <summary>A continuous command changed aggregate held state.</summary>
    | HeldActionChanged of command: CommandId * isHeld: bool
    /// <summary>Ask the host to deliver an explicit deadline observation.</summary>
    | RequestDeadline of token: string
    /// <summary>Cancel a previously requested deadline.</summary>
    | CancelDeadline of token: string
    /// <summary>Ask the host to focus a stable target.</summary>
    | RequestFocus of target: string
    /// <summary>Return an otherwise unbound raw gesture to binding capture.</summary>
    | CapturedGesture of InputGesture
    /// <summary>The delivered event was accepted and may be prevented by its host.</summary>
    | PreventDefault of eventId: InputEventId
    /// <summary>Report a deterministic recovery or stale-observation diagnostic.</summary>
    | ResolverDiagnostic of code: string

/// <summary>Ordered observations accepted by the pure resolver.</summary>
[<RequireQualifiedAccess>]
type CommandResolverObservation =
    /// <summary>A normalized device event.</summary>
    | InputEvent of CommandInputEvent
    /// <summary>A host-delivered sequence deadline with current availability.</summary>
    | DeadlineElapsed of token: string * availableCommands: CommandId list
    /// <summary>Replace owner-projected contexts immediately.</summary>
    | ContextsChanged of string list
    /// <summary>Replace the effective profile immediately.</summary>
    | ProfileChanged of EffectiveInputProfile
    /// <summary>Begin raw gesture capture.</summary>
    | BeginCapture of restoreFocus: string
    /// <summary>Cancel raw gesture capture.</summary>
    | CancelCapture
    /// <summary>Push an input-owned modal frame.</summary>
    | PushModal of InputModalFrame
    /// <summary>Pop the innermost input-owned modal frame.</summary>
    | PopModal
    /// <summary>Cancel the innermost interaction without fallthrough.</summary>
    | Escape of eventId: InputEventId * source: InputSourceId
    /// <summary>Native composition began.</summary>
    | CompositionStarted
    /// <summary>Native composition ended.</summary>
    | CompositionEnded
    /// <summary>The owned focus scope was lost.</summary>
    | FocusLost
    /// <summary>A new modal owner took over the host.</summary>
    | ModalTakenOver
    /// <summary>One source disconnected.</summary>
    | SourceDisconnected of InputSourceId
    /// <summary>Permanently neutralize and dispose the reducer.</summary>
    | Dispose

/// <summary>Portable resolver state. Product tool and simulation modes remain external projections.</summary>
type CommandResolverState =
    { /// <summary>Current effective binding profile.</summary>
      Profile: EffectiveInputProfile
      /// <summary>Owner-projected active catalog contexts.</summary>
      ActiveContexts: string list
      /// <summary>Input-owned modal stack, innermost last.</summary>
      ModalStack: InputModalFrame list
      /// <summary>Optional raw binding-capture focus target.</summary>
      CaptureRestoreFocus: string option
      /// <summary>Current sequence prefix.</summary>
      PendingSequence: PendingInputSequence option
      /// <summary>Source-owned presses used for repeat suppression and held recovery.</summary>
      OwnedPresses: OwnedCommandPress list
      /// <summary>Whether native composition currently owns keyboard input.</summary>
      IsComposing: bool
      /// <summary>Whether disposal has made the reducer inert.</summary>
      IsDisposed: bool }

/// <summary>Stable host-supplied identity for one delivered input event.</summary>
type InputEventId = string

/// <summary>Whether a gesture was pressed or released.</summary>
[<RequireQualifiedAccess>]
type InputGesturePhase =
    /// <summary>The gesture became active.</summary>
    | Pressed
    /// <summary>The gesture became inactive.</summary>
    | Released

/// <summary>An input-owned modal frame and its valid focus restoration target.</summary>
type InputModalFrame =
    { /// <summary>Catalog context activated by the frame.</summary>
      Context: string
      /// <summary>Stable control or scene target requested when the frame closes.</summary>
      RestoreFocus: string }

/// <summary>Stable identity for one physical or virtual input source.</summary>
type InputSourceId = string

/// <summary>A press retained until its owning source releases or is neutralized.</summary>
type OwnedCommandPress =
    { /// <summary>Source that owns the press.</summary>
      Source: InputSourceId
      /// <summary>Gesture whose release ends the press.</summary>
      Gesture: InputGesture
      /// <summary>Resolved semantic command.</summary>
      Command: CommandId
      /// <summary>Trigger policy captured at press time.</summary>
      Trigger: CommandTriggerPolicy }

/// <summary>A pending keyboard prefix awaiting a continuation or injected deadline.</summary>
type PendingInputSequence =
    { /// <summary>Ordered logical or physical key steps already accepted.</summary>
      Steps: (InputKeyIdentity * InputModifiers) list
      /// <summary>Deterministic deadline token the host must return.</summary>
      DeadlineToken: string
      /// <summary>Terminal command deferred because the terminal is also a prefix.</summary>
      TerminalCommand: CommandId option
      /// <summary>Context for the deferred terminal command.</summary>
      TerminalContext: string option }

/// <summary>Pure deterministic command resolution.</summary>
[<RequireQualifiedAccess>]
module CommandResolver =
    /// <summary>Creates resolver state from a validated profile and owner-projected contexts.</summary>
    val init: activeContexts: string list -> profile: EffectiveInputProfile -> CommandResolverState

    /// <summary>Reduces one ordered observation without reading clocks, IDs, DOM, storage, or network state.</summary>
    val update:
        catalog: InputCatalog ->
        observation: CommandResolverObservation ->
        state: CommandResolverState ->
        CommandResolverState * CommandResolverEffect list
