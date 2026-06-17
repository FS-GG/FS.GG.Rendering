# Quickstart: Interaction Overlay State Validation

This guide describes the validation expected after Feature 143 implementation tasks are generated and completed.

## Prerequisites

- .NET SDK with `net10.0` support.
- Repository restored from `/home/developer/projects/FS.GG.Rendering`.
- GL/offscreen support only for visual harness evidence; headless semantic tests must not depend on it.

## Baseline Commands

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx
```

Expected outcome: restore and build complete with warnings-as-errors still enforced.

## Focused Semantic Tests

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature143
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature143
```

Expected outcome:

- all eight supported surface kinds have open/dismiss/focus coverage
- pointer and keyboard activation open enabled surfaces
- disabled triggers do not open surfaces
- Escape and outside pointer dismiss only the topmost eligible surface
- modal overlays trap focus and block lower content
- product selection/command dispatch happens exactly once
- stale anchors and stale focus targets emit diagnostics without stale hit targets

## Compatibility And Regression Tests

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature140|Feature141|Feature142|PublicSurface"
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature141|Feature142"
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj
```

Expected outcome:

- Feature 140 layer/portal behavior remains compatible
- Feature 141 direct/retained parity remains compatible
- Feature 142 text behavior remains unchanged
- public surface changes are either zero or fully documented with refreshed baselines
- keyboard routing regressions are absent or documented as intentional contract changes

## Surface Baseline Check

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Expected outcome: either no public surface drift, or only the intentional overlay/runtime contract deltas with
migration guidance and versioning rationale recorded in readiness evidence.

## Interaction Replay Evidence

Run the Feature 143 replay suite once it exists:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature143Replay
```

Expected outcome:

- three repeated runs of each script produce byte-identical interaction logs
- logs include inputs, overlay transitions, focus transitions, product dispatches, dismissal reasons,
  diagnostics, and topmost hit decisions
- cache-enabled and cache-disabled runs have equivalent user-visible behavior and evidence

## Rendering Harness Evidence

When the host supports offscreen rendering:

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature143
```

Expected outcome:

- open overlays paint above covered content
- topmost hit evidence matches layer/paint order
- direct, first-frame retained, and warm retained paths match for representative overlay states
- unsupported GL/window-system conditions are recorded as limitations, not successes

## Reference Date Picker Flow

Run the showcase-level date-picker scenario once the Feature 143 tests/tasks add it:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature143ReferenceDatePicker
```

Expected outcome:

- closed date-picker starts without overlay content
- trigger activation opens the calendar near the trigger
- navigation selects one date
- exactly one product date selection is emitted
- the calendar closes
- focus returns to the trigger or field
- no stale overlay content remains visible or hit-testable

## Readiness Records

Before implementation readiness, record:

- public surface and compatibility impact
- closed-state baseline result
- direct/retained/cache parity result
- replay determinism result
- reference date-picker result
- intentional pixel/golden/diagnostic/log deltas
- validation limitations and pre-existing failures distinguished from Feature 143 behavior
