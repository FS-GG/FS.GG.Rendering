# Feature150 Full/Incremental Parity

## Corpus

The focused first-slice corpus includes a constrained root with measured leaves, deterministic child placements, intrinsic natural-height queries, duplicate diagnostic behavior, content size changes, visibility changes, and child reorder changes.

## Commands

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150FullIncrementalParity
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150MeasureDeterminism
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicProtocol
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150Diagnostics
```

## Local Verdicts

| Command | Verdict | Notes |
|---|---|---|
| `dotnet build tests/Layout.Tests/Layout.Tests.fsproj --no-restore` | accepted | Feature150 Layout tests compile. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --no-build --filter Feature150` | accepted | 10 passed. |

## Accepted Evidence

- Warm incremental layout with no changes preserves full layout bounds for the representative constrained root.
- Repeated equivalent protocol measurement returns stable measured size, child placements, and cache identity.
- Intrinsic query results are deterministic for equivalent query inputs.

## Limitations

The full 12-case representative layout corpus remains the acceptance bar for final P8 readiness. This file records the implemented first-slice parity evidence.
