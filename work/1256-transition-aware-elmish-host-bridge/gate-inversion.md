# Production performance gate inversion

- Date: 2026-08-22
- Fixture: `tests/Elmish.Tests/TransitionHostBrowser/measure.mjs`
- Workload: `workspace-transition-production`
- Workload SHA-256: `a49fc53e890dc93961e68d821c6b680a7f10f1f8d1aadd2cc96787cdbdd73acb`

The load-bearing maximum renderer-task comparison was temporarily changed from `maximum <= 16` to
`maximum <= 0`. No workload, scale, p95, p99, dropped-frame, browser, or compositor condition changed.

Command:

```text
npm run measure -- --out /tmp/fsgg-1256-gate-inversion.json
```

Observed exit: `1` (expected red).

```json
{"result":"fail","maximum":4.807,"p95":2.685,"p99":3.172,"droppedFrames":0,"samples":800}
```

The comparison was then restored exactly to `maximum <= 16`.

Command:

```text
npm run measure -- --out /tmp/fsgg-1256-gate-restored.json
```

Observed exit: `0` (expected green).

```json
{"result":"pass","maximum":3.715,"p95":2.737,"p99":3.352,"droppedFrames":0,"samples":874}
```

The committed fixture retains `max <= 16`, `p95 <= 16`, `p99 <= 32`, `droppedFrames = 0`, and a
positive live-compositor trace requirement. The mutation is not present in source.

## Frame-evidence fail-closed inversion

`npm run test:trace-inversion` executes the same `summarizeTrace` function used by the production measurement. A measured-run trace with an `AnimationFrame` event whose duration is missing, and a second trace whose duration is corrupt, both throw `zero usable AnimationFrame duration samples`; a valid 26 ms duration produces one sample and one dropped frame. The restored production measurement then retained positive usable frame samples in every one of 20 measured runs, with drop count zero. No max, p95, p99, or dropped-frame threshold changed.

The round-2 repair also removes the aggregate-compositor escape: a trace with a valid frame duration but
no `DrawFrame`, `CompositeLayers`, or `AnimationFrame::Presentation` event throws `zero
compositor/presentation samples`. The F# acceptance independently enumerates exactly 20 `traceRuns` and
requires every run to report positive frame and compositor counts plus zero drops.

## Independent-run and row-scale control

Hosted exact-head run 32551122887 bound both checkout and the artifact to commit
`6f84678c61ebf56e9d382e8fdd103549cd8cb94e`, but failed the unchanged limits at max 35.958 ms and one
dropped frame. The retained module-global host/ledger and React view were a plausible confounder, not a
proven sole cause. The repair now resets the host, ledger, counters, controlled input, and React view to a
mounted Editor outside each warmup/trace, then fails if the reset is pending, not Editor, or has any ledger
entry. Every run must finish with the same bounded ledger size.

The workload remains exactly 1,200 rendered workspace-row layout units. Each row is one visible
`data-workspace-row` element whose index/score and accessible label retain the row semantics; removing three
redundant nested spans per row avoids silently turning the declared 1,200-row scale into 4,800 DOM nodes.
Neither the workload digest/scale nor max/p95/p99/drop thresholds changed.

## Repair-phase row contract and layout-cost control

The successor repair removes only redundant per-row formatting contexts: an empty semantic row no longer
creates its own flex layout, runs an inapplicable `justify-content`, or establishes an individual content
containment/paint boundary. The grid, all 1,200 row elements, their `data-index`/`data-score` values and
accessible labels, the row background, and the fixed six-column/20 px rendered geometry remain unchanged.
The production measurement now emits and gates a `semanticRows` receipt outside the trace window so the
control itself does not inflate the timed workload.

The new row-contract gate was inverted by changing only the production subject's `data-score` value from
`(index * 17) % 101` to `((index * 17) % 101) + 1`, rebuilding the production bundle, and running the
unchanged measurement command. It exited 1 with `count:1200`, `semantics:false`, `visible:true`,
`columns:6`, `distinctColumnPositions:6`, and `widthSpread:0`, proving that retaining all elements while
corrupting their score semantics is refused. Restoring the subject produced a passing receipt with all
six fields valid. This is a subject mutation, not a predicate inversion.

A second subject mutation added only `visibility: hidden` to `.workspace-row`. Occupied geometry remained
1,200 rows, 20 px, and six columns, but the unchanged gate exited 1 with `semantics:true` and
`visible:false`. The restored subject checks CSS visibility, display, content visibility, opacity, and any
`aria-hidden` ancestor in addition to geometry, so a layout box cannot masquerade as a visible accessible
row.

