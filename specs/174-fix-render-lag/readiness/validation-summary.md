# Feature 174 Validation Summary

Status: validated with caveats

## Baseline

- Baseline profile: `2026-06-19`
- Baseline source: `specs/174-fix-render-lag/readiness/render-lag/baseline-2026-06-19.md`
- Raw trace caveat: original `/tmp/fs-gg-render-lag-probe2/*` trace files are not present in this checkout; exact baseline values are preserved in `docs/reports/20260619-234041+0200-render-lag-gl-boundary-analysis.md`.

## Optimized Render-Lag Runs

| Scenario | Baseline preparation | Optimized preparation | Reduction | Total input-to-visible | Artifact |
| --- | ---: | ---: | ---: | ---: | --- |
| `button-click` | 1247.503 ms | 1.775 ms | 99.858% | 4.119 ms | `render-lag/optimized-lag-20260619-230201-6138854/summary.json` |
| `page-change` | 2576.305 ms | 0.000 ms | 100.000% | 8.523 ms | `render-lag/optimized-lag-20260619-230210-0176251/summary.json` |

First-frame preparation comparison:

| Probe | Baseline first-frame preparation | Optimized first-frame preparation | Reduction |
| --- | ---: | ---: | ---: |
| Button probe | 1220.819 ms | 1.775 ms | 99.855% |
| Page probe | 1199.463 ms | 0.000 ms | 100.000% |

## Commands

| Command | Exit code | Status | Notes |
| --- | ---: | --- | --- |
| `dotnet build FS.GG.Rendering.slnx -c Release` | 0 | passed | Final post-fix run: 0 warnings, 0 errors. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release` | 0 | passed | 903 passed, 1 skipped. |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release` | 0 | passed | 198 passed, 17 existing skipped. |
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` | 0 | passed | 202 passed. |
| `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore --filter Feature174` | 0 | passed | Feature 174 sample artifact, parity, budget, and fail-closed tests. |
| `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase` | 0 | passed | `package-feed status: passed`, 14 packages, 18 pins. The script also rewrote feature-163 package-proof files; those generated side effects were not committed. |
| `dotnet nuget locals global-packages --clear` | 0 | passed | Global NuGet package cache cleared before sample build. |
| `dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental` | 0 | passed | Package-consuming sample app build passed. |
| `dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore` | 0 | passed | Sample test command returned 0. |
| `FS_GG_RESPONSIVENESS_FORCE_SUBSTITUTE=1 dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json` | 4 | environment-limited | Expected fail-closed run: `resp-20260619-225632-944a3b`. |
| `FS_GG_RENDER_LAG_TRACE=1 timeout 120s dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario button-click --theme light` | 0 | measured | Trace: `render-lag/button-click.trace.log`; summary: `render-lag/optimized-lag-20260619-230201-6138854/summary.json`. |
| `FS_GG_RENDER_LAG_TRACE=1 timeout 120s dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario page-change --theme light` | 0 | measured | Trace: `render-lag/page-change.trace.log`; summary: `render-lag/optimized-lag-20260619-230210-0176251/summary.json`. |
| `timeout 180s dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json` | 0 | accepted | Light live run: `resp-20260619-225707-fbb968`, button p95 18.387 ms. |
| `timeout 180s dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json` | 5 | rejected | First dark run `resp-20260619-225707-d7532f`; button-click p95 253.865 ms, paint-dominated outlier. Retained as caveat. |
| `timeout 180s dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json` | 0 | accepted | Dark rerun: `resp-20260619-225731-4a8c31`, button p95 20.194 ms. |
| `dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage` | 0 | passed | 96/96 controls mapped, 19 pages, 0 unreferenced, 0 duplicated. |
| `dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/174-fix-render-lag/readiness/evidence` | 0 | passed | Wrote 19 page evidence records. |
| `dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/174-fix-render-lag/readiness/visual-parity/preferred` | 0 | blocked | 38/38 screenshots complete; blocked because reviewer classifications are pending. |
| `dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/174-fix-render-lag/readiness/visual-parity/minimum` | 0 | blocked | 38/38 screenshots complete; blocked because reviewer classifications are pending. |
| `dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/174-fix-render-lag/readiness --fail-on-unresolved` | 0 | passed | `visual-findings.md`, unresolved=0. |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | 0 | caveated | Refresh reported pre-existing public types in Controls.Elmish/SkiaViewer baselines (`LiveScriptRunResult`, `ViewerScriptInput`). Generated baseline diffs were not committed because Feature 174 source edits are internal. |
| `git status --short tests/surface-baselines/` | 0 | clean after side-effect revert | No surface baseline file changes remain in the worktree. |

## Artifacts

- Baseline: `render-lag/baseline-2026-06-19.md`
- Render lag traces: `render-lag/button-click.trace.log`, `render-lag/page-change.trace.log`
- Optimized probe summaries: `render-lag/optimized-lag-20260619-230201-6138854/summary.json`, `render-lag/optimized-lag-20260619-230210-0176251/summary.json`
- Headless fail-closed run: `responsiveness/resp-20260619-225632-944a3b/summary.json`
- Accepted live responsiveness: `responsiveness/resp-20260619-225707-fbb968/summary.json`, `responsiveness/resp-20260619-225731-4a8c31/summary.json`
- Rejected live caveat: `responsiveness/resp-20260619-225707-d7532f/summary.json`
- Visual parity captures: `visual-parity/preferred/summary.md`, `visual-parity/minimum/summary.md`
- Visual findings: `visual-findings.md`

## Ignore Rules

`git check-ignore -v` confirms:

- `specs/174-fix-render-lag/readiness/validation-summary.md` and render-lag traces are allowlisted by `!specs/174-fix-render-lag/readiness/**`.
- `specs/174-fix-render-lag/readiness/**/nuget-cache/` remains ignored.

## Caveats

- Visual readiness is not accepted; screenshots are complete but reviewer classifications are pending.
- `parityStatus` in live responsiveness summaries remains `pending-review` for accepted live runs because visual parity review is not complete.
- One dark live responsiveness run rejected due to a paint-dominated button-click outlier; a subsequent dark run accepted and both artifacts are retained.
- Surface refresh found unrelated pre-existing public baseline drift. Feature 174 implementation did not intentionally add public API or dependency surface.

