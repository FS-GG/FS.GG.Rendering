# Synthetic Evidence Inventory

Synthetic tests are limited to controlled lane-runner fixtures where real child
processes would be slow, nondeterministic, or destructive.

| Location | Synthetic use | Rationale | Real evidence path |
|----------|---------------|-----------|--------------------|
| `Feature166LaneStatusTests.fs` | Status-token and MVU transition records | Public status vocabulary and pure effects do not require child processes. | `runLane` tests in the same file cover real pass/fail/timeout/no-progress/infrastructure process behavior. |
| `Feature166CancellationTests.fs` | Operator-cancel model state | Sending real Ctrl+C from a unit test would be host-sensitive. | Required-lane evidence records blocked/canceled semantics through summaries; manual cancellation reproduced Controls no-progress. |
| `Feature166ValidationSummaryTests.fs` | Mixed summary result records | Summary agreement and readiness rules are pure aggregation behavior. | `validation-20260619-104119-b56046` records real mixed required-lane evidence. |
| `Feature166SchedulingTests.fs` | Artificial conflicting lanes | Real parallel shared-output races are intentionally prevented before execution. | Preflight diagnostics prove unsafe schedules are rejected without starting work. |

PR disclosure text:

```text
Feature 166 includes synthetic lane-runner tests for pure status tokens, MVU
transitions, cancellation state, summary aggregation, and schedule conflicts.
Real process evidence covers pass, fail, total timeout, no-progress timeout, and
infrastructure error in Feature166LaneStatusTests plus committed lane-runner
readiness summaries.
```
