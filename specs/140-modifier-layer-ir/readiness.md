# Readiness: Modifier Layer IR Foundation

## Status

Implementation status on 2026-06-17: complete with recorded validation limitations for stale/missing package wrapper gates.

The feature landed:

- Internal Controls `Composition` model for modifier effects, classification, normalization, fingerprints, ordered contributions, layer hosts, portals, diagnostics, legacy lowering, and compatibility evidence.
- Current-node assembly routing through composition normalization and chain application.
- Retained invalidation classification delegated to the shared composition table.
- Public Scene glyph-run proof data/node/constructor surface.
- SkiaViewer glyph-run proof data and drawing support.
- Focused Feature 140 Controls, Scene, and SkiaViewer tests.
- Surface baseline update for the intentional `FS.GG.UI.Scene` glyph-run public type/node additions.

## Focused Validation

| Command | Result |
|---|---|
| `dotnet restore FS.GG.Rendering.slnx` | Passed |
| `dotnet build src/Scene/Scene.fsproj --no-restore` | Passed |
| `dotnet build src/Controls/Controls.fsproj --no-restore` | Passed |
| `dotnet build src/SkiaViewer/SkiaViewer.fsproj --no-restore` | Passed |
| `dotnet build tests/Scene.Tests/Scene.Tests.fsproj --no-restore` | Passed |
| `dotnet build tests/Controls.Tests/Controls.Tests.fsproj --no-restore` | Passed |
| `dotnet build tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-restore` | Passed |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140 --no-build` | Passed: 17 tests |
| `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature140 --no-build` | Passed: 4 tests |
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature140 -c Release` | Passed: 3 tests |

## Legacy Compatibility Oracles

| Command | Result |
|---|---|
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature137 --no-build` | Passed: 9 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139 --no-build` | Passed: 8 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091 --no-build` | Passed: 14 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092 --no-build` | Passed: 9 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_PictureCache --no-build` | Passed: 5 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_MemoCache --no-build` | Passed: 5 tests |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_TextCache --no-build` | Passed: 5 tests |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Audit --no-build` | Passed: 4 tests |

## Broad Validation

| Command | Result |
|---|---|
| `dotnet build FS.GG.Rendering.slnx -c Debug --no-restore` | Passed |
| `dotnet test tests/Color.Tests/Color.Tests.fsproj --no-build` | Passed: 15 tests |
| `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --no-build` | Passed: 40 tests |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --no-build` | Passed: 45 tests |
| `dotnet test tests/Input.Tests/Input.Tests.fsproj --no-build` | Passed: 12 tests |
| `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --no-build` | Passed: 16 tests |
| `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build` | Passed: 156 tests, 17 skipped |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build` | Passed: 785 tests, 1 skipped |
| `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-build` | Passed: 38 tests |
| `dotnet test tests/Lib.Tests/Lib.Tests.fsproj --no-build` | Passed: 30 tests |
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build` | Passed: 100 tests |
| `dotnet test tests/Smoke.Tests/Smoke.Tests.fsproj --no-build` | Passed: 4 tests, 3 skipped |

## Rendering and Surface Evidence

| Command | Result |
|---|---|
| `dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature140-harness` | Passed; `artifacts/feature140-harness/T1/run.json` reports `offscreen-pixels` evidence on X11/Mesa GL. |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | Passed; only `tests/surface-baselines/FS.GG.UI.Scene.txt` changed for glyph-run types/node. |

## Wrapper and Package Gate Limitations

| Command | Result | Classification |
|---|---|---|
| `./fake.sh build -t PackageSurfaceCheck` | Failed because `./fake.sh` is not present in the repository. | Missing wrapper, not Feature 140 behavior. |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface --no-restore` | Failed because stale package tests expect missing `readiness/surface-baselines/*` and `scripts/controls-prelude.fsx` paths while the active surface generator writes `tests/surface-baselines`. | Pre-existing/stale package gate. |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter PackageApi --no-restore` | No tests matched. | No matching package API tests in current suite. |

## Visibility Review

Reviewed touched implementation files against the `.fsi` visibility rule:

- `src/Controls/Composition.fsi`/`.fs`: internal cross-file surface is declared in `.fsi`.
- `src/Controls/Control.fs`: no new public surface.
- `src/Controls/RetainedRender.fsi`/`.fs`: new classification helper is declared in `.fsi`.
- `src/Scene/Scene.fsi`/`.fs`: glyph-run public proof surface is declared in `.fsi`.
- `src/SkiaViewer/Fonts.fsi`/`.fs`: glyph-run proof helper is declared in `.fsi`.
- `src/SkiaViewer/SceneRenderer.fs`: exhaustive rendering case added; no new public surface.

Result: PASS.
