module AppRoot.GameShell

// ============================================================================================
// GENERIC GAME SHELL (issue #991) — a reusable menu / start screen + settings + key rebinding
// that every scaffolded game gets out of the box, so a game does not hand-roll its own.
//
//   YOURS TO ADAPT, but game-AGNOSTIC by design. This module owns no gameplay: it is a pure
//   Elmish state machine (a screen router) plus view helpers that COMPOSE the framework pieces
//   the shell requirement names — it rebuilds none of them:
//     * key rebinding  — `KeyRebind` (the config-screen control) over the immutable `Keymap`
//       (rebind mechanism) with `KeymapCodec` for persistence, captured through the
//       `ViewerKeyboard.mapKeyRaw` seam (the forwarding seam a rebind capture must use — see the
//       fs-gg-keyboard-input skill's "capturing a key for a rebind" recipe);
//     * resolution / fullscreen — a `DisplaySettings` mapped onto a `ViewerWindowBehaviorRequest`
//       (window startup state) and `LogicalCanvas` (the fixed-logical-resolution letterbox, #246);
//     * UI — the typed `Controls` front door (Button / Stack / TextBlock) plus the `KeyRebind`
//       control, lifted into the same widget tree with `Widget.ofControl`.
//
//   A game parameterizes the shell with a `Config` (its NAME, its rebindable key→command
//   `Keymap`, and the resolutions/modes it offers) and threads the shell `Msg`/`Effect` through
//   its own Elmish loop. See the `fs-gg-game-shell` skill for the host wiring (the pointer-aware
//   interactive host + the `mapKeyRaw` raw-key seam).
//
//   Effort note (#991 is XL): this is the tractable CORE — the router, the settings model, the
//   rebind capture, and the display/keymap seams — deterministic and host-free. The turnkey
//   default-launch wiring, display-settings persistence, and release-lane behaviour tests are
//   decomposed into child follow-ups; see the issue.
// ============================================================================================

open System
open System.IO
open System.Text.Json
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls

// The typed Props front door (aliased exactly as the Controls starter does, so these names are
// the typed builders here and the legacy stringly ones stay available fully-qualified).
module Button = FS.GG.UI.Controls.Typed.Button
module Stack = FS.GG.UI.Controls.Typed.Stack
module TextBlock = FS.GG.UI.Controls.Typed.TextBlock

/// The screens the shell routes between. `Playing` is the only screen the shell does NOT draw
/// over — the game owns it — while `MainMenu`, `Paused`, and `Settings` are shell chrome. The
/// launch screen and the Esc-pause overlay are the SAME menu surface, reached from different
/// screens (the requirement: "the same menu is the launch screen").
type Screen =
    | MainMenu
    | Playing
    | Paused
    | Settings

/// How the window presents. Maps onto SkiaViewer's `ViewerWindowStartupState` in `windowBehavior`.
type DisplayMode =
    | Windowed
    | Borderless
    | Fullscreen

/// The display half of settings: the fixed logical resolution the game renders in (letterboxed
/// onto whatever surface via `LogicalCanvas`) plus how the window presents.
type DisplaySettings =
    { Resolution: Size
      Mode: DisplayMode }

/// The state a game supplies once to parameterize the shell for itself.
type Config =
    { /// The game's name — the title label on the main menu.
      Title: string
      /// The rebindable key→command set and its default bindings.
      DefaultKeymap: Keymap
      /// The display modes the settings screen offers, in menu order.
      DisplayModes: DisplayMode list
      /// The resolutions the settings screen offers, in menu order.
      Resolutions: Size list
      /// The display settings the game starts in.
      InitialDisplay: DisplaySettings }

/// The shell's whole state. A game embeds this in its own model and threads `Msg` through
/// `update`; the game's own model carries gameplay, which the shell never touches.
type Model =
    { Screen: Screen
      Display: DisplaySettings
      Keymap: Keymap
      /// The command currently awaiting its next key press (a rebind in flight), or `None`.
      Rebinding: CommandId option
      /// The screen `Back`/Esc returns to when leaving `Settings` (`MainMenu` or `Paused`).
      SettingsReturn: Screen }

