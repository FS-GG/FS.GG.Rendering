namespace FS.GG.UI.SkiaViewer

open System

/// Feature 187 (US1): input-queue + dirty-state bodies carved out of the `Viewer` module
/// (bodies out, contracts stay). No `.fsi`; `module internal` keeps it off the public surface
/// (FR-007), matching the SceneRenderer.fs / Numeric.fs precedent. Pure functions over the
/// Viewer.Types input records — verbatim moves, so `Feature167InputQueue`/`SchedulerDrain` behave
/// identically. `module Viewer` keeps byte-identical public delegators to these.
module internal ViewerInputQueueOps =
    let emptyInputQueue =
        { Discrete = []
          LatestContinuousPointer = None
          ContinuousCoalescedCount = 0
          Lifecycle = []
          NextSequenceId = 1L
          MaxObservedDepth = 0 }

    let inputQueueDepth queue =
        queue.Discrete.Length
        + queue.Lifecycle.Length
        + (match queue.LatestContinuousPointer with
           | Some _ -> 1
           | None -> 0)

    let priorityForInput kind =
        match kind with
        | ViewerResponsivenessInputKind.PointerMove -> Continuous
        | ViewerResponsivenessInputKind.Resize
        | ViewerResponsivenessInputKind.Lifecycle -> Lifecycle
        | ViewerResponsivenessInputKind.Tick -> Background
        | ViewerResponsivenessInputKind.PointerDiscrete
        | ViewerResponsivenessInputKind.KeyDown
        | ViewerResponsivenessInputKind.KeyUp
        | ViewerResponsivenessInputKind.Wheel -> Discrete

    let enqueueInput receivedAt inputKind payload queue =
        let depthBefore = inputQueueDepth queue

        let envelope =
            { SequenceId = queue.NextSequenceId
              ReceivedAt = receivedAt
              InputKind = inputKind
              PriorityLane = priorityForInput inputKind
              ReceiptQueueDepth = depthBefore
              Payload = payload }

        let next =
            match envelope.PriorityLane with
            | Continuous ->
                { queue with
                    LatestContinuousPointer = Some envelope
                    ContinuousCoalescedCount =
                        queue.ContinuousCoalescedCount
                        + (if Option.isSome queue.LatestContinuousPointer then 1 else 0) }
            | Lifecycle ->
                { queue with Lifecycle = queue.Lifecycle @ [ envelope ] }
            | Discrete
            | Background ->
                { queue with Discrete = queue.Discrete @ [ envelope ] }

        let observedDepth = max (depthBefore + 1) (inputQueueDepth next)

        envelope,
        { next with
            NextSequenceId = queue.NextSequenceId + 1L
            MaxObservedDepth = max queue.MaxObservedDepth observedDepth }

    let drainInputQueue batchId drainReason queue =
        let before = inputQueueDepth queue
        let orderedNonContinuous =
            let laneRank envelope =
                match envelope.PriorityLane with
                | Discrete -> 0
                | Lifecycle -> 1
                | Continuous -> 2
                | Background -> 3

            (queue.Lifecycle @ queue.Discrete)
            |> List.mapi (fun index envelope -> laneRank envelope, index, envelope)
            |> List.sortBy (fun (rank, index, _) -> rank, index)
            |> List.map (fun (_, _, envelope) -> envelope)

        let drain =
            { BatchId = batchId
              DiscreteInputs = orderedNonContinuous
              CoalescedPointer = queue.LatestContinuousPointer
              CoalescedMovementCount = queue.ContinuousCoalescedCount
              QueueDepthBeforeDrain = before
              QueueDepthAfterDrain = 0
              DrainReason = drainReason }

        let queue' =
            { queue with
                Discrete = []
                LatestContinuousPointer = None
                ContinuousCoalescedCount = 0
                Lifecycle = [] }

        drain, queue'

    let dirtyState
        productModelChanged
        runtimeStateChanged
        sizeChanged
        themeChanged
        (dirtyRegion: ViewerResponsivenessDirtyRegion option)
        reason
        =
        let sceneDirty =
            productModelChanged
            || runtimeStateChanged
            || sizeChanged
            || themeChanged
            || Option.isSome dirtyRegion

        { ProductModelChanged = productModelChanged
          RuntimeStateChanged = runtimeStateChanged
          SizeChanged = sizeChanged
          ThemeChanged = themeChanged
          SceneDirty = sceneDirty
          DirtyRegionSummary = dirtyRegion
          Reason = reason }

    let dirtyStateRequiresRecompose dirty =
        dirty.SceneDirty
