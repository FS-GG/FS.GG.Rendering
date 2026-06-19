# Data Model: Input/Render Responsiveness

## Input Envelope

Represents one normalized input received by the live viewer.

**Fields**

- `SequenceId`: monotonic id assigned at receipt.
- `ReceiptTimestamp`: host timestamp or monotonic stopwatch timestamp at receipt.
- `InputKind`: `pointer-move`, `pointer-discrete`, `key-down`, `key-up`, `wheel`, `resize`, `lifecycle`, or `tick`.
- `PriorityLane`: `discrete`, `continuous`, `lifecycle`, or `background`.
- `Payload`: normalized `ViewerPointerInput`, keyboard event, size, or lifecycle token.
- `DiagnosticContext`: optional page/screen, control group, raw host facts, and environment facts.
- `ReceiptQueueDepth`: queue depth observed before enqueue.

**Validation Rules**

- `SequenceId` values are unique and increase by receipt order.
- Timestamps are monotonic when the host can provide monotonic time; otherwise the record declares timestamp precision/limitation.
- Pointer/key inputs that can affect live interaction must have an envelope.
- Missing or invalid payload classification is a diagnostics failure, not a silent drop.

## Input Queue

Holds pending input envelopes until the frame/update loop drains them.

**Fields**

- `Discrete`: ordered queue of non-coalescible envelopes.
- `LatestContinuousPointer`: latest pending pointer-move envelope, if any.
- `ContinuousCoalescedCount`: count of movement envelopes merged into `LatestContinuousPointer`.
- `Lifecycle`: urgent lifecycle/close entries that may bypass normal input batching.
- `NextSequenceId`: next sequence id to assign.
- `MaxObservedDepth`: high-water queue depth for the current run.

**Validation Rules**

- Discrete entries are dequeued in original receipt order.
- Continuous pointer moves may replace earlier pending moves only within the continuous lane.
- Movement coalescing never removes, reorders, or absorbs discrete pointer/key events.
- Close/lifecycle policy is explicit and cannot be delayed behind non-urgent work indefinitely.

## Frame Drain Batch

Represents the set of input envelopes selected for one frame/update pass.

**Fields**

- `BatchId`: monotonic frame-drain id.
- `DrainedAt`: timestamp when drain begins.
- `DiscreteInputs`: ordered list of discrete envelopes selected for this drain.
- `CoalescedPointer`: latest continuous pointer envelope, if any.
- `CoalescedMovementCount`: number of movement samples represented by `CoalescedPointer`.
- `QueueDepthBeforeDrain` / `QueueDepthAfterDrain`
- `DrainReason`: input, resize, tick, explicit wake, or evidence request.

**Validation Rules**

- Discrete inputs preserve receipt order inside and across batches.
- A coalesced pointer sample may be processed before the following discrete event only when it arrived earlier and the ordering policy records that it was flushed.
- Empty batches do not run product routing or recomposition unless a dirty scene, resize, tick, or lifecycle effect requires work.

## Scheduler Model

Represents live viewer scheduling state.

**Fields**

- `Queue`: current `InputQueue`.
- `CurrentModel`: product model owned by the viewer host.
- `CurrentSize`: latest window/client size.
- `CurrentScene`: latest committed scene.
- `DirtyState`: current dirty/invalidation state.
- `PendingLatencyRecords`: input records waiting for visible-response/presentation completion.
- `Diagnostics`: run diagnostics and write status.
- `Environment`: timing/presentation capability facts.

**State Transitions**

1. `Running`
2. `InputReceived`
3. `WakeSignaled`
4. `DrainingInputs`
5. `Routing`
6. `UpdatingModel`
7. `SceneDirty` or `NoVisibleResponse`
8. `RecomposingScene`
9. `Presenting`
10. `LatencyRecorded`
11. `Failed` or `EnvironmentLimited` when required boundaries cannot be measured

## Dirty State

Represents whether recomposition is required after draining input.

**Fields**

- `ProductModelChanged`
- `RuntimeStateChanged`
- `SizeChanged`
- `ThemeChanged`
- `SceneDirty`
- `DirtyRegionSummary`: dirty rect count, dirty area, repaint count, or unavailable.
- `Reason`: input sequence ids or runtime/lifecycle reason.

