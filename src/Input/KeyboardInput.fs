namespace FS.Skia.UI.Input

open System
open System.IO
open YamlDotNet.RepresentationModel
open FS.Skia.UI.Scene
open FS.Skia.UI.SkiaViewer.Host

type CommandId = string
type ModeId = string
type StateId = string
type LayoutId = string
type KeyPositionId = string

type InputSeverity =
    | InputInfo
    | InputWarning
    | InputError
    | InputFatal

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

type InputDiagnostic =
    { Severity: InputSeverity
      Code: InputDiagnosticCode
      Message: string
      ModeId: ModeId option
      CommandId: CommandId option
      KeyPositionId: KeyPositionId option }

type CommandDefinition =
    { Id: CommandId
      DisplayName: string
      Category: string option }

type CommandRegistry =
    { Commands: CommandDefinition list }

type ModeKind =
    | StandardMode
    | StatefulMode
    | PopupMode
    | TemporaryHeldMode

type ModeDefinition =
    { Id: ModeId
      DisplayName: string
      Kind: ModeKind
      States: StateId list
      DefaultState: StateId option
      CancelKeys: KeyPositionId list }

type Hand =
    | LeftHand
    | RightHand
    | EitherHand
    | UnknownHand

type Finger =
    | Thumb
    | Index
    | Middle
    | Ring
    | Pinky
    | UnknownFinger

type KeyPosition =
    { Id: KeyPositionId
      Hand: Hand
      Finger: Finger
      Row: int
      Column: int }

type LayoutProfile =
    { Id: LayoutId
      DisplayName: string
      Positions: KeyPosition list
      Labels: Map<KeyPositionId, string> }

type KeyChord =
    { Position: KeyPositionId
      RequiredHeld: KeyPositionId list }

type BindingOutcome =
    | EmitCommand of CommandId
    | SetState of ModeId * StateId
    | SetLayoutOutcome of LayoutId
    | PushPopup of ModeId
    | PushTemporary of ModeId
    | CancelTopMode
    | NoInputOp

type BindingDefinition =
    { ModeId: ModeId
      Sequence: KeyChord list
      WhenState: StateId option
      Outcome: BindingOutcome
      Weight: float option }

type DisambiguationPolicy =
    { TimeoutMilliseconds: int }

type BigramWeight =
    { First: CommandId
      Second: CommandId
      Weight: float }

type BigramProfile =
    { Weights: BigramWeight list
      SuggestionLimit: int }

type DisplayOptions =
    { ShowLayoutState: bool
      ShowPendingSequence: bool }

type CommandIntent =
    { Id: string
      CommandId: CommandId
      Constraints: string list }

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

type CanonicalInputModel =
    { Configuration: InputConfiguration
      Registry: CommandRegistry }

type ModeFrame =
    { ModeId: ModeId
      State: StateId option
      EnteredBy: KeyPositionId option }

type InputEventId = Guid

type InputEvent =
    { Id: InputEventId
      OccurredAt: DateTimeOffset
      Description: string }

type PendingSequence =
    { StartedAt: DateTimeOffset
      Chords: KeyChord list }

type InputRuntime =
    { Model: CanonicalInputModel
      ModeStack: ModeFrame list
      PressedKeys: Set<KeyPositionId>
      PendingSequence: PendingSequence option
      ActiveLayout: LayoutId
      Events: InputEvent list
      Diagnostics: InputDiagnostic list }

type InputMsg =
    | KeyDown of KeyPositionId
    | KeyUp of KeyPositionId
    | FocusLost
    | Timeout
    | SetLayout of LayoutId
    | Cancel

type LayoutStateView =
    { ActiveModeStack: ModeFrame list
      HeldModes: ModeFrame list
      PendingSequence: PendingSequence option
      ActiveLayout: LayoutProfile
      ActiveLabels: Map<KeyPositionId, string> }

type ResolvedCommand =
    { CommandId: CommandId
      ModeStack: ModeFrame list
      SourceKey: KeyPositionId }

type InputEffect =
    | CommandResolved of ResolvedCommand
    | LayoutStateChanged of LayoutStateView
    | InputDiagnosticEmitted of InputDiagnostic
    | InputEventRecorded of InputEvent

type KeyboardStateDisplayVisibility =
    | KeyboardStateDisplayHidden
    | KeyboardStateDisplayVisible

type KeyboardStateDisplayDensity =
    | KeyboardStateDisplayCompact
    | KeyboardStateDisplayExpanded

type KeyboardStateDisplayOptions =
    { Visibility: KeyboardStateDisplayVisibility
      Density: KeyboardStateDisplayDensity
      ShowKeyLabels: bool
      ShowPendingSequence: bool
      ShowRecentCommand: bool
      ShowDiagnostic: bool
      MaxCompactLabels: int
      MaxExpandedLabels: int }

type KeyboardStateDisplayLayout =
    { Id: LayoutId
      DisplayName: string option
      IsAvailable: bool }

type KeyboardStateDisplayContextKind =
    | DisplayPermanentContext
    | DisplayStatefulContext
    | DisplayPopupContext
    | DisplayTemporaryHeldContext
    | DisplayUnknownContext

type KeyboardStateDisplayStackEntry =
    { ModeId: ModeId
      DisplayName: string option
      Kind: KeyboardStateDisplayContextKind
      State: StateId option
      EnteredBy: KeyPositionId option
      IsTop: bool
      IsPersistent: bool }

type KeyboardStateDisplayLabel =
    { KeyPositionId: KeyPositionId
      Label: string
      CommandId: CommandId option
      Outcome: string }

type KeyboardStateDisplayPendingSequence =
    { Chords: KeyChord list
      StartedAt: DateTimeOffset
      IsTimed: bool
      TimeoutMilliseconds: int option }

type KeyboardStateDisplayRecentCommand =
    { CommandId: CommandId
      DisplayName: string option
      SourceKey: KeyPositionId }

type KeyboardStateDisplayDiagnostic =
    { Severity: InputSeverity
      Code: InputDiagnosticCode
      Message: string
      ModeId: ModeId option
      CommandId: CommandId option
      KeyPositionId: KeyPositionId option }

type KeyboardStateDisplayOmission =
    | OmittedLabels of omittedCount: int
    | OmittedPendingSequence
    | OmittedRecentCommand
    | OmittedDiagnostic
    | OmittedStackEntries of omittedCount: int

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

