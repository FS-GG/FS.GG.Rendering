# Contract: Live Evidence Artifacts

## Artifact Layout

Each run writes a run-id child directory:

```text
<out>/<run-id>/
|-- records.jsonl
|-- summary.json
|-- summary.md
`-- environment.md
```

Optional logs may be written in the same directory, but readiness must be computable from `summary.json` and `records.jsonl`.

## JSONL Record Shape

Each line in `records.jsonl` is one representative action record.

```json
{
  "recordId": "resp-20260619-193000-a1b2c3-000001",
  "runId": "resp-20260619-193000-a1b2c3",
  "inputSequenceId": 1,
  "inputKind": "pointer-discrete",
  "inputName": "primary-click",
  "page": "buttons",
  "controlGroup": "button-click",
  "controlFamily": "button-click",
  "controlIds": ["button", "icon-button"],
  "actionType": "click",
  "expectedVisibleResult": "status area and command counter change",
  "observedVisibleResult": "status area and command counter changed",
  "receiptTimestamp": "2026-06-19T17:30:00Z",
  "queueDepthAtReceipt": 0,
  "queueDepthAtDrain": 1,
  "coalescedMovementCount": 0,
  "productMessageCount": 1,
  "productStateChanged": true,
  "runtimeStateChanged": true,
  "visibleResponse": "presented-frame",
  "presentedFrameId": 42,
  "environmentStatus": "measured",
  "phaseTiming": {
    "receiptDurationMs": 0.2,
    "queueDelayMs": 1.0,
    "routingDurationMs": 0.4,
    "updateDurationMs": 0.3,
    "viewDurationMs": 0.1,
    "retainedStepDurationMs": 2.0,
    "layoutDurationMs": 0.6,
    "textDurationMs": 0.1,
    "paintDurationMs": 4.1,
    "presentDurationMs": 6.2,
    "totalInputToVisibleMs": 14.9
  },
  "dirtyRegion": {
    "dirtyRectCount": 1,
    "dirtyArea": 6400,
    "repaintedNodeCount": 3,
    "status": "measured"
  },
  "dragContinuity": null,
  "longFrame": false,
  "acceptanceStatus": "accepted",
  "diagnostics": []
}
```

## Drag Continuity Shape

Value-changing drag records include `dragContinuity`:

```json
{
  "sampleCount": 8,
  "visibleFeedbackSamples": 8,
  "maxSampleGapMs": 24.0,
  "delayedCatchUp": false,
  "classification": "continuous"
}
```

Stable `classification` tokens:

- `continuous`
- `delayed-catch-up`
- `insufficient-samples`
- `missing-boundary`
- `failed`

## Stable Tokens

`visibleResponse`:

- `presented-frame`
- `no-visible-response`
- `failed`
- `environment-limited`
- `not-run`

`environmentStatus`:

- `measured`
- `missing-boundary`
- `low-precision-timestamp`
- `non-monotonic-timestamp`
- `no-visible-surface`
- `headless-substitute`
- `write-failed`
- `failed`

`acceptanceStatus`:

- `accepted`
- `rejected`
- `blocked`
- `environment-limited`
- `excluded`
- `failed`

## Summary JSON Shape

```json
{
  "runId": "resp-20260619-193000-a1b2c3",
  "scope": "second-antshowcase/all-interactive/light",
  "overallReadiness": "accepted",
  "startedUtc": "2026-06-19T17:30:00Z",
  "completedUtc": "2026-06-19T17:31:00Z",
  "recordsPath": "records.jsonl",
  "budgets": {
    "inputReceiptP95Ms": 4,
    "inputReceiptMaxMs": 16,
    "inputToVisibleP95Ms": 100,
    "inputToVisibleMaxMs": 150,
    "longFrameThresholdMs": 50
  },
  "coverage": {
    "requiredInteractiveFamilies": ["button-click"],
    "acceptedInteractiveFamilies": ["button-click"],
    "rejectedInteractiveFamilies": [],
    "blockedInteractiveFamilies": [],
    "displayOnlyExclusions": [
      { "controlId": "text-block", "reason": "static typography sample" }
    ],
    "missingInteractiveFamilies": []
  },
  "groups": [
    {
      "page": "buttons",
      "inputKind": "pointer-discrete",
      "controlGroup": "button-click",
      "count": 1,
      "p50Ms": 14.9,
      "p95Ms": 14.9,
      "maxMs": 14.9,
      "longFrameCount": 0,
      "readiness": "accepted"
    }
  ],
  "firstFailedBudget": null,
  "slowestInteractions": [
    {
      "recordId": "resp-20260619-193000-a1b2c3-000001",
      "inputSequenceId": 1,
      "totalInputToVisibleMs": 14.9,
      "dominantPhase": "present"
    }
  ],
  "dragContinuity": [
    {
      "controlFamily": "slider-rating",
      "classification": "continuous"
    }
  ],
  "environmentLimitations": [],
  "artifactWriteStatus": "complete",
  "diagnostics": []
}
```

## Summary Markdown Requirements

`summary.md` is the reviewer entry point and includes:

- run id, scope, theme, and overall readiness
- budget table with measured p50, p95, and max values
- first failed budget when present
- five slowest interactions when measured data exists
- drag continuity classifications
- interactive coverage counts and missing families
- display-only exclusions with reasons
- environment limitations and missing timing boundaries
- links to `summary.json`, `records.jsonl`, and `environment.md`
- caveats for substitute, skipped, timed-out, blocked, failed, or manual-pending evidence

## Agreement Rules

- `summary.json` and `summary.md` agree on readiness, budgets, coverage counts, first failed budget, slowest interactions, drag classifications, limitations, and artifact-write status.
- `recordsPath` is relative to the run directory.
- Incomplete or inconsistent artifacts make the run `failed`.
