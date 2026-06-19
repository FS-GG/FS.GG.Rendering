# Contract: Input Scheduler

## Package Boundary

The scheduler contract is split by ownership:

- `FS.GG.UI.SkiaViewer` owns input envelopes, queue policy, frame-drain policy, dirty scene scheduling, viewer diagnostics, and host-facing options.
- `FS.GG.UI.SkiaViewer.Host.OpenGl` owns native input callbacks, native timestamp capture when available, frame wake/signal, and presentation timing.
- `FS.GG.UI.Controls.Elmish` owns retained pointer/key routing, focus/runtime state, retained step metrics, and adapter-level timing contribution.
- Product hosts keep their existing `Init`, `Update`, `View`, `MapKey`, `MapPointer`, and `Tick` functions.

## Planned Surface

Exact F# names may be refined during implementation, but the public/additive contract must cover:

- A diagnostics option that enables/disables responsiveness timing.
- A callback or sink for latency records and summary facts.
- Stable input kind, environment status, visible-response, and readiness tokens.
- A way for sample/app edges to route JSONL/summary output without editing source.
- Existing viewer/app launch functions remain callable with existing arguments.

Internal/private scheduler types cover:

- `ViewerInputEnvelope`
- `ViewerInputQueue`
- `ViewerInputPriorityLane`
- `ViewerFrameDrain`
- `ViewerDirtyState`
- `ViewerSchedulerModel`
- `ViewerSchedulerMsg`
- `ViewerSchedulerEffect`

## Receipt Rules

- Native pointer/key callbacks normalize input, assign a sequence id, capture receipt timestamp, enqueue an envelope, signal the frame/update loop, and return.
- Native callbacks must not call product `Update`, product `View`, retained scene recomposition, paint, or presentation.
- Receipt duration includes normalization, sequencing, enqueue, diagnostics bookkeeping, and signal/wake work only.
- Receipt failures are recorded as diagnostics and surfaced as non-green run outcomes.

## Queue Rules

- Discrete pointer and keyboard input is stored and drained in receipt order.
- Continuous pointer movement may be coalesced before heavy processing; the latest movement sample wins.
- Coalescing records how many movement samples were represented by the processed sample.
- Discrete input is never dropped by movement coalescing.
- Queue depth is captured at receipt and at drain.

## Frame Drain Rules

- The frame/update loop drains eligible input at frame cadence or explicit wake.
- For each discrete input, routing maps the input to product messages without recomposing after every message.
- All product messages produced by one input are folded through `Update` in order before recomposition.
- After the selected batch is processed, scene recomposition runs at most once for the dirty frame.
- If no product/runtime/size/presentation-affecting state changed, the input receives an explicit no-visible-response classification and no recomposition is required.

## Resize, Lifecycle, and Close Rules

- Resize input marks the scene dirty and records the size change; it may bypass ordinary coalescing policy but must still avoid duplicate recompositions where possible.
- Close, app-requested close, and failure-driven close cannot be delayed behind non-urgent input.
- Screenshot/readback/evidence requests observe the latest committed scene and must state if queued input was pending at capture time.

## Error Rules

- Exceptions during routing, update, recomposition, paint, presentation, or diagnostic writing produce a failed latency record or run diagnostic.
- The queue is not silently cleared after an error unless the viewer is shutting down and records pending inputs as not-run/failed.
- Environment limitations are distinct from product failures and infrastructure failures.

## Compatibility Rules

- Existing pointer activation, focus routing, keyboard activation, and product state outcomes remain unchanged for the same ordered discrete inputs.
- `Enter` and `Space` key-down activation behavior for representative AntShowcase keyboard checks remains unchanged.
- Key-up remains non-activating unless a focused control handles it.
- Diagnostics disabled means no user-visible behavior change and no required timing overhead.
