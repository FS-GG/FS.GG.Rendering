# Quickstart: Fix Mouse Interaction Lag

Run commands from the repository root unless noted.

## Prerequisites

- A visible desktop session capable of opening the OpenGL/Skia viewer.
- Local package feed at `~/.local/share/nuget-local`.
- `dotnet` SDK for `net10.0`.

## Build and Deterministic Regression Checks

```sh
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
```

Expected outcome: pointer semantics, retained routing, input queue, responsiveness summary,
and viewer diagnostics tests pass. Skipped tests must be existing environment skips with
written rationale, not newly hidden failures.

For this implementation run, use an explicit timeout wrapper when recording full validation
so a stalled test process remains a visible caveat rather than being summarized as green:

```sh
timeout 240s dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release
```

Exit code `124` from `timeout` means the project did not complete and must be recorded as a
timed-out validation caveat.

## Refresh Package-Consuming Sample

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/SecondAntShowcase
dotnet nuget locals global-packages --clear
dotnet build samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release --no-incremental
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release --no-restore
```

Expected outcome: the sample consumes freshly packed `FS.GG.UI.*` packages and all sample
tests pass.

## Coverage and Existing Evidence

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- coverage
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- evidence --seed 1 --out specs/172-fix-mouse-lag/readiness/evidence
```

Expected outcome: coverage remains clean with all catalog controls mapped and no duplicated
controls. Evidence output is written under this feature's readiness directory.

## Visible Responsiveness Evidence

Run in a visible desktop session:

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme light --all-interactive --require-live --out specs/172-fix-mouse-lag/readiness/responsiveness --json
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- responsiveness --script representative --theme dark --all-interactive --require-live --out specs/172-fix-mouse-lag/readiness/responsiveness --json
```

Expected outcome: each command exits `0` only when accepted measured live evidence is written.
Exit `4` means the feature is blocked or environment-limited, not accepted. Exit `5` means
live evidence ran but failed the timing budget.

Review each `summary.json` and `summary.md` for:

- `overallReadiness = accepted`.
- Every interactive family has accepted evidence or an explicit display-only exclusion.
- At least 95% of measured pointer actions are at or below 100 ms.
- No accepted pointer action exceeds 150 ms.
- Drag/value-changing actions show continuous visible feedback.

## Visual Regression Checks

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1600x1000 --themes light,dark --out specs/172-fix-mouse-lag/readiness/visual-preferred
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- visual-readiness --seed 1 --size 1280x800 --themes light,dark --out specs/172-fix-mouse-lag/readiness/visual-minimum
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- review-findings --out specs/172-fix-mouse-lag/readiness --fail-on-unresolved
```

Expected outcome: no black transparent regions, no primary-filled navigation rail regression,
no loss of mapped-control coverage, and no unresolved visual findings.

## Manual Review

```sh
dotnet run --no-build --project samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj -c Release -- interactive buttons --theme light
```

Exercise navigation, buttons, switches, selectors, overlays, and value-changing controls.
The reviewer must not report unchanged laggy mouse interaction.
