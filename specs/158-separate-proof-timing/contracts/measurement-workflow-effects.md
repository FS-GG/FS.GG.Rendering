# Contract: Measurement Workflow Effects

## Scope

Feature 158 timing and probe collection is a stateful I/O workflow. Decision logic must remain pure
and native host work must run through edge interpreters.

## Model

The workflow model records:

- Run id.
- Expected host profile.
- Active host profile.
- Policy id.
- Scenario queue.
- Warmup count.
- Repetition count.
- Collected samples.
- Excluded samples.
- Probe artifacts.
- Published artifact paths.
- Final status.
- Diagnostics.

## Messages

Required messages include:

- `HostProfileDetected`
- `HostProfileRejected`
- `MeasurementPolicyDeclared`
- `ScenarioPrepared`
- `WarmupCompleted`
- `TimingPathMeasured`
- `SampleClassified`
- `ProbeReadbackCaptured`
- `SampleExcluded`
- `RunRejected`
- `RunEnvironmentLimited`
- `ArtifactPublished`
- `SummaryPublished`
- `DiagnosticRecorded`

## Effects

Required effects include:

- Detect host profile.
- Load Feature 155/157 proof references.
- Prepare scenario.
- Run full-redraw warmup.
- Run damage-scoped warmup.
- Measure full-redraw path without readback.
- Measure damage-scoped path without readback.
- Capture explicit proof/probe readback.
- Write raw samples.
- Write excluded-sample report.
- Write scenario report.
- Write timing summary.
- Write proof/probe report.
- Write unsupported-host report.
- Write compatibility, package, and regression summaries.

## Edge Interpreter Rules

- `update` remains pure: it transforms model and message into next model plus requested effects.
- Native window, GL, Skia, readback, timer, process, and filesystem work happens only in
  interpreters.
- Interpreters return messages with enough detail to fail closed on host mismatch, missing display,
  renderer absence, timeout, invalid samples, readback contamination, artifact write failure, or
  proof/probe failure.
- No effect may silently drop samples or reclassify probe samples as accepted timing.
- Synthetic fixtures may be used only for rejection tests and must carry `Synthetic` in the test
  name and source comment.

## Terminal States

- `accepted`: required readback-free timing scenarios and readiness evidence are complete.
- `rejected`: contaminated, mixed, missing, or unverifiable evidence prevents acceptance.
- `fallback-only`: proof/probe evidence exists but no accepted readback-free timing set exists.
- `environment-limited`: host or presentation environment prevented comparable evidence.
