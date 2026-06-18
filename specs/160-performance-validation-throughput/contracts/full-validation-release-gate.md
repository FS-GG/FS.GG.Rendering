# Contract: Full Validation Release Gate

## Scope

This contract preserves full solution validation as the release gate while allowing focused
performance iterations to run without waiting for broad regression suites every time.

## Focused Lane Boundary

The focused performance command must not run broad release validation as part of each focused
iteration. It may run only the focused timing scenarios, unsupported-host checks, and artifact
publication needed for Feature 160 throughput evidence.

The focused lane output must state that broad release validation remains separate.

## Full Validation Record

Readiness records full validation under:

```text
specs/160-performance-validation-throughput/readiness/full-validation/
```

The record includes:

- Command.
- Started timestamp.
- Completed timestamp.
- Duration.
- Exit status.
- Output artifact path.
- Included suites or projects.
- Staleness marker.
- Diagnostics.

## Required Release Gate

Before Feature 160 can be marked release-ready, readiness must include a current full validation
record for:

```bash
dotnet test FS.GG.Rendering.slnx --no-restore
```

Additional package, surface, or pack validation is required when public `.fsi`, package output, or
compatibility behavior changes.

Current means the record was produced for the same implementation commit, validation command,
package/surface baseline, and readiness artifact set being marked ready. A later implementation,
package, surface, validation-command, or readiness-artifact change makes the record stale.

## Blocking Conditions

Release-ready status is refused when full validation is:

- Missing.
- Failing.
- Interrupted.
- Stale.
- Undocumented.
- Ambiguous about which suites ran.
- Known to expose undocumented consumer-visible drift.

Focused throughput may still be reported as accepted while release-ready status is blocked.

## Readiness Summary Requirements

`readiness/validation-summary.md` reports focused throughput status and full validation status as
separate decisions. It must be possible for a reviewer to determine:

- Whether focused throughput is accepted.
- Whether full validation passed.
- Whether release-ready status is blocked.
- Which artifacts prove each decision.
- Whether the shipped compositor performance claim remains `performance-not-accepted`.

## Acceptance Tests

- Passing focused throughput with missing full validation produces a blocked release-ready result.
- Passing focused throughput with failing or interrupted full validation produces a blocked
  release-ready result.
- Passing focused throughput with current passing full validation reports both as separate
  evidence.
- Broad validation drift remains a closeout blocker unless explicitly documented as deferred in
  feature artifacts.
