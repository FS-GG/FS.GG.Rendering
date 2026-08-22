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
