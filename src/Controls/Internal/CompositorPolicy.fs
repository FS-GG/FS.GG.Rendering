namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

// Feature 190 (Pattern E, research R3): the step-INDEPENDENT compositor/reuse policy cluster,
// relocated out of `RetainedRender.fs` so that file meets the SC-001 size goal (≤≈1,500 lines)
// while the high-risk `step` stage extraction stays in one file with its helper graph intact.
//
// Everything here is pure policy + data: the feature-147 damage/snapshot helpers (`unionArea`,
// `damageRegionSet`, `placementDamage`, `classifyDamageFallback`, `promotionDecision`,
// `snapshotVerdict`) and the feature-159 identity/reuse/promotion family. NONE of it references the
// retained render structure (`RetainedRender<'msg>`/`RetainedNode<'msg>`) or the step pipeline, so
// compiling it BEFORE `RetainedRender.fs` introduces no producer→consumer back-edge (FR-009).
//
// `WorkReductionRecord` and the `Compositor*`/`PromotionDecision*`/`Feature159*`/
// `SnapshotResourceVerdict` types move here with the functions (they are namespace-level types in
// `namespace FS.GG.UI.Controls`, so every UNQUALIFIED consumer reference — `step`'s record
// construction, `Inspection`, `Controls.Elmish` — resolves unchanged). Internal test call sites that
// qualified these as `RetainedRender.*` now qualify them as `CompositorPolicy.*` (a rename only; the
// public package surface is unchanged — every binding is `internal`, no bump, FR-004/FR-014).
// `module internal` with no `.fsi` — the `Internal/` convention (AttrKeys/Hashing precedent).

type internal WorkReductionRecord =
    { BaselineNodeCount: int
      MetadataVisitedNodeCount: int
      MetadataFallbackCount: int
      RecomputedNodeCount: int
      ChangedSubtreeBound: int
      ShiftedNodeCount: int
      // Feature 097 (R2, FR-006): nodes actually re-measured this frame (post-propagation dirty set).
      RemeasuredNodeCount: int
      // Feature 113 (Phase 5, FR-009/FR-010): memoizable-control reuse outcomes this frame.
      MemoHits: int
      MemoMisses: int
      // Feature 114 (Phase 6, FR-013): materialized data-grid-row nodes + logical row total this frame.
      VirtualMaterialized: int
      VirtualTotal: int
      // Feature 116 (Phase 7, FR-001/FR-004): the damage set — repainted-node count, distinct dirty-rect
      // count, summed integer dirty area.
      RepaintedNodeCount: int
      DirtyRectCount: int
      DirtyArea: int
      // Feature 116 (Phase 7, FR-005/FR-009/FR-010): picture-cache hits, misses, and live entry count.
      PictureCacheHits: int
      PictureCacheMisses: int
      PictureCacheEntryCount: int
      // Feature 117 (Phase 8, FR-001/FR-005): per-frame text-measure cache hits + misses.
      TextMeasureCacheHits: int
      TextMeasureCacheMisses: int
      // Feature 117 (Phase 8, FR-006): the pre-pinning layout dirty-set size (<= RemeasuredNodeCount).
      LayoutInvalidatedNodeCount: int
      // Feature 120 (US3, FR-014): backend replay-cache per-frame outcomes (deterministic model).
      ReplayHits: int
      ReplayMisses: int
      ReplayRecords: int
      ReplaySkippedNodes: int
      ReplayCacheNativeBytes: int
      AvoidedContentWork: int
      PlacementOnlyReuseCount: int
      ContentRecordCount: int
      ContentRerecordCount: int
      PromotionCount: int
      DemotionCount: int
      FallbackCount: int
      PromotionOverhead: int
      NetSavedWork: int }

type internal CompositorDamageRegion =
    { DamageX: int
      DamageY: int
      DamageWidth: int
      DamageHeight: int }

type internal CompositorDamageRegionSet =
    { FrameWidth: int
      FrameHeight: int
      Regions: CompositorDamageRegion list
      UnionArea: int
      FullFrameInvalidation: bool
      Cause: string }

type internal CompositorFallbackReason =
    | MissingProof
    | FailedProof of reason: string
    | EnvironmentLimited of reason: string
    | FullFrameInvalidation
    | EmptyDamage
    | UnsafeDamage of reason: string

type internal CompositorTier =
    | NoCompositorTier
    | RetainedTier
    | ReplayTier
    | SnapshotTier
    | DemotedTier

