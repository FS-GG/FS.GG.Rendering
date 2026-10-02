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

let externalOption formatter =
    function
    | Some value -> formatter value
    | None -> "-"

let externalEpoch = externalOption id
let externalRevision = externalOption string
let externalAcquisition = externalOption string
let externalBoolean value = if value then "true" else "false"

let externalOutcome =
    externalOption (function
        | SvgExternalPresentationOutcome.Applied(acquisition, epoch, revision) ->
            $"applied:{acquisition}:{epoch}:{revision}"
        | SvgExternalPresentationOutcome.Rejected reason -> $"rejected:{reason}"
        | SvgExternalPresentationOutcome.Failed(failure, acquisition) ->
            $"failed:{failure}:{externalAcquisition acquisition}")

let externalEffect =
    function
    | SvgExternalSessionEffect.CancelAcquisition(generation, acquisition, epoch) ->
        $"cancel:{generation}:{acquisition}:{externalEpoch epoch}"
    | SvgExternalSessionEffect.RequestPresentation(generation, acquisition, epoch) ->
        $"request:{generation}:{acquisition}:{epoch}"
    | SvgExternalSessionEffect.ApplyPresentation(generation, acquisition, epoch, revision) ->
        $"apply:{generation}:{acquisition}:{epoch}:{revision}"
    | SvgExternalSessionEffect.PresentationCoalesced(generation, acquisition, epoch) ->
        $"coalesce:{generation}:{acquisition}:{epoch}"
    | SvgExternalSessionEffect.PresentationRejected(reason, generation, acquisition, epoch, revision) ->
        $"reject:{reason}:{externalAcquisition generation}:{externalAcquisition acquisition}:{externalEpoch epoch}:{externalRevision revision}"
    | SvgExternalSessionEffect.AcquisitionFailed(failure, generation, acquisition, epoch, revision) ->
        $"failed:{failure}:{generation}:{acquisition}:{epoch}:{externalRevision revision}"
    | SvgExternalSessionEffect.EpochBound(generation, epoch, preserved) ->
        $"bound:{generation}:{epoch}:{externalBoolean preserved}"
    | SvgExternalSessionEffect.Disconnected generation -> $"disconnected:{generation}"
    | SvgExternalSessionEffect.GenerationExhausted generation -> $"generation-exhausted:{generation}"
    | SvgExternalSessionEffect.AcquisitionIdExhausted acquisition -> $"acquisition-exhausted:{acquisition}"
    | SvgExternalSessionEffect.Disposed generation -> $"disposed:{generation}"

let externalState name index state effects =
    let effectsText = effects |> List.map externalEffect |> String.concat ","

    externalOutput.Add(
        $"{name}:{index}:{state.MountGeneration}:{externalEpoch state.Epoch}:{state.Status}:{externalRevision state.AcceptedRevision}:{state.NextAcquisitionId}:{externalAcquisition state.PendingAcquisitionId}:{externalBoolean state.AcquisitionPending}:{externalBoolean state.PresentationQueued}:{externalOutcome state.LastOutcome}:{effectsText}"
    )

let externalRun name observations =
    let mutable state = SvgExternalSessionPolicy.initialize ()
    externalState name 0 state []

    observations
    |> List.iteri (fun index observation ->
        let next, effects = SvgExternalSessionPolicy.update observation state
        state <- next
        externalState name (index + 1) state effects)

    state

let bind epoch =
    SvgExternalSessionObservation.BindEpoch epoch

let demand = SvgExternalSessionObservation.DemandPresentation

let complete generation acquisition epoch revision =
    SvgExternalSessionObservation.CompletePresentation(generation, acquisition, epoch, revision)

let fail generation acquisition epoch =
    SvgExternalSessionObservation.FailAcquisition(generation, acquisition, epoch, SvgExternalCompletionFailure.Lost)

externalRun "normal" [ bind "1"; demand; complete 1UL 0UL "1" 1UL ] |> ignore

externalRun
    "duplicate"
    [
        bind "1"
        demand
        complete 1UL 0UL "1" 1UL
        demand
        complete 1UL 0UL "1" 1UL
        complete 1UL 1UL "1" 2UL
    ]
|> ignore

externalRun
    "current-rejection"
    [
        bind "1"
        demand
        complete 1UL 0UL "1" 2UL
        demand
        demand
        complete 1UL 1UL "1" 2UL
    ]
|> ignore

externalRun "old-failure" [ bind "1"; demand; demand; fail 1UL 0UL "1"; fail 1UL 0UL "1" ]
|> ignore

externalRun "late-completion" [ bind "1"; demand; demand; fail 1UL 0UL "1"; complete 1UL 0UL "1" 2UL ]
|> ignore

externalRun "same-epoch" [ bind "1"; demand; complete 1UL 0UL "1" 2UL; bind "1" ]
|> ignore

externalRun "new-epoch" [ bind "1"; demand; complete 1UL 0UL "1" 2UL; bind "2" ]
|> ignore

externalRun "stale-generation" [ bind "1"; demand; complete 0UL 0UL "1" 1UL ]
|> ignore

externalRun "stale-epoch" [ bind "1"; demand; complete 1UL 0UL "2" 1UL ]
|> ignore

externalRun "lost" [ bind "1"; demand; demand; fail 1UL 0UL "1" ] |> ignore

externalRun
    "callback"
    [
        bind "1"
        demand
        complete 1UL 0UL "1" 2UL
        SvgExternalSessionObservation.PresentationCallbackFailed(0UL, "1", 2UL)
    ]
|> ignore

externalRun "invalidate" [ bind "1"; demand; SvgExternalSessionObservation.InvalidatePresentation ]
|> ignore

externalRun "dispose" [ bind "1"; demand; SvgExternalSessionObservation.Dispose ]
|> ignore

let externalText = String.concat "|" externalOutput
output.Add($"external={externalText}")
printfn "%s" (String.concat "|" output)
