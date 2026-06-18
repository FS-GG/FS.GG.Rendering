# Quickstart: Intrinsic Layout Protocol Validation

This guide lists the validation scenarios expected after implementation. It is a run guide, not
implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Native dependencies already required by existing SkiaSharp/OpenGL tests when viewer evidence is
  run.
- Existing Feature138 layout attribute behavior, Feature141 retained renderer parity, Feature145
  overlay proof, Feature146 render-anywhere protocol, and Feature149 compositor readiness artifacts
  as regression baselines.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds with warnings as errors.

## Layout Protocol

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicProtocol
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150Diagnostics
```

Expected outcome:

- Public constraint, measure, placement, intrinsic, and diagnostic records are exercised through the
  package-visible Layout surface.
- Repeated equivalent measurement produces identical sizes, placements, cache keys, and diagnostics.
- Invalid, contradictory, unbounded, zero-sized, and fallback constraints produce actionable
  diagnostics and no accepted misleading result.
- Default Yoga-backed flex behavior remains compatible unless listed in the compatibility ledger.

## Intrinsic Cache and Invalidation

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150IntrinsicCache
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature150FullIncrementalParity
```

Expected outcome:

- Intrinsic queries are deterministic for matching participant, axis, cross-axis constraint, and
  layout input keys.
- Cache reuse occurs only when constraints/query, content identity, layout-affecting inputs, child
  dependencies, and evaluator revision match.
- At least 5 input-change categories reject stale measured or intrinsic entries.
- Cold full, warm incremental, and changed-input incremental layout produce equivalent bounds,
  placements, scroll extents, and diagnostics over the agreed corpus.

## ScrollViewer Extent

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150ScrollViewerExtent
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150LayoutDiagnostics
```

Expected outcome:

- Empty, smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested
  scroll, clipped/layered parent, text/content-driven natural size, dynamic content change, and
  invalid intrinsic fallback cases are covered.
- ScrollViewer viewport bounds stay fixed while content extent and max offset reflect natural
  content size.
- `Control.scrollViewport` reports extent from layout/intrinsic results, not descendant bounds
  inspection.
- Changed intrinsic content updates scroll range without changing unrelated surrounding layout.

## Controls, Elmish, and Regression

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature150LayoutCompatibility
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature150LayoutMetrics
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature097|Feature101|Feature117|Feature137|Feature138|Feature141"
```

Expected outcome:

- Existing default layout bounds and placements remain unchanged except documented intentional
  Feature150 deltas.
- Layout dirty-set and incremental invalidation classifiers include new intrinsic/layout-affecting
  inputs.
- Elmish metrics expose deterministic layout/intrinsic work without executing I/O in update logic.
- Retained/full rendering, overlay, and prior layout compatibility guards remain green.

## Public Surface and Package Validation

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature150
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature150
```

Expected outcome:

- Public surface baselines reflect only intentional layout protocol additions.
- Semantic FSI coverage exercises the new Layout package surface.
- Testing helpers remain consumer-validation focused and do not widen runtime dependencies.
- Compatibility ledger matches the refreshed surface baseline deltas.

## Readiness Package

```bash
dotnet test FS.GG.Rendering.slnx --filter Feature150
```

Expected outcome:

- Readiness artifacts under `specs/150-intrinsic-layout-protocol/readiness/` record validation
  status for compatibility, ScrollViewer, intrinsic/cache, full/incremental parity, diagnostics,
  limitations, and final acceptance.
- `validation-summary.md` links to the supporting evidence and states accepted, failed,
  incomplete, or limited status.

## Repository Tooling Note

The layout skill names FAKE targets such as `CapabilityCheck`, `DependencyReport`,
`PackageSurfaceCheck`, and `GeneratedProductCheck`. This checkout currently has no root
`./fake.sh`; when that wrapper is restored, run those targets and record their output in the
readiness summary. Until then, use the runnable `dotnet` and `scripts/refresh-surface-baselines.fsx`
commands above as the local validation path.
