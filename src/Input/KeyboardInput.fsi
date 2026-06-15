namespace FS.GG.UI.Input

open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer.Host

/// Public contract type exposed by this FS.GG.UI package.
type CommandId = string
/// Public contract type exposed by this FS.GG.UI package.
type ModeId = string
/// Public contract type exposed by this FS.GG.UI package.
type StateId = string
/// Public contract type exposed by this FS.GG.UI package.
type LayoutId = string
/// Public contract type exposed by this FS.GG.UI package.
type KeyPositionId = string

/// Public contract type exposed by this FS.GG.UI package.
type InputSeverity =
    | InputInfo
    | InputWarning
    | InputError
    | InputFatal

/// Public contract type exposed by this FS.GG.UI package.
type InputDiagnosticCode =
    | InvalidYaml
    | UnsupportedSchemaVersion
    | DuplicateBinding
    | UnknownMode
    | UnknownCommand
    | UnknownLayout
    | InvalidModeState
    | AmbiguousSequence
    | StaleInputEvent
    | LostKeyReleaseRecovered
    | HostActionRejected
    | UnsatisfiedCommandIntent

/// Public contract type exposed by this FS.GG.UI package.
type InputDiagnostic =
    { Severity: InputSeverity
      Code: InputDiagnosticCode
      Message: string
      ModeId: ModeId option
      CommandId: CommandId option
      KeyPositionId: KeyPositionId option }

/// Public contract type exposed by this FS.GG.UI package.
type CommandDefinition =
    { Id: CommandId
      DisplayName: string
      Category: string option }

/// Public contract type exposed by this FS.GG.UI package.
type CommandRegistry =
    { Commands: CommandDefinition list }

/// Public contract type exposed by this FS.GG.UI package.
type ModeKind =
    | StandardMode
    | StatefulMode
    | PopupMode
    | TemporaryHeldMode

/// Public contract type exposed by this FS.GG.UI package.
type ModeDefinition =
    { Id: ModeId
      DisplayName: string
      Kind: ModeKind
      States: StateId list
      DefaultState: StateId option
      CancelKeys: KeyPositionId list }

/// Public contract type exposed by this FS.GG.UI package.
type Hand =
    | LeftHand
    | RightHand
    | EitherHand
    | UnknownHand

/// Public contract type exposed by this FS.GG.UI package.
type Finger =
    | Thumb
    | Index
    | Middle
    | Ring
    | Pinky
    | UnknownFinger

/// Public contract type exposed by this FS.GG.UI package.
type KeyPosition =
    { Id: KeyPositionId
      Hand: Hand
      Finger: Finger
      Row: int
      Column: int }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutProfile =
    { Id: LayoutId
      DisplayName: string
      Positions: KeyPosition list
      Labels: Map<KeyPositionId, string> }

/// Public contract type exposed by this FS.GG.UI package.
type KeyChord =
    { Position: KeyPositionId
      RequiredHeld: KeyPositionId list }

/// Public contract type exposed by this FS.GG.UI package.
type BindingOutcome =
    | EmitCommand of CommandId
    | SetState of ModeId * StateId
    | SetLayoutOutcome of LayoutId
    | PushPopup of ModeId
    | PushTemporary of ModeId
    | CancelTopMode
    | NoInputOp

/// Public contract type exposed by this FS.GG.UI package.
type BindingDefinition =
    { ModeId: ModeId
      Sequence: KeyChord list
      WhenState: StateId option
      Outcome: BindingOutcome
      Weight: float option }

/// Public contract type exposed by this FS.GG.UI package.
type DisambiguationPolicy =
    { TimeoutMilliseconds: int }

/// Public contract type exposed by this FS.GG.UI package.
type BigramWeight =
    { First: CommandId
      Second: CommandId
      Weight: float }

/// Public contract type exposed by this FS.GG.UI package.
type BigramProfile =
    { Weights: BigramWeight list
      SuggestionLimit: int }

/// Public contract type exposed by this FS.GG.UI package.
type DisplayOptions =
    { ShowLayoutState: bool
      ShowPendingSequence: bool }

/// Public contract type exposed by this FS.GG.UI package.
type CommandIntent =
    { Id: string
      CommandId: CommandId
      Constraints: string list }

/// Public contract type exposed by this FS.GG.UI package.
type InputConfiguration =
    { Version: int
      DefaultLayout: LayoutId
      DefaultMode: ModeId
      Layouts: LayoutProfile list
      Modes: ModeDefinition list
      Bindings: BindingDefinition list
      Disambiguation: DisambiguationPolicy
      BigramProfile: BigramProfile option
      Display: DisplayOptions
      CommandIntents: CommandIntent list }

