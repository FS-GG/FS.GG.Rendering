# US3 Reporting

Command:

```sh
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164
```

Result: passed, 8 tests.

Covered:

- Contact-sheet metadata carries deterministic target ids, paths, grouping fields, missing target ids, and diagnostics.
- Markdown summary includes readiness status, target counts, status counts, contact sheet paths, caveats, and diagnostics.
- JSON summary exposes machine-readable `targetCount`, `requiredTargetCount`, `captureStatusCounts`, `reviewerStatusCounts`, `readinessStatus`, targets, captures, reviewer classifications, contact sheets, caveats, and diagnostics.

Documentation:

- `src/Testing/README.md` now documents shared visual-readiness usage.
