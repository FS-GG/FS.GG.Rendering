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

let private demand state =
    state |> step SvgExternalSessionObservation.DemandPresentation

let private complete generation acquisition epoch revision state =
    state
    |> step (SvgExternalSessionObservation.CompletePresentation(generation, acquisition, epoch, revision))

[<Tests>]
let tests =
    testList
        "SVG external-authority presentation policy"
        [
            test "first acquisition carries independent generation epoch and acquisition identity" {
                let pending, effects = connected "a" |> demand
                Expect.equal pending.MountGeneration 1UL "mount generation is local"
                Expect.equal pending.PendingAcquisitionId (Some 0UL) "the request has its own identity"
                Expect.equal pending.NextAcquisitionId 1UL "the next identity advances before the callback"

                Expect.equal
                    effects
                    [ SvgExternalSessionEffect.RequestPresentation(1UL, 0UL, "a") ]
                    "all ownership is emitted"

                let applied, applyEffects = pending |> complete 1UL 0UL "a" 8UL

                Expect.equal
                    applyEffects
                    [ SvgExternalSessionEffect.ApplyPresentation(1UL, 0UL, "a", 8UL) ]
                    "apply retains request identity"

                Expect.equal applied.AcceptedRevision (Some 8UL) "revision is accepted"
                Expect.equal applied.PendingAcquisitionId None "the exact slot is released"

                Expect.equal
                    applied.LastOutcome
                    (Some(SvgExternalPresentationOutcome.Applied(0UL, "a", 8UL)))
                    "outcome retains payload"
            }

            test "old duplicate cannot drain the next acquisition in the same mount and epoch" {
                let request1, _ = connected "a" |> demand
                let settled1, _ = request1 |> complete 1UL 0UL "a" 1UL
                let request2, request2Effects = settled1 |> demand

                Expect.equal
                    request2Effects
                    [ SvgExternalSessionEffect.RequestPresentation(1UL, 1UL, "a") ]
                    "request two is distinct"

                let afterDuplicate, duplicateEffects = request2 |> complete 1UL 0UL "a" 1UL
                Expect.equal afterDuplicate.PendingAcquisitionId (Some 1UL) "old request cannot consume request two"

                Expect.equal
                    duplicateEffects
                    [
                        SvgExternalSessionEffect.PresentationRejected(
                            "stale-acquisition:some:1:0",
                            Some 1UL,
                            Some 0UL,
                            Some "a",
                            Some 1UL
                        )
                    ]
                    "old identity is explicit"

                let settled2, apply2 = afterDuplicate |> complete 1UL 1UL "a" 2UL

                Expect.equal
                    apply2
                    [ SvgExternalSessionEffect.ApplyPresentation(1UL, 1UL, "a", 2UL) ]
                    "genuine request two still settles"

                Expect.equal settled2.AcceptedRevision (Some 2UL) "newer presentation applies"
            }

            test "rejected current completion releases its own slot and drains one queued demand" {
                let request1, _ = connected "a" |> demand
                let first, _ = request1 |> complete 1UL 0UL "a" 8UL
                let request2, _ = first |> demand
                let queued, _ = request2 |> demand
                let next, effects = queued |> complete 1UL 1UL "a" 8UL

                Expect.equal
                    effects
                    [
                        SvgExternalSessionEffect.PresentationRejected(
                            "non-increasing-revision:8:8",
                            Some 1UL,
                            Some 1UL,
                            Some "a",
                            Some 8UL
                        )
                        SvgExternalSessionEffect.RequestPresentation(1UL, 2UL, "a")
                    ]
                    "reject precedes replacement"

                Expect.equal next.PendingAcquisitionId (Some 2UL) "replacement has a fresh identity"
                Expect.isFalse next.PresentationQueued "one queue slot was drained"
            }

            test "old failure after lost acquisition cannot retire its replacement" {
                let request1, _ = connected "a" |> demand
                let queued, _ = request1 |> demand

                let request2, failed =
                    queued
                    |> step (
                        SvgExternalSessionObservation.FailAcquisition(1UL, 0UL, "a", SvgExternalCompletionFailure.Lost)
                    )

                Expect.equal
                    failed
                    [
                        SvgExternalSessionEffect.AcquisitionFailed(
                            SvgExternalCompletionFailure.Lost,
                            1UL,
                            0UL,
                            "a",
                            None
                        )
                        SvgExternalSessionEffect.RequestPresentation(1UL, 1UL, "a")
                    ]
                    "lost truth precedes replacement"

                let unchanged, stale =
                    request2
                    |> step (
                        SvgExternalSessionObservation.FailAcquisition(1UL, 0UL, "a", SvgExternalCompletionFailure.Lost)
                    )

                Expect.equal unchanged.PendingAcquisitionId (Some 1UL) "late failure cannot consume replacement"

                Expect.equal
                    stale
                    [
                        SvgExternalSessionEffect.PresentationRejected(
                            "stale-acquisition:some:1:0",
                            Some 1UL,
                            Some 0UL,
                            Some "a",
                            None
                        )
                    ]
                    "late failure is rejected"

                let afterLateCompletion, late = unchanged |> complete 1UL 0UL "a" 99UL

                Expect.equal
                    afterLateCompletion.PendingAcquisitionId
                    (Some 1UL)
                    "late increasing completion cannot consume replacement"

                Expect.equal afterLateCompletion.AcceptedRevision None "late projection cannot apply"

                Expect.equal
                    late
                    [
                        SvgExternalSessionEffect.PresentationRejected(
                            "stale-acquisition:some:1:0",
                            Some 1UL,
                            Some 0UL,
                            Some "a",
                            Some 99UL
                        )
                    ]
                    "late completion is rejected by identity"
            }

            test "same epoch reconnect preserves baseline and new epoch resets it" {
                let request, _ = connected "a" |> demand
                let applied, _ = request |> complete 1UL 0UL "a" 41UL

                let same, sameEffects =
                    applied |> step (SvgExternalSessionObservation.BindEpoch "a")

                Expect.equal same.AcceptedRevision (Some 41UL) "same authority keeps baseline"

                Expect.equal
                    sameEffects
                    [ SvgExternalSessionEffect.EpochBound(2UL, "a", true) ]
                    "same epoch is explicit"

                let changed, changedEffects =
                    same |> step (SvgExternalSessionObservation.BindEpoch "b")

                Expect.equal changed.AcceptedRevision None "new authority starts its baseline"
                Expect.equal changed.NextAcquisitionId 1UL "request identity remains monotonic across mounts"

                Expect.equal
                    changedEffects
                    [ SvgExternalSessionEffect.EpochBound(3UL, "b", false) ]
                    "new epoch is explicit"
            }

            test "stale generation and epoch preserve current acquisition" {
                let pending, _ = connected "a" |> demand
                let staleGeneration, generationEffects = pending |> complete 0UL 0UL "a" 1UL
                Expect.equal staleGeneration.PendingAcquisitionId (Some 0UL) "generation mismatch is inert"

                Expect.equal
                    generationEffects
                    [
                        SvgExternalSessionEffect.PresentationRejected(
                            "stale-generation:1:0",
                            Some 0UL,
                            Some 0UL,
                            Some "a",
                            Some 1UL
                        )
                    ]
                    "generation values are retained"

                let staleEpoch, epochEffects = pending |> complete 1UL 0UL "old" 1UL
                Expect.equal staleEpoch.PendingAcquisitionId (Some 0UL) "epoch mismatch is inert"

                Expect.equal
                    epochEffects
                    [
                        SvgExternalSessionEffect.PresentationRejected(
                            "stale-epoch:some:a:old",
                            Some 1UL,
                            Some 0UL,
                            Some "old",
                            Some 1UL
                        )
                    ]
                    "epoch values are retained"
            }

            test "burst owns one acquisition and one replaceable queued presentation" {
                let pending, _ = connected "a" |> demand
                let queued, first = pending |> demand
                let bounded, second = queued |> demand
                let expected = [ SvgExternalSessionEffect.PresentationCoalesced(1UL, 0UL, "a") ]
                Expect.equal first expected "first burst queues replacement"
                Expect.equal second expected "later burst replaces same queue slot"
                Expect.equal bounded.PendingAcquisitionId (Some 0UL) "one acquisition remains"
                Expect.isTrue bounded.PresentationQueued "one replacement remains"
            }

            test "lost cancelled and callback-failed acquisitions drain with fresh identity" {
                for failure in
                    [
                        SvgExternalCompletionFailure.Lost
                        SvgExternalCompletionFailure.Cancelled
                        SvgExternalCompletionFailure.CallbackFailed
                    ] do
                    let pending, _ = connected "a" |> demand
                    let queued, _ = pending |> demand

                    let next, effects =
                        queued
                        |> step (SvgExternalSessionObservation.FailAcquisition(1UL, 0UL, "a", failure))

                    Expect.equal
                        effects
                        [
                            SvgExternalSessionEffect.AcquisitionFailed(failure, 1UL, 0UL, "a", None)
                            SvgExternalSessionEffect.RequestPresentation(1UL, 1UL, "a")
                        ]
                        "failure precedes fresh request"

                    Expect.equal next.PendingAcquisitionId (Some 1UL) "replacement identity advances"

                    Expect.equal
                        next.LastOutcome
                        (Some(SvgExternalPresentationOutcome.Failed(failure, Some 0UL)))
                        "failure owns its acquisition"
            }

            test "presentation callback failure retains accepted authority truth" {
                let pending, _ = connected "a" |> demand
                let accepted, _ = pending |> complete 1UL 0UL "a" 8UL

                let failed, effects =
                    accepted
                    |> step (SvgExternalSessionObservation.PresentationCallbackFailed(0UL, "a", 8UL))

                Expect.equal
                    effects
                    [
                        SvgExternalSessionEffect.AcquisitionFailed(
                            SvgExternalCompletionFailure.CallbackFailed,
                            1UL,
                            0UL,
                            "a",
                            Some 8UL
                        )
                    ]
                    "callback failure is explicit"

                Expect.equal failed.AcceptedRevision (Some 8UL) "callback cannot rewrite authority"

                Expect.equal
                    failed.LastOutcome
                    (Some(SvgExternalPresentationOutcome.Failed(SvgExternalCompletionFailure.CallbackFailed, Some 0UL)))
                    "failure retains identity"
            }

            test "invalidation disconnect and disposal cancel exact ownership in order" {
                let pending, _ = connected "a" |> demand

                let invalidated, invalidation =
                    pending |> step SvgExternalSessionObservation.InvalidatePresentation

                Expect.equal
                    invalidation
                    [ SvgExternalSessionEffect.CancelAcquisition(1UL, 0UL, Some "a") ]
                    "invalidation cancels exact request"

                Expect.equal invalidated.MountGeneration 2UL "mount advances"

                let active, _ = invalidated |> demand

                let disconnected, disconnect =
                    active |> step SvgExternalSessionObservation.Disconnect

                Expect.equal
                    disconnect
                    [
                        SvgExternalSessionEffect.CancelAcquisition(2UL, 1UL, Some "a")
                        SvgExternalSessionEffect.Disconnected 3UL
                    ]
                    "cancel precedes disconnect"

                let rebound, _ = disconnected |> step (SvgExternalSessionObservation.BindEpoch "a")
                let finalRequest, _ = rebound |> demand
                let disposed, disposal = finalRequest |> step SvgExternalSessionObservation.Dispose

                Expect.equal
                    disposal
                    [
                        SvgExternalSessionEffect.CancelAcquisition(4UL, 2UL, Some "a")
                        SvgExternalSessionEffect.Disposed 4UL
                    ]
                    "cancel precedes dispose"

                Expect.equal disposed.Status SvgExternalSessionStatus.Disposed "terminal state is explicit"
                Expect.equal (disposed |> demand) (disposed, []) "late demand is inert"
            }

            test "generation and acquisition identity exhaustion refuse wrap" {
                let generationExhausted =
                    { connected "a" with
                        MountGeneration = UInt64.MaxValue
                        PendingAcquisitionId = Some 7UL
                        AcquisitionPending = true
                    }

                let unchangedGeneration, generationEffects =
                    generationExhausted |> step (SvgExternalSessionObservation.BindEpoch "b")

                Expect.equal unchangedGeneration generationExhausted "generation never wraps"

                Expect.equal
                    generationEffects
                    [ SvgExternalSessionEffect.GenerationExhausted UInt64.MaxValue ]
                    "generation refusal carries value"

                let acquisitionExhausted =
                    { connected "a" with
                        NextAcquisitionId = UInt64.MaxValue
                    }

                let unchangedAcquisition, acquisitionEffects = acquisitionExhausted |> demand
                Expect.equal unchangedAcquisition acquisitionExhausted "acquisition identity never wraps"

                Expect.equal
                    acquisitionEffects
                    [ SvgExternalSessionEffect.AcquisitionIdExhausted UInt64.MaxValue ]
                    "identity refusal carries value"
            }
        ]
