# Contract: Validation Lane Runner

## Entry Point

```bash
dotnet fsi scripts/run-validation-lanes.fsx \
  --lane package-proof \
  --lane antshowcase-sample \
  --lane controls \
  --lane rendering-harness \
  --lane aggregate-solution \
  --out specs/163-package-feed-validation-lanes/readiness/lanes
```

The script delegates to the `Rendering.Harness` lane runner. The script arguments are the stable
maintainer contract.

## Lane Definition Fields

- `id`: Stable lane id.
- `description`: Human-readable purpose.
- `command`: Command and arguments.
- `workingDirectory`: Project-relative working directory.
- `required`: Whether non-pass blocks overall readiness.
- `timeout`: Maximum elapsed time.
- `noProgressTimeout`: Maximum time without stdout/stderr progress before hang classification.
- `logPath`: Lane-specific log path.
- `resultPath`: Lane-specific result path.
- `diagnosticsPath`: Lane-specific diagnostic path.
- `outputRoot`: Lane-specific generated output/build root.
- `concurrencyGroup`: Optional group for lanes that must not run together.

## Minimum Lanes

- `package-proof`: Runs package pin and source proof for selected samples.
- `antshowcase-sample`: Builds/tests the selected AntShowcase package-consuming sample.
- `controls`: Runs Controls validation with explicit timeout and logs.
- `rendering-harness`: Runs rendering/harness validation.
- `aggregate-solution`: Runs full solution validation as a distinct aggregate lane.

## Status Values

- `passed`
- `failed`
- `timed-out`
- `hung`
- `skipped`
- `canceled`
- `not-run`
- `environment-limited`

## Output Isolation

Each lane must write logs and result artifacts under its own directory:

```text
lanes/
|-- package-proof/
|-- antshowcase-sample/
|-- controls/
|-- rendering-harness/
`-- aggregate-solution/
```

When two dotnet lanes can run concurrently, each lane must receive a distinct generated output
root, for example a lane-specific `BaseOutputPath` or equivalent. Two lanes must not write to the
same runtime output directory under the default configuration.

## Timeout and No-Progress Behavior

- A lane exceeding `timeout` is marked `timed-out`.
- A lane exceeding `noProgressTimeout` while still running is marked `hung`.
- Timed-out or hung lanes preserve partial stdout/stderr logs.
- Diagnostics record lane id, command, elapsed time, last output timestamp, process id when known,
  and diagnostic artifact path.
- Hosts that cannot produce a requested diagnostic record `environment-limited` caveats instead of
  passing silently.

## Cancellation

Manual cancellation marks running lanes `canceled`, preserves available logs, and writes a summary
update. A canceled lane is not green even if earlier sub-steps passed.

## Result JSON Fields

- `laneId`.
- `status`.
- `command`.
- `startedUtc`.
- `completedUtc`.
- `elapsed`.
- `exitCode`.
- `logPath`.
- `resultArtifacts`.
- `diagnostics`.
- `caveats`.
- `acceptedException`.

## Readiness Rule

Required lanes with any status other than `passed` block overall readiness unless the summary links
an explicit accepted exception. Optional lanes are still listed and cannot be represented as
passed when skipped, canceled, timed out, hung, not run, or environment-limited.
