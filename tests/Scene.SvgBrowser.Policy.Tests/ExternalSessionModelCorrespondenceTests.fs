module SvgExternalSessionModelCorrespondenceTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene.SvgBrowser

type private Action =
    | Bind of int
    | Demand
    | Complete of int * int * int * int
    | Fail of int * int * int * SvgExternalCompletionFailure
    | CallbackFailure of int * int * int
    | Invalidate
    | Dispose
    | GenerationExhaust
    | AcquisitionExhaust

let private epochText =
    function
    | 1 -> "1"
    | 2 -> "2"
    | value -> string value

let private epochValue =
    function
    | Some "1" -> 1L
    | Some "2" -> 2L
    | _ -> 0L

let private bounded value =
    if value = UInt64.MaxValue then 3L else int64 value

let private boundedAcquisition value =
    if value = UInt64.MaxValue then 4L else int64 value

let private optional map =
    function
    | Some value -> map value
    | None -> -1L

let private failureValue =
    function
    | SvgExternalCompletionFailure.Cancelled -> 2L
    | SvgExternalCompletionFailure.Lost -> 3L
    | SvgExternalCompletionFailure.CallbackFailed -> 5L

let private rejectionValue (reason: string) =
    if reason.StartsWith("stale-generation:", StringComparison.Ordinal) then
        1L
    elif reason.StartsWith("stale-epoch:", StringComparison.Ordinal) then
        2L
    elif reason.StartsWith("stale-acquisition:", StringComparison.Ordinal) then
        3L
    elif reason.StartsWith("non-increasing-revision:", StringComparison.Ordinal) then
        4L
    elif reason.StartsWith("unsolicited-", StringComparison.Ordinal) then
        5L
    else
        6L

let private emptyEffect = [ 0L; -1L; -1L; 0L; -1L; -1L ]

let private effectProjection =
    function
    | SvgExternalSessionEffect.CancelAcquisition(generation, acquisition, epoch) ->
        [
            1L
            bounded generation
            boundedAcquisition acquisition
            epochValue epoch
            -1L
            -1L
        ]
    | SvgExternalSessionEffect.RequestPresentation(generation, acquisition, epoch) ->
        [
            2L
            bounded generation
            boundedAcquisition acquisition
            epochValue (Some epoch)
            -1L
            -1L
        ]
    | SvgExternalSessionEffect.ApplyPresentation(generation, acquisition, epoch, revision) ->
        [
            3L
            bounded generation
            boundedAcquisition acquisition
            epochValue (Some epoch)
            int64 revision
            -1L
        ]
    | SvgExternalSessionEffect.PresentationRejected(reason, generation, acquisition, epoch, revision) ->
        [
            4L
            optional bounded generation
            optional boundedAcquisition acquisition
            epochValue epoch
            optional int64 revision
            rejectionValue reason
        ]
    | SvgExternalSessionEffect.AcquisitionFailed(failure, generation, acquisition, epoch, revision) ->
        [
            5L
            bounded generation
            boundedAcquisition acquisition
            epochValue (Some epoch)
            optional int64 revision
            failureValue failure
        ]
    | SvgExternalSessionEffect.EpochBound(generation, epoch, preserved) ->
        [
            6L
            bounded generation
            -1L
            epochValue (Some epoch)
            -1L
            if preserved then 1L else 0L
        ]
    | SvgExternalSessionEffect.Disconnected generation -> [ 7L; bounded generation; -1L; 0L; -1L; -1L ]
    | SvgExternalSessionEffect.GenerationExhausted generation -> [ 8L; bounded generation; -1L; 0L; -1L; -1L ]
    | SvgExternalSessionEffect.Disposed generation -> [ 9L; bounded generation; -1L; 0L; -1L; -1L ]
    | SvgExternalSessionEffect.PresentationCoalesced(generation, acquisition, epoch) ->
        [
            10L
            bounded generation
            boundedAcquisition acquisition
            epochValue (Some epoch)
            -1L
            -1L
        ]
    | SvgExternalSessionEffect.AcquisitionIdExhausted acquisition ->
        [ 11L; -1L; boundedAcquisition acquisition; 0L; -1L; -1L ]

let private project state effects =
    let outcomeKind, outcomeAcq, outcomeEpoch, outcomeRevision, rejection =
        match state.LastOutcome with
        | None -> 0L, -1L, 0L, -1L, 0L
        | Some(SvgExternalPresentationOutcome.Applied(acquisition, epoch, revision)) ->
            1L, boundedAcquisition acquisition, epochValue (Some epoch), int64 revision, 0L
        | Some(SvgExternalPresentationOutcome.Rejected reason) -> 2L, -1L, 0L, -1L, rejectionValue reason
        | Some(SvgExternalPresentationOutcome.Failed(failure, acquisition)) ->
            failureValue failure, optional boundedAcquisition acquisition, 0L, -1L, 0L

    let slots = effects |> List.map effectProjection
    let e1 = slots |> List.tryItem 0 |> Option.defaultValue emptyEffect
    let e2 = slots |> List.tryItem 1 |> Option.defaultValue emptyEffect

    [
        bounded state.MountGeneration
        epochValue state.Epoch
        match state.Status with
        | SvgExternalSessionStatus.Disconnected -> 0L
        | SvgExternalSessionStatus.Connected -> 1L
        | SvgExternalSessionStatus.Disposed -> 2L
        optional int64 state.AcceptedRevision
        boundedAcquisition state.NextAcquisitionId
        optional boundedAcquisition state.PendingAcquisitionId
        if state.PresentationQueued then 1L else 0L
        outcomeKind
        outcomeAcq
        outcomeEpoch
        outcomeRevision
        rejection
    ]
    @ e1
    @ e2