type internal PromotionDecisionKind =
    | Promote
    | Keep
    | Demote
    | Reject
    | Observe

type internal PromotionDecision =
    { BoundaryId: string
      Decision: PromotionDecisionKind
      Reason: string
      ObservedStabilityFrames: int
      ExpectedSavedWork: int
      MeasuredOverhead: int
      Tier: CompositorTier }

[<RequireQualifiedAccess>]
type internal Feature159Reason =
    | Instability
    | LowCost
    | OverheadExceedsSavedWork
    | StaleContentIdentity
    | StalePlacementIdentity
    | AmbiguousIdentity
    | CrossProfileEvidence
    | MissingRetainedContent
    | ResourceLimited
    | UnsupportedHost
    | ParityMismatch
    | NonBeneficialCounters
    | RunIdentityMismatch
    | ScenarioDefinitionMismatch
    | MissingPolicy
    | EnvironmentLimited

[<RequireQualifiedAccess>]
type internal Feature159PromotionStatus =
    | Promoted
    | Observing
    | Kept
    | Demoted
    | Rejected
    | Bypassed
    | NonBeneficial
    | FallbackOnly
    | PromotionEnvironmentLimited

[<RequireQualifiedAccess>]
type internal Feature159ReuseStatus =
    | ContentReusedPlacementUpdated
    | ContentRecorded
    | ContentRerecorded
    | FallbackFullRedraw
    | ReuseRejected
    | ReuseEnvironmentLimited

[<RequireQualifiedAccess>]
type internal Feature159RetainedLayerState =
    | Recorded
    | Reused
    | Refreshed
    | Bypassed
    | Evicted
    | Demoted
    | Invalid
    | Unavailable

type internal Feature159ContentIdentity =
    { BoundaryId: string
      ContentId: uint64
      LocalContentFingerprint: uint64
      AlgorithmVersion: string
      RunId: string
      ArtifactPath: string option }

type internal Feature159PlacementIdentity =
    { BoundaryId: string
      PlacementId: uint64
      Box: FS.GG.UI.Scene.Rect option
      ScrollOffsetX: float
      ScrollOffsetY: float
      Scale: float
      Coverage: FS.GG.UI.Scene.Rect list
      AlgorithmVersion: string }

type internal Feature159ReuseCounters =
    { AvoidedContentWork: int
      PlacementOnlyReuseCount: int
      ContentRecordCount: int
      ContentRerecordCount: int
      PromotionCount: int
      DemotionCount: int
      FallbackCount: int
      ReplayHits: int
      ReplayMisses: int
      ReplayRecords: int
      PromotionOverhead: int
      NetSavedWork: int }

type internal Feature159ReuseDecision =
    { BoundaryId: string
      Status: Feature159ReuseStatus
      PrimaryReason: Feature159Reason option
      PriorContentIdentity: Feature159ContentIdentity option
      CurrentContentIdentity: Feature159ContentIdentity option
      PriorPlacementIdentity: Feature159PlacementIdentity option
      CurrentPlacementIdentity: Feature159PlacementIdentity option
      CounterDelta: Feature159ReuseCounters
      ArtifactPaths: string list }

type internal Feature159PromotionCandidate =
    { BoundaryId: string
      ScenarioId: string
      HostProfileId: string
      ObservationWindow: int
      ObservedStabilityFrames: int
      ExpectedSavedWork: int
      MeasuredOverhead: int
      ReductionPercent: float
      ContentStable: bool
      ParityPassed: bool
      ResourceLimited: bool
      CurrentTier: CompositorTier }

type internal Feature159ParityResult =
    { ScenarioId: string
      AttemptId: string
      Verdict: string
      OutsideDamageDriftCount: int
      ArtifactPaths: string list
      Diagnostics: string list }

type internal Feature159PromotionDecision =
    { BoundaryId: string
      Status: Feature159PromotionStatus
      PrimaryReason: Feature159Reason option
      ObservedStabilityFrames: int
      ExpectedSavedWork: int
      MeasuredOverhead: int
      ReductionPercent: float
      TargetTier: CompositorTier
      Parity: Feature159ParityResult option
      ArtifactPaths: string list }

type internal Feature159RetainedLayer =
    { LayerId: string
      BoundaryId: string
      ContentIdentity: Feature159ContentIdentity
      LastPlacementIdentity: Feature159PlacementIdentity
      HostProfileId: string
      RunId: string
      State: Feature159RetainedLayerState
      ResourceEstimate: int
      Diagnostics: string list }

