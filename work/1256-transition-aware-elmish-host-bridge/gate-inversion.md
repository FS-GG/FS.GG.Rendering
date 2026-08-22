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
