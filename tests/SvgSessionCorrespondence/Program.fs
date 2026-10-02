module SvgSessionCorrespondence

open System
open FS.GG.Game.Core
open FS.GG.UI.Scene.SvgBrowser

type World = { Ticks: uint64; Revision: uint64 }

let compatibility =
    {
        ContractVersion = 1
        EngineId = "continuous-arena"
        EngineVersion = "runtime-01"
        ProfileId = "browser"
        SchemaId = "world"
        SchemaVersion = 1
    }

let snapshot world =
    {
        SessionId = "correspondence"
        Revision = world.Revision
        Compatibility = compatibility
        Value = world
    }

let contract =
    {
        Initialize = fun _ -> Ok { Ticks = 0UL; Revision = 0UL }
        AdmitInput = fun _ world -> Ok world
        Advance =
            fun advance world ->
                Ok
                    {
                        Ticks = world.Ticks + advance.StepCount
                        Revision = world.Revision + 1UL
                    }
        Project =
            fun world ->
                {
                    SessionId = "correspondence"
                    Revision = world.Revision
                    Value = world.Ticks
                }
        Snapshot = snapshot
        Restore = fun value -> Ok value.Value
    }

let runtimeConfig =
    {
        StepMicroseconds = 16667UL
        MaxCatchUpSteps = 4u
    }

let request =
    {
        SessionId = "correspondence"
        Compatibility = compatibility
        Configuration = ()
    }

let mutable runtime =
    SessionRuntime.initialize runtimeConfig contract request
    |> Result.defaultWith (fun error -> failwithf "%A" error)

let mutable policy = SvgSessionPolicy.initialize { SuspensionMilliseconds = 1000.0 }
let output = ResizeArray<string>()

let runtimeObservation observation =
    let next, effects = SessionRuntime.update contract observation runtime
    runtime <- next

    effects
    |> List.iter (function
        | SessionRuntimeEffect.Advanced count -> output.Add($"advanced:{count}")
        | SessionRuntimeEffect.CatchUpClamped dropped -> output.Add($"clamped:{dropped}")
        | SessionRuntimeEffect.StatusChanged status -> output.Add($"status:{status}")
        | SessionRuntimeEffect.ProjectionReady projection ->
            output.Add($"game-projection:{projection.Revision}:{projection.Value}")
        | SessionRuntimeEffect.Disposed -> output.Add("game-disposed")
        | _ -> ())

let apply observation =
    let next, effects =
        SvgSessionPolicy.update { SuspensionMilliseconds = 1000.0 } observation policy

    policy <- next

    effects
    |> List.iter (function
        | SvgSessionPolicyEffect.AdvanceElapsed elapsed ->
            runtimeObservation (SessionRuntimeObservation.AdvanceElapsed elapsed)
        | SvgSessionPolicyEffect.PauseAuthority -> runtimeObservation SessionRuntimeObservation.Pause
        | SvgSessionPolicyEffect.ResumeAuthority -> runtimeObservation SessionRuntimeObservation.Resume
        | SvgSessionPolicyEffect.StepAuthority -> runtimeObservation SessionRuntimeObservation.StepOnce
        | SvgSessionPolicyEffect.ResetAuthority -> runtimeObservation SessionRuntimeObservation.Reset
        | SvgSessionPolicyEffect.RequestProjection generation -> output.Add($"request:{generation}")
        | SvgSessionPolicyEffect.ApplyProjection revision -> output.Add($"apply:{revision}")
        | SvgSessionPolicyEffect.CancelGeneration generation -> output.Add($"cancel:{generation}")
        | SvgSessionPolicyEffect.RequestRecovery generation -> output.Add($"recover:{generation}")
        | SvgSessionPolicyEffect.GenerationReplaced generation -> output.Add($"replace:{generation}")
        | SvgSessionPolicyEffect.ProjectionDemandCoalesced generation -> output.Add($"coalesce:{generation}")
        | SvgSessionPolicyEffect.ProjectionRejected(expected, actual, revision) ->
            output.Add($"reject-generation:{expected}:{actual}:{revision}")
        | SvgSessionPolicyEffect.ProjectionRevisionRejected(accepted, candidate) ->
            output.Add($"reject-revision:{accepted}:{candidate}")
        | SvgSessionPolicyEffect.Disposed -> runtimeObservation SessionRuntimeObservation.Dispose)

apply (SvgSessionPolicyObservation.Frame 0.0)
apply (SvgSessionPolicyObservation.Frame 16.667)
apply (SvgSessionPolicyObservation.Frame 33.334)
apply (SvgSessionPolicyObservation.CompleteProjection(0UL, 1UL))
apply SvgSessionPolicyObservation.Pause
apply SvgSessionPolicyObservation.StepOnce
apply SvgSessionPolicyObservation.Reset
apply SvgSessionPolicyObservation.Resume
apply (SvgSessionPolicyObservation.Frame 100.0)
apply (SvgSessionPolicyObservation.Frame 1200.0)
apply (SvgSessionPolicyObservation.CompleteProjection(0UL, 99UL))
apply SvgSessionPolicyObservation.Dispose

let externalOutput = ResizeArray<string>()

