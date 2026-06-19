# Contract: SecondAntShowcase Responsiveness Evidence

## CLI Surface

The sample exposes responsiveness review through the existing subcommand:

```text
SecondAntShowcase responsiveness --script representative --theme <light|dark> --out <dir> [--page <page-id> | --all-interactive] [--require-live] [--json]
```

Required behavior:

- `--page <page-id>` keeps the current focused-page workflow.
- `--all-interactive` is additive and enumerates every interactive family in
  `SecondAntShowcase.Core.InteractionContracts.all`.
- `--require-live` requires a visible measured presentation boundary. If that boundary is
  unavailable, the command writes blocked/environment-limited artifacts and exits non-zero.
- `--json` prints a compact machine-readable pointer to the summary artifact.

Exit codes:

- `0`: accepted measured responsiveness evidence was written.
- `2`: invalid arguments.
- `3`: output write failure.
- `4`: live evidence unavailable, incomplete, blocked, or environment-limited.
- `5`: measured evidence ran but failed an acceptance budget.

## Artifact Layout

The command writes a run directory:

```text
<out>/<run-id>/
├── records.jsonl
├── summary.json
├── summary.md
└── environment.md
```

Paths in `summary.json` must be relative to the run directory unless they point outside the
run root.

## JSONL Record Shape

Each line in `records.jsonl` is one responsiveness evidence record. It must contain the
stable viewer latency fields and the sample review fields below:

```json
{
  "recordId": "resp-...-000001",
  "runId": "resp-...",
  "inputSequenceId": 1,
  "inputKind": "pointer-discrete",
  "inputName": "primary-click",
  "page": "buttons",
  "controlGroup": "button-click",
  "controlIds": ["button", "icon-button"],
  "actionType": "click",
  "expectedVisibleResult": "pressed state and counter update",
  "observedVisibleResult": "pressed state and counter update",
  "receiptTimestamp": "2026-06-19T18:30:00Z",
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
    "viewDurationMs": 0.0,
    "retainedStepDurationMs": 1.2,
    "layoutDurationMs": 0.0,
    "textDurationMs": 0.0,
    "paintDurationMs": 3.0,
    "presentDurationMs": 8.3,
    "totalInputToVisibleMs": 14.4
  },
  "dirtyRegion": {
    "dirtyRectCount": 1,
    "dirtyArea": 6400,
    "repaintedNodeCount": 3,
    "status": "measured"
  },
  "longFrame": false,
  "acceptanceStatus": "accepted",
  "diagnostics": []
}
```

## Summary Shape

`summary.json` must include:

- `runId`, `scope`, `overallReadiness`, `startedUtc`, `completedUtc`, `recordsPath`.
- `budgets` with `inputToVisibleP95Ms = 100` and `inputToVisibleMaxMs = 150` for this
  feature, plus any lower-level receipt/long-frame thresholds used by the viewer.
- `groups` by page, input kind, and control family, including `count`, `p50Ms`, `p95Ms`,
  `maxMs`, `longFrameCount`, and `readiness`.
- `coverage` with `requiredInteractiveFamilies`, `acceptedInteractiveFamilies`,
  `displayOnlyExclusions`, and `missingInteractiveFamilies`.
- `firstFailedBudget`, `slowestInteractions`, `environmentLimitations`, and `diagnostics`.

## Acceptance Rules

- A record is accepted only when `environmentStatus = "measured"`,
  `visibleResponse = "presented-frame"`, `acceptanceStatus = "accepted"`, and
  `phaseTiming.totalInputToVisibleMs` is present.
- No accepted record may exceed 150 ms.
- At least 95% of accepted representative pointer actions must be at or below 100 ms.
- Every value-changing drag action must show accepted continuous visible feedback.
- Every interactive family must have accepted evidence or the summary must be non-accepted.
- Display-only exclusions must include a documented reason and must not be counted as failed
  interactions.
- Any missing visible desktop session, missing presentation boundary, low precision timestamp,
  non-monotonic timestamp, write failure, or headless substitute keeps the summary
  non-accepted.
