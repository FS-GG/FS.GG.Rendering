# Contract: Lane Runner Effects

The lane runner keeps state transitions pure and pushes process, timer, console,
and filesystem work to an edge interpreter.

## Model

The model tracks:

- lane catalog
- selected lane ids
- active lane id
- pending lane ids
- completed lane results
- canceled lane ids
- session run id and artifact root
- diagnostics
- summary state

## Messages

| Message | Meaning |
|---------|---------|
| `RunRequested` | Operator requested a list, required run, or explicit lanes. |
| `PreflightPassed` | Request and lane catalog are valid. |
| `PreflightFailed` | Request or configuration error prevents execution. |
| `LaneStarted` | A lane process started. |
| `LaneOutputReceived` | Child output was captured. |
| `LaneHeartbeatDue` | Runner should publish progress for the active lane. |
| `LaneCompleted` | Lane produced a final result. |
| `LaneTimedOut` | Lane exceeded total time budget. |
| `LaneNoProgressTimedOut` | Lane exceeded no-progress budget. |
| `OperatorCanceled` | User canceled the session. |
| `LaneCanceled` | Active or pending lane was marked canceled. |
| `InfrastructureErrorRaised` | Runner could not start, stop, observe, or write evidence. |
| `SummaryRequested` | Session should compute summary. |
| `SummaryWritten` | Summary artifacts were written. |

## Effects

| Effect | Interpreter responsibility |
|--------|----------------------------|
| `ValidateRequest` | Check lane ids, duplicates, output paths, timeouts, and schedule safety. |
| `CreateRunRoot` | Create the run-id evidence directory. |
| `CreateLaneEvidenceRoot` | Create lane evidence paths before process start. |
| `StartProcess` | Start the lane command with redirected output. |
| `AppendLaneLog` | Append captured output to the lane log. |
| `PublishHeartbeat` | Print active lane, elapsed time, timeout budget, and last activity. |
| `PollProcess` | Check process status and timeout/no-progress budgets. |
| `StopProcess` | Terminate the child process tree. |
| `WriteLaneResult` | Write per-lane `result.json` and diagnostics. |
| `WriteSummary` | Write `summary.md` and `summary.json`. |
| `RegisterCancelHandler` | Convert operator cancellation into `OperatorCanceled`. |

## State Rules

- `RunRequested` must produce `ValidateRequest` before any `StartProcess`.
- `PreflightFailed` produces no lane execution effects.
- A lane must have an evidence root before `StartProcess`.
- Timeout and no-progress messages produce `StopProcess` and then a non-passing
  lane result.
- `OperatorCanceled` stops the active process, preserves completed results, and
  marks pending lanes as `canceled` or `not-run` with a reason.
- `SummaryRequested` happens after all selected lanes are terminal or preflight
  failed.
- `SummaryWritten` is the final successful runner state even when readiness is
  blocked.
