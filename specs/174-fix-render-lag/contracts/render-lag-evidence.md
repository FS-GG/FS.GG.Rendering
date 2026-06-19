# Contract: Render Lag Evidence

## Artifact Layout

Feature evidence is written under:

```text
specs/174-fix-render-lag/readiness/
|-- render-lag/
|   |-- baseline-2026-06-19.md
|   |-- optimized-<run-id>/
|   |   |-- phase-records.jsonl
|   |   |-- summary.json
|   |   |-- summary.md
|   |   `-- trace.log
|-- responsiveness/
|   `-- <run-id>/
|       |-- records.jsonl
|       |-- summary.json
|       |-- summary.md
|       `-- environment.md
|-- visual-parity/
`-- validation-summary.md
```

The existing Feature 173 responsiveness layout is reused for live runner output. Feature 174 render-lag artifacts may add scenario-specific phase records and baseline comparisons.

## Phase Record JSONL Shape

Each line in `phase-records.jsonl` describes one measured or classified frame:

```json
{
  "runId": "lag-20260620-120000",
  "scenarioId": "button-click",
  "frameIndex": 2,
  "environmentStatus": "measured",
  "inputHandlingMs": 1.2,
  "modelUpdateMs": 0.5,
  "framePreparationMs": 18.4,
  "layoutMs": 2.0,
  "textMs": 0.1,
  "retainedStepMs": 12.0,
  "paintMs": 5.7,
  "presentationMs": 8.0,
  "totalInputToVisibleMs": 43.8,
  "dominantPhase": "frame-preparation",
  "metadataVisitedNodeCount": 42,
  "baselineNodeCount": 900,
  "fallbackCount": 0,
  "diagnostics": []
}
```

## Stable Tokens

`scenarioId`:

- `button-click`
- `page-change`

`environmentStatus`:

- `measured`
- `environment-limited`
- `failed`
- `not-run`

`dominantPhase`:

- `input-handling`
- `model-update`
- `frame-preparation`
- `paint`
- `presentation`
- `unknown`

## Summary Requirements

`summary.json` includes:

- run id, timestamp, git branch, and scenario list
- baseline profile id (`2026-06-19`)
- optimized profile id
- per-scenario median, p95, and max input-to-visible values
- per-scenario largest non-paint preparation contribution before and after
- computed preparation reduction percentage
- first-frame preparation before and after when measured
- parity status and linked parity artifacts
- environment limitations and diagnostics

`summary.md` is the reviewer entry point and must restate:

- pass/fail/environment-limited status for each scenario
- the first failed budget when any scenario fails
- slowest measured frames
- dominant phase after optimization
- caveats for paint/presentation costs that remain after preparation is fixed
- links to JSON, trace logs, responsiveness summaries, and parity evidence

## Acceptance Rules

- Accepted performance evidence requires measured live responsiveness for both required scenarios in a supported desktop session.
- Button activation must satisfy median <= 150 ms and p95 <= 250 ms.
- Page navigation must satisfy median <= 250 ms and p95 <= 500 ms.
- The largest non-paint preparation contribution must be reduced by >= 80% from the 2026-06-19 baseline.
- First-frame preparation must be reduced by >= 50% where the same bottleneck exists.
- Parity evidence must pass.
- Environment-limited or substitute evidence cannot be accepted.

## Fail-Closed Rules

- Missing live presentation boundary produces `environment-limited`.
- Missing phase attribution produces `failed` or `blocked`; it is never treated as a pass.
- Missing or stale baseline values block percentage-improvement claims.
- Incomplete or inconsistent artifacts make the run `failed`.
