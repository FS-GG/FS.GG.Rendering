# Feature 173 Validation Summary

Status: `blocked`

Feature 173 implementation and automated regression checks are complete, but final accepted live responsiveness readiness is not claimed. The light and dark live responsiveness commands both produced non-accepted `environment-limited` evidence because this environment did not provide a measured visible desktop presentation boundary.

## Command Results

| Command | Result | Log |
|---------|--------|-----|
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` | passed, 202 passed / 0 skipped | `logs/skia-viewer-tests.log` |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release` | passed, 194 passed / 17 skipped | `logs/elmish-tests.log` |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release` | passed, 893 passed / 1 skipped | `logs/controls-tests.log` |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase` | passed, 14 packages / 18 pins | `logs/package-refresh.log` |
| `dotnet restore samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | passed; required before `--no-restore` because package cache entries were missing after refresh | `logs/second-antshowcase-restore.log` |
| `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore` | passed, 142 passed / 0 skipped | `logs/second-antshowcase-tests.log` |
| headless fail-closed responsiveness command | exit `4`, `environment-limited` | `logs/headless-fail-closed.log` |
| light live responsiveness command | exit `4`, `environment-limited` | `logs/live-light.log` |
| dark live responsiveness command | exit `4`, `environment-limited` | `logs/live-dark.log` |
| `coverage` | passed, 96/96 controls mapped | `logs/coverage.log` |
| `evidence --seed 1` | passed, 19 page evidence records | `logs/evidence.log` |
| preferred visual readiness | blocked, 38/38 screenshots, pending reviewer classification | `logs/visual-preferred.log` |
| minimum visual readiness | blocked, 38/38 screenshots, pending reviewer classification | `logs/visual-minimum.log` |
| `review-findings --fail-on-unresolved` | passed, unresolved=0 | `logs/review-findings.log` |

## Responsiveness Runs

| Run | Theme | Readiness | Exit | Summary |
|-----|-------|-----------|------|---------|
| `resp-20260619-201656-0ef9b0` | light, headless fail-closed | `environment-limited` | 4 | `responsiveness/headless-fail-closed/resp-20260619-201656-0ef9b0/summary.json` |
| `resp-20260619-201707-d02e83` | light | `environment-limited` | 4 | `responsiveness/resp-20260619-201707-d02e83/summary.json` |
| `resp-20260619-201717-3f1141` | dark | `environment-limited` | 4 | `responsiveness/resp-20260619-201717-3f1141/summary.json` |

All responsiveness summaries report `artifactWriteStatus = complete` and `firstFailedBudget.kind = environment-boundary`. No run reports accepted live responsiveness.

## Caveats

- Live responsiveness acceptance is blocked: no measured visible desktop presentation boundary was available, so the runner correctly returned exit code `4`.
- Visual readiness is blocked: screenshots were generated, but reviewer classification remains pending in both preferred and minimum matrices.
- Elmish validation contains 17 existing skipped Feature 109 performance-golden tests.
- Controls validation contains 1 existing skipped typed-controls FSI transcript expectation.
- The first sample `--no-restore` attempt after package refresh failed because package cache entries were absent; an explicit restore from the refreshed feed passed, and the final `--no-restore` sample test passed.

## Git Ignore Proof

`git check-ignore -v` reports `.gitignore:90:!specs/173-live-responsiveness-runner/readiness/**` for:

- `specs/173-live-responsiveness-runner/readiness/validation-summary.md`
- `specs/173-live-responsiveness-runner/readiness/logs/skia-viewer-tests.log`
- `specs/173-live-responsiveness-runner/readiness/responsiveness/resp-20260619-201707-d02e83/summary.json`
