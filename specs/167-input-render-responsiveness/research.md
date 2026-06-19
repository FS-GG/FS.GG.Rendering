# Research: Input/Render Responsiveness

## Decision: Put the input queue and frame scheduler in `SkiaViewer`

**Rationale**: The lag path starts in native pointer/key callbacks and the viewer currently owns the synchronous `host.MapPointer`/`host.MapKey` -> `host.Update` -> `host.View` path. `SkiaViewer` also owns window lifecycle, resize, present cadence, input dispatch status, and environment diagnostics. Keeping queue ownership there avoids leaking host-specific scheduling into Controls or generated products.

**Alternatives considered**: Putting the queue in `Controls.Elmish` was rejected because `GeneratedAppHost` and lower viewer hosts also need a consistent input receipt rule, and Controls should not own native callback timing. Putting the queue in `Host.OpenGl` alone was rejected because message folding and dirty scene recomposition need the viewer host model and public diagnostics vocabulary.

## Decision: Keep product-facing host APIs source-compatible

**Rationale**: The feature is about when heavy work runs, not which product messages are produced. Existing `InteractiveViewerHost`, `InteractiveAppHost`, `GeneratedAppHost`, AntShowcase, and generated products should keep their pure `Init`/`Update`/`View` contracts. Additive diagnostics/options are acceptable; mandatory product rewrites are out of scope.

**Alternatives considered**: Replacing product hosts with a new scheduler-aware API was rejected because it would churn generated products and samples without proving a better scheduling boundary. Requiring product code to batch messages was rejected because message folding is a viewer responsibility.

## Decision: Expose additive diagnostics through viewer/adapter surfaces

**Rationale**: Maintainers need stable fields for input receipt duration, queue delay, routing, update, recomposition, paint/present, long frames, queue depth, coalesced movement, dirty state, and environment status. These diagnostics are reusable framework contract data, not AntShowcase-specific logs.

**Alternatives considered**: Logging only strings through `ViewerDiagnosticEvent` was rejected because readiness summaries need machine-readable percentiles and first failed budgets. Adding only `FrameMetrics` fields was rejected because existing frame metrics do not correlate one native input to the next visible response.

## Decision: Use collector-neutral .NET diagnostics plus JSONL evidence

**Rationale**: `ActivitySource`, `System.Diagnostics.Metrics`, `Stopwatch`, and JSONL records provide enough observability without coupling core packages to OpenTelemetry. JSONL gives deterministic evidence files that validation lanes and reviewers can consume directly.

**Alternatives considered**: Adding an OpenTelemetry dependency was rejected because it is unnecessary for the first contract and would add maintenance/version ownership. Console-only diagnostics were rejected because they are hard to aggregate and compare.

## Decision: Preserve deterministic `Perf.runScript`; add live/benchmark timing separately

**Rationale**: Existing `ControlsElmish.Perf.runScript` count/bool fields are byte-stable and useful for semantic regressions. Wall-clock timing would make those goldens flaky. Responsiveness timings should be produced by a live or explicitly benchmark-oriented diagnostics path, while deterministic tests still assert shape, ordering, counts, and compatibility.

**Alternatives considered**: Adding wall-clock durations to `Perf.runScript` was rejected because it would undermine deterministic tests. Creating a completely separate fake scheduler was rejected because it could drift from live behavior; tests should share the same pure queue/drain policy where possible.

## Decision: Coalesce continuous movement at queue drain, never coalesce discrete input

**Rationale**: Pointer moves are continuous and the latest position is what matters before a frame is processed. Press, release, click, key-down, key-up, wheel, resize, and lifecycle inputs are discrete and must preserve receipt order. Coalesced movement counts must be recorded so the responsiveness summary shows pressure rather than hiding it.

**Alternatives considered**: Processing every move was rejected because it recreates the backlog under high pointer sample rates. Dropping moves without a count was rejected because it hides input pressure. Reordering discrete input around moves was rejected because it risks behavioral regressions.

## Decision: Fold all product messages for an input before recomposition

**Rationale**: `InteractiveViewerHost.MapKey` and `MapPointer` can produce multiple messages. The current repeated `dispatchHostMsg` pattern can call `host.View` after each message. A single input should route, produce messages, fold all updates in order, mark dirty once, and participate in at most one recomposition before the next presented frame.

**Alternatives considered**: Recomputing after every product message was rejected because it is the current lag source. Coalescing product messages across discrete inputs was rejected because it can change observable product state and command ordering.

## Decision: Use a dirty-state/frame-drain model rather than immediate scene assignment

**Rationale**: The viewer needs to know whether product state, runtime focus/hover state, size, theme, or presentation facts changed before recomposing. A dirty flag with changed-region summary lets no-state-change input produce an explicit no-visible-response record and avoids unnecessary retained work.

**Alternatives considered**: Always recomposing after every drained batch was rejected because no-change inputs would still block the loop. Full damage narrowing was deferred because the first fix is the scheduling boundary; deeper render optimization remains follow-up work.

## Decision: Treat long-frame work as a readiness fact

**Rationale**: A run can have fast routing but slow presentation. Long recomposition/paint/present segments over 50 ms must be counted and shown in the same summary as input-to-visible latency so readiness cannot pass by measuring only handlers.

**Alternatives considered**: Reporting only p95 input latency was rejected because environment-limited runs or no-visible-response cases may hide frame work. Reporting long frames as warnings was rejected because a long frame can explain blocked user input.

## Decision: Environment-limited evidence fails closed

**Rationale**: Some CI/desktop hosts may lack a visible GL surface or precise presentation timestamps. The feature must say which boundary is unavailable and whether substitute evidence exists. An unavailable required boundary cannot silently produce accepted readiness.

**Alternatives considered**: Treating missing presentation timing as zero was rejected as false evidence. Skipping live evidence entirely was rejected because the feature's central value is end-to-end latency.

## Decision: AntShowcase is the first representative diagnostic target

**Rationale**: The retrospective measured the worst lag in AntShowcase and it already has a pure `Host.defaultHost`, deterministic scripts, and package-consuming app edge. It includes pointer activation and a keyboard baseline: Enter and Space on key-down map to a visible state-changing command, while key-up and unrelated keys remain non-activating unless focused controls consume them.

**Alternatives considered**: Building a new sample was rejected because it would not prove the reported problem. Using only a tiny synthetic screen was rejected because it could meet budgets without exercising the real retained/render costs that caused the lag.

## Decision: Defer renderer-thread separation

**Rationale**: Moving GL/Skia rendering to another thread has thread-affinity and context-ownership risk. The first architectural correction is to remove retained recomposition from native input callbacks and measure the result. If latency remains unacceptable, a later feature can design chunked rendering or thread separation with evidence.

**Alternatives considered**: A renderer-thread rewrite in this feature was rejected because it expands blast radius and could obscure whether the scheduler boundary alone solved the input backlog.
