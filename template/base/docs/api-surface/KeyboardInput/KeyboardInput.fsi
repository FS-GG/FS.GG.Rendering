// See skill: fs-gg-keyboard-input
namespace FS.GG.UI.KeyboardInput

/// Public contract type exposed by this FS.GG.UI package.
type CommandId = string
/// Public contract type exposed by this FS.GG.UI package.
type KeyId = string

/// Public contract type exposed by this FS.GG.UI package.
type ViewerKey =
    | ArrowLeft
    | ArrowRight
    | ArrowUp
    | ArrowDown
    | Enter
    | Space
    | Escape
    | Backspace
    | Letter of char
    | Digit of int
    | Function of int
    | Unknown of raw: string

/// Public contract type exposed by this FS.GG.UI package.
type ViewerKeyDirection =
    | KeyDown
    | KeyUp

/// Public contract type exposed by this FS.GG.UI package.
type ViewerKeyEvent =
    { RawKey: string
      Direction: ViewerKeyDirection }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardBinding =
    { Key: KeyId
      Command: CommandId }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardDiagnostic =
    { Code: string
      Severity: string
      Message: string
      Key: KeyId option }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplay =
    { PressedKeys: KeyId list
      ActiveLayout: string
      ActiveModeStack: string list
      PendingSequence: KeyId list
      LastCommand: CommandId option }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardEffect =
    | CommandResolved of CommandId
    | KeyStateChanged of KeyId list
    | LayoutChanged of string
    | ModeChanged of string list
    | PendingSequenceChanged of KeyId list
    | StateDisplayChanged of KeyboardStateDisplay
    | ReportKeyboardDiagnostic of KeyboardDiagnostic
    /// Issue 456: NOT INTERPRETED BY ANY HOST. No `ViewerEffect` carries a `KeyboardEffect`
    /// (`DispatchInput` is host->product only), so this request never reaches a host and the capture it
    /// appears to arm never fires. `ControlsElmish.interpretKeyboardEffect` reports it as a
    /// `keyboard-input/HostKeyCaptureNotInterpreted` diagnostic rather than pretending to serve it.
    /// To actually capture a rebind key, set the host's `MapKey` to a seam that FORWARDS the raw key
    /// (`fun key isDown -> Some(YourMsg(ViewerKeyboard.toKeyId key, isDown))`) and route the key in your
    /// `update`, where your keymap and capture state live. See `ViewerKeyboard.toKeyId`.
    | RequestHostKeyCapture of KeyId

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardModel =
    { Bindings: KeyboardBinding list
      PressedKeys: Set<KeyId>
      LastCommand: CommandId option
      ActiveLayout: string
      ActiveModeStack: string list
      PersistentModeState: Map<string, string>
      PendingSequence: KeyId list
      Diagnostics: KeyboardDiagnostic list
      RecentEffects: KeyboardEffect list
      StateDisplay: KeyboardStateDisplay }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardMsg =
    | KeyDown of KeyId
    | KeyUp of KeyId
    | FocusLost
    | Reset
    | SetActiveLayout of string
    | PushTemporaryMode of string
    | PopTemporaryMode
    | SetPersistentMode of key: string * value: string
    | ResolvePendingSequence of KeyId list

/// Public contract module exposed by this FS.GG.UI package.
module Keyboard =
    /// Public contract function exposed by this FS.GG.UI package.
    val init: bindings: KeyboardBinding list -> KeyboardModel * KeyboardEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val update: msg: KeyboardMsg -> model: KeyboardModel -> KeyboardModel * KeyboardEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val stateDisplay: model: KeyboardModel -> KeyboardStateDisplay

/// Issue 331 (epic 330): an immutable key→command keymap — the **mechanism** layer for rebinding,
/// Rendering-owned and command-id-agnostic (a `CommandId` is any string). Indexes `KeyboardBinding`s
/// by `KeyId`, so a key binds to at most one command; the edit ops in the `Keymap` module each return
/// a new value, and the representation is opaque so a keymap can only be built through them. Round-trips
/// to/from `KeyboardBinding list`, the shape `Keyboard.init` already consumes.
type Keymap

/// Public contract module exposed by this FS.GG.UI package. Pure, total edit operations over `Keymap`;
/// each returns a new keymap and never mutates its argument.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Keymap =

    /// Assign `key` to `command`, whether or not `key` was already bound (a key-indexed upsert).
    /// This operation says nothing about other keys already assigned to the same command.
    val assignKey: key: KeyId -> command: CommandId -> keymap: Keymap -> Keymap

    /// Compatibility name for `assignKey`. Despite its historical name this is a KEY-indexed upsert:
    /// assigning a fresh key does not remove another key already assigned to the same command. Use
    /// `replaceCommandBinding` for the player-facing "change this action's key" operation.
    val rebind: key: KeyId -> command: CommandId -> keymap: Keymap -> Keymap

    /// Replace a command's binding using the single-binding policy used by player-facing controls.
    /// Every existing key for `command` is removed, the requested `key` is displaced from any other
    /// command, and the result contains exactly one binding for `command`: `key`.
    val replaceCommandBinding: command: CommandId -> key: KeyId -> keymap: Keymap -> Keymap

/// Feature 108 (US5, FR-016): the modifier state recovered at the key boundary. The raw key
/// string can carry `Ctrl+`/`Alt+`/`Shift+`/`Meta+` prefixes that the plain `normalize` collapses
/// into `Unknown "Ctrl+L"` and loses; parsing them here makes chords as dependable as plain keys,
/// with no backend change. A closed record of four bools — total, deterministic, equatable.
type KeyModifiers =
    { Ctrl: bool
      Alt: bool
      Shift: bool
      Meta: bool }

/// Public contract module exposed by this FS.GG.UI package.
module ViewerKeyboard =
    /// Public contract function exposed by this FS.GG.UI package.
    val normalize: raw: string -> ViewerKey
    /// Public contract function exposed by this FS.GG.UI package. Does NOT strip modifiers: a host
    /// reports a chord as the raw `Ctrl+L`, which normalizes to `ViewerKey.Unknown "Ctrl+L"` so the
    /// `MapKeyChord` seam can recover them (issue 183). Use `normalizeEventWithModifiers` for the
    /// base key plus its modifiers.
    val normalizeEvent: event: ViewerKeyEvent -> ViewerKey * bool
    /// Public contract function exposed by this FS.GG.UI package.
    val toKeyId: key: ViewerKey -> KeyId

    /// Feature 108 (US5, FR-016): the all-false `KeyModifiers` — an unmodified key's modifier set.
    val noModifiers: KeyModifiers

    /// Feature 108 (US5, FR-016): strip the leading `Ctrl+`/`Alt+`/`Shift+`/`Meta+` prefixes
    /// (case-insensitive, any order, repeats tolerated) off the raw key, then `normalize` the base
    /// key. Returns the base `ViewerKey`, the down/up flag (as `normalizeEvent`), and the held
    /// `KeyModifiers`. An unmodified key yields `noModifiers` and the SAME `ViewerKey` as
    /// `normalizeEvent` (byte-identical routing); a chord recovers every held modifier — zero silent
    /// loss (SC-009). Pure, total; never throws.
    val normalizeEventWithModifiers: event: ViewerKeyEvent -> ViewerKey * bool * KeyModifiers
