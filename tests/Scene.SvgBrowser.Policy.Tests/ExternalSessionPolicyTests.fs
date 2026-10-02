module SvgExternalSessionPolicyTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene.SvgBrowser

let private step observation state =
    SvgExternalSessionPolicy.update observation state

let private connected epoch =
    SvgExternalSessionPolicy.initialize ()
    |> step (SvgExternalSessionObservation.BindEpoch epoch)
    |> fst

type private ModelProjection =
    {
        Generation: int
        Epoch: int
        Status: int
        Accepted: int64
        Pending: bool
        Queued: bool
        Outcome: int
        Effect1: int
        Effect2: int
    }

let private modelStatus =
    function
    | SvgExternalSessionStatus.Disconnected -> 0
    | SvgExternalSessionStatus.Connected -> 1
    | SvgExternalSessionStatus.Disposed -> 2

let private modelOutcome =
    function
    | None -> 0
    | Some(SvgExternalPresentationOutcome.Applied _) -> 1
    | Some(SvgExternalPresentationOutcome.Rejected _) -> 2
    | Some(SvgExternalPresentationOutcome.Failed SvgExternalCompletionFailure.Lost) -> 3
    | Some(SvgExternalPresentationOutcome.Failed SvgExternalCompletionFailure.Cancelled) -> 4
    | Some(SvgExternalPresentationOutcome.Failed SvgExternalCompletionFailure.CallbackFailed) -> 5

let private modelEffects effects =
    let codes =
        effects
        |> List.map (function
            | SvgExternalSessionEffect.CancelAcquisition _ -> 1
            | SvgExternalSessionEffect.RequestPresentation _ -> 2
            | SvgExternalSessionEffect.ApplyPresentation _ -> 3
            | SvgExternalSessionEffect.PresentationRejected _ -> 4
            | SvgExternalSessionEffect.AcquisitionFailed _ -> 5
            | SvgExternalSessionEffect.EpochBound _ -> 6
            | SvgExternalSessionEffect.Disconnected -> 7
            | SvgExternalSessionEffect.GenerationExhausted -> 8
            | SvgExternalSessionEffect.Disposed -> 9
            | SvgExternalSessionEffect.PresentationCoalesced _ -> 10)

    match codes with
    | [] -> 0, 0
    | [ first ] -> first, 0
    | [ first; second ] -> first, second
    | _ -> failwithf "unbounded production effect sequence: %A" codes

let private projectState state effects =
    let effect1, effect2 = modelEffects effects

    {
        Generation =
            if state.MountGeneration = UInt64.MaxValue then
                3
            else
                int state.MountGeneration
        Epoch = state.Epoch |> Option.map int |> Option.defaultValue 0
        Status = modelStatus state.Status
        Accepted = state.AcceptedRevision |> Option.map int64 |> Option.defaultValue -1L
        Pending = state.AcquisitionPending
        Queued = state.PresentationQueued
        Outcome = modelOutcome state.LastOutcome
        Effect1 = effect1
        Effect2 = effect2
    }

