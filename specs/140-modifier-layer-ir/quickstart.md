# Quickstart: Modifier Layer IR Foundation Validation

## Prerequisites

- Work from repository root: `/home/developer/projects/FS.GG.Rendering`
- Use branch `140-modifier-layer-ir`
- Ensure `.specify/feature.json` points to `specs/140-modifier-layer-ir`
- Review the contract in `contracts/modifier-layer-foundation.md`

## Focused Modifier and Layer Validation

Run the focused Feature 140 Controls suite after tests are added:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140
```

Expected outcome:

- Every supported modifier category has ordering and invalidation coverage.
- At least 12 modifier-chain normalization cases preserve output, diagnostics, and fingerprints.
- Layout-affecting changes report layout and paint invalidation.
- Paint-only and order-only changes avoid false layout invalidation.
- Local z-order is scoped to siblings and equal z-order falls back to declaration order.
- Portal content escapes ancestor clipping only through its target layer.
- Paint order and hit-test order are derived from the same ordering evidence.

## Legacy Compatibility Oracles

Run existing compatibility suites that guard behavior this feature must preserve:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature137
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_PictureCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_MemoCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_TextCache
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Audit
```

Expected outcome:

- Existing clipping and overlay scenes remain compatible or intentional differences are disclosed.
- Shared assembly extraction remains the single current-node assembly seam.
- Full rendering, retained initialization, and retained warm steps remain equivalent for affected scenes.
- Cache-enabled and cache-disabled paths remain equivalent.
- Incremental layout compatibility remains unchanged for paint-only and order-only modifier changes.

## Glyph-Run Proof Validation

The implementation adds the glyph-run proof surface, so run:

```bash
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature140
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature140 -c Release
```

Expected outcome:

- At least five deterministic sample text cases produce stable glyph-run fingerprints.
- Measured advance used for layout equals advance used for drawing in proof cases.
- Existing non-opt-in text scenes remain compatible with fallback behavior.
- Unsupported shaping requirements are recorded as deferred diagnostics, not partial implementations.

## Surface and Broad Verification

The historical wrapper targets are still listed here for traceability, but this checkout does not contain
`./fake.sh`. Use the direct `dotnet` checks below as the current validated equivalent and record wrapper
absence in readiness.

```bash
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t ControlsRenderingCheck
./fake.sh build -t VerifyPreflight
dotnet build FS.GG.Rendering.slnx -c Debug --no-restore
dotnet test tests/Color.Tests/Color.Tests.fsproj --no-build
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --no-build
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --no-build
dotnet test tests/Input.Tests/Input.Tests.fsproj --no-build
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --no-build
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-build
dotnet test tests/Lib.Tests/Lib.Tests.fsproj --no-build
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build
dotnet test tests/Smoke.Tests/Smoke.Tests.fsproj --no-build
dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature140-harness
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Expected outcome:

- Package surface evidence reports the intentional `FS.GG.UI.Scene` glyph-run type/node additions.
- Rendering checks require no unexplained baseline updates.
- Direct build/test preflight completes with zero new failures attributable to Feature 140.
- SkiaViewer tests pass, or GL/window-system limitations are recorded as environment limitations.
- Rendering.Harness offscreen emits pass evidence for portal/layer and glyph-run scenarios, or records a clean
  headless/GL limitation with `run.json` status.

Observed Feature 140 status on 2026-06-17:

- `dotnet build FS.GG.Rendering.slnx -c Debug --no-restore`: passed.
- Focused Feature 140 Controls, Scene, and SkiaViewer suites: passed.
- Legacy compatibility oracles for Feature137, Feature139, Feature091, Feature092, cache audits, text cache,
  picture cache, and layout audit: passed.
- Default deterministic test projects and GL-local SkiaViewer/Smoke suites: passed.
- Offscreen harness: passed with `artifacts/feature140-harness/T1/run.json`.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed with only the intended
  `tests/surface-baselines/FS.GG.UI.Scene.txt` diff.
- `./fake.sh` wrapper targets: not runnable because `./fake.sh` is absent.
- `tests/Package.Tests` surface filter: stale path expectation, recorded in readiness.

## Evidence to Record During Implementation

Record the final command results in this file or readiness notes:

- Focused Feature 140 Controls result
- Legacy compatibility oracle results
- Glyph-run proof result for Scene data and Skia drawing
- Surface check result and any updated baseline paths
- Rendering.Harness output path and pass/failure classification
- Public surface compatibility plan and versioning recommendation for every intentional delta
- Pixel disclosure ledger entries for every intentional visual baseline change
- Verification limitations and pre-existing failures, including command, observed status, environment facts, and
  why they are not attributable to feature 140
- Confirmation that R1b retained unification, R4 overlay interaction state, R5 portable serialization, R6
  compositor work, full R7 shaping, and intrinsic layout stayed out of scope
