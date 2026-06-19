# Contract: Latency Records

## Files

A responsiveness run writes one JSONL record file and may write matching Markdown diagnostics:

```text
<run-root>/
|-- records.jsonl
|-- summary.json
|-- summary.md
`-- environment.md
```

Each JSONL line is one latency record. Additional host logs may be collected, but readiness must be computable from `records.jsonl` and `summary.json`.

## JSONL Shape

```json
{
  "recordId": "resp-20260619-101112-000004",
  "runId": "resp-20260619-101112",
  "inputSequenceId": 4,
  "inputKind": "pointer-discrete",
  "inputName": "primary-click",
  "page": "buttons",
  "controlGroup": "button",
  "receiptTimestamp": "2026-06-19T08:11:12.345678Z",
  "queueDepthAtReceipt": 2,
  "queueDepthAtDrain": 3,
  "coalescedMovementCount": 0,
  "productMessageCount": 1,
  "productStateChanged": true,
  "runtimeStateChanged": true,
  "visibleResponse": "presented-frame",
  "presentedFrameId": 42,
  "environmentStatus": "measured",
  "phaseTiming": {
    "receiptDurationMs": 0.42,
    "queueDelayMs": 4.10,
    "routingDurationMs": 1.25,
    "updateDurationMs": 0.33,
    "viewDurationMs": 0.08,
    "retainedStepDurationMs": 18.40,
    "layoutDurationMs": 5.10,
    "textDurationMs": 2.40,
    "paintDurationMs": 8.00,
    "presentDurationMs": 3.20,
    "totalInputToVisibleMs": 36.11
  },
  "dirtyRegion": {
    "dirtyRectCount": 2,
    "dirtyArea": 42000,
    "repaintedNodeCount": 5,
    "status": "measured"
  },
  "longFrame": false,
  "diagnostics": []
}
```

## Stable Tokens

`inputKind`:

- `pointer-move`
- `pointer-discrete`
- `key-down`
- `key-up`
- `wheel`
- `resize`
- `tick`
- `lifecycle`

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

`phaseTiming` values may be null only when accompanied by a non-`measured` boundary status and diagnostic reason.

## Required Fields

Every completed discrete pointer/key latency record includes:

- run id
- input sequence id
- input kind
- receipt timestamp or declared timestamp limitation
- queue depth at receipt and drain
- queue delay
- routing duration
- update duration
- recomposition/retained-step duration when state changed
- paint/present duration when available
- total input-to-visible duration or explicit limitation
- product state changed flag
- visible response classification
- environment status

Every coalesced movement record includes:

- latest movement sequence id
- coalesced movement count
- queue depth
- routing/update/recomposition facts if work ran
- no-visible-response classification when movement produced no visible change

## Long-Frame Rules

- Any recomposition, paint, present, or combined frame segment over 50 ms sets `longFrame = true`.
- Long-frame facts are counted even when input-to-present timing is unavailable.
- A long frame cannot be hidden by a fast `routingDurationMs`.

## Failure Rules

- Diagnostic write failure creates an infrastructure diagnostic and non-green summary status.
- Routing/update/render/present exceptions create failed records with stage, message, and pending input count.
- Missing visible presentation support creates `environment-limited` or `missing-boundary`, never a zero-duration success.

## Markdown Companion

If per-record Markdown is written, it must link to the JSONL record id and summarize:

- input identity and page/control group
- queue delay and total latency
- dominant phase
- long-frame facts
- environment limitations
- failure diagnostics
