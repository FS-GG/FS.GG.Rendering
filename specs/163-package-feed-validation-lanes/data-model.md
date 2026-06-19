# Data Model: Package Feed Validation Lanes

## Packable Framework Package

**Purpose**: Source-controlled package metadata that defines the expected local package version.

**Fields**:

- Package id.
- Version.
- Project path.
- Packable flag.
- Package output path in the local feed.

**Validation rules**:

- Package id must start with `FS.GG.UI.`.
- Expected version comes from the source project, not from a sample or existing feed package.
- Missing `PackageId`, missing `Version`, or non-packable source metadata excludes the project
  from current-package authority and is reported.
- The local feed must contain the expected package id/version before source proof can pass.

## Package-Consuming Sample

**Purpose**: A selected sample that validates the framework through package references.

**Fields**:

- Sample id.
- Sample root.
- Project files.
- NuGet config path.
- Selected package references.
- Compatibility exceptions.

**Validation rules**:

- AntShowcase is the first selected sample.
- A selected sample must not consume `FS.GG.UI.*` through direct source project references.
- A selected sample with no `FS.GG.UI.*` package references is reported as `no-package-pins` and
  cannot satisfy package-consuming proof.
- Compatibility exceptions must name package id, declared version, expected version, sample path,
  reason, owner, and expiry/review condition.

## Package Pin

**Purpose**: One source-controlled `PackageReference` in a selected sample.

**Fields**:

- Package id.
- Declared version.
- Expected version.
- Project file path.
- XML location when available.
- Compatibility exception id when present.
- Status.

**Status values**:

- `current`
- `stale`
- `missing-expected-package`
- `compatibility-exception`
- `not-selected`

**Validation rules**:

- `stale` fails the pin check before sample restore/build/test execution.
- `compatibility-exception` passes only when the exception is recorded in evidence.
- Refresh mode may update `stale` pins to the expected version and must list every changed file.

## Local Package Feed

**Purpose**: Configured local NuGet source for current `FS.GG.UI.*` packages.

**Fields**:

- Feed path.
- Package files found.
- Expected package files.
- Older package versions found.
- Missing expected package ids.
- Pack command and status.

**Validation rules**:

- Default feed path is `~/.local/share/nuget-local/`.
- Missing expected id/version packages fail package source proof.
- Older versions in the feed are disclosed; they do not pass stale pins.
- Feed refresh must record command, elapsed time, output path, and packages produced.

## Package Source Rule

**Purpose**: Rule constraining which source may satisfy a package id pattern.

**Fields**:

- Rule id.
- Package pattern.
- Allowed source keys.
- Allowed source paths/URLs.
- Applies to selected samples.

**Validation rules**:

- `FS.GG.UI.*` may resolve only from the local feed.
- Approved third-party packages may resolve from approved external sources such as nuget.org.
- Any `FS.GG.UI.*` package resolved from a non-local source is a source violation.
- Source rules used for proof must be written to evidence.

## Package Cache Proof

**Purpose**: Evidence proving cache/source conditions for a restore.

**Fields**:

- Cache path.
- Isolated cache flag.
- Global cache cleared flag.
- Restore command.
- Generated NuGet config path.
- Package source rules.
- Resolved package ids and versions.
- Violations.
- Logs and assets paths.

**Validation rules**:

- Default proof uses an isolated cache and does not clear global caches.
- Cold proof may clear global caches only when explicitly requested and records that fact.
- Source proof passes only when pins are current, expected packages exist in the local feed,
  restore succeeds, and no source violations are present.

## Validation Lane

**Purpose**: Named validation unit with isolated execution policy.

**Fields**:

- Lane id.
- Description.
- Command.
- Working directory.
- Required flag.
- Timeout.
- No-progress timeout.
- Log path.
- Result path.
- Diagnostics path.
- Output isolation root.
- Concurrency group.

**Validation rules**:

- Minimum lanes: `package-proof`, `antshowcase-sample`, `controls`, `rendering-harness`, and
  `aggregate-solution`.
- Each lane writes to distinct result and log locations.
- Concurrent dotnet lanes must not share generated build/runtime output paths.
- Required lanes that do not pass keep overall readiness blocked unless an accepted exception is
  recorded.

## Lane Result

**Purpose**: Outcome of one lane run.

**Status values**:

- `passed`
- `failed`
- `timed-out`
- `hung`
- `skipped`
- `canceled`
- `not-run`
- `environment-limited`

**Fields**:

- Lane id.
- Status.
- Exit code when available.
- Started timestamp.
- Completed timestamp.
- Elapsed time.
- Last output timestamp.
- Log path.
- Result artifacts.
- Diagnostics.
- Caveats.
- Accepted exception id when present.

**Validation rules**:

- `timed-out`, `hung`, `canceled`, `skipped`, `not-run`, and `environment-limited` are never
  counted as passed.
- A no-progress policy trigger is classified as `hung` when the process is still alive and stopped
  producing output.
- Canceled lanes preserve partial logs and are not green.
- Result artifacts must be lane-specific.

## Validation Summary

**Purpose**: Reviewer and machine-checkable record of package proof, lane results, and readiness.

**Fields**:

- Current package versions.
- Selected samples.
- Local feed path.
- Package cache path.
- Package source rules.
- Package proof status.
- Lane results.
- Aggregate solution status.
- Overall readiness status.
- Required incomplete evidence.
- Accepted exceptions.
- Caveats and environment limits.
- Artifact locations.

**Overall readiness values**:

- `ready`
- `blocked`
- `incomplete`
- `environment-limited`

**Validation rules**:

- `ready` requires current pins, passing source proof, all required lanes passed, and no required
  incomplete evidence.
- Any required lane or package proof with failed, timed-out, hung, canceled, skipped, not-run, or
  environment-limited status makes readiness `blocked` or `incomplete`.
- Focused lane success and aggregate full-solution validation status are displayed separately.

## State Transitions

```text
Package pin: unknown -> current
Package pin: unknown -> stale -> refreshed -> current
Package pin: stale -> compatibility-exception

Package proof: not-run -> running -> passed
Package proof: not-run -> running -> failed
Package proof: not-run -> running -> environment-limited

Lane result: not-run -> running -> passed
Lane result: not-run -> running -> failed
Lane result: not-run -> running -> timed-out
Lane result: not-run -> running -> hung
Lane result: running -> canceled

Validation summary: draft -> incomplete
Validation summary: draft -> blocked
Validation summary: incomplete -> ready
Validation summary: incomplete -> blocked
```
