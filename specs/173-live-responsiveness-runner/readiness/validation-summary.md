# Feature 173 Validation Summary

Status: `accepted-live-responsiveness`

Feature 173 live responsiveness now has accepted light and dark evidence from the GL viewer path. The runner writes measured `presented-frame` records with non-null paint/present/total timings and exits `0` for both themes. Visual-review caveats remain separate below.

## Command Results

| Command | Result | Log |
|---------|--------|-----|
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` | passed, 202 passed / 0 skipped | `logs/skia-viewer-tests.log` |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release` | passed, 194 passed / 17 skipped | `logs/elmish-tests.log` |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release` | passed, 893 passed / 1 skipped | `logs/controls-tests.log` |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase` | passed, 14 packages / 18 pins | `logs/package-refresh.log` |
| `dotnet restore samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | passed; required before `--no-restore` because package cache entries were missing after refresh | `logs/second-antshowcase-restore.log` |
| `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore` | passed, 142 passed / 0 skipped | `logs/second-antshowcase-tests.log` |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase --mode refresh --pack --out specs/173-live-responsiveness-runner/readiness/package-feed-post-merge` | passed, packed 14 packages / aligned 18 pins at `0.1.35-preview.1` | `logs/package-feed-post-merge-refresh.log` |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase --mode proof --isolated-cache specs/173-live-responsiveness-runner/readiness/package-feed-post-merge/nuget-cache --out specs/173-live-responsiveness-runner/readiness/package-feed-post-merge` | passed, isolated source proof | `logs/package-feed-post-merge-proof.log` |
| `dotnet restore samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` after post-merge package bump | passed | `logs/second-antshowcase-post-merge-restore.log` |
| `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore` after post-merge package bump | passed, 142 passed / 0 skipped | `logs/second-antshowcase-post-merge-tests.log` |
| `dotnet pack src/SkiaViewer/SkiaViewer.fsproj -c Release -o /home/developer/.local/share/nuget-local` | passed, packed `FS.GG.UI.SkiaViewer.0.1.38-preview.1` | local feed |
| `dotnet pack src/Controls.Elmish/Controls.Elmish.fsproj -c Release -o /home/developer/.local/share/nuget-local` | passed, packed `FS.GG.UI.Controls.Elmish.0.1.38-preview.1` | local feed |
| headless fail-closed responsiveness command | exit `4`, `environment-limited` | `logs/headless-fail-closed.log` |
| light live responsiveness command | exit `0`, `accepted` | `logs/live-light.log` |
| dark live responsiveness command | exit `0`, `accepted` | `logs/live-dark.log` |
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
| `resp-20260619-210350-fb50be` | light | `accepted` | 0 | `responsiveness/resp-20260619-210350-fb50be/summary.json` |
| `resp-20260619-210354-bef07e` | dark | `accepted` | 0 | `responsiveness/resp-20260619-210354-bef07e/summary.json` |

The accepted light run covers 14 interactive groups with 0 missing families and max input-to-visible `19.927ms`. The accepted dark run covers 14 interactive groups with 0 missing families and max input-to-visible `9.834ms`. Older `environment-limited` runs remain as fail-closed historical evidence.

## Post-Merge Package Evidence

The previous post-merge package proof remains under `package-feed-post-merge/` for the `0.1.35-preview.1` batch. This live-runner fix additionally packed `FS.GG.UI.SkiaViewer` and `FS.GG.UI.Controls.Elmish` at `0.1.38-preview.1` into `/home/developer/.local/share/nuget-local`, and the package-consuming SecondAntShowcase projects now reference those two package versions.

## Caveats

- Historical light/dark `environment-limited` responsiveness runs remain in the readiness folder from the earlier implementation; the latest light/dark runs are accepted and use measured GL presentation timing.
- Visual readiness is blocked: screenshots were generated, but reviewer classification remains pending in both preferred and minimum matrices.
- Elmish validation contains 17 existing skipped Feature 109 performance-golden tests.
- Controls validation contains 1 existing skipped typed-controls FSI transcript expectation.
- The first sample `--no-restore` attempt after package refresh failed because package cache entries were absent; an explicit restore from the refreshed feed passed, and the final `--no-restore` sample test passed.

## Git Ignore Proof

`git check-ignore -v` reports `.gitignore:90:!specs/173-live-responsiveness-runner/readiness/**` for:

- `specs/173-live-responsiveness-runner/readiness/validation-summary.md`
- `specs/173-live-responsiveness-runner/readiness/logs/skia-viewer-tests.log`
- `specs/173-live-responsiveness-runner/readiness/responsiveness/resp-20260619-201707-d02e83/summary.json`
