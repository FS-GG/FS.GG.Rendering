# Feature150 Validation Summary

## Current Status

Feature150 is implemented as a bounded first intrinsic-layout slice:

- `FS.GG.UI.Layout` exposes explicit normalized constraints, measurement requests/results, child placements, intrinsic queries/results, cache entries, and content extent records.
- `Layout.contentExtent` derives scrollable content extent from intrinsic queries over a `LayoutNode`, not rendered descendant bounds.
- `FS.GG.UI.Controls.Control.scrollViewport` reports content width, content height, horizontal/vertical max offsets, extent source, and diagnostics while keeping the legacy vertical `Offset`/`MaxOffset` aliases.
- `FS.GG.UI.Controls.Elmish` exposes `LayoutWorkMetrics` as a pure projection over existing `FrameMetrics`.
- `FS.GG.UI.Testing` exposes a pure `LayoutReadiness` helper for consumer-facing readiness reports.

This slice does not claim a general relational constraint solver, new compositor behavior, browser rendering, or text-shaping changes.

## Evidence Links

- Public compatibility: [compatibility-ledger.md](compatibility-ledger.md)
- ScrollViewer validation: [scrollviewer-validation.md](scrollviewer-validation.md)
- Intrinsic/cache validation: [intrinsic-cache-validation.md](intrinsic-cache-validation.md)
- Full/incremental parity: [full-incremental-parity.md](full-incremental-parity.md)
- FSI transcript: [fsi/layout-intrinsic-authoring.fsx](fsi/layout-intrinsic-authoring.fsx)

## Validation Commands

Planned and focused validation path:

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature150
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature150
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature150
dotnet build FS.GG.Rendering.slnx --no-restore
```

## Local Run Log

| Command | Verdict | Notes |
|---|---|---|
| `dotnet restore FS.GG.Rendering.slnx` | accepted | Restored missing package cache before focused validation. |
| `dotnet build src/Layout/Layout.fsproj --no-restore` | accepted | Feature150 Layout protocol surface compiles. |
| `dotnet build src/Controls/Controls.fsproj --no-restore` | accepted | ScrollViewport extent-source additions compile. |
| `dotnet build src/Controls.Elmish/Controls.Elmish.fsproj --no-restore` | accepted | Layout metrics projection compiles. |
| `dotnet build src/Testing/Testing.fsproj --no-restore` | accepted | Layout readiness helper compiles. |
| `dotnet build tests/Layout.Tests/Layout.Tests.fsproj --no-restore` | accepted | Layout Feature150 focused tests compile. |
| `dotnet build tests/Controls.Tests/Controls.Tests.fsproj --no-restore` | accepted | Controls Feature150 focused tests compile. |
| `dotnet build tests/Testing.Tests/Testing.Tests.fsproj --no-restore` | accepted | Testing readiness helper tests compile. |
| `dotnet build tests/Elmish.Tests/Elmish.Tests.fsproj --no-restore` | accepted | Controls.Elmish metrics tests compile. |
| `dotnet build tests/Package.Tests/Package.Tests.fsproj --no-restore` | accepted | Package compatibility and FSI transcript tests compile. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --no-build --filter Feature150` | accepted | 10 passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature150` | accepted | 4 passed. |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --filter Feature150` | accepted | 1 passed. |
| `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-build --filter Feature150` | accepted | 2 passed. |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-build --filter Feature150` | accepted | 3 passed. |
| `dotnet build FS.GG.Rendering.slnx --no-restore` | accepted | Solution build passed with 0 warnings and 0 errors. |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | accepted | Refreshed Layout, Controls, Controls.Elmish, and Testing public surface baselines with intentional additive Feature150 deltas. |
| `dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local` | accepted | Source packages packed at `0.1.13-preview.1`. |
| `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local` | accepted | Template package packed at `0.1.7-preview.1`. |

## Limitations

- The first slice adds explicit intrinsic/query/cache contracts and ScrollViewer extent readback; it does not replace Yoga or introduce a solver.
- Intrinsic extent currently evaluates a content `LayoutNode` under explicit large query bounds and records dependency identities; future work can optimize the evaluator internals without changing the public query contract.
- Readiness evidence is focused on Feature150 filters and package build gates, not broad visual or compositor proof.
- The full representative corpus, evaluator-internal measured/intrinsic cache reuse, retained rendering regression sweep, and full solution test remain open follow-up work.
