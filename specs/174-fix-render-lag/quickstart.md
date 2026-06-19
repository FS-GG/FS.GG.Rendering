# Quickstart: Fix Render Lag

Run commands from the repository root unless noted.

## Prerequisites

- .NET SDK for `net10.0`.
- A visible, focusable desktop session capable of opening the OpenGL/Skia viewer for accepted latency evidence.
- Local package feed at `~/.local/share/nuget-local` when validating the package-consuming sample.
- Headless CI can run deterministic checks and environment-limited paths, but it cannot produce accepted live responsiveness evidence.

## Build

```sh
dotnet build FS.GG.Rendering.slnx -c Release
```

Expected outcome: repository builds without new warnings or errors.

## Focused Framework Regression Checks

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
```

Expected outcome: retained render parity, work-scaling, frame metrics, routing, timing contribution, and viewer diagnostics tests pass. Any skipped test must use the test framework skip mechanism with rationale.

## Package-Consuming Sample Checks

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase
dotnet nuget locals global-packages --clear
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore
```

Expected outcome: SecondAntShowcase consumes freshly packed `FS.GG.UI.*` packages and all sample tests pass, including Feature 174 render-lag, parity, fail-closed, and responsiveness budget tests once implemented.

## Headless Fail-Closed Check

Run where no live presentation boundary is available, or force substitute mode:

```sh
FS_GG_RESPONSIVENESS_FORCE_SUBSTITUTE=1 dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json
```

Expected outcome: command exits non-zero, writes diagnostics when possible, reports `environment-limited` or `blocked`, and never reports `accepted`.

## Render Lag Probe Diagnostics

Run in a visible desktop session:

```sh
FS_GG_RENDER_LAG_TRACE=1 dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario button-click --theme light 2> specs/174-fix-render-lag/readiness/render-lag/button-click.trace.log

FS_GG_RENDER_LAG_TRACE=1 dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- render-lag-probe --scenario page-change --theme light 2> specs/174-fix-render-lag/readiness/render-lag/page-change.trace.log
```

Expected outcome: each probe exits `0`, prints viewer outcome and metrics count, and writes trace lines that separate product view/stamp, retained init/step, layout, metadata/render result preparation, paint, and presentation where available.

## Accepted Live Responsiveness Evidence

Run in a visible desktop session:

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out specs/174-fix-render-lag/readiness/responsiveness --json
```

Expected outcome: each command exits `0` only when accepted measured live evidence is written. Exit `4` means live evidence is blocked or environment-limited. Exit `5` means live evidence ran but failed an acceptance budget.

Required review points:

- button activation median <= 150 ms and p95 <= 250 ms
- page navigation median <= 250 ms and p95 <= 500 ms
- largest non-paint preparation contribution reduced by >= 80% from the 2026-06-19 baseline
- first-frame preparation reduced by >= 50% where the same bottleneck is present
- no required scenario silently skipped
- no substitute or environment-limited evidence counted as accepted

## Visual and Interaction Parity

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/174-fix-render-lag/readiness/evidence
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/174-fix-render-lag/readiness/visual-parity/preferred
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/174-fix-render-lag/readiness/visual-parity/minimum
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/174-fix-render-lag/readiness --fail-on-unresolved
```

Expected outcome: coverage remains clean and visual readiness reports no unintended changes to the showcase.

## Public Surface Check

```sh
dotnet fsi scripts/refresh-surface-baselines.fsx
git status --short tests/surface-baselines/
```

Expected outcome: no public surface baseline changes. Any delta means the feature has left Tier 2 and the plan/spec must be revised before implementation closeout.

## Final Readiness Review

Create or update `specs/174-fix-render-lag/readiness/validation-summary.md` with:

- commands run and exit codes
- baseline and optimized phase values
- live responsiveness run directories
- render-lag probe trace paths
- parity and visual evidence paths
- all blocked, environment-limited, substitute, skipped, timed-out, degraded, or manual-review-pending caveats

The final report is accepted only when no caveat is summarized as green and supported live runs satisfy the feature budgets.
