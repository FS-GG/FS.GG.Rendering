namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

[<RequireQualifiedAccess>]
type TransientSurfaceKind =
    | Menu
    | ContextMenu
    | SplitButtonMenu
    | ComboDropdown
    | AutoCompleteSuggestions
    | DatePickerCalendar
    | ColorPickerPalette
    | DialogModal

[<RequireQualifiedAccess>]
type DismissalReason =
    | Escape
    | OutsidePointer
    | SelectionCompletion
    | ExplicitClose
    | AnchorRemoved
    | ProductClosed

type OverlayActivationSource =
    | PointerActivation
    | KeyboardActivation
    | ProductOwnedOpen
    | NestedOpen

type DismissalRule =
    | AllowDismiss
    | BlockDismiss
    | IgnoreDismiss

type SelectionCompletionPolicy =
    | CloseOnSelection
    | KeepOpenOnSelection

type AnchorRemovalPolicy =
    | CloseOnAnchorRemoval
    | KeepOpenWithDiagnostic

type TrapMode =
    | ModalTrap
    | LocalScope
    | NoFocusCapture

type OverlaySurfaceId =
    { SurfaceId: ControlId
      ParentSurfaceId: ControlId option
      TriggerId: ControlId }

type OverlayTrigger =
    { ControlId: ControlId
      Enabled: bool
      ActivationSource: OverlayActivationSource
      RecoveryTarget: ControlId option }

type AnchorEvidence =
    { AnchorId: ControlId
      AnchorBounds: Rect option
      SurfaceBounds: Rect option
      Placement: string
      NoFit: string option
      FrameFingerprint: uint64 option }

type DismissalPolicy =
    { Escape: DismissalRule
      OutsidePointer: DismissalRule
      SelectionCompletion: SelectionCompletionPolicy
      ExplicitClose: DismissalRule
      AnchorRemoval: AnchorRemovalPolicy
      PassThroughAfterDismissal: bool }

type FocusScope =
    { SurfaceId: ControlId
      Stops: ControlId list
      InitialFocus: ControlId option
      RecoveryTarget: ControlId option
      TrapMode: TrapMode }

type OverlaySurface =
    { Id: OverlaySurfaceId
      Kind: TransientSurfaceKind
      Trigger: OverlayTrigger
      LayerPriority: int
      Anchor: AnchorEvidence
      DismissalPolicy: DismissalPolicy
      FocusScope: FocusScope
      Modal: bool }

type TopmostHitDecision =
    { Input: string
      CandidateLayers: ControlId list
      ChosenTarget: ControlId option
      BlockedByModal: ControlId option
      OutsideOfSurface: ControlId option }

type OverlayTransition =
    { SurfaceId: ControlId option
      Kind: string
      Reason: string
      Stack: ControlId list }

type FocusTransition =
    { From: ControlId option
      To: ControlId option
      Reason: string }

type ProductDispatch =
    { SurfaceId: ControlId
      DispatchKey: string
      Payload: string option }

type DismissalOutcome =
    { SurfaceId: ControlId
      Reason: DismissalReason
      Dismissed: bool
      PassThrough: bool
      Diagnostic: ControlDiagnostic option }

type InteractionReplayLog =
    { Inputs: string list
      OverlayTransitions: OverlayTransition list
      FocusTransitions: FocusTransition list
      ProductDispatches: ProductDispatch list
      DismissalReasons: DismissalOutcome list
      Diagnostics: ControlDiagnostic list
      HitDecisions: TopmostHitDecision list
      RenderEvidence: string list }

type OverlayState =
    { OpenSurfaces: OverlaySurface list
      ActiveSurface: ControlId option
      FocusedControl: ControlId option
      RecentTransitions: OverlayTransition list
      ReplayLog: InteractionReplayLog
      Diagnostics: ControlDiagnostic list
      DispatchedSelectionKeys: Set<string> }

