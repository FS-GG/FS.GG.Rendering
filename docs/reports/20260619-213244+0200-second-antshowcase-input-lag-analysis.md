# SecondAntShowcase Input Lag Analysis

Timestamp: 2026-06-19 21:32:44 +0200

Scope: code-path analysis only. No live desktop timing was collected for this
report.

## Summary

The observed lag is reported as approximately 500 ms. That magnitude is too
large to be explained by ordinary 60 Hz frame pacing alone. A one-frame or
two-frame queueing penalty should normally land around 30-80 ms, and even an
awkward coalescing path should usually remain near or below 100 ms.

The more likely explanation is that the live UI/render thread is periodically
blocked by render, presentation, or broad scene recomposition work. The current
architecture can amplify that stall because input is queued and only drained on
the paced update tick, while visible output is presented on the paced render
tick. If a frame blocks for hundreds of milliseconds, input events accumulate and
become visible late.

## Relevant Code Paths

- Native mouse/keyboard callbacks are mapped in
  `src/SkiaViewer/Host/OpenGl.fs` around `attachInputEventMapping`.
- `src/SkiaViewer/SkiaViewer.fs` enqueues pointer/key inputs in
  `runPresentedPersistentWindow` and drains them from `LegacyUpdateTick`.
- `src/Controls.Elmish/ControlsElmish.fs` routes retained pointer input, applies
  runtime visual state, emits frame metrics, and performs a second layer of
  pointer-move coalescing.
- `src/SkiaViewer/Host/OpenGl.fs` renders and presents through
  `renderFrameDirect`, including the direct draw, `surface.Snapshot()`,
  `context.Flush()`, and `window.SwapBuffers()`.
- `samples/SecondAntShowcase/SecondAntShowcase.App/Responsiveness.fs` currently
  writes deterministic substitute evidence; it is not the live interactive path.

## Hypotheses

### H1: Render or Present Blocking on the UI Thread

Confidence: high.

The OpenGL host event loop is single-threaded for input, update, and render.
`DoEvents()` runs native callbacks, then the loop gates update and render by the
frame interval. If rendering or presenting blocks, input cannot be drained or
presented until that work completes.

The most suspicious present operations are:

- `drawScene scene surface.Canvas`
- `surface.Canvas.Flush()`
- `surface.Snapshot()`
- `context.Flush()`
- `window.SwapBuffers()`

`surface.Snapshot()` is especially worth checking. Although this is not a
GPU-to-CPU readback path, it can still cause GPU synchronization or memory work
depending on the driver and framebuffer state. `SwapBuffers()` can also block on
the compositor.

Expected symptom: queue delay and/or present duration spikes near the reported
500 ms lag.

### H2: Input Queueing Adds Latency, but Not 500 ms by Itself

Confidence: high.

Pointer events are enqueued quickly and not handled immediately. They are drained
from `LegacyUpdateTick`; visible output is then presented from `LegacyRenderTick`.
At 60 Hz this design adds a bounded frame-alignment delay. By itself this should
not create half-second lag.

It becomes serious when a render/present frame blocks. The queue then ages while
the same thread is busy, so every later input appears late.

Expected symptom: native receipt timestamps continue, but drain timestamps lag
behind by hundreds of milliseconds.

### H3: Double Pointer-Move Coalescing Makes Drags Trail

Confidence: medium-high.

There are two coalescing layers:

- `SkiaViewer` keeps only the latest continuous pointer move in the input queue.
- `Controls.Elmish` stores each move and processes the previously stored move at
  the next sample boundary.

That can make value-changing drags visibly trail the pointer. It probably does
not explain 500 ms alone, but it can make a blocked render loop feel much worse.

Expected symptom: drag records show stale move positions, delayed drag start, or
the final move applied after release.

### H4: Coalesced Move Ordering Can Break Drag Semantics

Confidence: medium.