type internal SnapshotResourceVerdict =
    | SnapshotReady
    | SnapshotDemoted of reason: string
    | SnapshotLimited of reason: string

module internal CompositorPolicy =

    let unionArea (boxes: FS.GG.UI.Scene.Rect list) (frameArea: int) : int =
        match boxes with
        | [] -> 0
        | boxes ->
            let xs = boxes |> List.collect (fun b -> [ b.X; b.X + b.Width ]) |> List.distinct |> List.sort
            let ys = boxes |> List.collect (fun b -> [ b.Y; b.Y + b.Height ]) |> List.distinct |> List.sort
            let mutable area = 0.0 // mutable: hot path / union accumulator

            for i in 0 .. xs.Length - 2 do
                let x0, x1 = xs.[i], xs.[i + 1]

                for j in 0 .. ys.Length - 2 do
                    let y0, y1 = ys.[j], ys.[j + 1]

                    let covered =
                        boxes
                        |> List.exists (fun b -> b.X <= x0 && x1 <= b.X + b.Width && b.Y <= y0 && y1 <= b.Y + b.Height)

                    if covered then
                        area <- area + (x1 - x0) * (y1 - y0)

            min (int area) frameArea

    let private rectOfDamage (damage: CompositorDamageRegion) : Rect =
        { X = float damage.DamageX
          Y = float damage.DamageY
          Width = float damage.DamageWidth
          Height = float damage.DamageHeight }

    let private damageOfRect frameWidth frameHeight (rect: Rect) : CompositorDamageRegion option =
        // Feature 178 (US3): shared Numeric.clamp (same (lo, hi, value) order, identical semantics).
        let x0 = Numeric.clamp 0 frameWidth (int (System.Math.Floor rect.X))
        let y0 = Numeric.clamp 0 frameHeight (int (System.Math.Floor rect.Y))
        let x1 = Numeric.clamp 0 frameWidth (int (System.Math.Ceiling(rect.X + rect.Width)))
        let y1 = Numeric.clamp 0 frameHeight (int (System.Math.Ceiling(rect.Y + rect.Height)))
        let width = x1 - x0
        let height = y1 - y0

        if width <= 0 || height <= 0 then
            None
        else
            Some
                { DamageX = x0
                  DamageY = y0
                  DamageWidth = width
                  DamageHeight = height }

    type DamageSetInputs =
        { FrameWidth: int
          FrameHeight: int
          FullFrameInvalidation: bool
          Cause: string
          Boxes: FS.GG.UI.Scene.Rect list }

    let damageRegionSet (inputs: DamageSetInputs) =
        let frameWidth = inputs.FrameWidth
        let frameHeight = inputs.FrameHeight
        let fullFrameInvalidation = inputs.FullFrameInvalidation
        let cause = inputs.Cause
        let boxes = inputs.Boxes
        let frameArea = max 0 frameWidth * max 0 frameHeight

        let regions =
            if fullFrameInvalidation then
                if frameArea = 0 then
                    []
                else
                    [ { DamageX = 0
                        DamageY = 0
                        DamageWidth = max 0 frameWidth
                        DamageHeight = max 0 frameHeight } ]
            else
                boxes
                |> List.choose (damageOfRect frameWidth frameHeight)
                |> List.distinct

        { FrameWidth = frameWidth
          FrameHeight = frameHeight
          Regions = regions
          UnionArea = unionArea (regions |> List.map rectOfDamage) frameArea
          FullFrameInvalidation = fullFrameInvalidation
          Cause = cause }

    let placementDamage frameWidth frameHeight oldBox newBox =
        damageRegionSet
            { FrameWidth = frameWidth
              FrameHeight = frameHeight
              FullFrameInvalidation = false
              Cause = "placement-only movement"
              Boxes = [ oldBox; newBox ] }

    let classifyDamageFallback proofReady (proofReason: string option) (damage: CompositorDamageRegionSet) =
        match proofReady, proofReason, damage.FullFrameInvalidation, damage.Regions with
        | false, Some reason, _, _ when reason.Contains("environment", System.StringComparison.OrdinalIgnoreCase) -> Some(CompositorFallbackReason.EnvironmentLimited reason)
        | false, Some reason, _, _ -> Some(FailedProof reason)
        | false, None, _, _ -> Some MissingProof
        | true, _, true, _ -> Some FullFrameInvalidation
        | true, _, false, [] -> Some EmptyDamage
        | true, _, false, _ when damage.UnionArea > damage.FrameWidth * damage.FrameHeight -> Some(UnsafeDamage "damage exceeds frame area")
        | _ -> None

    type PromotionInputs =
        { BoundaryId: string
          ObservedStabilityFrames: int
          ObservationWindow: int
          ExpectedSavedWork: int
          MeasuredOverhead: int
          ParityPassed: bool }

    let promotionDecision (inputs: PromotionInputs) =
        let boundaryId = inputs.BoundaryId
        let observedStabilityFrames = inputs.ObservedStabilityFrames
        let observationWindow = inputs.ObservationWindow
        let expectedSavedWork = inputs.ExpectedSavedWork
        let measuredOverhead = inputs.MeasuredOverhead
        let parityPassed = inputs.ParityPassed
        if not parityPassed then
            { BoundaryId = boundaryId
              Decision = Reject
              Reason = "parity failed"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = NoCompositorTier }
        elif observedStabilityFrames < observationWindow then
            { BoundaryId = boundaryId
              Decision = Observe
              Reason = "stability window incomplete"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = NoCompositorTier }
        elif expectedSavedWork <= 0 then
            { BoundaryId = boundaryId
              Decision = Reject
              Reason = "no expected saved work"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = NoCompositorTier }
        elif measuredOverhead >= expectedSavedWork then
            { BoundaryId = boundaryId
              Decision = Demote
              Reason = "bookkeeping overhead exceeds saved work"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = DemotedTier }
        else
            { BoundaryId = boundaryId
              Decision = Promote
              Reason = "stable and beneficial"
              ObservedStabilityFrames = observedStabilityFrames
              ExpectedSavedWork = expectedSavedWork
              MeasuredOverhead = measuredOverhead
              Tier = ReplayTier }

    let feature159ReasonToken reason =
        match reason with
        | Feature159Reason.Instability -> "instability"
        | Feature159Reason.LowCost -> "low-cost"
        | Feature159Reason.OverheadExceedsSavedWork -> "overhead-exceeds-saved-work"
        | Feature159Reason.StaleContentIdentity -> "stale-content-identity"
        | Feature159Reason.StalePlacementIdentity -> "stale-placement-identity"
        | Feature159Reason.AmbiguousIdentity -> "ambiguous-identity"
        | Feature159Reason.CrossProfileEvidence -> "cross-profile-evidence"
        | Feature159Reason.MissingRetainedContent -> "missing-retained-content"
        | Feature159Reason.ResourceLimited -> "resource-limited"
        | Feature159Reason.UnsupportedHost -> "unsupported-host"
        | Feature159Reason.ParityMismatch -> "parity-mismatch"
        | Feature159Reason.NonBeneficialCounters -> "non-beneficial-counters"
        | Feature159Reason.RunIdentityMismatch -> "run-identity-mismatch"
        | Feature159Reason.ScenarioDefinitionMismatch -> "scenario-definition-mismatch"
        | Feature159Reason.MissingPolicy -> "missing-policy"
        | Feature159Reason.EnvironmentLimited -> "environment-limited"

    let feature159PromotionStatusToken status =
        match status with
        | Feature159PromotionStatus.Promoted -> "promoted"
        | Feature159PromotionStatus.Observing -> "observing"
        | Feature159PromotionStatus.Kept -> "kept"
        | Feature159PromotionStatus.Demoted -> "demoted"
        | Feature159PromotionStatus.Rejected -> "rejected"
        | Feature159PromotionStatus.Bypassed -> "bypassed"
        | Feature159PromotionStatus.NonBeneficial -> "non-beneficial"
        | Feature159PromotionStatus.FallbackOnly -> "fallback-only"
        | Feature159PromotionStatus.PromotionEnvironmentLimited -> "environment-limited"

    let feature159ReuseStatusToken status =
        match status with
        | Feature159ReuseStatus.ContentReusedPlacementUpdated -> "content-reused-placement-updated"
        | Feature159ReuseStatus.ContentRecorded -> "content-recorded"
        | Feature159ReuseStatus.ContentRerecorded -> "content-re-recorded"
        | Feature159ReuseStatus.FallbackFullRedraw -> "fallback-full-redraw"
        | Feature159ReuseStatus.ReuseRejected -> "reuse-rejected"
        | Feature159ReuseStatus.ReuseEnvironmentLimited -> "environment-limited"

    let private feature159Hash (parts: string list) =
        // Feature 178 (US2): constants + core step from the shared Hashing primitive. Keeps the
        // per-char `int ch` widening and `'|'` separator; Hashing.step h x = (h ^^^ x) * prime is
        // exactly the prior xor-then-multiply pair, so the fold is byte-identical.
        let mutable hash = Hashing.offsetBasis // mutable: compact deterministic FNV-1a fold.
        for part in parts do
            for ch in part do
                hash <- Hashing.step hash (uint64 (int ch))
            hash <- Hashing.step hash (uint64 (int '|'))
        hash

    let private rectToken (rect: Rect option) =
        match rect with
        | None -> "none"
        | Some r -> sprintf "%.3f,%.3f,%.3f,%.3f" r.X r.Y r.Width r.Height

    let feature159ContentIdentity boundaryId runId localContentFingerprint artifactPath =
        { BoundaryId = boundaryId
          ContentId = feature159Hash [ boundaryId; runId; string localContentFingerprint; "content-v1" ]
          LocalContentFingerprint = localContentFingerprint
          AlgorithmVersion = "content-identity-v1"
          RunId = runId
          ArtifactPath = artifactPath }

    let feature159PlacementIdentity boundaryId box scrollOffsetX scrollOffsetY scale coverage =
        let coverageToken =
            coverage
            |> List.map (Some >> rectToken)
            |> String.concat ";"

        { BoundaryId = boundaryId
          PlacementId =
            feature159Hash
                [ boundaryId
                  rectToken box
                  sprintf "%.3f" scrollOffsetX
                  sprintf "%.3f" scrollOffsetY
                  sprintf "%.3f" scale
                  coverageToken
                  "placement-v1" ]
          Box = box
          ScrollOffsetX = scrollOffsetX
          ScrollOffsetY = scrollOffsetY
          Scale = scale
          Coverage = coverage
          AlgorithmVersion = "placement-identity-v1" }

    let private zeroFeature159Counters =
        { AvoidedContentWork = 0
          PlacementOnlyReuseCount = 0
          ContentRecordCount = 0
          ContentRerecordCount = 0
          PromotionCount = 0
          DemotionCount = 0
          FallbackCount = 0
          ReplayHits = 0
          ReplayMisses = 0
          ReplayRecords = 0
          PromotionOverhead = 0
          NetSavedWork = 0 }

    let private feature159ReuseDecision
        status
        reason
        (priorContent: Feature159ContentIdentity option)
        (currentContent: Feature159ContentIdentity)
        (priorPlacement: Feature159PlacementIdentity option)
        (currentPlacement: Feature159PlacementIdentity)
        counters =
        { BoundaryId = currentContent.BoundaryId
          Status = status
          PrimaryReason = reason
          PriorContentIdentity = priorContent
          CurrentContentIdentity = Some currentContent
          PriorPlacementIdentity = priorPlacement
          CurrentPlacementIdentity = Some currentPlacement
          CounterDelta = counters
          ArtifactPaths = [] }

    let feature159ClassifyReuse
        (priorContent: Feature159ContentIdentity option)
        (currentContent: Feature159ContentIdentity)
        (priorPlacement: Feature159PlacementIdentity option)
        (currentPlacement: Feature159PlacementIdentity)
        retainedResident
        sameProfile
        parityPassed
        resourceLimited =
        let recordCounters =
            { zeroFeature159Counters with
                ContentRecordCount = 1
                ReplayMisses = 1
                ReplayRecords = 1
                NetSavedWork = -1 }

        let rerecordCounters =
            { zeroFeature159Counters with
                ContentRerecordCount = 1
                ReplayMisses = 1
                ReplayRecords = 1
                NetSavedWork = -1 }

        if resourceLimited then
            feature159ReuseDecision Feature159ReuseStatus.FallbackFullRedraw (Some Feature159Reason.ResourceLimited) priorContent currentContent priorPlacement currentPlacement { zeroFeature159Counters with FallbackCount = 1 }
        elif not sameProfile then
            feature159ReuseDecision Feature159ReuseStatus.ReuseRejected (Some Feature159Reason.CrossProfileEvidence) priorContent currentContent priorPlacement currentPlacement zeroFeature159Counters
        elif not parityPassed then
            feature159ReuseDecision Feature159ReuseStatus.ReuseRejected (Some Feature159Reason.ParityMismatch) priorContent currentContent priorPlacement currentPlacement zeroFeature159Counters
        elif not retainedResident then
            feature159ReuseDecision Feature159ReuseStatus.FallbackFullRedraw (Some Feature159Reason.MissingRetainedContent) priorContent currentContent priorPlacement currentPlacement { zeroFeature159Counters with FallbackCount = 1 }
        else
            match priorContent, priorPlacement with
            | None, _ ->
                feature159ReuseDecision Feature159ReuseStatus.ContentRecorded None priorContent currentContent priorPlacement currentPlacement recordCounters
            | Some prior, _ when prior.ContentId <> currentContent.ContentId ->
                feature159ReuseDecision Feature159ReuseStatus.ContentRerecorded None priorContent currentContent priorPlacement currentPlacement rerecordCounters
            | Some _, None ->
                feature159ReuseDecision Feature159ReuseStatus.ContentRecorded (Some Feature159Reason.StalePlacementIdentity) priorContent currentContent priorPlacement currentPlacement recordCounters
            | Some _, Some placement when placement.PlacementId <> currentPlacement.PlacementId ->
                let counters =
                    { zeroFeature159Counters with
                        AvoidedContentWork = 1
                        PlacementOnlyReuseCount = 1
                        ReplayHits = 1
                        NetSavedWork = 1 }

                feature159ReuseDecision Feature159ReuseStatus.ContentReusedPlacementUpdated None priorContent currentContent priorPlacement currentPlacement counters
            | Some _, Some _ ->
                let counters =
                    { zeroFeature159Counters with
                        AvoidedContentWork = 1
                        ReplayHits = 1
                        NetSavedWork = 1 }

                feature159ReuseDecision Feature159ReuseStatus.ContentReusedPlacementUpdated None priorContent currentContent priorPlacement currentPlacement counters

    let feature159EvaluatePromotion (candidate: Feature159PromotionCandidate) parity =
        let status, reason =
            if candidate.ResourceLimited then
                Feature159PromotionStatus.Bypassed, Some Feature159Reason.ResourceLimited
            elif not candidate.ParityPassed then
                Feature159PromotionStatus.Rejected, Some Feature159Reason.ParityMismatch
            elif not candidate.ContentStable then
                Feature159PromotionStatus.Demoted, Some Feature159Reason.Instability
            elif candidate.ObservedStabilityFrames < candidate.ObservationWindow then
                Feature159PromotionStatus.Observing, Some Feature159Reason.Instability
            elif candidate.ExpectedSavedWork <= candidate.MeasuredOverhead then
                Feature159PromotionStatus.NonBeneficial, Some Feature159Reason.OverheadExceedsSavedWork
            elif candidate.ReductionPercent < 30.0 then
                Feature159PromotionStatus.Kept, Some Feature159Reason.LowCost
            else
                Feature159PromotionStatus.Promoted, None

        { BoundaryId = candidate.BoundaryId
          Status = status
          PrimaryReason = reason
          ObservedStabilityFrames = candidate.ObservedStabilityFrames
          ExpectedSavedWork = candidate.ExpectedSavedWork
          MeasuredOverhead = candidate.MeasuredOverhead
          ReductionPercent = candidate.ReductionPercent
          TargetTier = ReplayTier
          Parity = parity
          ArtifactPaths = [] }

    let feature159CountersFromWork (work: WorkReductionRecord) =
        { AvoidedContentWork = work.AvoidedContentWork
          PlacementOnlyReuseCount = work.PlacementOnlyReuseCount
          ContentRecordCount = work.ContentRecordCount
          ContentRerecordCount = work.ContentRerecordCount
          PromotionCount = work.PromotionCount
          DemotionCount = work.DemotionCount
          FallbackCount = work.FallbackCount
          ReplayHits = work.ReplayHits
          ReplayMisses = work.ReplayMisses
          ReplayRecords = work.ReplayRecords
          PromotionOverhead = work.PromotionOverhead
          NetSavedWork = work.NetSavedWork }

    let snapshotVerdict supported (byteEstimate: int64) (byteBudget: int64) (benefitPercent: float) (thresholdPercent: float) =
        if not supported then
            SnapshotLimited "snapshot host unsupported"
        elif byteEstimate > byteBudget then
            SnapshotDemoted "snapshot budget exceeded"
        elif benefitPercent < thresholdPercent then
            SnapshotDemoted "snapshot benefit below threshold"
        else
            SnapshotReady
