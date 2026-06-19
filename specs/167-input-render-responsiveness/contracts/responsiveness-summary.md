# Contract: Responsiveness Summary

## Summary Files

Each run writes:

```text
<run-root>/
|-- summary.md
|-- summary.json
|-- records.jsonl
`-- environment.md
```

`summary.md` is the reviewer entry point. `summary.json` is the machine-readable readiness result. `records.jsonl` contains the per-input evidence.

## Summary JSON Shape

```json
{
  "runId": "resp-20260619-101112",
  "scope": "antshowcase/buttons/light",
  "overallReadiness": "blocked",
  "startedUtc": "2026-06-19T08:11:12Z",
  "completedUtc": "2026-06-19T08:11:45Z",
  "recordsPath": "records.jsonl",
  "budgets": {
    "inputReceiptP95Ms": 4,
    "inputReceiptMaxMs": 16,
    "inputToVisibleP95Ms": 50,
    "longFrameThresholdMs": 50
  },
  "firstFailedBudget": {
    "kind": "input-to-visible-p95",
    "scope": "buttons",
    "inputKind": "pointer-discrete",
    "measuredMs": 72.4,
    "budgetMs": 50
  },
  "groups": [
    {
      "page": "buttons",
      "inputKind": "pointer-discrete",
      "controlGroup": "button",
      "count": 20,
      "p50Ms": 34.2,
      "p95Ms": 72.4,
      "maxMs": 88.0,
      "longFrameCount": 3,
      "readiness": "blocked"
    }
  ],
  "slowestInteractions": [
    {
      "recordId": "resp-20260619-101112-000004",
      "inputSequenceId": 4,
      "totalInputToVisibleMs": 88.0,
      "dominantPhase": "retained-step"
    }
  ],
  "environmentLimitations": [],
  "diagnostics": []
}
```

## Readiness Tokens

- `accepted`: all required records and boundaries exist, budgets pass, and no blocking long-frame or diagnostics failure exists.
- `blocked`: a measured budget fails, a required long-frame rule fails, or processing/render/present diagnostics fail.
- `incomplete`: required inputs or summaries were not produced.
- `environment-limited`: required live timing or presentation cannot be measured in the current host.
- `failed`: infrastructure failure, invalid output, or unhandled exception prevents classification.

## Percentile Rules

- Summaries report p50, p95, and max by page/screen, input kind, and control group when known.
- Percentiles are computed from measured `totalInputToVisibleMs` for visible-response records.
- No-visible-response records are counted separately and included in record totals but not used as successful visible latency unless the checked script expected no visible change.
- Missing or environment-limited timing does not become zero in percentile calculations.

## Budget Rules

Default budgets:

- input receipt p95 <= 4 ms
- input receipt max <= 16 ms
- input-to-visible p95 <= 50 ms
- long-frame threshold = 50 ms

Accepted readiness requires:

- required pointer activation records present
- required keyboard activation records present
- required coalesced-movement evidence present for burst checks
- no required timing boundary silently missing
- p95/max values within budget
- long-frame counts surfaced and within policy
- environment limitations either absent or explicitly accepted as substitute evidence with non-accepted readiness clearly stated

## Markdown Requirements

`summary.md` includes:

- run id, checked scope, and overall readiness
- budget table with measured p50/p95/max values
- first failed budget
- long-frame counts
- three slowest interactions
- environment limitations and missing timing boundaries
- links to `summary.json`, `records.jsonl`, and host/environment notes
- caveats about synthetic or headless substitute evidence

The first failed budget and the three slowest interactions must be identifiable without opening `records.jsonl`.

## Agreement Rules

- `summary.md` and `summary.json` agree on overall readiness, first failed budget, long-frame counts, record count, and environment limitations.
- `summary.json` can be consumed by validation lanes without parsing Markdown.
- Invalid or partial summary output is a failed run.