let private correspondenceScenarios =
    let bind epoch =
        SvgExternalSessionObservation.BindEpoch(string epoch)

    let complete epoch revision =
        SvgExternalSessionObservation.CompletePresentation(1UL, string epoch, uint64 revision)

    let failure value =
        SvgExternalSessionObservation.FailAcquisition(1UL, "1", value)

    [
        "normalFlow", [ bind 1; SvgExternalSessionObservation.DemandPresentation; complete 1 1 ], false
        "sameEpochPreservesBaseline",
        [
            bind 1
            SvgExternalSessionObservation.DemandPresentation
            complete 1 2
            bind 1
        ],
        false
        "newEpochRestartsBaseline",
        [
            bind 1
            SvgExternalSessionObservation.DemandPresentation
            complete 1 2
            bind 2
        ],
        false
        "staleEpochCannotApply", [ bind 1; SvgExternalSessionObservation.DemandPresentation; complete 2 1 ], false
        "queuedAfterRevisionRejection",
        [
            bind 1
            SvgExternalSessionObservation.DemandPresentation
            complete 1 2
            SvgExternalSessionObservation.DemandPresentation
            SvgExternalSessionObservation.DemandPresentation
            complete 1 1
        ],
        false
        "lostThenQueuedReplacement",
        [
            SvgExternalSessionObservation.BindEpoch "1"
            SvgExternalSessionObservation.DemandPresentation
            SvgExternalSessionObservation.DemandPresentation
            failure SvgExternalCompletionFailure.Lost
        ],
        false
        "cancelledThenQueuedReplacement",
        [
            SvgExternalSessionObservation.BindEpoch "1"
            SvgExternalSessionObservation.DemandPresentation
            SvgExternalSessionObservation.DemandPresentation
            failure SvgExternalCompletionFailure.Cancelled
        ],
        false
        "callbackFailureThenQueuedReplacement",
        [
            SvgExternalSessionObservation.BindEpoch "1"
            SvgExternalSessionObservation.DemandPresentation
            SvgExternalSessionObservation.DemandPresentation
            failure SvgExternalCompletionFailure.CallbackFailed
        ],
        false
        "presentationCallbackFailureIsExplicit",
        [
            bind 1
            SvgExternalSessionObservation.DemandPresentation
            complete 1 2
            SvgExternalSessionObservation.PresentationCallbackFailed("1", 2UL)
        ],
        false
        "invalidateCancels",
        [
            bind 1
            SvgExternalSessionObservation.DemandPresentation
            SvgExternalSessionObservation.InvalidatePresentation
        ],
        false
        "disposeCancelsAndTerminates",
        [
            bind 1
            SvgExternalSessionObservation.DemandPresentation
            SvgExternalSessionObservation.Dispose
        ],
        false
        "generationExhaustionRefusesWrap", [ bind 1; bind 1; bind 1; bind 1 ], true
    ]

let private loadModelTraces () =
    let path =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "SvgSessionCorrespondence", "external-session-production-traces.tsv")

    File.ReadAllLines path
    |> Array.skip 1
    |> Array.map (fun line ->
        let fields = line.Split '\t'

        fields[0],
        int fields[1],
        {
            Generation = int fields[2]
            Epoch = int fields[3]
            Status = int fields[4]
            Accepted = int64 fields[5]
            Pending = bool.Parse fields[6]
            Queued = bool.Parse fields[7]
            Outcome = int fields[8]
            Effect1 = int fields[9]
            Effect2 = int fields[10]
        })
    |> Array.groupBy (fun (name, _, _) -> name)
    |> Map.ofArray

