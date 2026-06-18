# Feature 152 Regression Validation

Status: `accepted-with-environment-limited-compositor-proof`

## Focused Validation

| Command | Result |
|---------|--------|
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature152 --no-restore` | passed, 6 tests |
| `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature152 --no-restore` | passed, 7 tests |
| `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature152 --no-restore` | passed, 3 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature152 --no-restore` | passed, 2 tests |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature152 --no-restore` | passed, 2 tests |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature152 --no-restore` | passed, 3 tests |
| `dotnet test FS.GG.Rendering.slnx --no-restore` | passed |

## Adjacent Surface Verdicts

| Surface | Current Verdict |
|---------|-----------------|
| Feature 149 diagnostics | accepted by existing baseline, rechecked through Feature152 tests |
| Deterministic readiness | accepted as context-only baseline |
| Render-anywhere | accepted as adjacent baseline |
| Overlay | accepted as adjacent baseline |
| Text shaping | accepted as adjacent baseline |
| Layout / Feature151 P8 | accepted as independent baseline |
| Package readiness | accepted with root `fake.sh` tooling limitation recorded in `package-validation.md` |
| Surface baselines | refreshed through `dotnet fsi scripts/refresh-surface-baselines.fsx`; package surface suite passed |

Feature 152 does not reopen P8 layout acceptance or convert Feature 149 environment-limited compositor evidence into a live partial-redraw claim.