type OverlayEffect =
    | DispatchProductMessage of surface: ControlId * payload: string option
    | RequestFocus of ControlId option
    | RequestOpenStateChange of surface: ControlId * isOpen: bool
    | ReportOverlayDiagnostic of ControlDiagnostic
    | ConsumeInput
    | AllowPassThrough
    | RecordTopmostHit of TopmostHitDecision

type OverlayMsg =
    | OpenRequested of OverlaySurface
    | DismissRequested of surfaceId: ControlId option * reason: DismissalReason
    | PointerRouted of TopmostHitDecision
    | KeyRouted of surfaceId: ControlId option * key: string
    | SelectionCompleted of surfaceId: ControlId * dispatchKey: string * payload: string option
    | AnchorChanged of surfaceId: ControlId * anchor: AnchorEvidence
    | AnchorRemoved of surfaceId: ControlId
    | FocusTargetRemoved of targetId: ControlId
    | Reset

module OverlayState =
    let supportedSurfaceKinds () =
        [ TransientSurfaceKind.Menu
          TransientSurfaceKind.ContextMenu
          TransientSurfaceKind.SplitButtonMenu
          TransientSurfaceKind.ComboDropdown
          TransientSurfaceKind.AutoCompleteSuggestions
          TransientSurfaceKind.DatePickerCalendar
          TransientSurfaceKind.ColorPickerPalette
          TransientSurfaceKind.DialogModal ]

    let defaultDismissalPolicy () =
        { Escape = AllowDismiss
          OutsidePointer = AllowDismiss
          SelectionCompletion = CloseOnSelection
          ExplicitClose = AllowDismiss
          AnchorRemoval = CloseOnAnchorRemoval
          PassThroughAfterDismissal = false }

    let modalDismissalPolicy () =
        { Escape = AllowDismiss
          OutsidePointer = BlockDismiss
          SelectionCompletion = CloseOnSelection
          ExplicitClose = AllowDismiss
          AnchorRemoval = CloseOnAnchorRemoval
          PassThroughAfterDismissal = false }

    let private emptyReplayLog =
        { Inputs = []
          OverlayTransitions = []
          FocusTransitions = []
          ProductDispatches = []
          DismissalReasons = []
          Diagnostics = []
          HitDecisions = []
          RenderEvidence = [] }

    let private stackIds surfaces =
        surfaces |> List.map (fun surface -> surface.Id.SurfaceId)

    let private activeSurface surfaces =
        surfaces |> List.tryLast |> Option.map (fun surface -> surface.Id.SurfaceId)

    let private orderStack surfaces =
        surfaces
        |> List.mapi (fun index surface -> index, surface)
        |> List.sortBy (fun (index, surface) -> surface.LayerPriority, index, surface.Id.SurfaceId)
        |> List.map snd

    let init () =
        { OpenSurfaces = []
          ActiveSurface = None
          FocusedControl = None
          RecentTransitions = []
          ReplayLog = emptyReplayLog
          Diagnostics = []
          DispatchedSelectionKeys = Set.empty }

    let diagnostics state =
        state.Diagnostics

    let replayLog state =
        state.ReplayLog

    let private rememberInput input state =
        { state with ReplayLog = { state.ReplayLog with Inputs = state.ReplayLog.Inputs @ [ input ] } }

    let private recordTransition surfaceId kind reason surfaces state =
        let transition =
            { SurfaceId = surfaceId
              Kind = kind
              Reason = reason
              Stack = stackIds surfaces }

        { state with
            RecentTransitions = state.RecentTransitions @ [ transition ]
            ReplayLog = { state.ReplayLog with OverlayTransitions = state.ReplayLog.OverlayTransitions @ [ transition ] } }

    let private recordFocus fromFocus toFocus reason state =
        if fromFocus = toFocus then
            state
        else
            let transition =
                { From = fromFocus
                  To = toFocus
                  Reason = reason }

            { state with ReplayLog = { state.ReplayLog with FocusTransitions = state.ReplayLog.FocusTransitions @ [ transition ] } }

    let private addDiagnostic diagnostic state =
        { state with
            Diagnostics = state.Diagnostics @ [ diagnostic ]
            ReplayLog = { state.ReplayLog with Diagnostics = state.ReplayLog.Diagnostics @ [ diagnostic ] } }

    let private addDismissal outcome state =
        { state with ReplayLog = { state.ReplayLog with DismissalReasons = state.ReplayLog.DismissalReasons @ [ outcome ] } }

    let private addDispatch dispatch state =
        { state with ReplayLog = { state.ReplayLog with ProductDispatches = state.ReplayLog.ProductDispatches @ [ dispatch ] } }

    let private addHit decision state =
        { state with ReplayLog = { state.ReplayLog with HitDecisions = state.ReplayLog.HitDecisions @ [ decision ] } }

    let private invalid surfaceId message state =
        let diagnostic = Diagnostics.invalidOverlayMessage surfaceId message
        addDiagnostic diagnostic state, [ ReportOverlayDiagnostic diagnostic ]

    let private focusForOpen surface =
        match surface.FocusScope.TrapMode, surface.FocusScope.InitialFocus with
        | NoFocusCapture, _ -> None
        | _, Some target -> Some target
        | _, None -> surface.FocusScope.Stops |> List.tryHead

    let private recoveryFocus surface remaining =
        surface.FocusScope.RecoveryTarget
        |> Option.orElse surface.Trigger.RecoveryTarget
        |> Option.orElseWith (fun () ->
            surface.Id.ParentSurfaceId
            |> Option.bind (fun parentId ->
                remaining
                |> List.tryFind (fun candidate -> candidate.Id.SurfaceId = parentId)
                |> Option.bind focusForOpen))

    let private dismissRule reason policy =
        match reason with
        | DismissalReason.Escape -> Some policy.Escape
        | DismissalReason.OutsidePointer -> Some policy.OutsidePointer
        | DismissalReason.ExplicitClose
        | DismissalReason.ProductClosed -> Some policy.ExplicitClose
        | DismissalReason.SelectionCompletion
        | DismissalReason.AnchorRemoved -> None

    let private reasonText reason =
        match reason with
        | DismissalReason.Escape -> "escape"
        | DismissalReason.OutsidePointer -> "outside-pointer"
        | DismissalReason.SelectionCompletion -> "selection-completion"
        | DismissalReason.ExplicitClose -> "explicit-close"
        | DismissalReason.AnchorRemoved -> "anchor-removed"
        | DismissalReason.ProductClosed -> "product-closed"

    let private closeSurface surface reason state =
        let removed =
            state.OpenSurfaces
            |> List.filter (fun candidate ->
                candidate.Id.SurfaceId <> surface.Id.SurfaceId
                && candidate.Id.ParentSurfaceId <> Some surface.Id.SurfaceId)
            |> orderStack

        let focus = recoveryFocus surface removed

        let next =
            { state with
                OpenSurfaces = removed
                ActiveSurface = activeSurface removed
                FocusedControl = focus }
            |> recordFocus state.FocusedControl focus (reasonText reason)
            |> recordTransition (Some surface.Id.SurfaceId) "dismiss" (reasonText reason) removed
            |> addDismissal
                { SurfaceId = surface.Id.SurfaceId
                  Reason = reason
                  Dismissed = true
                  PassThrough = surface.DismissalPolicy.PassThroughAfterDismissal
                  Diagnostic = None }

        let effects =
            [ RequestOpenStateChange(surface.Id.SurfaceId, false)
              RequestFocus focus
              if surface.DismissalPolicy.PassThroughAfterDismissal then
                  AllowPassThrough
              else
                  ConsumeInput ]

        next, effects

    let rec private dismiss surfaceId reason state =
        let target =
            match surfaceId with
            | Some id -> state.OpenSurfaces |> List.tryFind (fun surface -> surface.Id.SurfaceId = id)
            | None -> state.OpenSurfaces |> List.tryLast

        match target with
        | None -> invalid surfaceId $"Overlay dismiss `{reasonText reason}` referenced no open surface." state
        | Some surface ->
            let topmost = state.OpenSurfaces |> List.tryLast |> Option.map (fun candidate -> candidate.Id.SurfaceId)

            if (reason = DismissalReason.Escape || reason = DismissalReason.OutsidePointer) && surfaceId.IsSome && topmost <> Some surface.Id.SurfaceId then
                let diagnostic = Diagnostics.blockedOverlayDismissal surface.Id.SurfaceId $"{reasonText reason}:not-topmost"

                let next =
                    state
                    |> addDiagnostic diagnostic
                    |> addDismissal
                        { SurfaceId = surface.Id.SurfaceId
                          Reason = reason
                          Dismissed = false
                          PassThrough = false
                          Diagnostic = Some diagnostic }

                next, [ ReportOverlayDiagnostic diagnostic; ConsumeInput ]
            else
                match reason with
                | DismissalReason.SelectionCompletion ->
                    match surface.DismissalPolicy.SelectionCompletion with
                    | CloseOnSelection -> closeSurface surface reason state
                    | KeepOpenOnSelection ->
                        let next =
                            state
                            |> addDismissal
                                { SurfaceId = surface.Id.SurfaceId
                                  Reason = reason
                                  Dismissed = false
                                  PassThrough = false
                                  Diagnostic = None }

                        next, [ ConsumeInput ]
                | DismissalReason.AnchorRemoved ->
                    match surface.DismissalPolicy.AnchorRemoval with
                    | CloseOnAnchorRemoval -> closeSurface surface reason state
                    | KeepOpenWithDiagnostic ->
                        let diagnostic = Diagnostics.missingOverlayAnchor surface.Id.SurfaceId surface.Anchor.AnchorId
                        let next =
                            state
                            |> addDiagnostic diagnostic
                            |> addDismissal
                                { SurfaceId = surface.Id.SurfaceId
                                  Reason = reason
                                  Dismissed = false
                                  PassThrough = false
                                  Diagnostic = Some diagnostic }

                        next, [ ReportOverlayDiagnostic diagnostic; ConsumeInput ]
                | _ ->
                    match dismissRule reason surface.DismissalPolicy with
                    | Some AllowDismiss -> closeSurface surface reason state
                    | Some BlockDismiss ->
                        let diagnostic = Diagnostics.blockedOverlayDismissal surface.Id.SurfaceId (reasonText reason)
                        let next =
                            state
                            |> addDiagnostic diagnostic
                            |> addDismissal
                                { SurfaceId = surface.Id.SurfaceId
                                  Reason = reason
                                  Dismissed = false
                                  PassThrough = false
                                  Diagnostic = Some diagnostic }

                        next, [ ReportOverlayDiagnostic diagnostic; ConsumeInput ]
                    | Some IgnoreDismiss ->
                        let next =
                            state
                            |> addDismissal
                                { SurfaceId = surface.Id.SurfaceId
                                  Reason = reason
                                  Dismissed = false
                                  PassThrough = true
                                  Diagnostic = None }

                        next, [ AllowPassThrough ]
                    | None -> invalid (Some surface.Id.SurfaceId) $"Overlay dismiss `{reasonText reason}` has no policy rule." state

    let private openSurface surface state =
        if not surface.Trigger.Enabled then
            let diagnostic = Diagnostics.disabledOverlayTrigger surface.Trigger.ControlId surface.Id.SurfaceId
            addDiagnostic diagnostic state, [ ReportOverlayDiagnostic diagnostic; AllowPassThrough ]
        elif surface.Id.TriggerId <> surface.Trigger.ControlId then
            invalid (Some surface.Id.SurfaceId) $"Overlay surface `{surface.Id.SurfaceId}` trigger identity does not match its trigger record." state
        elif surface.Anchor.AnchorBounds.IsNone then
            let diagnostic = Diagnostics.missingOverlayAnchor surface.Id.SurfaceId surface.Anchor.AnchorId
            addDiagnostic diagnostic state, [ ReportOverlayDiagnostic diagnostic; AllowPassThrough ]
        else
            let diagnostics =
                surface.Anchor.NoFit
                |> Option.map (fun placement -> Diagnostics.noFitOverlayPlacement surface.Id.SurfaceId placement)
                |> Option.toList

            let withoutDuplicate =
                state.OpenSurfaces
                |> List.filter (fun existing -> existing.Id.SurfaceId <> surface.Id.SurfaceId)

            let opened = orderStack (withoutDuplicate @ [ surface ])
            let focus = focusForOpen surface
            let nextFocus = focus |> Option.orElse state.FocusedControl

            let next =
                { state with
                    OpenSurfaces = opened
                    ActiveSurface = activeSurface opened
                    FocusedControl = nextFocus }
                |> recordFocus state.FocusedControl nextFocus "open"
                |> recordTransition (Some surface.Id.SurfaceId) "open" $"{surface.Kind}" opened

            let nextWithDiagnostics =
                diagnostics |> List.fold (fun current diagnostic -> addDiagnostic diagnostic current) next

            let effects =
                [ yield RequestOpenStateChange(surface.Id.SurfaceId, true)
                  match focus with
                  | Some target -> yield RequestFocus(Some target)
                  | None -> ()
                  yield ConsumeInput
                  for diagnostic in diagnostics do
                      yield ReportOverlayDiagnostic diagnostic ]

            nextWithDiagnostics, effects

    let private routeKey surfaceId key state =
        match key with
        | "Escape" -> dismiss surfaceId DismissalReason.Escape state
        | "Tab"
        | "Shift+Tab" ->
            let surface =
                surfaceId
                |> Option.orElse state.ActiveSurface
                |> Option.bind (fun id -> state.OpenSurfaces |> List.tryFind (fun candidate -> candidate.Id.SurfaceId = id))

            match surface with
            | None -> invalid surfaceId $"Overlay key `{key}` referenced no open surface." state
            | Some active when active.FocusScope.TrapMode = NoFocusCapture -> state, [ AllowPassThrough ]
            | Some active ->
                match active.FocusScope.Stops with
                | [] ->
                    let diagnostic = Diagnostics.staleOverlayFocusTarget (Some active.Id.SurfaceId) "<empty-focus-scope>"
                    addDiagnostic diagnostic state, [ ReportOverlayDiagnostic diagnostic; ConsumeInput ]
                | stops ->
                    let current =
                        state.FocusedControl
                        |> Option.bind (fun id ->
                            stops
                            |> List.tryFindIndex ((=) id)
                            |> Option.map (fun index -> index, id))

                    let nextIndex =
                        match current, key with
                        | Some(index, _), "Shift+Tab" -> (index - 1 + stops.Length) % stops.Length
                        | Some(index, _), _ -> (index + 1) % stops.Length
                        | None, "Shift+Tab" -> stops.Length - 1
                        | None, _ -> 0

                    let focus = Some stops[nextIndex]
                    let next =
                        { state with FocusedControl = focus }
                        |> recordFocus state.FocusedControl focus key

                    next, [ RequestFocus focus; ConsumeInput ]
        | _ -> state, [ AllowPassThrough ]

    let update msg state =
        let state = rememberInput $"{msg}" state

        match msg with
        | OpenRequested surface -> openSurface surface state
        | DismissRequested(surfaceId, reason) -> dismiss surfaceId reason state
        | PointerRouted decision ->
            let stateWithHit = addHit decision state

            match decision.BlockedByModal with
            | Some modalId ->
                let diagnostic = Diagnostics.lowerLayerBlocked modalId decision.ChosenTarget
                addDiagnostic diagnostic stateWithHit, [ RecordTopmostHit decision; ReportOverlayDiagnostic diagnostic; ConsumeInput ]
            | None ->
                match decision.OutsideOfSurface with
                | Some surfaceId ->
                    let next, effects = dismiss (Some surfaceId) DismissalReason.OutsidePointer stateWithHit
                    next, RecordTopmostHit decision :: effects
                | None -> stateWithHit, [ RecordTopmostHit decision ]
        | KeyRouted(surfaceId, key) -> routeKey surfaceId key state
        | SelectionCompleted(surfaceId, dispatchKey, payload) ->
            if state.DispatchedSelectionKeys.Contains dispatchKey then
                let diagnostic = Diagnostics.duplicateOverlayDispatch surfaceId dispatchKey
                addDiagnostic diagnostic state, [ ReportOverlayDiagnostic diagnostic; ConsumeInput ]
            else
                let dispatch =
                    { SurfaceId = surfaceId
                      DispatchKey = dispatchKey
                      Payload = payload }

                let withDispatch =
                    { state with DispatchedSelectionKeys = state.DispatchedSelectionKeys.Add dispatchKey }
                    |> addDispatch dispatch

                let dismissed, effects = dismiss (Some surfaceId) DismissalReason.SelectionCompletion withDispatch
                dismissed, DispatchProductMessage(surfaceId, payload) :: effects
        | AnchorChanged(surfaceId, anchor) ->
            let mutable found = false

            let surfaces =
                state.OpenSurfaces
                |> List.map (fun surface ->
                    if surface.Id.SurfaceId = surfaceId then
                        found <- true
                        { surface with Anchor = anchor }
                    else
                        surface)

            if not found then
                invalid (Some surfaceId) $"Overlay anchor update referenced stale surface `{surfaceId}`." state
            else
                let next =
                    { state with OpenSurfaces = surfaces }
                    |> recordTransition (Some surfaceId) "anchor-changed" anchor.Placement surfaces

                match anchor.NoFit with
                | Some placement ->
                    let diagnostic = Diagnostics.noFitOverlayPlacement surfaceId placement
                    addDiagnostic diagnostic next, [ ReportOverlayDiagnostic diagnostic ]
                | None -> next, []
        | AnchorRemoved surfaceId -> dismiss (Some surfaceId) DismissalReason.AnchorRemoved state
        | FocusTargetRemoved targetId ->
            let active =
                state.ActiveSurface
                |> Option.bind (fun id -> state.OpenSurfaces |> List.tryFind (fun surface -> surface.Id.SurfaceId = id))

            let surfaceId = active |> Option.map (fun surface -> surface.Id.SurfaceId)

            if state.FocusedControl <> Some targetId && (active |> Option.exists (fun surface -> surface.FocusScope.Stops |> List.contains targetId) |> not) then
                state, []
            else
                let focus =
                    active
                    |> Option.bind (fun surface ->
                        surface.FocusScope.RecoveryTarget
                        |> Option.orElse surface.Trigger.RecoveryTarget
                        |> Option.orElse (surface.FocusScope.Stops |> List.tryFind ((<>) targetId)))

                let diagnostic = Diagnostics.staleOverlayFocusTarget surfaceId targetId

                let next =
                    { state with FocusedControl = focus }
                    |> recordFocus state.FocusedControl focus "focus-target-removed"
                    |> addDiagnostic diagnostic

                next, [ RequestFocus focus; ReportOverlayDiagnostic diagnostic; ConsumeInput ]
        | Reset ->
            let effects =
                state.OpenSurfaces
                |> List.rev
                |> List.map (fun surface -> RequestOpenStateChange(surface.Id.SurfaceId, false))

            init (), effects
