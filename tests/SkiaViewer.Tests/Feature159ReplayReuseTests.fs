module Feature159ReplayReuseTests

open Expecto
open FS.GG.UI.SkiaViewer

// SYNTHETIC: Split replay requests are minimal cache-state fixtures for deterministic replay decisions.
let private request content placement : PictureReplayCache.SplitReplayRequest =
    { ContentCacheId = content
      ContentFingerprint = content
      PlacementFingerprint = placement
      RunProfileMatches = true
      RetainedResident = true
      ResourceLimited = false
      ParityPassed = true
      ReplayEnabled = true }

[<Tests>]
let tests =
    testList "Feature159 replay reuse" [
        test "Synthetic content-keyed replay hits across placement-only movement" {
            let prior = request 42UL 1UL
            let current = request 42UL 2UL
            let decision = PictureReplayCache.classifySplitReplay (Some prior) current

            Expect.equal decision.Status "content-reused-placement-updated" "hit"
            Expect.isTrue decision.PlacementOnlyChange "placement-only change"
            Expect.isFalse decision.RecordRequired "no record required"
        }

        test "Synthetic content change re-records and disabled or unsafe replay fails closed" {
            let prior = request 42UL 1UL
            let changed = request 99UL 1UL
            let disabled = { changed with ReplayEnabled = false }
            let parity = { changed with ParityPassed = false }

            Expect.equal (PictureReplayCache.classifySplitReplay (Some prior) changed).Status "content-re-recorded" "content change records"
            Expect.equal (PictureReplayCache.classifySplitReplay (Some prior) disabled).FallbackReason (Some PictureReplayCache.DisabledReplay) "disabled replay"
            Expect.equal (PictureReplayCache.classifySplitReplay (Some prior) parity).FallbackReason (Some PictureReplayCache.ParityMismatch) "parity mismatch"
        }
    ]
