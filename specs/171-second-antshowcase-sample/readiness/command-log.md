# Command Log

Date: 2026-06-19

## Package Feed

Command:

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx
```

Result: FAIL, exit 2.

Output: `package-feed: at least one --sample <path> is required`

Follow-up command:

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase
```

Result: PASS.

Output:

```text
package-feed status: passed
packages: 14
pins: 18
specs/163-package-feed-validation-lanes/readiness/package-proof/package-versions.md
specs/163-package-feed-validation-lanes/readiness/package-proof/package-pins.md
```

## Build

Command:

```sh
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release
```

Result: PASS, 0 warnings, 0 errors.

## Tests

Command:

```sh
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
```

Result: PASS.

```text
Passed! - Failed: 0, Passed: 104, Skipped: 0, Total: 104
```

## FSI

Command:

```sh
dotnet fsi specs/171-second-antshowcase-sample/readiness/fsi/second-ant-showcase-authoring.fsx
```

Result: PASS.

Output:

```text
SecondAntShowcase FSI surface OK: 96/96 controls mapped, 19 pages (13 catalog + 6 template), 0 unreferenced, 0 duplicated, findings=0, mode=antDark
```

## Coverage

Command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage
```

Result: PASS.

Output:

```text
96/96 controls mapped, 19 pages (13 catalog + 6 template), 0 unreferenced, 0 duplicated
```

## Evidence

Command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/171-second-antshowcase-sample/readiness
```

Result: PASS with caveats. The command reported `provesScreenshot=true` for all 19 pages.
The run remains not authoritative for pixel-level Ant fidelity, live pointer behavior beyond
seeded scripts, or chart/graph semantics beyond seeded sample data.

## Visual Readiness

Preferred command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/171-second-antshowcase-sample/readiness/preferred
```

Result: BLOCKED, screenshots 38/38, pending reviewer classification.

Minimum command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/171-second-antshowcase-sample/readiness/minimum
```

Result: BLOCKED, screenshots 38/38, pending reviewer classification.

Summary command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --summarize specs/171-second-antshowcase-sample/readiness/preferred --minimum-size specs/171-second-antshowcase-sample/readiness/minimum --out specs/171-second-antshowcase-sample/readiness
```

Result: PASS; generated summary files under `specs/171-second-antshowcase-sample/readiness`.

## Review Findings

Command:

```sh
dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/171-second-antshowcase-sample/readiness --fail-on-unresolved
```

Result: PASS, unresolved findings 0. Visual acceptance remains blocked by missing reviewer
classification.

## Guardrails

Command:

```sh
git diff --name-only -- samples/AntShowcase
```

Result: PASS, no output.

Command:

```sh
git diff --name-only -- src/
```

Result: PASS, no output.

Readiness allowlist proof:

```text
git check-ignore -q specs/171-second-antshowcase-sample/readiness/command-log.md -> exit 1 (not ignored)
git check-ignore -v specs/171-second-antshowcase-sample/readiness/command-log.md -> .gitignore:84:!specs/171-second-antshowcase-sample/readiness/**
```
