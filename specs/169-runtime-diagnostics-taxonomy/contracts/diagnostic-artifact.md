# Contract: Diagnostic Artifact

## Files

Feature/sample readiness runs write:

```text
<run-output>/
|-- diagnostics-summary.json
|-- diagnostics-summary.md
`-- diagnostics-records.jsonl
```

`diagnostics-summary.json` is the machine-readable summary. Markdown is a
reviewer view generated from the same summary. JSONL preserves individual
records when verbose evidence is requested.

## Summary JSON Shape

```json
{
  "schemaVersion": "runtime-diagnostics-v1",
  "runId": "feature169-fixture",
  "status": "blocked",
  "countsBySeverity": {
    "informational": 2,
    "warning": 2,
    "error": 1
  },
  "countsByCategory": {
    "environment": 1,
    "backend-cost": 1,
    "rendering-limitation": 1,
    "developer-action": 1,
    "readiness-blocker": 1
  },
  "blockerCount": 1,
  "unclassifiedCount": 0,
  "reviewRequiredCount": 0,
  "exceptionCount": 0,
  "artifactPaths": [
    "diagnostics-records.jsonl"
  ],
  "groups": [
    {
      "fingerprint": "skia-viewer:DamageScopedDecision:backend-cost:informational",
      "source": {
        "packageId": "FS.GG.UI.SkiaViewer",
        "subsystem": "opengl-host",
        "laneId": null,
        "sampleId": "ant-showcase"
      },
      "code": "DamageScopedDecision",
      "severity": "informational",
      "category": "backend-cost",
      "message": "Damage-scoped redraw used an offscreen fallback.",
      "action": "No action required unless the fallback appears in a performance-blocked lane.",
      "occurrenceCount": 100,
      "firstOccurrence": {
        "runId": "feature169-fixture",
        "timestampUtc": "2026-06-19T12:00:00Z",
        "outputPath": null,
        "details": {
          "frame": "1"
        }
      },
      "lastOccurrence": {
        "runId": "feature169-fixture",
        "timestampUtc": "2026-06-19T12:00:03Z",
        "outputPath": null,
        "details": {
          "frame": "100"
        }
      },
      "exampleIds": [
        "diag-001",
        "diag-100"
      ]
    }
  ],
  "exceptions": [],
  "artifactWriteDiagnostics": []
}
```

## Token Values

Severity tokens:

- `informational`
- `warning`
- `error`

Category tokens:

- `environment`
- `backend-cost`
- `rendering-limitation`
- `readiness-blocker`
- `developer-action`

Status tokens:

- `accepted`
- `blocked`
- `review-required`
- `environment-limited`

## JSONL Record Shape

Each line contains one `RuntimeDiagnostic`:

```json
{
  "id": "diag-001",
  "source": {
    "packageId": "FS.GG.UI.Controls",
    "subsystem": "control-runtime",
    "laneId": null,
    "sampleId": "controls-gallery"
  },
  "code": "OffscreenComposition",
  "severity": "informational",
  "category": "backend-cost",
  "message": "Offscreen composition allocated a separate layer.",
  "action": "Review only if this appears in a performance-blocked scenario.",
  "context": {
    "runId": "sample-run",
    "timestampUtc": "2026-06-19T12:00:00Z",
    "outputPath": "readiness/diagnostics-records.jsonl",
    "details": {
      "controlId": "panel-1"
    }
  },
  "fingerprint": "controls:OffscreenComposition:backend-cost:informational"
}
```

## Markdown Requirements

The Markdown summary includes:

- run id and diagnostic status
- counts by severity and category
- blocker count and first blocker source
- unclassified/review-required count
- accepted exceptions
- artifact paths
- grouped diagnostics table with occurrence count
- artifact write warnings

Markdown must not require parsing to determine readiness; JSON is authoritative.

## Artifact Write Failure

If a writer cannot create JSON, Markdown, or JSONL:

- the in-memory summary remains available;
- a new `developer-action` warning is added with source `diagnostic-artifact`;
- console output reports the failure;
- readiness evaluation treats the warning according to the readiness-evaluation
  contract.