[<Tests>]
let tests =
    testList
        "SVG external-authority presentation policy"
        [
            test "all extracted Quint states and ordered effects match the production reducer" {
                let expected = loadModelTraces ()

                for name, observations, saturateBeforeLast in correspondenceScenarios do
                    let observed = ResizeArray<ModelProjection>()
                    let mutable state = SvgExternalSessionPolicy.initialize ()
                    observed.Add(projectState state [])

                    observations
                    |> List.iteri (fun index observation ->
                        if saturateBeforeLast && index = observations.Length - 1 then
                            state <-
                                { state with
                                    MountGeneration = UInt64.MaxValue
                                }

                        let next, effects = SvgExternalSessionPolicy.update observation state
                        state <- next
                        observed.Add(projectState state effects))

                    let model =
                        expected[name]
                        |> Array.sortBy (fun (_, index, _) -> index)
                        |> Array.map (fun (_, _, projection) -> projection)

                    Expect.sequenceEqual observed model $"every state/effect projection matches canonical trace {name}"
            }

            test "epoch and mount generation are independent and revision is monotonic" {
                let bound = connected "authority-a"
                Expect.equal bound.MountGeneration 1UL "binding retires the unbound mount"
                Expect.equal bound.Epoch (Some "authority-a") "the authority epoch is opaque"

                let pending, request =
                    bound |> step SvgExternalSessionObservation.DemandPresentation

                Expect.equal
                    request
                    [ SvgExternalSessionEffect.RequestPresentation(1UL, "authority-a") ]
                    "one acquisition starts"

                let applied, effects =
                    pending
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "authority-a", 8UL))

                Expect.equal
                    effects
                    [ SvgExternalSessionEffect.ApplyPresentation("authority-a", 8UL) ]
                    "current projection applies"

                Expect.equal applied.AcceptedRevision (Some 8UL) "revision is retained"
                Expect.isFalse applied.AcquisitionPending "the slot is released"
            }

            test "same epoch reconnect preserves baseline and new epoch resets it" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                let applied, _ =
                    pending
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "a", 41UL))

                let same, sameEffects =
                    applied |> step (SvgExternalSessionObservation.BindEpoch "a")

                Expect.equal same.AcceptedRevision (Some 41UL) "same authority keeps its baseline"
                Expect.equal sameEffects [ SvgExternalSessionEffect.EpochBound(2UL, "a", true) ] "reconnect is explicit"

                let changed, changedEffects =
                    same |> step (SvgExternalSessionObservation.BindEpoch "b")

                Expect.equal changed.AcceptedRevision None "a distinct authority starts its own baseline"
                Expect.equal changed.MountGeneration 3UL "local mount changes independently"

                Expect.equal
                    changedEffects
                    [ SvgExternalSessionEffect.EpochBound(3UL, "b", false) ]
                    "replacement is explicit"
            }

            test "burst coalescing owns one acquisition and one replaceable presentation" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                let queued, first = pending |> step SvgExternalSessionObservation.DemandPresentation

                let bounded, second =
                    queued |> step SvgExternalSessionObservation.DemandPresentation

                Expect.equal
                    first
                    [ SvgExternalSessionEffect.PresentationCoalesced(1UL, "a") ]
                    "first burst queues one replacement"

                Expect.equal second first "later demand replaces the same queue slot"

                Expect.equal
                    bounded
                    { pending with
                        PresentationQueued = true
                    }
                    "state differs from pending only by queue ownership"

                Expect.isTrue bounded.AcquisitionPending "only one acquisition is active"
                Expect.isTrue bounded.PresentationQueued "only one replacement is retained"
            }

            test "rejected current completion releases the slot and requests its queued replacement" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                let first, _ =
                    pending
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "a", 8UL))

                let pendingAgain, _ = first |> step SvgExternalSessionObservation.DemandPresentation

                let queued, _ =
                    pendingAgain |> step SvgExternalSessionObservation.DemandPresentation

                let rejected, effects =
                    queued
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "a", 8UL))

                Expect.equal
                    effects
                    [
                        SvgExternalSessionEffect.PresentationRejected "non-increasing-revision"
                        SvgExternalSessionEffect.RequestPresentation(1UL, "a")
                    ]
                    "rejection cannot strand the queued valid demand"

                Expect.isTrue rejected.AcquisitionPending "the replacement now owns the slot"
                Expect.isFalse rejected.PresentationQueued "the queue was drained"
            }

            test "stale mount epoch duplicate and unsolicited replies never consume current ownership" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                for observation, reason in
                    [
                        SvgExternalSessionObservation.CompletePresentation(0UL, "a", 1UL), "stale-generation"
                        SvgExternalSessionObservation.CompletePresentation(1UL, "old", 1UL), "stale-epoch"
                    ] do
                    let unchanged, effects = pending |> step observation
                    Expect.isTrue unchanged.AcquisitionPending "a stale reply does not release the current request"
                    Expect.equal effects [ SvgExternalSessionEffect.PresentationRejected reason ] "reason is closed"

                let accepted, _ =
                    pending
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "a", 3UL))

                let duplicate, duplicateEffects =
                    accepted
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "a", 3UL))

                Expect.equal duplicate.AcceptedRevision (Some 3UL) "duplicate cannot alter presentation"

                Expect.equal
                    duplicateEffects
                    [ SvgExternalSessionEffect.PresentationRejected "unsolicited-completion" ]
                    "no slot means unsolicited"
            }

            test "lost and cancelled acquisitions are explicit and drain queued demand" {
                for failure in
                    [
                        SvgExternalCompletionFailure.Lost
                        SvgExternalCompletionFailure.Cancelled
                        SvgExternalCompletionFailure.CallbackFailed
                    ] do
                    let pending, _ =
                        connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                    let queued, _ = pending |> step SvgExternalSessionObservation.DemandPresentation

                    let next, effects =
                        queued
                        |> step (SvgExternalSessionObservation.FailAcquisition(1UL, "a", failure))

                    Expect.equal
                        effects
                        [
                            SvgExternalSessionEffect.AcquisitionFailed failure
                            SvgExternalSessionEffect.RequestPresentation(1UL, "a")
                        ]
                        "failure truth precedes replacement"

                    Expect.equal
                        next.LastOutcome
                        (Some(SvgExternalPresentationOutcome.Failed failure))
                        "failure is observable"
            }

            test "presentation callback failure is a production reducer transition" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                let accepted, _ =
                    pending
                    |> step (SvgExternalSessionObservation.CompletePresentation(1UL, "a", 8UL))

                let failed, effects =
                    accepted
                    |> step (SvgExternalSessionObservation.PresentationCallbackFailed("a", 8UL))

                Expect.equal
                    effects
                    [
                        SvgExternalSessionEffect.AcquisitionFailed SvgExternalCompletionFailure.CallbackFailed
                    ]
                    "callback failure is explicit"

                Expect.equal
                    failed.LastOutcome
                    (Some(SvgExternalPresentationOutcome.Failed SvgExternalCompletionFailure.CallbackFailed))
                    "the reducer retains failure truth"

                Expect.equal failed.AcceptedRevision (Some 8UL) "a callback cannot rewrite authority revision"
                Expect.isFalse failed.AcquisitionPending "a settled callback cannot acquire a second slot"
            }

            test "invalidation cancels before replacement and preserves authority baseline" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                let queued, _ = pending |> step SvgExternalSessionObservation.DemandPresentation

                let invalidated, effects =
                    queued |> step SvgExternalSessionObservation.InvalidatePresentation

                Expect.equal effects [ SvgExternalSessionEffect.CancelAcquisition 1UL ] "owned work is cancelled first"
                Expect.equal invalidated.MountGeneration 2UL "presentation identity advances"
                Expect.equal invalidated.Epoch (Some "a") "authority identity does not change"
                Expect.isFalse invalidated.AcquisitionPending "no acquisition survives"
                Expect.isFalse invalidated.PresentationQueued "no replacement survives"
            }

            test "disconnect replacement and disposal retain ordered bounded truth" {
                let pending, _ =
                    connected "a" |> step SvgExternalSessionObservation.DemandPresentation

                let disconnected, effects = pending |> step SvgExternalSessionObservation.Disconnect

                Expect.equal
                    effects
                    [
                        SvgExternalSessionEffect.CancelAcquisition 1UL
                        SvgExternalSessionEffect.Disconnected
                    ]
                    "disconnect cancels before notification"

                let replaced, bind =
                    disconnected |> step (SvgExternalSessionObservation.BindEpoch "b")

                Expect.equal
                    bind
                    [ SvgExternalSessionEffect.EpochBound(3UL, "b", false) ]
                    "replacement has a new local mount"

                let active, _ = replaced |> step SvgExternalSessionObservation.DemandPresentation
                let disposed, disposal = active |> step SvgExternalSessionObservation.Dispose

                Expect.equal
                    disposal
                    [
                        SvgExternalSessionEffect.CancelAcquisition 3UL
                        SvgExternalSessionEffect.Disposed
                    ]
                    "dispose cancels before terminal notification"

                Expect.equal disposed.Status SvgExternalSessionStatus.Disposed "disposed is terminal"

                Expect.equal
                    (disposed |> step SvgExternalSessionObservation.DemandPresentation)
                    (disposed, [])
                    "late demand is inert"

                Expect.equal
                    (disposed |> step SvgExternalSessionObservation.Dispose)
                    (disposed, [])
                    "dispose is idempotent"
            }

            test "generation exhaustion refuses replacement without wrap" {
                let exhausted =
                    { connected "a" with
                        MountGeneration = UInt64.MaxValue
                        AcquisitionPending = true
                        PresentationQueued = true
                    }

                let unchanged, effects =
                    exhausted |> step (SvgExternalSessionObservation.BindEpoch "b")

                Expect.equal unchanged exhausted "exhaustion never aliases old work"
                Expect.equal effects [ SvgExternalSessionEffect.GenerationExhausted ] "refusal is explicit"
            }
        ]