/// Public contract type exposed by this FS.GG.UI package.
type CanonicalInputModel =
    { Configuration: InputConfiguration
      Registry: CommandRegistry }

/// Public contract type exposed by this FS.GG.UI package.
type ModeFrame =
    { ModeId: ModeId
      State: StateId option
      EnteredBy: KeyPositionId option }

/// Public contract type exposed by this FS.GG.UI package.
type InputEventId = Guid

/// Public contract type exposed by this FS.GG.UI package.
type InputEvent =
    { Id: InputEventId
      OccurredAt: DateTimeOffset
      Description: string }

/// Public contract type exposed by this FS.GG.UI package.
type PendingSequence =
    { StartedAt: DateTimeOffset
      Chords: KeyChord list }

/// Public contract type exposed by this FS.GG.UI package.
type InputRuntime =
    { Model: CanonicalInputModel
      ModeStack: ModeFrame list
      PressedKeys: Set<KeyPositionId>
      PendingSequence: PendingSequence option
      ActiveLayout: LayoutId
      Events: InputEvent list
      Diagnostics: InputDiagnostic list }

/// Public contract type exposed by this FS.GG.UI package.
type InputMsg =
    | KeyDown of KeyPositionId
    | KeyUp of KeyPositionId
    | FocusLost
    | Timeout
    | SetLayout of LayoutId
    | Cancel

/// Public contract type exposed by this FS.GG.UI package.
type LayoutStateView =
    { ActiveModeStack: ModeFrame list
      HeldModes: ModeFrame list
      PendingSequence: PendingSequence option
      ActiveLayout: LayoutProfile
      ActiveLabels: Map<KeyPositionId, string> }

/// Public contract type exposed by this FS.GG.UI package.
type ResolvedCommand =
    { CommandId: CommandId
      ModeStack: ModeFrame list
      SourceKey: KeyPositionId }

/// Public contract type exposed by this FS.GG.UI package.
type InputEffect =
    | CommandResolved of ResolvedCommand
    | LayoutStateChanged of LayoutStateView
    | InputDiagnosticEmitted of InputDiagnostic
    | InputEventRecorded of InputEvent

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayVisibility =
    | KeyboardStateDisplayHidden
    | KeyboardStateDisplayVisible

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayDensity =
    | KeyboardStateDisplayCompact
    | KeyboardStateDisplayExpanded

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayOptions =
    { Visibility: KeyboardStateDisplayVisibility
      Density: KeyboardStateDisplayDensity
      ShowKeyLabels: bool
      ShowPendingSequence: bool
      ShowRecentCommand: bool
      ShowDiagnostic: bool
      MaxCompactLabels: int
      MaxExpandedLabels: int }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayLayout =
    { Id: LayoutId
      DisplayName: string option
      IsAvailable: bool }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayContextKind =
    | DisplayPermanentContext
    | DisplayStatefulContext
    | DisplayPopupContext
    | DisplayTemporaryHeldContext
    | DisplayUnknownContext

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayStackEntry =
    { ModeId: ModeId
      DisplayName: string option
      Kind: KeyboardStateDisplayContextKind
      State: StateId option
      EnteredBy: KeyPositionId option
      IsTop: bool
      IsPersistent: bool }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayLabel =
    { KeyPositionId: KeyPositionId
      Label: string
      CommandId: CommandId option
      Outcome: string }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayPendingSequence =
    { Chords: KeyChord list
      StartedAt: DateTimeOffset
      IsTimed: bool
      TimeoutMilliseconds: int option }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayRecentCommand =
    { CommandId: CommandId
      DisplayName: string option
      SourceKey: KeyPositionId }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayDiagnostic =
    { Severity: InputSeverity
      Code: InputDiagnosticCode
      Message: string
      ModeId: ModeId option
      CommandId: CommandId option
      KeyPositionId: KeyPositionId option }

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayOmission =
    | OmittedLabels of omittedCount: int
    | OmittedPendingSequence
    | OmittedRecentCommand
    | OmittedDiagnostic
    | OmittedStackEntries of omittedCount: int

/// Public contract type exposed by this FS.GG.UI package.
type KeyboardStateDisplayModel =
    { Visibility: KeyboardStateDisplayVisibility
      Density: KeyboardStateDisplayDensity
      Layout: KeyboardStateDisplayLayout option
      Stack: KeyboardStateDisplayStackEntry list
      TopContext: KeyboardStateDisplayStackEntry option
      ActiveState: StateId option
      Labels: KeyboardStateDisplayLabel list
      PendingSequence: KeyboardStateDisplayPendingSequence option
      RecentCommand: KeyboardStateDisplayRecentCommand option
      Diagnostic: KeyboardStateDisplayDiagnostic option
      Omitted: KeyboardStateDisplayOmission list
      IsPartial: bool }