/// The messages the shell reduces. A game maps these into its own `Msg` (e.g. `Shell of Msg`).
type Msg =
    /// MainMenu → Playing.
    | Start
    /// MainMenu or Paused → Settings.
    | OpenSettings
    /// Settings → wherever it was opened from.
    | LeaveSettings
    /// Ask the host to close the app.
    | Quit
    /// Playing → Paused.
    | PauseGame
    /// Paused → Playing.
    | ResumeGame
    /// Choose the logical resolution.
    | SetResolution of Size
    /// Choose how the window presents.
    | SetDisplayMode of DisplayMode
    /// Begin capturing the next key press as the new binding for a command (from the rebind UI).
    | ArmRebind of CommandId
    /// The next raw key after `ArmRebind` — completes the capture (or cancels, if it is the menu key).
    | CaptureKey of KeyId
    /// Abandon an in-flight capture without rebinding.
    | CancelRebind
    /// The universal Esc route (pause / resume / back / cancel-capture).
    | EscapePressed

/// A shell-level intent for the host to interpret — the effects `update` emits. The host applies
/// these at its own boundary (there is no shell runner; the game routes them, like any Elmish effect).
type Effect =
    /// The player chose Exit — the host should close the window.
    | ExitRequested
    /// The display settings changed — the host should re-apply the window behaviour + logical size
    /// (`windowBehavior` / `logicalSize`).
    | DisplayChanged of DisplaySettings
    /// The keymap changed (a rebind fired) — the host should persist it (`encodeKeymap`).
    | KeymapChanged of Keymap

/// The `KeyId` the shell treats as the universal menu / back / pause / cancel key (Esc).
let menuKey: KeyId = ViewerKeyboard.toKeyId ViewerKey.Escape

/// The initial shell state for a game: the main menu, the game's default keymap and display.
let init (config: Config) : Model =
    { Screen = MainMenu
      Display = config.InitialDisplay
      Keymap = config.DefaultKeymap
      Rebinding = None
      SettingsReturn = MainMenu }

/// The pure Esc router. A capture in flight is cancelled first (Esc never rebinds to Escape);
/// otherwise Esc pauses live play, resumes a pause, or backs out of settings. At the main menu it
/// is a no-op (the menu is already showing).
let private routeEscape (model: Model) : Model * Effect list =
    match model.Rebinding with
    | Some _ -> { model with Rebinding = None }, []
    | None ->
        match model.Screen with
        | Playing -> { model with Screen = Paused }, []
        | Paused -> { model with Screen = Playing }, []
        | Settings -> { model with Screen = model.SettingsReturn; Rebinding = None }, []
        | MainMenu -> model, []

/// The shell reducer. Pure and total — every transition is deterministic and host-free, so a
/// scripted `Msg` sequence replays identically.
let update (msg: Msg) (model: Model) : Model * Effect list =
    match msg with
    | Start ->
        match model.Screen with
        | MainMenu -> { model with Screen = Playing }, []
        | _ -> model, []
    | OpenSettings ->
        match model.Screen with
        | MainMenu
        | Paused -> { model with Screen = Settings; SettingsReturn = model.Screen; Rebinding = None }, []
        | _ -> model, []
    | LeaveSettings ->
        match model.Screen with
        | Settings -> { model with Screen = model.SettingsReturn; Rebinding = None }, []
        | _ -> model, []
    | Quit -> model, [ ExitRequested ]
    | PauseGame ->
        match model.Screen with
        | Playing -> { model with Screen = Paused }, []
        | _ -> model, []
    | ResumeGame ->
        match model.Screen with
        | Paused -> { model with Screen = Playing }, []
        | _ -> model, []
    | SetResolution size ->
        let display = { model.Display with Resolution = size }
        { model with Display = display }, [ DisplayChanged display ]
    | SetDisplayMode mode ->
        let display = { model.Display with Mode = mode }
        { model with Display = display }, [ DisplayChanged display ]
    | ArmRebind command ->
        match model.Screen with
        | Settings -> { model with Rebinding = Some command }, []
        | _ -> model, []
    | CaptureKey key ->
        match model.Rebinding with
        // Esc during a capture cancels it — a rebind never binds a command to the menu key.
        | Some _ when key = menuKey -> { model with Rebinding = None }, []
        | Some command ->
            let keymap = Keymap.rebind key command model.Keymap
            { model with Keymap = keymap; Rebinding = None }, [ KeymapChanged keymap ]
        | None -> model, []
    | CancelRebind -> { model with Rebinding = None }, []
    | EscapePressed -> routeEscape model

// ---- display seams --------------------------------------------------------------------------

