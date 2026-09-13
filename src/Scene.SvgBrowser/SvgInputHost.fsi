namespace FS.GG.UI.Scene.SvgBrowser

open System
open Browser.Types
open FS.GG.UI.KeyboardInput

/// <summary>One browser Gamepad API snapshot, expressed without synthetic keyboard events.</summary>
type SvgGamepadSnapshot =
    { /// <summary>Stable source identity for the connected pad.</summary>
      Source: InputSourceId
      /// <summary>Pressed state for each button in browser index order.</summary>
      Buttons: bool list }

/// <summary>Host policy and injectable browser observations for the SVG command adapter.</summary>
type SvgInputHostOptions =
    { /// <summary>Delay used when the resolver requests a sequence deadline.</summary>
      SequenceTimeoutMilliseconds: int
      /// <summary>Whether the host continuously polls the Gamepad API.</summary>
      PollGamepads: bool
      /// <summary>Returns the current pads; production callers normally use <c>SvgInputHost.browserGamepads</c>.</summary>
      Gamepads: unit -> SvgGamepadSnapshot list }

/// <summary>Bounded ownership counters exposed for browser lifecycle evidence.</summary>
type SvgInputHostObservation =
    { /// <summary>Listeners still owned by the host.</summary>
      OwnedListenerCount: int
      /// <summary>Sequence deadlines still owned by the host.</summary>
      OwnedDeadlineCount: int
      /// <summary>Whether a Gamepad API animation-frame poll is scheduled.</summary>
      GamepadPollScheduled: bool
      /// <summary>Keyboard, pointer, touch, and gamepad sources with retained presses.</summary>
      OwnedSourceCount: int
      /// <summary>Whether disposal has made the adapter inert.</summary>
      IsDisposed: bool }

/// <summary>
/// Disposable DOM adapter that normalizes keyboard, pointer, touch, and Gamepad API observations
/// into the portable command resolver while preserving native editing and focus behavior.
/// </summary>
[<Sealed>]
type SvgInputHost =
    /// <summary>Creates and attaches an adapter to one owned focus scope.</summary>
    new:
        root: HTMLElement *
        catalog: InputCatalog *
        initialState: CommandResolverState *
        availableCommands: (unit -> CommandId list) *
        onEffect: (CommandResolverEffect -> unit) *
        options: SvgInputHostOptions -> SvgInputHost

    /// <summary>The current pure resolver state.</summary>
    member State: CommandResolverState

    /// <summary>Delivers an owner observation such as a context, profile, modal, or capture change.</summary>
    member Update: observation: CommandResolverObservation -> CommandResolverEffect list

    /// <summary>Samples the configured Gamepad API provider once.</summary>
    member PollGamepadsOnce: unit -> unit

    /// <summary>Returns lifecycle ownership evidence without mutating the host.</summary>
    member Observe: unit -> SvgInputHostObservation

    interface IDisposable

/// <summary>Construction helpers for the browser command adapter.</summary>
[<RequireQualifiedAccess>]
module SvgInputHost =
    /// <summary>Reads connected pads from <c>navigator.getGamepads()</c>.</summary>
    val browserGamepads: unit -> SvgGamepadSnapshot list

    /// <summary>Default sequence timeout and live Gamepad API polling policy.</summary>
    val defaultOptions: SvgInputHostOptions