**Validation Rules**

- No-state-change input can produce `NoVisibleResponse` without recomposition.
- If any product state, runtime focus/hover, size, or presentation-affecting fact changes, `SceneDirty` is true.
- A dirty frame recomposes at most once after all eligible input for that frame is folded.

## Phase Timing

Represents measured work for one input or frame segment.

**Fields**

- `ReceiptDuration`
- `QueueDelay`
- `RoutingDuration`
- `UpdateDuration`
- `ViewDuration`
- `RetainedStepDuration`
- `LayoutDuration`
- `TextDuration`
- `PaintDuration`
- `PresentDuration`
- `TotalInputToVisibleDuration`
- `MeasurementStatus`: measured, unavailable, estimated, not-applicable, or environment-limited.

**Validation Rules**

- Missing required boundaries are represented by status fields and reasons.
- Timing values cannot be negative.
- Wall-clock timing is excluded from deterministic golden counts.
- Any measured recomposition, paint, present, or combined frame segment over 50 ms creates a long-frame fact.

## Latency Record

Represents one input's correlated evidence from receipt through visible response.

**Fields**

- `RecordId`
- `RunId`
- `InputSequenceId`
- `InputKind`
- `PageOrScreen`
- `ControlGroup`
- `ReceiptTimestamp`
- `QueueDepthAtReceipt`
- `QueueDepthAtDrain`
- `CoalescedMovementCount`
- `PhaseTiming`
- `ProductMessageCount`
- `ProductStateChanged`
- `RuntimeStateChanged`
- `VisibleResponse`: presented-frame, no-visible-response, failed, or environment-limited.
- `PresentedFrameId`
- `DirtyRegionSummary`
- `EnvironmentStatus`
- `Diagnostics`

**Validation Rules**

- Every completed discrete pointer/key interaction produces one latency record.
- A coalesced movement batch produces one batch record with coalesced count.
- No-visible-response is explicit and must say why no frame was required.
- Failures include actionable diagnostics and cannot be omitted from summaries.

## Responsiveness Budget

Represents readiness thresholds.

**Fields**

- `InputReceiptP95`: default 4 ms.
- `InputReceiptMax`: default 16 ms.
- `InputToVisibleP95`: default 50 ms.
- `LongFrameThreshold`: default 50 ms.
- `RequiredBoundaryPolicy`: which timing boundaries are required for accepted readiness.
- `EnvironmentLimitPolicy`: how substitute evidence is declared.

**Validation Rules**

- Accepted readiness requires p95/max values within budget and required boundaries measured or accepted substitute evidence recorded.
- Long-frame counts over threshold block or caveat readiness according to the checked scope's policy; they are never hidden.

## Responsiveness Summary

Represents run-level rollup for reviewers and machines.

**Fields**

- `RunId`
- `Scope`
- `OverallReadiness`
- `StartedUtc` / `CompletedUtc`
- `Budgets`
- `Percentiles`: p50, p95, max by page/screen, input type, and control group when known.
- `LongFrameCounts`
- `FirstFailedBudget`
- `SlowestInteractions`
- `EnvironmentLimitations`
- `RecordPath`
- `Diagnostics`

**Validation Rules**

- `summary.md` and `summary.json` agree on readiness, budgets, counts, slowest interactions, and limitations.
- The first failed budget is visible without parsing JSONL.
- Required environment limitations prevent accepted readiness unless explicitly accepted substitute evidence is recorded and surfaced.

## Diagnostic Run Request

Represents a maintainer-requested responsiveness capture.

**Fields**

- `ScopeId`
- `HostOrSample`
- `PageOrScreen`
- `Theme`
- `InputScript`
- `Budgets`
- `OutputRoot`
- `RequireLivePresentation`
- `DiagnosticsEnabled`

**Validation Rules**

- A documented representative AntShowcase run requires at least one pointer activation and one keyboard activation.
- Diagnostics-disabled runs must leave user-visible behavior unchanged and avoid writing timing records.
- Output paths are created before the run writes records; write failure is a run failure.