/// The `ViewerWindowBehaviorRequest` for a display setting: the window's presentation mode maps
/// onto a `ViewerWindowStartupState` (windowed / borderless work-area / exclusive fullscreen); the
/// rest of the request keeps the framework defaults.
let windowBehavior (display: DisplaySettings) : ViewerWindowBehaviorRequest =
    let startupState, resizePolicy =
        match display.Mode with
        | Windowed -> ViewerWindowStartupState.Normal, Resizable
        | Borderless -> ViewerWindowStartupState.WindowedFullscreen, FixedSize
        | Fullscreen -> ViewerWindowStartupState.Fullscreen, FixedSize

    { Viewer.defaultWindowBehavior with
        ResizePolicy = resizePolicy
        StartupState = startupState }

/// The fixed logical canvas size a game renders in for a display setting — feed it to
/// `ViewerOptions.LogicalSize` so the chosen resolution letterboxes onto any surface (#246).
let logicalSize (display: DisplaySettings) : Size = display.Resolution

/// The uniform, centered fit of the chosen resolution inside a given output surface — the pure
/// arithmetic (`LogicalCanvas.fit`) behind the letterbox, so a game can assert it with no window.
let logicalFit (display: DisplaySettings) (surface: Size) : LogicalCanvasFit =
    LogicalCanvas.fit display.Resolution surface

// ---- raw-key routing ------------------------------------------------------------------------

/// What a raw key-down means right now, given the shell state — the single decision the host's
/// `mapKeyRaw` seam consults. `Game` carries the value a live-play command resolves to; the shell
/// resolves the key through the (possibly rebound) keymap only while `Playing` and not capturing.
type KeyOutcome<'game> =
    /// Feed this shell `Msg` back into `update` (a capture completion, or an Esc route).
    | ShellMsg of Msg
    /// Gameplay is live and the keymap resolves the key to this game value; the game acts on it.
    | Game of 'game
    /// The key means nothing right now.
    | NoInput

