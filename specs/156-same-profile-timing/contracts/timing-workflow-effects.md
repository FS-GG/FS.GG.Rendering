# Contract: Timing Workflow Effects

## Scope

Timing collection is a stateful I/O workflow. Feature 156 must keep the decision logic pure and
move host probing, rendering, timing, and filesystem writes to edge interpreters.

## Model

The workflow model owns:

- Run identity.
- Expected host profile.
- Current host facts.
- Policy declaration.
- Scenario queue.
- Warmup count.
- Repetition count.
- Collected full-redraw samples.
- Collected damage-scoped samples.
- Scenario verdicts.
- Published artifact paths.
- Diagnostics.

## Messages

Required messages include:

- `HostProfileDetected`
- `HostProfileRejected`
- `PolicyDeclared`
- `ScenarioPrepared`
- `WarmupCompleted`
- `PathMeasured`
- `ScenarioEvaluated`
- `ArtifactPublished`
- `RunRejected`
- `RunEnvironmentLimited`
- `SummaryPublished`

## Effects

Required effects include:

- Detect host profile.
- Load Feature 155 proof/parity baseline.
- Prepare scenario.
- Run full-redraw warmup.
- Run damage-scoped warmup.
- Measure full-redraw path.
- Measure damage-scoped path.
- Write raw samples.
- Write scenario report.
- Write timing summary.
- Write unsupported-host report.
- Write compatibility, package, and regression summaries.

## Validation Rules

- `update` remains pure: it transforms model and message into next model plus requested effects.
- Interpreters return messages with enough detail to fail closed on host mismatch, missing display,
  renderer absence, timeout, artifact write failure, invalid samples, or path measurement failure.
- No effect may silently drop failed samples or replace them with synthetic timing.
- Synthetic fixtures may be used only for rejection tests and must be disclosed in test names and
  comments.
- The final model cannot be positive until every required scenario is evaluated as positive under
  `same-profile-live-threshold-v2`.
