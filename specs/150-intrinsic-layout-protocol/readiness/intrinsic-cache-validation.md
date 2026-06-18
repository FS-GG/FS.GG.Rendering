# Feature150 Intrinsic Cache Validation

## Covered Inputs

Focused tests cover deterministic cache identity and invalidation evidence for:

- constraints/query identity;
- content size changes;
- visibility changes;
- child order changes;
- intrinsic content extent dependency keys.

## Commands

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicCache
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicInvalidation
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature150LayoutMetrics
```

## Local Verdicts

| Command | Verdict | Notes |
|---|---|---|
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --no-build --filter Feature150` | accepted | 10 passed, including intrinsic cache and invalidation focused tests. |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --filter Feature150` | accepted | 1 passed for `ControlsElmish.layoutMetrics`. |

## Accepted Evidence

- `Layout.cacheEntry` returns stable ids for equivalent full keys and distinct ids when layout input keys change.
- `Layout.layoutInputKey` changes when layout-affecting inputs change.
- `Layout.contentExtent` returns intrinsic dependency keys for ScrollViewer consumers.
- `ControlsElmish.layoutMetrics` projects deterministic layout work and invalidation counts without I/O in update logic.

## Limitations

Measured and intrinsic cache entries are public, deterministic contract records in this slice. A future evaluator-internal cache can consume these identities directly.
