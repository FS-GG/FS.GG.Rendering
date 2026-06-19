# US1 Targeted Lanes Evidence

Focused tests:

```text
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature166
Passed! - Failed: 0, Passed: 18, Skipped: 0, Total: 18
```

Single-lane smoke:

```text
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out specs/166-validation-lane-runner/readiness/lanes
specs/166-validation-lane-runner/readiness/lanes/validation-20260619-103826-f55357/summary.md
```

Result: `rendering-harness` passed and only that required lane ran. The summary
links to per-lane `log.txt`, `result.json`, and `diagnostics.md`.