/// Public contract type exposed by this FS.GG.UI package.
type BigramRiskKind =
    | SameFinger
    | LongTravel
    | AwkwardHold
    | SameHandRepeat

/// Public contract type exposed by this FS.GG.UI package.
type BigramRisk =
    { First: CommandId
      Second: CommandId
      Weight: float
      Kind: BigramRiskKind
      Description: string }

/// Public contract type exposed by this FS.GG.UI package.
type BigramSuggestion =
    { First: CommandId
      Second: CommandId
      Description: string
      ExpectedScoreDelta: float }

/// Public contract type exposed by this FS.GG.UI package.
type BigramReport =
    { LayoutId: LayoutId
      TopPairs: BigramWeight list
      Risks: BigramRisk list
      Suggestions: BigramSuggestion list }

/// Public contract type exposed by this FS.GG.UI package.
type CommandPlanStatus =
    | Planned
    | AwaitingApproval
    | Executing
    | Completed
    | Failed
    | Cancelled

/// Public contract type exposed by this FS.GG.UI package.
type CommandPlan =
    { Id: string
      Intent: CommandIntent
      Steps: string list
      Status: CommandPlanStatus
      Failure: string option }

/// Public contract module exposed by this FS.GG.UI package.
module KeyboardInput =
    /// Public contract function exposed by this FS.GG.UI package.
    val commandRegistry : commands: CommandDefinition list -> Result<CommandRegistry, InputDiagnostic list>

    /// Public contract function exposed by this FS.GG.UI package.
    val parseYaml : yaml: string -> Result<InputConfiguration, InputDiagnostic list>

    /// Public contract function exposed by this FS.GG.UI package.
    val validate :
        registry: CommandRegistry ->
        configuration: InputConfiguration ->
            Result<CanonicalInputModel, InputDiagnostic list>

    /// Public contract function exposed by this FS.GG.UI package.
    val init :
        activeLayout: LayoutId ->
        model: CanonicalInputModel ->
            Result<InputRuntime * InputEffect list, InputDiagnostic list>

    /// Public contract function exposed by this FS.GG.UI package.
    val update : msg: InputMsg -> runtime: InputRuntime -> InputRuntime * InputEffect list

    /// Public contract function exposed by this FS.GG.UI package.
    val viewerInputMsg : event: ViewerEvent -> InputMsg option

    /// Public contract function exposed by this FS.GG.UI package.
    val updateFromViewerEvent : event: ViewerEvent -> runtime: InputRuntime -> InputRuntime * InputEffect list

    /// Public contract function exposed by this FS.GG.UI package.
    val layoutState : runtime: InputRuntime -> LayoutStateView

    /// Public contract function exposed by this FS.GG.UI package.
    val defaultStateDisplayOptions : KeyboardStateDisplayOptions

    /// Public contract function exposed by this FS.GG.UI package.
    val compactStateDisplayOptions : KeyboardStateDisplayOptions

    /// Public contract function exposed by this FS.GG.UI package.
    val expandedStateDisplayOptions : KeyboardStateDisplayOptions

    /// Public contract function exposed by this FS.GG.UI package.
    val keyboardStateDisplay :
        options: KeyboardStateDisplayOptions ->
        recentEffects: InputEffect list ->
        runtime: InputRuntime ->
            KeyboardStateDisplayModel

    /// Public contract function exposed by this FS.GG.UI package.
    val renderKeyboardStateDisplay :
        options: KeyboardStateDisplayOptions ->
        recentEffects: InputEffect list ->
        runtime: InputRuntime ->
            Scene

    /// Public contract function exposed by this FS.GG.UI package.
    val renderKeyboardStateDisplayAt :
        position: float * float ->
        options: KeyboardStateDisplayOptions ->
        recentEffects: InputEffect list ->
        runtime: InputRuntime ->
            Scene

    /// Public contract function exposed by this FS.GG.UI package.
    val renderLayoutState : runtime: InputRuntime -> Scene

    /// Public contract function exposed by this FS.GG.UI package.
    val renderLayoutStateAt : position: float * float -> runtime: InputRuntime -> Scene

    /// Public contract function exposed by this FS.GG.UI package.
    val replay :
        initial: InputRuntime ->
        messages: InputMsg list ->
            InputRuntime * InputEffect list

    /// Public contract function exposed by this FS.GG.UI package.
    val analyzeBigrams : model: CanonicalInputModel -> layout: LayoutId -> BigramReport
