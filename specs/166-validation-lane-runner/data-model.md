# Data Model: Validation Lane Runner

## Validation Lane

Represents one named validation unit.

**Fields**

- `Id`: stable CLI/result id, lowercase kebab-case, unique within the lane catalog.
- `DisplayName`: short reviewer-facing name.
- `Description`: purpose and protected contract.
- `ReadinessRole`: `required`, `optional`, or `informational`.
- `Command`: executable plus argument list, or an ordered list of command steps if
  implementation keeps multi-step lanes inside the runner.
- `WorkingDirectory`: repository-root-relative or absolute process working directory.
- `Timeout`: maximum wall-clock duration for the lane.
- `NoProgressTimeout`: optional maximum duration without child output before the
  lane is stopped as a no-progress timeout.
- `ProgressInterval`: maximum interval between runner heartbeat messages.
- `ConcurrencyGroup`: optional group for lanes that must not run concurrently.
- `OutputScope`: generated output location used by the child command, if known.
- `EvidenceDirectory`: session-relative lane evidence directory.
- `IsAggregate`: true only for the full-solution aggregate lane.
- `SubstitutesFor`: optional aggregate or external validation that this lane
  partially substitutes for; substitutions are visible in summaries.

**Validation Rules**

- `Id` values are unique and non-empty.
- `ReadinessRole` is explicit; no default role is inferred from a boolean.
- `Timeout` is positive.
- `ProgressInterval` is at most 60 seconds.
- `EvidenceDirectory`, `LogPath`, and `ResultPath` are unique per lane within a
  session.
- Lanes sharing a `ConcurrencyGroup` or `OutputScope` are serialized, isolated, or
  rejected before concurrent execution starts.
- Aggregate lanes are not required by default.

## Validation Session

Represents one operator-requested run.

**Fields**

- `RunId`: unique session id, default UTC timestamp plus short suffix.
- `RequestedLaneIds`: lane ids passed by the operator.
- `SelectionMode`: `required`, `explicit`, or `list-only`.
- `SelectedLaneIds`: validated lane ids scheduled to run.
- `StartedUtc` / `CompletedUtc`: session timestamps.
- `ArtifactRoot`: root directory for this run's evidence.
- `PolicyVersion`: lane catalog/policy identifier.
- `OverallReadiness`: `ready`, `blocked`, `incomplete`, or `environment-limited`.
- `Results`: ordered lane results.
- `Caveats`: visible session caveats, including aggregate/substitute status.
- `Diagnostics`: runner-level diagnostics.

**State Transitions**

1. `Requested`
2. `PreflightFailed` or `Scheduled`
3. `Running`
4. `Canceling` when the operator cancels
5. `Summarizing`
6. `Completed`

Preflight failure stops before lane work begins and writes request diagnostics
only if the evidence root can be created safely.

## Lane Result

Represents the outcome of one lane within a session.

**Fields**

- `LaneId`
- `ReadinessRole`
- `Status`
- `CommandText`
- `StartedUtc` / `CompletedUtc`
- `Elapsed`
- `TimeoutBudget`
- `LastActivityUtc`
- `LastActivityText`
- `ExitCode`
- `LogPath`
- `ResultPath`
- `DiagnosticsPath`
- `ResultArtifacts`
- `Reason`
- `Diagnostics`
- `Caveats`
- `AcceptedEnvironmentLimitation`
- `Substitution`

**Status Tokens**

- `passed`
- `failed`
- `timed-out`
- `no-progress-timeout`
- `canceled`
- `skipped`
- `infrastructure-error`
- `environment-limited`
- `not-run`

`no-progress-timeout`, `environment-limited`, and `not-run` are non-green. A
required `environment-limited` result blocks readiness unless an accepted
environment limitation is recorded and the summary still exposes the limitation.

## Lane Evidence

Represents files produced or collected for one lane.

**Fields**

- `LogPath`: operator-visible combined output.
- `ResultPath`: lane `result.json`.
- `DiagnosticsPath`: runner diagnostics and captured failure details.
- `TestArtifacts`: TRX, blame sequence, dumps, screenshots, or other collected
  artifacts.
- `OutputRoot`: isolated child-process output when the lane needs one.

**Validation Rules**

- Evidence directories are created before the child command starts.
- Failure to create or write required evidence is an `infrastructure-error`.
- Lane evidence paths are session-local and cannot overwrite a previous run
  unless the operator uses an explicit replacement option that writes a notice.

## Run Policy

Represents runner behavior that is independent of a specific lane.

**Fields**

- `RequiredLaneIds`
- `OptionalLaneIds`
- `InformationalLaneIds`
- `ProgressInterval`
- `DefaultArtifactRoot`
- `RunIdFormat`
- `CancellationPolicy`
- `ConcurrencyPolicy`
- `ReadinessRules`

**Validation Rules**

- Every required lane id exists in the catalog.
- Optional and informational lanes are visible in `--list`.
- Readiness is unsuccessful when any required lane fails, times out, is canceled,
  is skipped without accepted limitation, is not run, or has infrastructure error.
- Optional and informational results never convert required readiness to success.

## Validation Summary

Represents the human-readable and structured session result.

**Fields**

- `RunId`
- `OverallReadiness`
- `ArtifactRoot`
- `RequiredLaneResults`
- `OptionalLaneResults`
- `InformationalLaneResults`
- `FirstBlockingRequiredLane`
- `AggregateLaneStatus`
- `Substitutions`
- `Caveats`
- `GeneratedAtUtc`

**Validation Rules**

- `summary.md` and `summary.json` agree on statuses, roles, elapsed times,
  evidence paths, and overall readiness.
- The first blocking required lane is visible without reading per-lane logs.
- Optional aggregate status is shown even when targeted lanes substitute for it.
