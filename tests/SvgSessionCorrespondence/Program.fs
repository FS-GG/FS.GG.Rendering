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

printfn "%s" (String.concat "|" output)
