# Quickstart: Validation Lane Runner

## Prerequisites

- .NET SDK for `net10.0`
- Repository restored and built as usual for local validation:
  `dotnet restore FS.GG.Rendering.slnx`
- Local package feed available when running the package-proof lane:
  `~/.local/share/nuget-local/`
- Package-consuming sample pins current with packable project versions:
  `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode refresh --out specs/166-validation-lane-runner/readiness/package-refresh`

## 1. List Lanes

```sh
dotnet fsi scripts/run-validation-lanes.fsx --list
```

Expected outcome:

- exit code `0`
- output lists at least `build`, `library-tests`, `package-proof`, `controls`,
  `rendering-harness`, `antshowcase-sample`, and `aggregate-solution`
- each listed lane shows readiness role and timeout

## 2. Run One Required Lane

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out artifacts/validation-lanes
```

Expected outcome:

- only `rendering-harness` runs
- output prints a run-specific `summary.md` path
- per-lane evidence contains `log.txt`, `result.json`, and diagnostics
- summary names the lane, status, elapsed time, role, and evidence paths

## 3. Run Required Readiness Lanes

```sh
dotnet fsi scripts/run-validation-lanes.fsx --required --out specs/166-validation-lane-runner/readiness/lanes
```

Expected outcome:

- required lanes run without the optional aggregate unless explicitly included
- final summary lists every required lane and the overall readiness result
- `summary.md` and `summary.json` agree on statuses and evidence paths
- any required non-passing lane makes readiness unsuccessful

## 4. Include Optional Aggregate Separately

```sh
dotnet fsi scripts/run-validation-lanes.fsx --required --include-optional aggregate-solution --out specs/166-validation-lane-runner/readiness/lanes
```

Expected outcome:

- required lanes still determine readiness
- `aggregate-solution` appears in an optional/aggregate section
- aggregate failure, timeout, cancellation, or omission cannot hide required lane
  outcomes

## 5. Reuse Or Replace a Run Id

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --run-id local-check --out artifacts/validation-lanes
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --replace-run local-check --out artifacts/validation-lanes
```

Expected outcome:

- the first command creates `artifacts/validation-lanes/local-check`
- a later run with the same run id fails preflight unless replacement is explicit
- replacement writes a visible replacement notice in the summary

## 6. Machine-Readable Summary Path

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --json --out artifacts/validation-lanes
```

Expected outcome:

- output is a small JSON object with `summaryJson` and `overallReadiness`

## 7. Verify Request Preflight

```sh
dotnet fsi scripts/run-validation-lanes.fsx --lane does-not-exist --out artifacts/validation-lanes
```

Expected outcome:

- exit code `2`
- no validation lane starts
- diagnostic names the unknown lane

## 8. Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Required selected lanes passed. |
| `1` | Required selected lanes completed but readiness is blocked or incomplete. |
| `2` | Request or catalog preflight failed before lane work started. |
| `3` | Runner infrastructure error occurred. |
| `130` | Operator canceled the run. |

## 9. Run Focused Feature Tests

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature166
```

Expected outcome:

- tests cover lane catalog, request preflight, pass/fail/timeout/no-progress
  status classification, cancellation, infrastructure errors, summary rendering,
  run-id no-overwrite behavior, and unsafe-concurrency handling

## 10. Preserve Direct Workflows

Direct commands remain valid for focused debugging:

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore
```

Expected outcome:

- documentation presents the lane runner as orchestration, not as a replacement
  for direct validation commands
