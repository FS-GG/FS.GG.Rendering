# Quickstart: Live Responsiveness Runner

Run commands from the repository root unless noted.

## Prerequisites

- .NET SDK for `net10.0`.
- A visible, focusable desktop session capable of opening the OpenGL/Skia viewer.
- Local package feed at `~/.local/share/nuget-local`.
- The live responsiveness commands must be run where the desktop session can present frames; headless CI can only produce non-accepted diagnostics.

## Build and Framework Regression Checks

```sh
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
```

Expected outcome: input queue, presentation timing diagnostics, retained routing, responsiveness summary helpers, and existing interaction semantics pass. Skipped tests must use the test framework skip mechanism with rationale.

## Refresh Package-Consuming Sample

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase
dotnet nuget locals global-packages --clear
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore
```

Expected outcome: the sample consumes freshly packed `FS.GG.UI.*` packages and all sample tests pass, including Feature 173 CLI, artifact, budget, coverage, fail-closed, and regression tests.

## Headless Fail-Closed Check

Run in an environment without a live presentation boundary:

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/173-live-responsiveness-runner/readiness/responsiveness --json
```

Expected outcome: the command exits non-zero, writes `summary.json` and `environment.md` when possible, and reports `environment-limited` or `blocked`. It must not report `accepted`.

## Visible Live Responsiveness Evidence

Run in a visible desktop session:

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/173-live-responsiveness-runner/readiness/responsiveness --json
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out specs/173-live-responsiveness-runner/readiness/responsiveness --json
```

Expected outcome: each command exits `0` only when accepted measured live evidence is written. Exit `4` means live evidence is blocked or environment-limited. Exit `5` means live evidence ran but failed an acceptance budget or drag-continuity rule.

Review each run directory for:

- `summary.json.overallReadiness = "accepted"`
- every required interactive family accepted or explicitly display-only excluded
- at least 95% of representative actions at or below 100 ms
- no accepted action above 150 ms
- value-changing drag actions classified as `continuous`
- no environment limitations or artifact write failures

## Coverage and Existing Evidence

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/173-live-responsiveness-runner/readiness/evidence
```

Expected outcome: coverage remains clean, all catalog controls are mapped or display-only excluded, and deterministic evidence remains clearly separate from accepted live responsiveness.

## Visual Regression Checks

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/173-live-responsiveness-runner/readiness/visual-preferred
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/173-live-responsiveness-runner/readiness/visual-minimum
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/173-live-responsiveness-runner/readiness --fail-on-unresolved
```

Expected outcome: no regression in opaque backgrounds, Ant-like navigation appearance, mapped-control coverage, slider/rating behavior, or visual readiness.

## Final Readiness Review

Create or update `specs/173-live-responsiveness-runner/readiness/validation-summary.md` with:

- commands run and exit codes
- live responsiveness run directories
- first failed budget and slowest interactions for any rejected run
- all blocked, environment-limited, substitute, skipped, timed-out, degraded, or manual-review-pending caveats
- visual and interaction regression results

The final report is accepted only when no caveat is summarized as green and the live responsiveness runs are accepted.