type BigramRiskKind =
    | SameFinger
    | LongTravel
    | AwkwardHold
    | SameHandRepeat

type BigramRisk =
    { First: CommandId
      Second: CommandId
      Weight: float
      Kind: BigramRiskKind
      Description: string }

type BigramSuggestion =
    { First: CommandId
      Second: CommandId
      Description: string
      ExpectedScoreDelta: float }

type BigramReport =
    { LayoutId: LayoutId
      TopPairs: BigramWeight list
      Risks: BigramRisk list
      Suggestions: BigramSuggestion list }

type CommandPlanStatus =
    | Planned
    | AwaitingApproval
    | Executing
    | Completed
    | Failed
    | Cancelled

type CommandPlan =
    { Id: string
      Intent: CommandIntent
      Steps: string list
      Status: CommandPlanStatus
      Failure: string option }

module KeyboardInput =
    let diagnostic severity code message modeId commandId keyPositionId : InputDiagnostic =
        { Severity = severity
          Code = code
          Message = message
          ModeId = modeId
          CommandId = commandId
          KeyPositionId = keyPositionId }

    let error code message modeId commandId keyPositionId : InputDiagnostic =
        diagnostic InputError code message modeId commandId keyPositionId

    let commandRegistry (commands: CommandDefinition list) =
        let duplicates =
            commands
            |> List.countBy _.Id
            |> List.choose (fun (id, count) -> if count > 1 then Some id else None)

        if duplicates.IsEmpty then
            Result.Ok { Commands = commands }
        else
            duplicates
            |> List.map (fun id -> error DuplicateBinding $"Duplicate command id '{id}'." None (Some id) None)
            |> Result.Error

    let asMap (node: YamlNode) =
        match node with
        | :? YamlMappingNode as map -> Some map
        | _ -> None

    let asSeq (node: YamlNode) =
        match node with
        | :? YamlSequenceNode as sequence -> sequence.Children |> Seq.toList
        | _ -> []

    let scalar (node: YamlNode) =
        match node with
        | :? YamlScalarNode as scalar -> scalar.Value |> Option.ofObj |> Option.defaultValue ""
        | _ -> ""

    let tryGet key (map: YamlMappingNode) =
        map.Children
        |> Seq.tryPick (fun kv ->
            if scalar kv.Key = key then Some kv.Value else None)

    let str key map defaultValue =
        tryGet key map |> Option.map scalar |> Option.filter ((<>) "") |> Option.defaultValue defaultValue

    let boolValue key map defaultValue =
        match tryGet key map |> Option.map scalar with
        | Some "true" -> true
        | Some "false" -> false
        | _ -> defaultValue

    let intValue key map defaultValue =
        match tryGet key map |> Option.map scalar with
        | Some value ->
            match Int32.TryParse value with
            | true, parsed -> parsed
            | _ -> defaultValue
        | None -> defaultValue

    let floatValue key map =
        match tryGet key map |> Option.map scalar with
        | Some value ->
            match Double.TryParse value with
            | true, parsed -> Some parsed
            | _ -> None
        | None -> None

    let strList key map =
        match tryGet key map with
        | Some node -> asSeq node |> List.map scalar |> List.filter ((<>) "")
        | None -> []

    let parseModeKind value =
        match value with
        | "standard" -> StandardMode
        | "stateful" -> StatefulMode
        | "popup" -> PopupMode
        | "temporary" | "held" | "temporary-held" -> TemporaryHeldMode
        | _ -> StandardMode

    let parseHand value =
        match value with
        | "left" -> LeftHand
        | "right" -> RightHand
        | "either" -> EitherHand
        | _ -> UnknownHand

    let parseFinger value =
        match value with
        | "thumb" -> Thumb
        | "index" -> Index
        | "middle" -> Middle
        | "ring" -> Ring
        | "pinky" -> Pinky
        | _ -> UnknownFinger

    let parseOutcome (map: YamlMappingNode) =
        match str "outcome" map "" with
        | "command" -> EmitCommand(str "command" map "")
        | "set-state" -> SetState(str "targetMode" map (str "mode" map ""), str "state" map "")
        | "set-layout" -> SetLayoutOutcome(str "layout" map "")
        | "popup" -> PushPopup(str "targetMode" map (str "mode" map ""))
        | "temporary" | "held" -> PushTemporary(str "targetMode" map (str "mode" map ""))
        | "cancel" -> CancelTopMode
        | _ -> NoInputOp

    let parseChord (node: YamlNode) =
        match asMap node with
        | Some map ->
            { Position = str "position" map (str "key" map "")
              RequiredHeld = strList "held" map }
        | None ->
            { Position = scalar node
              RequiredHeld = [] }

    let parseBinding node =
        match asMap node with
        | Some map ->
            let sequence =
                match tryGet "sequence" map with
                | Some sequence -> asSeq sequence |> List.map parseChord
                | None -> [ { Position = str "position" map (str "key" map ""); RequiredHeld = strList "held" map } ]

            { ModeId = str "mode" map ""
              Sequence = sequence
              WhenState = str "whenState" map "" |> function "" -> None | value -> Some value
              Outcome = parseOutcome map
              Weight = floatValue "weight" map }
        | None ->
            { ModeId = ""
              Sequence = []
              WhenState = None
              Outcome = NoInputOp
              Weight = None }

    let parseYaml yaml =
        try
            let stream = YamlStream()
            use reader = new StringReader(yaml)
            stream.Load(reader)

            if stream.Documents.Count = 0 then
                Result.Error [ error InvalidYaml "YAML document is empty." None None None ]
            else
                match stream.Documents.[0].RootNode |> asMap with
                | None ->
                    Result.Error [ error InvalidYaml "Root YAML node must be a mapping." None None None ]
                | Some root ->
                    let layouts =
                        match tryGet "layouts" root with
                        | Some node ->
                            asSeq node
                            |> List.choose (fun layoutNode ->
                                asMap layoutNode
                                |> Option.map (fun map ->
                                    let positions =
                                        match tryGet "positions" map with
                                        | Some positions ->
                                            asSeq positions
                                            |> List.choose (asMap >> Option.map (fun p ->
                                                { Id = str "id" p ""
                                                  Hand = parseHand (str "hand" p "")
                                                  Finger = parseFinger (str "finger" p "")
                                                  Row = intValue "row" p 0
                                                  Column = intValue "column" p 0 }))
                                        | None -> []

                                    let labels =
                                        match tryGet "labels" map |> Option.bind asMap with
                                        | Some labels ->
                                            labels.Children
                                            |> Seq.map (fun kv -> scalar kv.Key, scalar kv.Value)
                                            |> Map.ofSeq
                                        | None -> Map.empty

                                    { Id = str "id" map ""
                                      DisplayName = str "displayName" map (str "id" map "")
                                      Positions = positions
                                      Labels = labels }))
                        | None -> []

                    let modes =
                        match tryGet "modes" root with
                        | Some node ->
                            asSeq node
                            |> List.choose (fun modeNode ->
                                asMap modeNode
                                |> Option.map (fun map ->
                                    { Id = str "id" map ""
                                      DisplayName = str "displayName" map (str "id" map "")
                                      Kind = parseModeKind (str "kind" map "")
                                      States = strList "states" map
                                      DefaultState = str "defaultState" map "" |> function "" -> None | value -> Some value
                                      CancelKeys = strList "cancelKeys" map }))
                        | None -> []

                    let bindings =
                        match tryGet "bindings" root with
                        | Some node -> asSeq node |> List.map parseBinding
                        | None -> []

                    let disambiguation =
                        match tryGet "disambiguation" root |> Option.bind asMap with
                        | Some map -> { TimeoutMilliseconds = intValue "timeoutMilliseconds" map 200 }
                        | None -> { TimeoutMilliseconds = 200 }

                    let bigramProfile =
                        match tryGet "bigramProfile" root |> Option.bind asMap with
                        | Some map ->
                            let weights =
                                match tryGet "weights" map with
                                | Some weights ->
                                    asSeq weights
                                    |> List.choose (asMap >> Option.map (fun w ->
                                        { First = str "first" w ""
                                          Second = str "second" w ""
                                          Weight = floatValue "weight" w |> Option.defaultValue 1.0 }))
                                | None -> []

                            Some { Weights = weights; SuggestionLimit = intValue "suggestionLimit" map 20 }
                        | None -> None

                    let display =
                        match tryGet "display" root |> Option.bind asMap with
                        | Some map ->
                            { ShowLayoutState = boolValue "showLayoutState" map false
                              ShowPendingSequence = boolValue "showPendingSequence" map false }
                        | None ->
                            { ShowLayoutState = false
                              ShowPendingSequence = false }

                    let commandIntents =
                        match tryGet "commandIntents" root with
                        | Some node ->
                            asSeq node
                            |> List.choose (asMap >> Option.map (fun i ->
                                { Id = str "id" i ""
                                  CommandId = str "command" i ""
                                  Constraints = strList "constraints" i }))
                        | None -> []

                    Result.Ok
                        { Version = intValue "version" root 1
                          DefaultLayout = str "defaultLayout" root ""
                          DefaultMode = str "defaultMode" root ""
                          Layouts = layouts
                          Modes = modes
                          Bindings = bindings
                          Disambiguation = disambiguation
                          BigramProfile = bigramProfile
                          Display = display
                          CommandIntents = commandIntents }
        with ex ->
            Result.Error [ error InvalidYaml ex.Message None None None ]

    let modeById configuration id =
        configuration.Modes |> List.tryFind (fun mode -> mode.Id = id)

    let layoutById configuration id =
        configuration.Layouts |> List.tryFind (fun layout -> layout.Id = id)

    let validate registry configuration =
        let commandIds = registry.Commands |> List.map _.Id |> Set.ofList
        let modeIds = configuration.Modes |> List.map _.Id |> Set.ofList
        let layoutIds = configuration.Layouts |> List.map _.Id |> Set.ofList

        let duplicateBindingDiagnostics =
            configuration.Bindings
            |> List.countBy (fun binding -> binding.ModeId, binding.Sequence, binding.WhenState)
            |> List.choose (fun ((modeId, sequence, _), count) ->
                if count > 1 then
                    let key = sequence |> List.tryLast |> Option.map _.Position
                    Some(error DuplicateBinding $"Duplicate binding in mode '{modeId}'." (Some modeId) None key)
                else
                    None)

        let bindingDiagnostics =
            configuration.Bindings
            |> List.collect (fun binding ->
                let modeDiagnostics =
                    if modeIds.Contains binding.ModeId then [] else [ error UnknownMode $"Unknown mode '{binding.ModeId}'." (Some binding.ModeId) None None ]

                let commandDiagnostics =
                    match binding.Outcome with
                    | EmitCommand commandId when commandId.StartsWith("host:", StringComparison.OrdinalIgnoreCase) || commandId.Contains("shell", StringComparison.OrdinalIgnoreCase) ->
                        [ error HostActionRejected $"Host action-like command '{commandId}' is not allowed in YAML." (Some binding.ModeId) (Some commandId) (binding.Sequence |> List.tryLast |> Option.map _.Position) ]
                    | EmitCommand commandId when not (commandIds.Contains commandId) ->
                        [ error UnknownCommand $"Unknown command '{commandId}'." (Some binding.ModeId) (Some commandId) (binding.Sequence |> List.tryLast |> Option.map _.Position) ]
                    | PushPopup modeId | PushTemporary modeId when not (modeIds.Contains modeId) ->
                        [ error UnknownMode $"Unknown mode '{modeId}'." (Some modeId) None (binding.Sequence |> List.tryLast |> Option.map _.Position) ]
                    | SetState(modeId, state) ->
                        match modeById configuration modeId with
                        | Some mode when mode.States |> List.contains state -> []
                        | Some _ -> [ error InvalidModeState $"Unknown state '{state}' for mode '{modeId}'." (Some modeId) None None ]
                        | None -> [ error UnknownMode $"Unknown mode '{modeId}'." (Some modeId) None None ]
                    | SetLayoutOutcome layoutId when not (layoutIds.Contains layoutId) ->
                        [ error UnknownLayout $"Unknown layout '{layoutId}'." None None None ]
                    | _ -> []

                modeDiagnostics @ commandDiagnostics)

        let modeDiagnostics =
            configuration.Modes
            |> List.collect (fun mode ->
                match mode.Kind, mode.States, mode.DefaultState with
                | StatefulMode, [], _ ->
                    [ error InvalidModeState $"Stateful mode '{mode.Id}' must define at least one state." (Some mode.Id) None None ]
                | StatefulMode, states, Some state when not (states |> List.contains state) ->
                    [ error InvalidModeState $"Default state '{state}' is not valid for mode '{mode.Id}'." (Some mode.Id) None None ]
                | StatefulMode, _, None ->
                    [ error InvalidModeState $"Stateful mode '{mode.Id}' must define a default state." (Some mode.Id) None None ]
                | _ -> [])

        let intentDiagnostics =
            configuration.CommandIntents
            |> List.collect (fun intent ->
                if commandIds.Contains intent.CommandId then
                    []
                else
                    [ error UnsatisfiedCommandIntent $"Command intent '{intent.Id}' references unknown command '{intent.CommandId}'." None (Some intent.CommandId) None ])

        let rootDiagnostics = [
            if configuration.Version <> 1 then
                error UnsupportedSchemaVersion $"Unsupported input schema version {configuration.Version}." None None None

            if not (layoutIds.Contains configuration.DefaultLayout) then
                error UnknownLayout $"Unknown default layout '{configuration.DefaultLayout}'." None None None

            if not (modeIds.Contains configuration.DefaultMode) then
                error UnknownMode $"Unknown default mode '{configuration.DefaultMode}'." (Some configuration.DefaultMode) None None
        ]

        let diagnostics = rootDiagnostics @ duplicateBindingDiagnostics @ bindingDiagnostics @ modeDiagnostics @ intentDiagnostics

        if diagnostics.IsEmpty then
            Result.Ok { Configuration = configuration; Registry = registry }
        else
            Result.Error diagnostics

    let frameForMode configuration enteredBy modeId =
        modeById configuration modeId
        |> Option.map (fun mode ->
            { ModeId = mode.Id
              State = if mode.Kind = StatefulMode then mode.DefaultState else None
              EnteredBy = enteredBy })

    let event description =
        { Id = Guid.NewGuid()
          OccurredAt = DateTimeOffset.UnixEpoch
          Description = description }

    let layoutState runtime =
        let configuration = runtime.Model.Configuration
        let activeLayout =
            layoutById configuration runtime.ActiveLayout
            |> Option.defaultValue (configuration.Layouts |> List.head)

        { ActiveModeStack = runtime.ModeStack
          HeldModes =
            runtime.ModeStack
            |> List.filter (fun frame ->
                modeById configuration frame.ModeId
                |> Option.exists (fun mode -> mode.Kind = TemporaryHeldMode))
          PendingSequence = runtime.PendingSequence
          ActiveLayout = activeLayout
          ActiveLabels = activeLayout.Labels }

    let withEffects runtime effects =
        if runtime.Model.Configuration.Display.ShowLayoutState then
            runtime, effects @ [ LayoutStateChanged(layoutState runtime) ]
        else
            runtime, effects

    let init activeLayout model =
        let configuration = model.Configuration

        match layoutById configuration activeLayout, frameForMode configuration None configuration.DefaultMode with
        | Some _, Some frame ->
            let runtime =
                { Model = model
                  ModeStack = [ frame ]
                  PressedKeys = Set.empty
                  PendingSequence = None
                  ActiveLayout = activeLayout
                  Events = []
                  Diagnostics = [] }

            let started = event $"Initialized keyboard input with mode '{configuration.DefaultMode}' and layout '{activeLayout}'."
            let runtime = { runtime with Events = [ started ] }
            Result.Ok(withEffects runtime [ InputEventRecorded started ])
        | None, _ ->
            Result.Error [ error UnknownLayout $"Unknown layout '{activeLayout}'." None None None ]
        | _, None ->
            Result.Error [ error UnknownMode $"Unknown default mode '{configuration.DefaultMode}'." (Some configuration.DefaultMode) None None ]

    let topMode (runtime: InputRuntime) =
        runtime.ModeStack |> List.tryLast

    let topModeDefinition (runtime: InputRuntime) =
        topMode runtime |> Option.bind (fun frame -> modeById runtime.Model.Configuration frame.ModeId)

    let matchesBinding (runtime: InputRuntime) position (binding: BindingDefinition) =
        match topMode runtime with
        | Some frame when binding.ModeId = frame.ModeId ->
            match binding.Sequence with
            | [ chord ] ->
                chord.Position = position
                && (chord.RequiredHeld |> List.forall runtime.PressedKeys.Contains)
                && (binding.WhenState.IsNone || binding.WhenState = frame.State)
            | _ -> false
        | _ -> false

    let popTop (runtime: InputRuntime) =
        match runtime.ModeStack with
        | [] | [ _ ] -> runtime
        | frames -> { runtime with ModeStack = frames |> List.take (frames.Length - 1) }

    let replaceState modeId state (runtime: InputRuntime) =
        { runtime with
            ModeStack =
                runtime.ModeStack
                |> List.map (fun frame ->
                    if frame.ModeId = modeId then { frame with State = Some state } else frame) }

    let emitDiagnostic (runtime: InputRuntime) diagnostic =
        { runtime with Diagnostics = runtime.Diagnostics @ [ diagnostic ] }, [ InputDiagnosticEmitted diagnostic ]

    let update msg (runtime: InputRuntime) =
        let configuration = runtime.Model.Configuration

        let record description (runtime: InputRuntime) effects =
            let recorded = event description
            { runtime with Events = runtime.Events @ [ recorded ] }, effects @ [ InputEventRecorded recorded ]

        match msg with
        | KeyDown position ->
            let runtime = { runtime with PressedKeys = runtime.PressedKeys.Add position }

            match configuration.Bindings |> List.filter (matchesBinding runtime position) with
            | [] ->
                let diagnostic = diagnostic InputInfo StaleInputEvent $"No binding matched key '{position}'." (topMode runtime |> Option.map _.ModeId) None (Some position)
                let runtime, effects = emitDiagnostic runtime diagnostic
                record $"Ignored key down '{position}'." runtime effects |> fun (r, e) -> withEffects r e
            | _ :: _ :: _ ->
                let diagnostic = error AmbiguousSequence $"Multiple bindings matched key '{position}'." (topMode runtime |> Option.map _.ModeId) None (Some position)
                let runtime, effects = emitDiagnostic runtime diagnostic
                record $"Ambiguous key down '{position}'." runtime effects |> fun (r, e) -> withEffects r e
            | [ binding ] ->
                let sourceKey = binding.Sequence |> List.tryLast |> Option.map _.Position |> Option.defaultValue position

                let runtime, effects =
                    match binding.Outcome with
                    | EmitCommand commandId ->
                        let resolved =
                            { CommandId = commandId
                              ModeStack = runtime.ModeStack
                              SourceKey = sourceKey }

                        let runtime =
                            match topModeDefinition runtime with
                            | Some mode when mode.Kind = PopupMode -> popTop runtime
                            | _ -> runtime

                        runtime, [ CommandResolved resolved ]
                    | SetState(modeId, state) ->
                        replaceState modeId state runtime, []
                    | SetLayoutOutcome layoutId ->
                        if configuration.Layouts |> List.exists (fun layout -> layout.Id = layoutId) then
                            { runtime with ActiveLayout = layoutId }, []
                        else
                            let diagnostic = error UnknownLayout $"Unknown layout '{layoutId}'." None None None
                            emitDiagnostic runtime diagnostic
                    | PushPopup modeId ->
                        match frameForMode configuration None modeId with
                        | Some frame -> { runtime with ModeStack = runtime.ModeStack @ [ frame ] }, []
                        | None ->
                            let diagnostic = error UnknownMode $"Unknown mode '{modeId}'." (Some modeId) None (Some sourceKey)
                            emitDiagnostic runtime diagnostic
                    | PushTemporary modeId ->
                        match frameForMode configuration (Some sourceKey) modeId with
                        | Some frame -> { runtime with ModeStack = runtime.ModeStack @ [ frame ] }, []
                        | None ->
                            let diagnostic = error UnknownMode $"Unknown mode '{modeId}'." (Some modeId) None (Some sourceKey)
                            emitDiagnostic runtime diagnostic
                    | CancelTopMode ->
                        popTop runtime, []
                    | NoInputOp ->
                        runtime, []

                record $"Resolved key down '{position}'." runtime effects |> fun (r, e) -> withEffects r e

        | KeyUp position ->
            let runtime = { runtime with PressedKeys = runtime.PressedKeys.Remove position }
            let activeHeld =
                runtime.ModeStack
                |> List.tryFindIndexBack (fun frame -> frame.EnteredBy = Some position)

            let runtime, effects =
                match activeHeld with
                | Some index ->
                    { runtime with ModeStack = runtime.ModeStack |> List.mapi (fun i frame -> i, frame) |> List.filter (fun (i, _) -> i <> index) |> List.map snd }, []
                | None ->
                    let diagnostic = diagnostic InputWarning LostKeyReleaseRecovered $"Key up '{position}' did not match an active held mode." (topMode runtime |> Option.map _.ModeId) None (Some position)
                    emitDiagnostic runtime diagnostic

            record $"Released key '{position}'." runtime effects |> fun (r, e) -> withEffects r e

        | FocusLost ->
            let hadPressed = not runtime.PressedKeys.IsEmpty
            let runtime =
                { runtime with
                    PressedKeys = Set.empty
                    ModeStack =
                        runtime.ModeStack
                        |> List.filter (fun frame ->
                            modeById configuration frame.ModeId
                            |> Option.exists (fun mode -> mode.Kind <> TemporaryHeldMode))
                    PendingSequence = None }

            let runtime, effects =
                if hadPressed then
                    let diagnostic = diagnostic InputWarning LostKeyReleaseRecovered "Focus loss cleared pressed keys and held modes." None None None
                    emitDiagnostic runtime diagnostic
                else
                    runtime, []

            record "Focus lost." runtime effects |> fun (r, e) -> withEffects r e

        | Timeout ->
            let runtime =
                match topModeDefinition runtime with
                | Some mode when mode.Kind = PopupMode -> popTop runtime
                | _ -> runtime

            let diagnostic = diagnostic InputWarning AmbiguousSequence "Pending input timed out." (topMode runtime |> Option.map _.ModeId) None None
            let runtime, effects = emitDiagnostic { runtime with PendingSequence = None } diagnostic
            record "Input timeout." runtime effects |> fun (r, e) -> withEffects r e

        | SetLayout layoutId ->
            if configuration.Layouts |> List.exists (fun layout -> layout.Id = layoutId) then
                let runtime = { runtime with ActiveLayout = layoutId }
                record $"Set layout '{layoutId}'." runtime [] |> fun (r, e) -> withEffects r e
            else
                let diagnostic = error UnknownLayout $"Unknown layout '{layoutId}'." None None None
                let runtime, effects = emitDiagnostic runtime diagnostic
                record $"Rejected layout '{layoutId}'." runtime effects |> fun (r, e) -> withEffects r e

        | Cancel ->
            let runtime = popTop { runtime with PendingSequence = None }
            record "Cancelled top input mode." runtime [] |> fun (r, e) -> withEffects r e

    let normalizeViewerKey key =
        match key with
        | "H" | "KeyH" -> Some "KeyH"
        | "L" | "KeyL" -> Some "KeyL"
        | "C" | "KeyC" -> Some "KeyC"
        | "D" | "KeyD" -> Some "KeyD"
        | "D1" | "Number1" | "Num1" | "Digit1" | "Key1" -> Some "Digit1"
        | "Space" | "Spacebar" -> Some "Space"
        | "Escape" | "Esc" -> Some "Escape"
        | value when value.StartsWith("Key", StringComparison.Ordinal) -> Some value
        | value when value.Length = 1 && Char.IsLetter value.[0] -> Some($"Key{value.ToUpperInvariant()}")
        | _ -> None

    let viewerInputMsg event =
        match event with
        | ViewerEvent.KeyDown key -> normalizeViewerKey key |> Option.map KeyDown
        | ViewerEvent.KeyUp key -> normalizeViewerKey key |> Option.map KeyUp
        | CloseRequested -> Some FocusLost
        | _ -> None

    let updateFromViewerEvent event runtime =
        match viewerInputMsg event with
        | Some msg -> update msg runtime
        | None -> runtime, []

    let replay initial messages =
        messages
        |> List.fold (fun (runtime, effects) msg ->
            let next, emitted = update msg runtime
            next, effects @ emitted) (initial, [])

    let defaultStateDisplayOptions =
        { Visibility = KeyboardStateDisplayVisible
          Density = KeyboardStateDisplayCompact
          ShowKeyLabels = true
          ShowPendingSequence = true
          ShowRecentCommand = true
          ShowDiagnostic = true
          MaxCompactLabels = 6
          MaxExpandedLabels = 24 }

    let compactStateDisplayOptions =
        { defaultStateDisplayOptions with
            Density = KeyboardStateDisplayCompact }

    let expandedStateDisplayOptions =
        { defaultStateDisplayOptions with
            Density = KeyboardStateDisplayExpanded
            MaxExpandedLabels = 48 }

    let displayContextKind (mode: ModeDefinition option) =
        match mode with
        | Some { Kind = StandardMode } -> DisplayPermanentContext
        | Some { Kind = StatefulMode } -> DisplayStatefulContext
        | Some { Kind = PopupMode } -> DisplayPopupContext
        | Some { Kind = TemporaryHeldMode } -> DisplayTemporaryHeldContext
        | None -> DisplayUnknownContext

    let isDisplayPersistent kind =
        match kind with
        | DisplayPermanentContext | DisplayStatefulContext -> true
        | DisplayPopupContext | DisplayTemporaryHeldContext | DisplayUnknownContext -> false

    let commandDisplayName (model: CanonicalInputModel) commandId =
        model.Registry.Commands
        |> List.tryFind (fun command -> command.Id = commandId)
        |> Option.map _.DisplayName

    let modeDisplayName configuration modeId =
        configuration.Modes
        |> List.tryFind (fun mode -> mode.Id = modeId)
        |> Option.map _.DisplayName

    let layoutDisplayName configuration layoutId =
        configuration.Layouts
        |> List.tryFind (fun layout -> layout.Id = layoutId)
        |> Option.map _.DisplayName

    let actionLabel (value: string) =
        value.Trim().ToLowerInvariant().Replace(" ", "-").Replace(".", "-").Replace(":", "-")

    let outcomeText model outcome =
        let configuration = model.Configuration

        match outcome with
        | EmitCommand commandId -> commandDisplayName model commandId |> Option.defaultValue commandId |> actionLabel
        | SetState(_, stateId) -> $"{stateId}-state" |> actionLabel
        | SetLayoutOutcome layoutId -> layoutDisplayName configuration layoutId |> Option.defaultValue layoutId |> actionLabel
        | PushPopup modeId -> modeDisplayName configuration modeId |> Option.defaultValue modeId |> actionLabel
        | PushTemporary modeId ->
            let modeName = modeDisplayName configuration modeId |> Option.defaultValue modeId |> actionLabel
            if modeName.EndsWith("-layer", StringComparison.Ordinal) then modeName else $"{modeName}-layer"
        | CancelTopMode -> "cancel"
        | NoInputOp -> ""

    let actionableDiagnostic (diagnostic: InputDiagnostic) =
        match diagnostic.Severity with
        | InputWarning | InputError | InputFatal -> true
        | InputInfo -> false

    let displayDiagnostic (diagnostic: InputDiagnostic) : KeyboardStateDisplayDiagnostic =
        { Severity = diagnostic.Severity
          Code = diagnostic.Code
          Message = diagnostic.Message
          ModeId = diagnostic.ModeId
          CommandId = diagnostic.CommandId
          KeyPositionId = diagnostic.KeyPositionId }

    let emptyDisplayModel visibility density : KeyboardStateDisplayModel =
        { Visibility = visibility
          Density = density
          Layout = None
          Stack = []
          TopContext = None
          ActiveState = None
          Labels = []
          PendingSequence = None
          RecentCommand = None
          Diagnostic = None
          Omitted = []
          IsPartial = false }

    let keyboardStateDisplay (options: KeyboardStateDisplayOptions) (recentEffects: InputEffect list) (runtime: InputRuntime) =
        if options.Visibility = KeyboardStateDisplayHidden then
            emptyDisplayModel options.Visibility options.Density
        else
            let configuration = runtime.Model.Configuration

            let layout =
                match layoutById configuration runtime.ActiveLayout with
                | Some layout ->
                    Some
                        { Id = layout.Id
                          DisplayName = Some layout.DisplayName
                          IsAvailable = true },
                    Some layout,
                    []
                | None ->
                    let diagnostic = error UnknownLayout $"Unknown active layout '{runtime.ActiveLayout}'." None None None
                    Some
                        { Id = runtime.ActiveLayout
                          DisplayName = None
                          IsAvailable = false },
                    None,
                    [ diagnostic ]

            let layoutModel, activeLayout, layoutDiagnostics = layout

            let stack =
                runtime.ModeStack
                |> List.mapi (fun index frame ->
                    let mode = modeById configuration frame.ModeId
                    let kind = displayContextKind mode

                    { ModeId = frame.ModeId
                      DisplayName = mode |> Option.map _.DisplayName
                      Kind = kind
                      State = frame.State
                      EnteredBy = frame.EnteredBy
                      IsTop = index = runtime.ModeStack.Length - 1
                      IsPersistent = isDisplayPersistent kind })

            let stack, stackOmissions =
                match options.Density, stack with
                | KeyboardStateDisplayCompact, entries when entries.Length > 4 ->
                    let visible = (entries |> List.take 1) @ (entries |> List.skip (entries.Length - 3))
                    visible, [ OmittedStackEntries(entries.Length - visible.Length) ]
                | _ -> stack, []

            let topContext = stack |> List.tryFindBack _.IsTop
            let activeState = topContext |> Option.bind _.State

            let allLabels =
                match runtime.ModeStack |> List.tryLast, activeLayout with
                | Some topFrame, Some layout ->
                    configuration.Bindings
                    |> List.filter (fun binding ->
                        binding.ModeId = topFrame.ModeId
                        && (binding.WhenState.IsNone || binding.WhenState = topFrame.State))
                    |> List.choose (fun binding ->
                        binding.Sequence
                        |> List.tryLast
                        |> Option.map (fun chord ->
                            { KeyPositionId = chord.Position
                              Label = layout.Labels |> Map.tryFind chord.Position |> Option.defaultValue chord.Position
                              CommandId =
                                  match binding.Outcome with
                                  | EmitCommand commandId -> Some commandId
                                  | _ -> None
                              Outcome = outcomeText runtime.Model binding.Outcome }))
                    |> List.distinctBy (fun label -> label.KeyPositionId, label.Outcome)
                    |> List.sortBy (fun label -> label.KeyPositionId)
                | _ -> []

            let labelLimit =
                match options.Density with
                | KeyboardStateDisplayCompact -> max 0 options.MaxCompactLabels
                | KeyboardStateDisplayExpanded -> max 0 options.MaxExpandedLabels

            let labels, labelOmissions =
                if not options.ShowKeyLabels then
                    [], if allLabels.IsEmpty then [] else [ OmittedLabels allLabels.Length ]
                else if allLabels.Length > labelLimit then
                    allLabels |> List.truncate labelLimit, [ OmittedLabels(allLabels.Length - labelLimit) ]
                else
                    allLabels, []

            let pendingValue =
                runtime.PendingSequence
                |> Option.map (fun pending ->
                    { Chords = pending.Chords
                      StartedAt = pending.StartedAt
                      IsTimed = true
                      TimeoutMilliseconds = Some configuration.Disambiguation.TimeoutMilliseconds })

            let pending, pendingOmissions =
                if options.ShowPendingSequence then
                    pendingValue, []
                else
                    None, if pendingValue.IsSome then [ OmittedPendingSequence ] else []

            let recentCommandValue =
                recentEffects
                |> List.rev
                |> List.tryPick (function
                    | CommandResolved resolved ->
                        Some
                            { CommandId = resolved.CommandId
                              DisplayName = commandDisplayName runtime.Model resolved.CommandId
                              SourceKey = resolved.SourceKey }
                    | _ -> None)

            let recentCommand, commandOmissions =
                if options.ShowRecentCommand then
                    recentCommandValue, []
                else
                    None, if recentCommandValue.IsSome then [ OmittedRecentCommand ] else []

            let diagnostics =
                runtime.Diagnostics
                @ layoutDiagnostics
                @ (recentEffects
                   |> List.choose (function
                       | InputDiagnosticEmitted diagnostic -> Some diagnostic
                       | _ -> None))

            let diagnosticValue =
                diagnostics
                |> List.filter actionableDiagnostic
                |> List.tryLast
                |> Option.map displayDiagnostic

            let diagnostic, diagnosticOmissions =
                if options.ShowDiagnostic then
                    diagnosticValue, []
                else
                    None, if diagnosticValue.IsSome then [ OmittedDiagnostic ] else []

            { Visibility = options.Visibility
              Density = options.Density
              Layout = layoutModel
              Stack = stack
              TopContext = topContext
              ActiveState = activeState
              Labels = labels
              PendingSequence = pending
              RecentCommand = recentCommand
              Diagnostic = diagnostic
              Omitted = stackOmissions @ labelOmissions @ pendingOmissions @ commandOmissions @ diagnosticOmissions
              IsPartial = activeLayout.IsNone || (stack |> List.exists (fun entry -> entry.Kind = DisplayUnknownContext)) }

    let renderKeyboardStateDisplayAt position (options: KeyboardStateDisplayOptions) recentEffects runtime =
        let model = keyboardStateDisplay options recentEffects runtime

        if model.Visibility = KeyboardStateDisplayHidden then
            Scene.empty
        else
            let x, y = position
            let configuration = runtime.Model.Configuration
            let activeLayout = layoutById configuration runtime.ActiveLayout
            let activeOutcomes =
                model.Labels
                |> List.map (fun label -> label.KeyPositionId, label.Outcome)
                |> Map.ofList

            let positions =
                activeLayout
                |> Option.map (fun layout -> layout.Positions |> List.sortBy (fun position -> position.Row, position.Column))
                |> Option.defaultValue []

            let lineHeight =
                match model.Density with
                | KeyboardStateDisplayCompact -> 18.0
                | KeyboardStateDisplayExpanded -> 20.0

            let layoutText =
                model.Layout
                |> Option.map (fun layout ->
                    let name = layout.DisplayName |> Option.defaultValue layout.Id
                    if layout.IsAvailable then $"layout: {name} ({layout.Id})" else $"layout: {layout.Id} unavailable")
                |> Option.defaultValue "layout: unavailable"

            let stackText =
                if model.Stack.IsEmpty then
                    "stack: none"
                else
                    model.Stack
                    |> List.map (fun entry ->
                        let state = entry.State |> Option.map (fun value -> $":{value}") |> Option.defaultValue ""
                        let top = if entry.IsTop then "*" else ""
                        $"{entry.ModeId}{state}{top}")
                    |> String.concat " > "
                    |> fun value -> $"stack: {value}"

            let lines = [
                yield layoutText
                yield stackText
                let activeStateText = model.ActiveState |> Option.defaultValue "none"
                yield $"state: {activeStateText}"

                if model.Density = KeyboardStateDisplayExpanded || not model.Labels.IsEmpty then
                    let labels =
                        model.Labels
                        |> List.map _.Outcome
                        |> List.filter ((<>) "")
                        |> String.concat "  "
                        |> function
                            | "" -> "none"
                            | value -> value

                    yield $"active: {labels}"

                match model.PendingSequence with
                | Some pending ->
                    let pendingText = pending.Chords |> List.map _.Position |> String.concat " "
                    yield $"pending: {pendingText}"
                | None -> ()

                match model.RecentCommand with
                | Some command -> yield $"recent: {command.DisplayName |> Option.defaultValue command.CommandId} via {command.SourceKey}"
                | None -> ()

                match model.Diagnostic with
                | Some diagnostic -> yield $"diagnostic: {diagnostic.Code} {diagnostic.Message}"
                | None -> ()

                if not model.Omitted.IsEmpty then
                    let omitted =
                        model.Omitted
                        |> List.map (function
                            | OmittedLabels count -> $"labels:{count}"
                            | OmittedPendingSequence -> "pending"
                            | OmittedRecentCommand -> "recent"
                            | OmittedDiagnostic -> "diagnostic"
                            | OmittedStackEntries count -> $"stack:{count}")
                        |> String.concat ", "

                    yield $"omitted: {omitted}"
            ]

            let textAt x y size text =
                Scene.textRun
                    { Text = text
                      Position = { X = x; Y = y }
                      Font = { Family = None; Size = size; Weight = None }
                      Paint = Paint.fill Colors.white |> Paint.withAntialias true }

            let padding = 18.0
            let keyWidth = 60.0
            let keyHeight = 42.0
            let keyGap = 5.0
            let keyboardTop = y + padding + 22.0
            let minRow =
                if positions.IsEmpty then
                    0
                else
                    positions |> List.map _.Row |> List.min

            let rowStagger row =
                match row with
                | 1 -> keyWidth * 0.5
                | 2 -> keyWidth * 1.15
                | 4 -> keyWidth * 4.3
                | _ -> 0.0

            let keyScene position =
                let keyX = x + padding + rowStagger position.Row + float position.Column * (keyWidth + keyGap)
                let keyY = keyboardTop + float (position.Row - minRow) * (keyHeight + keyGap)
                let outcome = activeOutcomes |> Map.tryFind position.Id
                let isAvailable = outcome.IsSome
                let isPressed = runtime.PressedKeys.Contains position.Id
                let fill =
                    if isPressed then
                        Colors.rgba 82uy 150uy 118uy 255uy
                    elif isAvailable then
                        Colors.rgba 48uy 84uy 116uy 255uy
                    else
                        Colors.rgba 28uy 36uy 48uy 235uy

                let keyText (text: string) =
                    if text.Length <= 12 then
                        text
                    else
                        text.Substring(0, 11)

                Scene.group [
                    yield Scene.rectangle (keyX, keyY, keyWidth, keyHeight) fill
                    match outcome with
                    | Some value ->
                        yield textAt (keyX + 6.0) (keyY + 24.0) 9.0 (keyText value)
                    | _ -> ()
                ]

            let keyboardHeight =
                if positions.IsEmpty then
                    0.0
                else
                    let maxRow = positions |> List.map _.Row |> List.max
                    float (maxRow - minRow + 1) * (keyHeight + keyGap)

            let panelWidth =
                match model.Density with
                | KeyboardStateDisplayCompact -> 830.0
                | KeyboardStateDisplayExpanded -> 900.0

            let detailsTop = keyboardTop + keyboardHeight + 8.0
            let panelHeight = padding * 2.0 + keyboardHeight + 22.0 + lineHeight * float lines.Length

            Scene.group [
                Scene.rectangle (x, y, panelWidth, panelHeight) (Colors.rgba 10uy 14uy 20uy 220uy)
                yield! positions |> List.map keyScene
                for index, line in lines |> List.indexed do
                    textAt (x + padding) (detailsTop + float index * lineHeight) 14.0 line
            ]

    let renderKeyboardStateDisplay options recentEffects runtime =
        renderKeyboardStateDisplayAt (24.0, 24.0) options recentEffects runtime

    let renderLayoutStateAt position runtime =
        renderKeyboardStateDisplayAt position expandedStateDisplayOptions [] runtime

    let renderLayoutState runtime =
        renderLayoutStateAt (24.0, 24.0) runtime

    let bindingForCommand model commandId =
        model.Configuration.Bindings
        |> List.tryFind (fun binding ->
            match binding.Outcome with
            | EmitCommand id -> id = commandId
            | _ -> false)

    let keyPosition layout positionId =
        layout.Positions |> List.tryFind (fun position -> position.Id = positionId)

    let distance first second =
        let dx = float (first.Column - second.Column)
        let dy = float (first.Row - second.Row)
        Math.Sqrt(dx * dx + dy * dy)

    let analyzeBigrams model layout =
        let configuration = model.Configuration
        let layout = layoutById configuration layout |> Option.defaultWith (fun () -> configuration.Layouts |> List.head)
        let profile = configuration.BigramProfile |> Option.defaultValue { Weights = []; SuggestionLimit = 20 }

        let topPairs =
            profile.Weights
            |> List.sortByDescending _.Weight
            |> List.truncate profile.SuggestionLimit

        let risks =
            topPairs
            |> List.collect (fun pair ->
                match bindingForCommand model pair.First, bindingForCommand model pair.Second with
                | Some firstBinding, Some secondBinding ->
                    match firstBinding.Sequence |> List.tryLast, secondBinding.Sequence |> List.tryLast with
                    | Some firstChord, Some secondChord ->
                        match keyPosition layout firstChord.Position, keyPosition layout secondChord.Position with
                        | Some firstPosition, Some secondPosition ->
                            [ if firstPosition.Finger = secondPosition.Finger && firstPosition.Finger <> UnknownFinger then
                                  { First = pair.First
                                    Second = pair.Second
                                    Weight = pair.Weight
                                    Kind = SameFinger
                                    Description = "Frequent pair uses the same finger." }

                              if firstPosition.Hand = secondPosition.Hand && firstPosition.Hand <> UnknownHand then
                                  { First = pair.First
                                    Second = pair.Second
                                    Weight = pair.Weight
                                    Kind = SameHandRepeat
                                    Description = "Frequent pair repeats on the same hand." }

                              if distance firstPosition secondPosition > 2.5 then
                                  { First = pair.First
                                    Second = pair.Second
                                    Weight = pair.Weight
                                    Kind = LongTravel
                                    Description = "Frequent pair crosses a long physical distance." }

                              if not firstChord.RequiredHeld.IsEmpty || not secondChord.RequiredHeld.IsEmpty then
                                  { First = pair.First
                                    Second = pair.Second
                                    Weight = pair.Weight
                                    Kind = AwkwardHold
                                    Description = "Frequent pair involves a held-mode modifier." } ]
                        | _ -> []
                    | _ -> []
                | _ -> [])

        let suggestions =
            risks
            |> List.truncate profile.SuggestionLimit
            |> List.map (fun risk ->
                { First = risk.First
                  Second = risk.Second
                  Description = $"Review placement for '{risk.First}' then '{risk.Second}' under layout '{layout.Id}'."
                  ExpectedScoreDelta = risk.Weight * 0.1 })

        { LayoutId = layout.Id
          TopPairs = topPairs
          Risks = risks
          Suggestions = suggestions }
