namespace FS.GG.UI.Elmish

open System

type TransitionGeneration = private TransitionGeneration of int64

module TransitionGeneration =
    let value (TransitionGeneration value) = value

[<RequireQualifiedAccess>]
type TransitionVisibility =
    | Visible
    | Hidden

[<RequireQualifiedAccess>]
type TransitionResponseKind =
    | PlanningWorker
    | ClientFeatures
    | Other of name: string

type TransitionFocusTarget =
    { ControlId: string; AriaLabel: string }

type TransitionRequest<'target> =
    { Target: 'target
      PendingFocus: TransitionFocusTarget
      CommittedFocus: TransitionFocusTarget }

type TransitionCommitToken<'target> =
    { Generation: TransitionGeneration
      Target: 'target
      Revision: int64 }

type TransitionResponse<'target, 'response> =
    { Generation: TransitionGeneration
      Target: 'target
      Kind: TransitionResponseKind
      Payload: 'response }

[<RequireQualifiedAccess>]
type TransitionHostInput =
    | ControlledValueChanged of controlId: string * value: string
    | ControlledFileChanged of controlId: string * fileToken: string option
    | ControlledBlurred of controlId: string
    | PointerCaptureHeld of pointerId: int64
    | GlobalKeyAttempted of key: string
    | GlobalClickAttempted of targetId: string
    | GlobalFileAttempted of controlId: string * fileToken: string option

[<RequireQualifiedAccess>]
type TransitionRejectionReason =
    | StaleGeneration
    | TargetMismatch
    | RevisionMismatch
    | HiddenPresentation
    | NoPresentationRequested

type TransitionPresentation<'target, 'response> =
    { Token: TransitionCommitToken<'target>
      Responses: TransitionResponse<'target, 'response> list }

[<RequireQualifiedAccess>]
type TransitionHostEffect<'target, 'response> =
    | RequestPresentation of TransitionPresentation<'target, 'response>
    | ReleasePointerCapture of pointerId: int64
    | SuppressInput of TransitionHostInput
    | MoveFocus of TransitionFocusTarget

[<RequireQualifiedAccess>]
type TransitionLedgerEntry<'target> =
    | Began of TransitionCommitToken<'target>
    | ResponseAccepted of TransitionCommitToken<'target> * TransitionResponseKind
    | ResponseRejected of TransitionGeneration * 'target * TransitionResponseKind * TransitionRejectionReason
    | PresentationRequested of TransitionCommitToken<'target>
    | PresentationWithheld of TransitionCommitToken<'target>
    | VisibilityChanged of TransitionVisibility
    | InputApplied of TransitionHostInput
    | InputSuppressed of TransitionHostInput
    | PointerCaptureReleased of pointerId: int64
    | FocusMoved of TransitionFocusTarget
    | PresentationAcknowledged of TransitionCommitToken<'target>
    | PresentationRejected of TransitionCommitToken<'target> * TransitionRejectionReason
    | Committed of TransitionCommitToken<'target>

[<RequireQualifiedAccess>]
type TransitionHostMsg<'target, 'response> =
    | BeginTransition of TransitionRequest<'target>
    | ResponseArrived of TransitionResponse<'target, 'response>
    | VisibilityChanged of TransitionVisibility
    | Presented of TransitionCommitToken<'target>
    | InputAttempted of TransitionHostInput

type private CurrentTransition<'target, 'response> =
    { Request: TransitionRequest<'target>
      Token: TransitionCommitToken<'target>
      Responses: TransitionResponse<'target, 'response> list }

type TransitionHostModel<'target, 'response> =
    private
        { NextGeneration: int64
          Current: CurrentTransition<'target, 'response> option
          Requested: TransitionCommitToken<'target> option
          Committed: TransitionCommitToken<'target> option
          Visibility: TransitionVisibility
          FocusTarget: TransitionFocusTarget option
          ControlledValues: Map<string, string>
          ControlledFiles: Map<string, string option>
          Ledger: TransitionLedgerEntry<'target> list }

module TransitionHost =
    let init visibility =
        { NextGeneration = 0L
          Current = None
          Requested = None
          Committed = None
          Visibility = visibility
          FocusTarget = None
          ControlledValues = Map.empty
          ControlledFiles = Map.empty
          Ledger = [] }

    let private append (entries: TransitionLedgerEntry<'target> list) (model: TransitionHostModel<'target, 'response>) =
        { model with
            Ledger = model.Ledger @ entries }

    let private presentation (current: CurrentTransition<'target, 'response>) =
        { Token = current.Token
          Responses = current.Responses }

    let private pending (model: TransitionHostModel<'target, 'response>) =
        match model.Current with
        | None -> false
        | Some current -> model.Committed <> Some current.Token

    let private rejectionForToken
        (current: CurrentTransition<'target, 'response>)
        (token: TransitionCommitToken<'target>)
        =
        if token.Generation <> current.Token.Generation then
            TransitionRejectionReason.StaleGeneration
        elif token.Target <> current.Token.Target then
            TransitionRejectionReason.TargetMismatch
        else
            TransitionRejectionReason.RevisionMismatch

    let private beginTransitionInternal
        (request: TransitionRequest<'target>)
        (model: TransitionHostModel<'target, 'response>)
        =
        if model.NextGeneration = Int64.MaxValue then
            invalidOp "Transition generation space is exhausted."

        let generationValue = model.NextGeneration + 1L

        let token =
            { Generation = TransitionGeneration generationValue
              Target = request.Target
              Revision = 0L }

        let current =
            { Request = request
              Token = token
              Responses = [] }

        let baseModel =
            { model with
                NextGeneration = generationValue
                Current = Some current
                Requested = None
                FocusTarget = Some request.PendingFocus }

        match model.Visibility with
        | TransitionVisibility.Visible ->
            { baseModel with
                Requested = Some token }
            |> append
                [ TransitionLedgerEntry.Began token
                  TransitionLedgerEntry.PresentationRequested token
                  TransitionLedgerEntry.FocusMoved request.PendingFocus ],
            [ TransitionHostEffect.RequestPresentation(presentation current)
              TransitionHostEffect.MoveFocus request.PendingFocus ]
        | TransitionVisibility.Hidden ->
            baseModel
            |> append
                [ TransitionLedgerEntry.Began token
                  TransitionLedgerEntry.PresentationWithheld token ],
            []

    let private acceptResponse
        (response: TransitionResponse<'target, 'response>)
        (current: CurrentTransition<'target, 'response>)
        (model: TransitionHostModel<'target, 'response>)
        =
        if current.Token.Revision = Int64.MaxValue then
            invalidOp "Transition response revision space is exhausted."

        let token =
            { current.Token with
                Revision = current.Token.Revision + 1L }

        let nextCurrent =
            { current with
                Token = token
                Responses = current.Responses @ [ response ] }

        let wasPending = pending model

        let baseModel =
            { model with
                Current = Some nextCurrent
                Requested = None
                FocusTarget = Some current.Request.PendingFocus }

        let focusLedger, focusEffects =
            if wasPending || model.Visibility = TransitionVisibility.Hidden then
                [], []
            else
                [ TransitionLedgerEntry.FocusMoved current.Request.PendingFocus ],
                [ TransitionHostEffect.MoveFocus current.Request.PendingFocus ]

        match model.Visibility with
        | TransitionVisibility.Visible ->
            { baseModel with
                Requested = Some token }
            |> append (
                [ TransitionLedgerEntry.ResponseAccepted(token, response.Kind)
                  TransitionLedgerEntry.PresentationRequested token ]
                @ focusLedger
            ),
            ([ TransitionHostEffect.RequestPresentation(presentation nextCurrent) ]
             @ focusEffects)
        | TransitionVisibility.Hidden ->
            baseModel
            |> append (
                [ TransitionLedgerEntry.ResponseAccepted(token, response.Kind)
                  TransitionLedgerEntry.PresentationWithheld token ]
                @ focusLedger
            ),
            focusEffects

    let private responseArrived
        (response: TransitionResponse<'target, 'response>)
        (model: TransitionHostModel<'target, 'response>)
        =
        match model.Current with
        | None ->
            model
            |> append
                [ TransitionLedgerEntry.ResponseRejected(
                      response.Generation,
                      response.Target,
                      response.Kind,
                      TransitionRejectionReason.StaleGeneration
                  ) ],
            []
        | Some current when response.Generation <> current.Token.Generation ->
            model
            |> append
                [ TransitionLedgerEntry.ResponseRejected(
                      response.Generation,
                      response.Target,
                      response.Kind,
                      TransitionRejectionReason.StaleGeneration
                  ) ],
            []
        | Some current when response.Target <> current.Token.Target ->
            model
            |> append
                [ TransitionLedgerEntry.ResponseRejected(
                      response.Generation,
                      response.Target,
                      response.Kind,
                      TransitionRejectionReason.TargetMismatch
                  ) ],
            []
        | Some current -> acceptResponse response current model

    let private visibilityChanged (visibility: TransitionVisibility) (model: TransitionHostModel<'target, 'response>) =
        if visibility = model.Visibility then
            model, []
        else
            let changed =
                { model with
                    Visibility = visibility
                    Requested =
                        match visibility with
                        | TransitionVisibility.Hidden -> None
                        | TransitionVisibility.Visible -> model.Requested }
                |> append [ TransitionLedgerEntry.VisibilityChanged visibility ]

            match visibility, changed.Current, changed.FocusTarget with
            | TransitionVisibility.Visible, Some current, Some focusTarget when pending changed ->
                { changed with Requested = Some current.Token }
                |> append
                    [ TransitionLedgerEntry.PresentationRequested current.Token
                      TransitionLedgerEntry.FocusMoved focusTarget ],
                [ TransitionHostEffect.RequestPresentation(presentation current)
                  TransitionHostEffect.MoveFocus focusTarget ]
            | TransitionVisibility.Visible, _, Some focusTarget ->
                changed |> append [ TransitionLedgerEntry.FocusMoved focusTarget ],
                [ TransitionHostEffect.MoveFocus focusTarget ]
            | _ -> changed, []

    let private presented (token: TransitionCommitToken<'target>) (model: TransitionHostModel<'target, 'response>) =
        match model.Visibility, model.Current with
        | TransitionVisibility.Hidden, _ ->
            model
            |> append
                [ TransitionLedgerEntry.PresentationRejected(token, TransitionRejectionReason.HiddenPresentation) ],
            []
        | _, None ->
            model
            |> append
                [ TransitionLedgerEntry.PresentationRejected(token, TransitionRejectionReason.NoPresentationRequested) ],
            []
        | TransitionVisibility.Visible, Some current when model.Requested = Some token && current.Token = token ->
            { model with
                Requested = None
                Committed = Some token
                FocusTarget = Some current.Request.CommittedFocus }
            |> append
                [ TransitionLedgerEntry.PresentationAcknowledged token
                  TransitionLedgerEntry.Committed token
                  TransitionLedgerEntry.FocusMoved current.Request.CommittedFocus ],
            [ TransitionHostEffect.MoveFocus current.Request.CommittedFocus ]
        | TransitionVisibility.Visible, Some current ->
            let reason =
                match model.Requested with
                | None -> TransitionRejectionReason.NoPresentationRequested
                | Some _ -> rejectionForToken current token

            model |> append [ TransitionLedgerEntry.PresentationRejected(token, reason) ], []

    let private isControlledInput (input: TransitionHostInput) =
        match input with
        | TransitionHostInput.ControlledValueChanged _
        | TransitionHostInput.ControlledFileChanged _
        | TransitionHostInput.ControlledBlurred _ -> true
        | _ -> false

    let private applyControlled (input: TransitionHostInput) (model: TransitionHostModel<'target, 'response>) =
        match input with
        | TransitionHostInput.ControlledValueChanged(controlId, value) ->
            { model with
                ControlledValues = Map.add controlId value model.ControlledValues }
        | TransitionHostInput.ControlledFileChanged(controlId, fileToken) ->
            { model with
                ControlledFiles = Map.add controlId fileToken model.ControlledFiles }
        | TransitionHostInput.ControlledBlurred _ -> model
        | _ -> model

    let private inputAttempted (input: TransitionHostInput) (model: TransitionHostModel<'target, 'response>) =
        if isControlledInput input then
            applyControlled input model
            |> append [ TransitionLedgerEntry.InputApplied input ],
            []
        elif pending model then
            match input with
            | TransitionHostInput.PointerCaptureHeld pointerId ->
                model
                |> append
                    [ TransitionLedgerEntry.PointerCaptureReleased pointerId
                      TransitionLedgerEntry.InputSuppressed input ],
                [ TransitionHostEffect.ReleasePointerCapture pointerId
                  TransitionHostEffect.SuppressInput input ]
            | _ ->
                model |> append [ TransitionLedgerEntry.InputSuppressed input ],
                [ TransitionHostEffect.SuppressInput input ]
        else
            model |> append [ TransitionLedgerEntry.InputApplied input ], []

    let update (msg: TransitionHostMsg<'target, 'response>) (model: TransitionHostModel<'target, 'response>) =
        match msg with
        | TransitionHostMsg.BeginTransition request -> beginTransitionInternal request model
        | TransitionHostMsg.ResponseArrived response -> responseArrived response model
        | TransitionHostMsg.VisibilityChanged visibility -> visibilityChanged visibility model
        | TransitionHostMsg.Presented token -> presented token model
        | TransitionHostMsg.InputAttempted input -> inputAttempted input model

    let beginTransition (request: TransitionRequest<'target>) (model: TransitionHostModel<'target, 'response>) =
        update (TransitionHostMsg.BeginTransition request) model

    let isPending model = pending model

    let authoritative model = model.Current |> Option.map _.Token

    let committed model = model.Committed

    let responses model =
        model.Current |> Option.map _.Responses |> Option.defaultValue []

    let visibility model = model.Visibility

    let focusTarget model = model.FocusTarget

    let controlledValue controlId model =
        Map.tryFind controlId model.ControlledValues

    let controlledFile controlId model =
        Map.tryFind controlId model.ControlledFiles

    let ledger model = model.Ledger