/// Route a raw key-DOWN. `toGame` lifts a resolved live-play `CommandId` into the game's own value
/// (return `None` to decline). This is the whole raw-key contract the shell needs from the host:
/// wire the host's `mapKeyRaw` to forward every key-down here, dispatch a `ShellMsg` through
/// `update`, and hand a `Game` value to the game. A capture in flight swallows the next key
/// (`CaptureKey`); otherwise the menu key routes (`EscapePressed`) and, only while `Playing`, an
/// unbound-for-chrome key resolves through the keymap.
let routeKeyDown (toGame: CommandId -> 'game option) (key: KeyId) (model: Model) : KeyOutcome<'game> =
    match model.Rebinding with
    | Some _ -> ShellMsg(CaptureKey key)
    | None ->
        if key = menuKey then
            ShellMsg EscapePressed
        else
            match model.Screen with
            | Playing ->
                match Keymap.resolve model.Keymap key with
                | Some command ->
                    match toGame command with
                    | Some value -> Game value
                    | None -> NoInput
                | None -> NoInput
            | MainMenu
            | Paused
            | Settings -> NoInput

// ---- persistence ----------------------------------------------------------------------------

/// Serialize the current bindings to the versioned, deterministic keymap JSON envelope — the
/// blob the host writes so a player's rebindings survive a restart (the MUST persistence of #991).
let encodeKeymap (model: Model) : byte[] = KeymapCodec.encode model.Keymap

/// Restore bindings from a blob `encodeKeymap` produced. A decode failure (a corrupt or absent
/// file) is total: the model's current keymap is kept, so a bad save degrades to the defaults
/// rather than throwing at startup.
let decodeKeymap (bytes: byte[]) (model: Model) : Model =
    match KeymapCodec.decode bytes with
    | Ok keymap -> { model with Keymap = keymap }
    | Error _ -> model

// The display half of settings persists on the SAME seam as the bindings (#1001): a small,
// self-contained JSON envelope beside the keymap one — no framework codec, no package dependency,
// so JSON stays the single contract format. Deterministic by construction: a fixed field order
// and a `Utf8JsonWriter` with no formatting options give byte-identical output for equal state.

/// The versioned format marker the display envelope carries — the first thing `decodeDisplay`
/// validates, mirroring the keymap codec's `format`/`version` shape.
let private displayFormatId = "fsgg.gameshell.display"

/// The composed settings envelope's marker: the display object alongside the keymap object, so a
/// host can save the WHOLE settings screen (display + bindings) as one blob on one seam.
let private settingsFormatId = "fsgg.gameshell.settings"

/// The one format version both envelopes are stamped with. A future field change bumps it, and an
/// older/newer blob then degrades to the current model rather than mis-restoring.
let private settingsFormatVersion = 1

/// The stable on-disk token for a display mode. A closed round-trip with `modeOfToken`.
let private modeToken (mode: DisplayMode) : string =
    match mode with
    | Windowed -> "windowed"
    | Borderless -> "borderless"
    | Fullscreen -> "fullscreen"

/// The inverse of `modeToken` — an unknown token is `None` (an envelope from a newer game degrades).
let private modeOfToken (token: string) : DisplayMode option =
    match token with
    | "windowed" -> Some Windowed
    | "borderless" -> Some Borderless
    | "fullscreen" -> Some Fullscreen
    | _ -> None

/// Write one self-contained display object (`format`/`version`/`width`/`height`/`mode`, in that
/// fixed order) at the writer's current position — the standalone display blob AND the `display`
/// member of the settings envelope, so both share the exact same shape and reader.
let private writeDisplayObject (writer: Utf8JsonWriter) (display: DisplaySettings) =
    writer.WriteStartObject()
    writer.WriteString("format", displayFormatId)
    writer.WriteNumber("version", settingsFormatVersion)
    writer.WriteNumber("width", display.Resolution.Width)
    writer.WriteNumber("height", display.Resolution.Height)
    writer.WriteString("mode", modeToken display.Mode)
    writer.WriteEndObject()

/// Total parse of a display object written by `writeDisplayObject`. Any deviation — wrong kind,
/// missing/mistyped field, wrong format/version, or an unknown mode — is `None`, so a caller keeps
/// the model it already has rather than throwing.
let private readDisplayObject (element: JsonElement) : DisplaySettings option =
    if element.ValueKind <> JsonValueKind.Object then
        None
    else
        let stringOf (name: string) =
            match element.TryGetProperty name with
            | true, value when value.ValueKind = JsonValueKind.String ->
                match value.GetString() with
                | null -> None
                | text -> Some text
            | _ -> None

        let intOf (name: string) =
            match element.TryGetProperty name with
            | true, value when value.ValueKind = JsonValueKind.Number ->
                match value.TryGetInt32() with
                | true, number -> Some number
                | _ -> None
            | _ -> None

        match stringOf "format", intOf "version", intOf "width", intOf "height", stringOf "mode" with
        | Some format, Some version, Some width, Some height, Some mode when
            format = displayFormatId && version = settingsFormatVersion ->
            modeOfToken mode
            |> Option.map (fun mode -> { Resolution = { Width = width; Height = height }; Mode = mode })
        | _ -> None

/// Re-serialize one parsed JSON element to its own byte blob — how the settings envelope hands its
/// embedded `keymap` member back to `KeymapCodec` (via `decodeKeymap`) unchanged.
let private elementToBytes (element: JsonElement) : byte[] =
    use stream = new MemoryStream()
    use writer = new Utf8JsonWriter(stream)
    element.WriteTo writer
    writer.Flush()
    stream.ToArray()

/// Serialize the current display settings to the versioned, deterministic JSON envelope — the
/// display counterpart of `encodeKeymap`, so resolution + mode survive a restart on the same seam.
let encodeDisplay (model: Model) : byte[] =
    use stream = new MemoryStream()
    use writer = new Utf8JsonWriter(stream)
    writeDisplayObject writer model.Display
    writer.Flush()
    stream.ToArray()

/// Restore display settings from a blob `encodeDisplay` produced. Like `decodeKeymap`, it is total:
/// a corrupt, absent, or newer-format file keeps the model's current display, so a bad save degrades
/// to the game's initial resolution/mode rather than throwing at startup.
let decodeDisplay (bytes: byte[]) (model: Model) : Model =
    try
        use document = JsonDocument.Parse(ReadOnlyMemory bytes)
        match readDisplayObject document.RootElement with
        | Some display -> { model with Display = display }
        | None -> model
    with :? JsonException ->
        model

/// Serialize the WHOLE settings screen — display + bindings — to one deterministic envelope that
/// COMPOSES the display codec and `KeymapCodec`: a fixed-order object with a `display` member
/// (`writeDisplayObject`) and a `keymap` member (the embedded `KeymapCodec.encode` object). A host
/// that wants a single settings file writes this instead of the two blobs separately.
let encodeSettings (model: Model) : byte[] =
    use stream = new MemoryStream()
    use writer = new Utf8JsonWriter(stream)
    writer.WriteStartObject()
    writer.WriteString("format", settingsFormatId)
    writer.WriteNumber("version", settingsFormatVersion)
    writer.WritePropertyName("display")
    writeDisplayObject writer model.Display
    writer.WritePropertyName("keymap")
    use keymap = JsonDocument.Parse(ReadOnlyMemory(KeymapCodec.encode model.Keymap))
    keymap.RootElement.WriteTo writer
    writer.WriteEndObject()
    writer.Flush()
    stream.ToArray()

/// Restore the whole settings screen from a blob `encodeSettings` produced. Total and per-member:
/// each of `display` and `keymap` is applied through its own total decode, so a member that is
/// missing or corrupt leaves that half of the model at its current value while the other half still
/// restores — a partial save never throws and never wholesale-resets the settings.
let decodeSettings (bytes: byte[]) (model: Model) : Model =
    try
        use document = JsonDocument.Parse(ReadOnlyMemory bytes)
        let root = document.RootElement

        if root.ValueKind <> JsonValueKind.Object then
            model
        else
            let withDisplay (current: Model) =
                match root.TryGetProperty "display" with
                | true, element ->
                    match readDisplayObject element with
                    | Some display -> { current with Display = display }
                    | None -> current
                | _ -> current

            let withKeymap (current: Model) =
                match root.TryGetProperty "keymap" with
                | true, element -> decodeKeymap (elementToBytes element) current
                | _ -> current

            model |> withDisplay |> withKeymap
    with :? JsonException ->
        model

// ---- view -----------------------------------------------------------------------------------

/// The menu chrome for the current screen, as a typed widget tree, or `None` while `Playing` (the
/// game owns the screen then — the shell draws nothing over it). `dispatch` embeds a shell `Msg`
/// into the game's own message type. The settings screen wires the resolution + display-mode
/// choices and the `KeyRebind` control (each command row's rebind affordance dispatches
/// `ArmRebind`, which arms the `mapKeyRaw` capture).
let view (dispatch: Msg -> 'msg) (config: Config) (model: Model) : Widget<'msg> option =
    let button (id: string) (label: string) (msg: Msg) =
        Button.view
            { Button.defaults with
                Id = Some id
                Text = label
                OnClick = Some(dispatch msg) }

    let title (text: string) = TextBlock.view { TextBlock.defaults with Text = text }
    let stack (children: Widget<'msg> list) = Stack.view { Stack.defaults with Children = children }

    match model.Screen with
    | Playing -> None
    | MainMenu ->
        Some(
            stack
                [ title config.Title
                  button "start" "Start" Start
                  button "config" "Config" OpenSettings
                  button "exit" "Exit" Quit ]
        )
    | Paused ->
        Some(
            stack
                [ title (config.Title + " — Paused")
                  button "resume" "Resume" ResumeGame
                  button "config" "Config" OpenSettings
                  button "exit" "Exit" Quit ]
        )
    | Settings ->
        let modeLabel mode =
            match mode with
            | Windowed -> "Windowed"
            | Borderless -> "Borderless"
            | Fullscreen -> "Fullscreen"

        let modeButton mode =
            let selectedMark = if model.Display.Mode = mode then "> " else ""
            button ("mode-" + (modeLabel mode).ToLowerInvariant()) (selectedMark + modeLabel mode) (SetDisplayMode mode)

        let resButton (size: Size) =
            let label = sprintf "%dx%d" size.Width size.Height
            let selectedMark = if model.Display.Resolution = size then "> " else ""
            button ("res-" + label) (selectedMark + label) (SetResolution size)

        // The KeyRebind config-screen control (a legacy `Control<'msg>`), lifted into the typed
        // tree with `Widget.ofControl`. Activating a command's rebind affordance dispatches
        // `ArmRebind` — the capture then fires on the next key via `routeKeyDown`.
        let rebindWidget =
            KeyRebind.ofKeymap model.Keymap [ KeyRebind.onRebind (fun command -> dispatch (ArmRebind command)) ]
            |> Widget.ofControl

        let rebindHint =
            match model.Rebinding with
            | Some command -> "Press a key for: " + command + " (Esc to cancel)"
            | None -> "Choose a command to rebind"

        let children =
            [ title (config.Title + " — Settings")
              title "Display mode" ]
            @ (config.DisplayModes |> List.map modeButton)
            @ [ title "Resolution" ]
            @ (config.Resolutions |> List.map resButton)
            @ [ title "Controls"; title rebindHint; rebindWidget ]
            @ [ button "back" "Back" LeaveSettings ]

        Some(stack children)