let externalState name state effects =
    let effect =
        effects
        |> List.map (function
            | SvgExternalSessionEffect.CancelAcquisition _ -> "cancel"
            | SvgExternalSessionEffect.RequestPresentation _ -> "request"
            | SvgExternalSessionEffect.ApplyPresentation _ -> "apply"
            | SvgExternalSessionEffect.PresentationCoalesced _ -> "coalesce"
            | SvgExternalSessionEffect.PresentationRejected _ -> "reject"
            | SvgExternalSessionEffect.AcquisitionFailed _ -> "failed"
            | SvgExternalSessionEffect.EpochBound _ -> "bound"
            | SvgExternalSessionEffect.Disconnected -> "disconnected"
            | SvgExternalSessionEffect.GenerationExhausted -> "exhausted"
            | SvgExternalSessionEffect.Disposed -> "disposed")
        |> String.concat ","

    let optionText formatter =
        function
        | Some value -> formatter value
        | None -> "none"

    let epoch = state.Epoch |> optionText id
    let revision = state.AcceptedRevision |> optionText string
    let boolean value = if value then "true" else "false"

    let outcome =
        state.LastOutcome
        |> optionText (function
            | SvgExternalPresentationOutcome.Applied(epoch, revision) -> $"applied-{epoch}-{revision}"
            | SvgExternalPresentationOutcome.Rejected reason -> $"rejected-{reason}"
            | SvgExternalPresentationOutcome.Failed failure -> $"failed-{failure}")

    externalOutput.Add(
        $"{name}:{state.MountGeneration}:{epoch}:{state.Status}:{revision}:{boolean state.AcquisitionPending}:{boolean state.PresentationQueued}:{outcome}:{effect}"
    )

let externalRun name observations =
    let mutable state = SvgExternalSessionPolicy.initialize ()
    externalState $"{name}-0" state []

    observations
    |> List.iteri (fun index observation ->
        let next, emitted = SvgExternalSessionPolicy.update observation state
        state <- next
        externalState $"{name}-{index + 1}" state emitted)

    state

let bindA = SvgExternalSessionObservation.BindEpoch "1"
let demand = SvgExternalSessionObservation.DemandPresentation

externalRun
    "normal"
    [
        bindA
        demand
        SvgExternalSessionObservation.CompletePresentation(1UL, "1", 1UL)
    ]
|> ignore

externalRun
    "same-epoch"
    [
        bindA
        demand
        SvgExternalSessionObservation.CompletePresentation(1UL, "1", 2UL)
        bindA
    ]
|> ignore

externalRun
    "new-epoch"
    [
        bindA
        demand
        SvgExternalSessionObservation.CompletePresentation(1UL, "1", 2UL)
        SvgExternalSessionObservation.BindEpoch "2"
    ]
|> ignore

externalRun
    "stale-epoch"
    [
        bindA
        demand
        SvgExternalSessionObservation.CompletePresentation(1UL, "2", 1UL)
    ]
|> ignore

let rejection =
    externalRun
        "queued-rejection"
        [
            bindA
            demand
            SvgExternalSessionObservation.CompletePresentation(1UL, "1", 2UL)
            demand
            demand
            SvgExternalSessionObservation.CompletePresentation(1UL, "1", 1UL)
        ]

externalRun
    "lost"
    [
        bindA
        demand
        demand
        SvgExternalSessionObservation.FailAcquisition(1UL, "1", SvgExternalCompletionFailure.Lost)
    ]
|> ignore

externalRun
    "cancelled"
    [
        bindA
        demand
        demand
        SvgExternalSessionObservation.FailAcquisition(1UL, "1", SvgExternalCompletionFailure.Cancelled)
    ]
|> ignore

externalRun
    "callback-failed"
    [
        bindA
        demand
        demand
        SvgExternalSessionObservation.FailAcquisition(1UL, "1", SvgExternalCompletionFailure.CallbackFailed)
    ]
|> ignore

externalRun
    "presentation-callback-failed"
    [
        bindA
        demand
        SvgExternalSessionObservation.CompletePresentation(1UL, "1", 2UL)
        SvgExternalSessionObservation.PresentationCallbackFailed("1", 2UL)
    ]
|> ignore

externalRun "invalidate" [ bindA; demand; SvgExternalSessionObservation.InvalidatePresentation ]
|> ignore

let disposed =
    externalRun "dispose" [ bindA; demand; SvgExternalSessionObservation.Dispose ]

let mutable exhausted = SvgExternalSessionPolicy.initialize ()
externalState "exhaustion-0" exhausted []

for index in 1..3 do
    let next, effects =
        exhausted
        |> SvgExternalSessionPolicy.update (SvgExternalSessionObservation.BindEpoch "1")

    exhausted <- next
    externalState $"exhaustion-{index}" exhausted effects

exhausted <-
    { exhausted with
        MountGeneration = UInt64.MaxValue
    }

let exhaustedState, exhaustedEffects =
    exhausted
    |> SvgExternalSessionPolicy.update (SvgExternalSessionObservation.BindEpoch "1")

externalState "exhaustion-4" exhaustedState exhaustedEffects

// Real production traces kill controls that remove one accepted guard at a time.
let revisionGuardHeld =
    rejection.AcceptedRevision = Some 2UL && rejection.AcquisitionPending

let stale =
    externalRun
        "epoch-guard-source"
        [
            bindA
            demand
            SvgExternalSessionObservation.CompletePresentation(1UL, "2", 1UL)
        ]

let epochGuardHeld = stale.AcceptedRevision.IsNone && stale.AcquisitionPending

let disposeGuardHeld =
    let after, effects =
        disposed
        |> SvgExternalSessionPolicy.update SvgExternalSessionObservation.DemandPresentation

    not after.AcquisitionPending && effects.IsEmpty

if not revisionGuardHeld || not epochGuardHeld || not disposeGuardHeld then
    failwith "an unchanged bad-input trace crossed an external presentation guard"

let externalText = String.concat "|" externalOutput
output.Add($"external={externalText}")
printfn "%s" (String.concat "|" output)
