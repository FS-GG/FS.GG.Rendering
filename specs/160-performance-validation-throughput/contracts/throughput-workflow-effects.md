# Contract: Throughput Workflow Effects

## Scope

Feature 160 evidence collection is stateful and I/O-bearing. Decision logic must remain pure, and
process, timer, native host, and filesystem work must run through edge interpreters.

## Model

The workflow model owns:

- Run id.
- Expected host profile.
- Active host profile.
- Lane id.
- Policy id.
- Declared bound.
- Scenario set.
- Warmup count.
- Measured repetition count.
- Iteration queue.
- Active iteration.
- Collected samples.
- Excluded evidence.
- Published artifacts.
- Full validation record.
- Final throughput status.
- Release-ready status.
- Diagnostics.

## Messages

Workflow messages include:

- `HostProfileDetected`
- `HostProfileRejected`
- `LaneDeclared`
- `PolicyDeclared`
- `BoundDeclared`
- `ScenarioPrepared`
- `IterationStarted`
- `IterationTimedOut`
- `IterationCanceled`
- `IterationCompleted`
- `SampleClassified`
- `ScenarioCoverageRecorded`
- `IterationAccepted`
- `IterationExcluded`
- `UnsupportedHostRecorded`
- `FullValidationRecorded`
- `ArtifactPublished`
- `SummaryPublished`
- `DiagnosticRecorded`

## Effects

Workflow effects include:

- Detect host profile.
- Declare focused lane and policy.
- Declare iteration bound.
- Prepare required timing scenario.
- Run timing warmup.
- Measure full-redraw path.
- Measure damage-scoped path.
- Enforce iteration timeout.
- Classify sample inclusion.
- Write raw sample artifact.
- Write iteration artifact.
- Write excluded evidence artifact.
- Write unsupported-host artifact.
- Write full-validation record.
- Write compatibility, package, regression, and readiness summaries.

## Edge Interpreter Rules

- `update` remains pure: it transforms model and message into next model plus requested effects.
- Native window, GL, Skia, process execution, timers, and filesystem writes happen only in
  interpreters.
- Interpreters return messages with enough detail to fail closed on host mismatch, missing display,
  renderer absence, timeout, cancellation, invalid samples, readback contamination, artifact write
  failure, or full-validation failure.
- No effect may silently convert partial output into accepted throughput evidence.
- No focused-lane effect may invoke broad release validation.
- Synthetic fixtures may be used only for rejection tests and must carry `Synthetic` in the test
  name and source comment.

## Terminal States

- `accepted`: at least three fresh same-profile focused iterations complete within bounds with
  required coverage and metadata.
- `rejected`: invalid, partial, stale, mixed, or unverifiable evidence prevents throughput
  acceptance.
- `fallback-only`: focused lane cannot produce accepted timing but publishes safe fallback evidence.
- `environment-limited`: host or presentation environment prevented comparable evidence and zero
  accepted performance artifacts were recorded.
- `blocked`: throughput evidence is accepted but full validation blocks release readiness.

## Acceptance Tests

- Pure update transitions from bounded focused iterations to accepted throughput when all gates
  pass.
- Timeout transitions an iteration to excluded evidence with zero accepted contribution.
- Unsupported host transitions to `environment-limited` and emits write effects for limitation
  artifacts.
- Focused throughput accepted plus missing full validation transitions release readiness to
  `blocked`.
- The shipped performance claim remains `performance-not-accepted` unless all report-defined gates
  are present.
