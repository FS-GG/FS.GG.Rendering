# Contract: Validation Lane CLI

## Entry Point

```sh
dotnet fsi scripts/run-validation-lanes.fsx [options]
```

The script forwards to:

```sh
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- validation-lanes [options]
```

## Options

| Option | Meaning |
|--------|---------|
| `--list` | Print lane ids, readiness roles, timeouts, and descriptions. No validation work starts. |
| `--required` | Run the required lane set. This is the default when no `--lane` is supplied. |
| `--lane <id>` | Run one selected lane. Repeatable. Optional and informational lanes can be selected explicitly. |
| `--include-optional <id>` | Add an optional lane to a required run. Repeatable. |
| `--out <dir>` | Evidence root. The runner creates a run-id child directory under this root. |
| `--run-id <id>` | Caller-provided run id. Must be unique unless replacement is explicit. |
| `--replace-run <id>` | Replace an existing run directory and write a replacement notice in the summary. |
| `--json` | Print the structured summary path and final readiness token in machine-readable form. |

## Default Behavior

When neither `--list` nor `--lane` is supplied, the runner behaves as `--required`.
The optional aggregate lane is not part of the default required set.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | All required lanes passed. Optional and informational lanes may still have caveats. |
| `1` | At least one required lane failed, timed out, was canceled, was skipped, was not run, or hit an infrastructure error. |
| `2` | Request or lane configuration error found during preflight. No lane work started. |
| `3` | Runner infrastructure error before or during evidence collection. |
| `130` | Operator canceled the run. Completed lane results are preserved and incomplete lanes are marked. |

## Required Preflight

Before starting any lane, the CLI validates:

- all requested lane ids exist
- no duplicate lane ids or duplicate result ids exist
- requested run id is unique unless `--replace-run` is used
- evidence root can be created and written
- selected lanes have positive timeouts and progress intervals at or below 60 seconds
- selected lanes have safe scheduling for shared output locations

Preflight failures print diagnostics and return exit code `2`.

## Operator Output

The runner prints:

- selected lanes before execution starts
- start and completion line for each lane
- heartbeat at least every 60 seconds while a lane is active
- timeout/cancellation/infrastructure diagnostics when they occur
- final `summary.md` and `summary.json` paths

Operator output is also included in the session evidence.

## Examples

List lanes:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --list
```

Run a single lane:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane controls --out artifacts/validation-lanes
```

Run required lanes plus optional aggregate:

```sh
dotnet fsi scripts/run-validation-lanes.fsx --required --include-optional aggregate-solution --out specs/166-validation-lane-runner/readiness/lanes
```
