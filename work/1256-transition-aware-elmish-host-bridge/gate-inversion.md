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
