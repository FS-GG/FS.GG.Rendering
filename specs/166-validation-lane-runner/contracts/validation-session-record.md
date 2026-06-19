# Contract: Validation Session Record

## Files

Each validation session writes:

```text
<run-root>/
|-- summary.md
|-- summary.json
`-- <lane-id>/
    |-- log.txt
    |-- result.json
    `-- diagnostics.md
```

`summary.md` is the reviewer entry point. `summary.json` is the machine-readable
record. Per-lane `result.json` files contain the same lane status fields as the
aggregate summary.

## Summary JSON Shape

```json
{
  "runId": "validation-20260619-123456-abc123",
  "policyVersion": "validation-lanes-v1",
  "overallReadiness": "blocked",
  "artifactRoot": "specs/166-validation-lane-runner/readiness/lanes/validation-20260619-123456-abc123",
  "startedUtc": "2026-06-19T10:34:56Z",
  "completedUtc": "2026-06-19T10:42:10Z",
  "firstBlockingRequiredLane": "controls",
  "lanes": [
    {
      "laneId": "controls",
      "readinessRole": "required",
      "status": "timed-out",
      "command": "dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore",
      "timeoutBudget": "00:15:00",
      "elapsed": "00:15:04",
      "lastActivityUtc": "2026-06-19T10:39:12Z",
      "lastActivityText": "last captured output line",
      "exitCode": null,
      "logPath": "controls/log.txt",
      "resultPath": "controls/result.json",
      "diagnosticsPath": "controls/diagnostics.md",
      "artifacts": ["controls/log.txt", "controls/result.json"],
      "reason": "lane exceeded timeout 00:15:00",
      "required": true
    }
  ],
  "caveats": [
    "aggregate-solution was not selected"
  ]
}
```

## Status Semantics

| Status | Meaning | Required readiness |
|--------|---------|--------------------|
| `passed` | Command completed successfully and evidence was written. | Can be ready. |
| `failed` | Command exited non-zero or reported validation failure. | Blocked. |
| `timed-out` | Total time budget expired. | Blocked. |
| `no-progress-timeout` | No visible activity before the no-progress budget expired. | Blocked. |
| `canceled` | Operator canceled the active or pending lane. | Blocked or incomplete. |
| `skipped` | Lane was intentionally skipped with reason. | Blocked unless accepted limitation is explicit. |
| `infrastructure-error` | Runner could not start, observe, or write required evidence. | Blocked. |
| `environment-limited` | Required environment was unavailable and explicitly recorded. | Blocked unless accepted limitation is recorded; still never hidden. |
| `not-run` | Requested or expected lane did not execute. | Incomplete or blocked. |

## Readiness Rules

- `ready`: every required lane is `passed`.
- `blocked`: any required lane is `failed`, `timed-out`,
  `no-progress-timeout`, `canceled`, `infrastructure-error`, or has an
  unaccepted environment limitation.
- `incomplete`: any required lane is `skipped` or `not-run` without enough
  evidence to classify as blocked.
- `environment-limited`: required validation could not run because of a declared
  environment limitation; this is not success.

Optional and informational lanes are included in summaries but never make an
unsuccessful required set ready.

## Markdown Requirements

`summary.md` includes:

- run id and overall readiness
- first blocking required lane
- table of required lanes with status, elapsed time, and evidence path
- separate table for optional/informational lanes
- aggregate lane status, if selected or omitted
- substitutions and caveats
- links to `summary.json` and per-lane logs

The first blocking required lane must be identifiable from the concise summary
without reading detailed logs.
