# Quickstart: Layout Attributes and Metrics Green

## Prerequisites

- .NET SDK that can build the repository target framework (`net10.0`).
- Repository root: `/home/developer/projects/FS.GG.Rendering`.
- No GL/window-system setup is required for the focused validation in this feature.

## Validate Layout Authoring

Run the Controls tests:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj
```

Expected outcome after implementation:

- Authored padding, margin, gap, alignment, flex grow/shrink/basis, and min/max constraints affect measured
  bounds in the same frame.
- Explicit zero values override compatibility defaults.
- No-authored-value compatibility cases keep their previous bounds.
- The shell-chrome proof keeps header, footer, and navigation fixed while content receives remaining space.
- `Feature101LayoutDriftGuardTests` reports no drift between discovered geometry names and
  `layoutAffectingAttrNames`.

## Validate Incremental Layout Equivalence

Run the Layout tests:

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj
```

Expected outcome after implementation:

- Incremental layout remains byte-identical to full layout.
- Dirty-set propagation remains bounded and deterministic.
- No new Layout/Yoga dependency or public API behavior is required beyond the existing `LayoutIntent` fields.

## Validate Public Frame Metrics

Run the Elmish metrics tests:

```bash
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj
```

Expected outcome after implementation:

- A cold text-heavy retained frame reports `TextMeasureCacheHitCount = 0` and
  `TextMeasureCacheMissCount > 0`.
- The next equivalent warm frame reports `TextMeasureCacheHitCount > 0` and
  `TextMeasureCacheMissCount = 0`.
- Style-only and idle frames report zero text misses, zero layout invalidations, and zero re-measured nodes.
- Repeated same-sequence captures produce identical metric tuples.

## Validate Public Surface

Run the package surface check:

```bash
./fake.sh build -t PackageSurfaceCheck
```

Expected outcome after implementation:

- `FS.GG.UI.Controls` surface baselines are updated for intentional public builder additions.
- `FS.GG.UI.Layout` surface remains unchanged unless implementation discovers a required public change.
- `FS.GG.UI.Controls.Elmish` surface remains unchanged unless metric field shape changes, which is not
  expected.

## Broad Preflight

Before feature readiness sign-off, run:

```bash
./fake.sh build -t VerifyPreflight
```

Expected outcome:

- Layout, Controls, Elmish, package surface, and generated product checks complete without failing tests.
- If a broad runner reports an environment failure unrelated to this headless feature, record the focused
  green commands and rerun broad verification in a healthy shell/CI environment.

## Validation Results - 2026-06-17

- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138LayoutAttributes`: PASS (6 passed).
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature101LayoutDriftGuard`: PASS (14 passed).
- `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Feature138IncrementalLayout`: PASS (13 passed).
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature138ShellChrome`: PASS (3 passed).
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature117TextCache`: PASS (7 passed).
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature138TextMetrics`: PASS (3 passed).
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj`: PASS (760 passed, 1 skipped).
- `dotnet test tests/Layout.Tests/Layout.Tests.fsproj`: PASS (45 passed).
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj`: PASS (156 passed, 17 skipped).
- `dotnet build FS.GG.Rendering.slnx`: PASS (0 warnings, 0 errors).
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: PASS; `tests/surface-baselines/FS.GG.UI.Controls.txt` had no diff.
- `./fake.sh build -t PackageSurfaceCheck`: BLOCKED in this checkout because root `./fake.sh` is missing (`No such file or directory`).
- `./fake.sh build -t VerifyPreflight`: BLOCKED in this checkout because root `./fake.sh` is missing (`No such file or directory`).
- `dotnet test tests/Package.Tests/Package.Tests.fsproj`: BLOCKED as a substitute package/surface gate; the project fails on missing historical readiness artifacts and missing `readiness/surface-baselines`, not on Feature 138 code.