The first exact-head hosted successor run showed that the remaining actionable failure included allocation
pressure rather than stable row-layout cost: 18 journeys stayed near 10--13 ms, while one journey spent
18.109 ms in V8 incremental marking and dropped its only frame. The production fixture now builds the
immutable React element descriptors for Editor, Plan, and Simulate once, outside every journey. React still
reconciles the same
120/600/1,200 keyed elements into the same visible DOM rows at each target; only repeated construction of
identical element objects, score values, and accessible-label strings is removed. Three restored local
20-journey measurements passed independently at max 6.117/6.215/6.287 ms with zero drops, while every
run retained positive frame/compositor evidence and the 1,200-row semantic/geometry receipt.

## Exact-head release-base control

Hosted exact-head run 32554210957 passed the performance contract but exposed a release-gate conflict:
the gate checks out the immutable PR head, while version coherence still compared only `HEAD~1..HEAD`.
Because the 0.27.0 bump precedes the final evidence commits, that comparison falsely classified the release
as bump-less and demanded tags that cannot exist before merge. The gate now supplies the immutable PR base
SHA to both the script and its independent Package.Tests mirror; push/main retains the `HEAD~1` fallback.
The value must be a full lowercase SHA and resolve as a commit or the guard fails closed.

The multi-commit subject control ran the unchanged Feature209 test list with the immediate parent
`94fcf1117f694318fe0b9aee1421e1a493715165` as its explicit base. It exited 1 with `pin-no-tag` and
`pkg-no-template-tag`, reproducing the hosted false classification. Binding the actual PR base
`1154d053d316e27e39e9aa60da5d0df8f87a1270` made all 24 focused Feature209 tests pass and emitted the
three ordered 0.27.0 `RELEASE-PENDING` tags. A malformed explicit base independently exits 2 with
`GUARD ERROR`; the repair cannot gain a waiver from missing or ambiguous ancestry.

## Bounded production row commits

After release-base repair, exact-head hosted run 32554829089 retained 19 ordinary journeys at
10.504--11.189 ms but run 0 spent 23.890 ms in one compositor `Commit`, making its enclosing renderer
task 34.800 ms. Stable layout/style/prepaint/paint/layerize work in that task totaled about 10.7 ms; the
remaining vulnerability was the atomic insertion of 1,080 rows when Simulate replaced Editor.

The production fixture now materializes the same cached target descriptors in frame-aligned batches of
120. It does not resolve the production journey until all 1,200 Simulate rows are committed and the exact
row semantics/visibility/geometry receipt passes. The host still issues and acknowledges exactly one
Simulate presentation; acknowledgement is deferred until all 1,200 rows for that token are committed, so
the state machine remains pending, `aria-busy` remains true, the live status remains loading, and unsafe
input suppression remains active throughout every partial batch. The measurement observes at least one
such intermediate state in every journey and fails if pending/loading clears early. Only the renderer's
DOM materialization is divided into bounded concurrent React commits. Three restored 20-journey runs
passed at max 3.778/3.914/5.090 ms, p95 at most 2.122 ms, p99 at
most 2.674 ms, zero drops, and 220 positive frame/compositor observations each. No row, response,
replacement, input attempt, threshold, or trace journey was removed.

The pending-state control was inverted by restoring only the premature `acknowledge(view.token)` layout
effect. Each partial batch recorded the violation as data while continuing to the complete 1,200-row DOM;
the unchanged measurement exited 1 promptly on measured journey 0 with `rows:1200`,
`stagedPendingChecks:10`, and `stagedPendingValid:false`. Recording instead of throwing is load-bearing:
the first form of this control threw inside React, stopped later batches, and hung on an unresolved journey
promise rather than emitting a red verdict. The restored subject passes with
`stagedRowsRemainPending:true` across all 20 journeys.

## Independent heap isolation

Exact-head hosted run 32556145715 retained the full staged-row and semantic contract, but its fourteenth
measured journey failed at max 26.344 ms and one dropped frame. The raw trace binds 20.714 ms of that task
to `V8.GCIncrementalMarkingStart`: repeated harness resets had detached the previous journey's 1,200 DOM
rows, and the shared Chromium renderer eventually scheduled collection of that cumulative garbage inside
a later trace. This was not work owned by that independently reset journey.

The measurement now synchronously invokes CDP `HeapProfiler.collectGarbage` after each reset and before
tracing starts. It counts each successful command and fails unless the tracing warmup plus all twenty
measured journeys were isolated this way. The declared workload remains twenty production journeys with
1,200 visible semantic rows each; no trace, row, threshold, or in-trace event is excluded. Removing the
pre-trace collection call is the subject inversion demonstrated by hosted run 32556145715: the unchanged
hard-max/drop gate exits 1 on accumulated incremental marking. With the collection restored, three local
20-journey runs passed at max 3.565/3.943/7.035 ms, p95 at most 2.662 ms, p99 at most 3.599 ms, and zero
drops, while retaining all twenty raw traces and reporting twenty-one successful pre-trace collections.