let private advance action state =
    match action with
    | Bind epoch -> SvgExternalSessionPolicy.update (SvgExternalSessionObservation.BindEpoch(epochText epoch)) state
    | Demand -> SvgExternalSessionPolicy.update SvgExternalSessionObservation.DemandPresentation state
    | Complete(generation, acquisition, epoch, revision) ->
        SvgExternalSessionPolicy.update
            (SvgExternalSessionObservation.CompletePresentation(
                uint64 generation,
                uint64 acquisition,
                epochText epoch,
                uint64 revision
            ))
            state
    | Fail(generation, acquisition, epoch, failure) ->
        SvgExternalSessionPolicy.update
            (SvgExternalSessionObservation.FailAcquisition(
                uint64 generation,
                uint64 acquisition,
                epochText epoch,
                failure
            ))
            state
    | CallbackFailure(acquisition, epoch, revision) ->
        SvgExternalSessionPolicy.update
            (SvgExternalSessionObservation.PresentationCallbackFailed(
                uint64 acquisition,
                epochText epoch,
                uint64 revision
            ))
            state
    | Invalidate -> SvgExternalSessionPolicy.update SvgExternalSessionObservation.InvalidatePresentation state
    | Dispose -> SvgExternalSessionPolicy.update SvgExternalSessionObservation.Dispose state
    | GenerationExhaust ->
        SvgExternalSessionPolicy.update
            (SvgExternalSessionObservation.BindEpoch "1")
            { state with
                MountGeneration = UInt64.MaxValue
            }
    | AcquisitionExhaust ->
        SvgExternalSessionPolicy.update
            SvgExternalSessionObservation.DemandPresentation
            { state with
                NextAcquisitionId = UInt64.MaxValue
            }

let private scenarios =
    [
        "normalFlow", [ Bind 1; Demand; Complete(1, 0, 1, 1) ]
        "duplicateCannotDrainNext",
        [
            Bind 1
            Demand
            Complete(1, 0, 1, 1)
            Demand
            Complete(1, 0, 1, 1)
            Complete(1, 1, 1, 2)
        ]
        "currentRejectionDrainsQueued", [ Bind 1; Demand; Complete(1, 0, 1, 2); Demand; Demand; Complete(1, 1, 1, 2) ]
        "oldFailureCannotDrainReplacement",
        [
            Bind 1
            Demand
            Demand
            Fail(1, 0, 1, SvgExternalCompletionFailure.Lost)
            Fail(1, 0, 1, SvgExternalCompletionFailure.Lost)
        ]
        "lateCompletionAfterLostCannotApply",
        [
            Bind 1
            Demand
            Demand
            Fail(1, 0, 1, SvgExternalCompletionFailure.Lost)
            Complete(1, 0, 1, 2)
        ]
        "sameEpochPreservesBaseline", [ Bind 1; Demand; Complete(1, 0, 1, 2); Bind 1 ]
        "newEpochRestartsBaseline", [ Bind 1; Demand; Complete(1, 0, 1, 2); Bind 2 ]
        "staleGenerationCannotApply", [ Bind 1; Demand; Complete(0, 0, 1, 1) ]
        "staleEpochCannotApply", [ Bind 1; Demand; Complete(1, 0, 2, 1) ]
        "lostThenQueuedReplacement", [ Bind 1; Demand; Demand; Fail(1, 0, 1, SvgExternalCompletionFailure.Lost) ]
        "callbackFailureIsExplicit", [ Bind 1; Demand; Complete(1, 0, 1, 2); CallbackFailure(0, 1, 2) ]
        "invalidateCancelsExactRequest", [ Bind 1; Demand; Invalidate ]
        "disposeCancelsAndTerminates", [ Bind 1; Demand; Dispose ]
        "generationExhaustionRefusesWrap", [ Bind 1; Bind 1; Bind 1; GenerationExhaust ]
        "acquisitionExhaustionRefusesWrap",
        [
            Bind 1
            Demand
            Complete(1, 0, 1, 1)
            Demand
            Complete(1, 1, 1, 2)
            Demand
            Complete(1, 2, 1, 3)
            Demand
            Complete(1, 3, 1, 4)
            AcquisitionExhaust
        ]
    ]

let private productionRows () =
    scenarios
    |> List.sortBy fst
    |> List.collect (fun (name, actions) ->
        let mutable state = SvgExternalSessionPolicy.initialize ()
        let rows = ResizeArray<string>()
        let initial = project state [] |> List.map string |> String.concat "\t"
        rows.Add($"{name}\t0\t{initial}")

        actions
        |> List.iteri (fun index action ->
            let next, effects = advance action state
            state <- next
            let projected = project state effects |> List.map string |> String.concat "\t"
            rows.Add($"{name}\t{index + 1}\t{projected}"))

        rows |> Seq.toList)

[<Tests>]
let correspondence =
    test "canonical Quint traces match every production state and ordered effect argument" {
        let path =
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "..",
                "SvgSessionCorrespondence",
                "external-session-production-traces.tsv"
            )

        let expected = File.ReadAllLines(path) |> Array.skip 1 |> Array.toList
        Expect.sequenceEqual (productionRows ()) expected "the complete production projection matches all directed ITFs"
    }
