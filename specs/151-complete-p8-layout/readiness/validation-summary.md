# Feature151 P8 Validation Summary

## Current Status

Status: `accepted`

Feature151 completes the P8/R3b acceptance package on top of the Feature150 public
layout protocol. The accepted scope is representative corpus breadth, ScrollViewer
extent coverage, measured and intrinsic dependency identity evidence,
full/incremental parity, broad regression classification, compatibility review, and
package validation. No new public API delta is required by this feature.

## Evidence Links

| Area | Status | Evidence |
|---|---|---|
| Representative layout corpus | `accepted` | [corpus-validation.md](corpus-validation.md) |
| ScrollViewer corpus | `accepted` | [scrollviewer-validation.md](scrollviewer-validation.md) |
| Measured/intrinsic reuse | `accepted` | [reuse-validation.md](reuse-validation.md) |
| Full/incremental parity | `accepted` | [full-incremental-parity.md](full-incremental-parity.md) |
| Broad regression evidence | `accepted` | [regression-evidence.md](regression-evidence.md) |
| Compatibility ledger | `accepted` | [compatibility-ledger.md](compatibility-ledger.md) |
| Package validation | `accepted` | [package-validation.md](package-validation.md) |
| Limitations | `accepted` | [limitations.md](limitations.md) |

## Validation Commands

| Command | Status | Notes |
|---|---|---|
| `dotnet restore FS.GG.Rendering.slnx` | `accepted` | Restore completed locally. |
| `dotnet build FS.GG.Rendering.slnx --no-restore` | `accepted` | Solution build completed locally. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151RepresentativeCorpus` | `accepted` | Representative corpus passed. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151Diagnostics` | `accepted` | Diagnostic and failure-path checks passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature151ScrollViewerCorpus` | `accepted` | ScrollViewer extent corpus passed. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151ScrollLayoutProtocol` | `accepted` | Layout content extent protocol passed. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151MeasuredReuse` | `accepted` | Measured dependency identity checks passed. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151IntrinsicReuse` | `accepted` | Intrinsic query dependency identity checks passed. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature151FullIncrementalParity` | `accepted` | Full/cold/warm/changed incremental parity passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature151DisabledCacheParity` | `accepted` | Controls disabled-cache parity classification passed. |
| `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature151Readiness` | `accepted` | Readiness helper aggregation passed. |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature151` | `accepted` | Package compatibility/readiness checks passed. |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | `accepted` | No unexpected public surface drift. |
| `dotnet test FS.GG.Rendering.slnx` | `accepted` | Full solution validation completed locally. |
| `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local` | `accepted` | Source packages packed at `0.1.14-preview.1` after the squash merge. |
| `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local` | `accepted` | Template package packed at `0.1.8-preview.1` after the squash merge. |

## Blockers

None.

## Non-Claims

Feature151 does not replace Yoga, add a general solver, claim browser rendering,
or convert P7 live compositor evidence into accepted partial-redraw behavior.
Compositor live proof remains separately environment-limited as recorded in P7.