The viewer drain path orders discrete inputs first, then the coalesced pointer
move. If press, move, and release are all queued in one drain window, the move
can be applied after the release. Drag state depends on the press being active,
so the value-changing move may be delayed, dropped, or converted into an
unexpected sequence.

Expected symptom: representative slider/rating actions produce accepted press
and release records but weak or missing continuous drag feedback.

### H5: Multiple Product Messages Can Trigger Multiple Scene Recompositions

Confidence: medium.

`handlePointer` folds every produced message through `dispatchHostMsg`.
`dispatchHostMsg` recomputes `currentScene` immediately with `host.View`. If one
input produces multiple messages, it can recompute the scene multiple times
before the next present.

Expected symptom: update/view/retained-step durations scale with message count
rather than with one frame's final model state.

### H6: Runtime Visual Feedback May Not Always Trigger a Present

Confidence: medium.

Hover, press, and focus state are runtime visual states owned by the adapter.
For pointer events that produce no product message, the viewer may not recompute
`currentScene` immediately. Some visible feedback could therefore wait until a
later product-changing event or tick.

Expected symptom: click actions eventually update product state, but press/hover
feedback feels absent or late.

## Architecture Assessment

The architecture is clean at the macro level:

- Product state remains in the sample's pure model/update/view shape.
- Native windowing and OpenGL presentation stay in `SkiaViewer`.
- Retained routing and control runtime state live in `Controls.Elmish`.
- Evidence and persistence stay at the sample app edge.

The weakness is accumulated latency machinery across several layers. Input
queueing, frame pacing, retained diffing, pointer coalescing, runtime visual
state, present-buffer fill, and responsiveness evidence were added as separate
fixes. Each layer is defensible, but together they make the true
input-to-visible path hard to reason about. The comments describe intended
behavior well; the live path still needs measured proof that the layers compose
without hidden queue age or stale pointer state.

## Measurement Machinery Risk

The existing SecondAntShowcase live app likely is not slowed by the current
responsiveness command. The sample host uses `OnFrameMetrics = ignore`, and the
`responsiveness` subcommand runs deterministic `ControlsElmish.Perf.runScript`
substitute evidence rather than the live window path.

However, future live measurement can become part of the problem if it writes
JSON, Markdown, screenshots, readbacks, or summary files from the hot path.
Live measurement should record lightweight timestamps and small in-memory facts
during interaction, then flush artifacts after the run.

## Recommended Measurements

Add or enable a live runner that records these timestamps for every
representative interaction:

- native callback receipt
- enqueue complete
- queue drain start and end
- product routing start and end
- product update start and end
- scene recompute start and end
- retained step start and end
- draw start and end
- `surface.Snapshot()` start and end
- `context.Flush()` start and end
- `window.SwapBuffers()` start and end
- presented-frame id or missing-presentation-boundary reason

The first acceptance target should be diagnostic, not corrective: prove whether
the 500 ms is queue age, update/view work, retained diff/layout/text work, paint,
snapshot, flush, swap, or missing presentation timing.

## First Fix Candidates

1. Split live present timing so `surface.Snapshot()`, `context.Flush()`, and
   `SwapBuffers()` are measured separately.
2. Preserve input order across press/move/release when draining the viewer queue;
   do not process coalesced movement after later discrete release events.
3. Avoid double coalescing. Keep one owner for continuous pointer movement, and
   ensure value-changing drags process the latest move while the press is still
   active.
4. Fold all product messages from one input before recomputing `currentScene`.
5. Ensure runtime-only hover/press/focus changes mark the scene dirty and request
   a present even when no product message is emitted.

## Conclusion

A 500 ms lag is realistic in this architecture only if the single live
UI/render thread is being blocked or if queued input is aging behind expensive
frames. The current queueing and coalescing design can explain sluggishness and
drag trailing, but not half-second delay by itself. The next step should be a
live input-to-visible runner that separates queue delay from update, retained
render, paint, snapshot, flush, and swap durations.
