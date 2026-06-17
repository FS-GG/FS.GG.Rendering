# Quickstart: Shared Assembly Extraction Validation

## Prerequisites

- Work from repository root: `/home/developer/projects/FS.GG.Rendering`
- Use branch `139-shared-assembly-extraction`
- Ensure `.specify/feature.json` points to `specs/139-shared-assembly-extraction`

## Focused Feature Validation

Run the new focused Feature 139 suite after tests are added:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139
```

Expected outcome:

- The shared current-node assembly boundary is directly exercised.
- Nested clipping, offsets, cache boundaries, overlays, empty content, and warm retained reuse are covered.
- Immediate, retained initial, and retained warm outputs are equivalent for focused fixtures.

## Existing Compatibility Oracles

Run the existing tests that guard the behavior this refactor must preserve:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature137
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_PictureCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_MemoCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_TextCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature096
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature097
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Audit
```

Expected outcome:

- Full rendering remains byte-identical to retained initialization and retained warm steps.
- Cache-enabled and cache-disabled paths remain equivalent.
- Existing overlay and clipping behavior remains unchanged.
- Incremental layout compatibility remains unchanged.
- Existing work-reduction metrics still prove bounded recomputation and no extra full-tree rendering pass.

## Surface and Broad Verification

Run public surface and broad preflight checks before readiness:

```bash
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t ControlsRenderingCheck
./fake.sh build -t VerifyPreflight
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release
dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature139-harness
```

Expected outcome:

- `PackageSurfaceCheck` reports no intentional public surface drift.
- Rendering checks require no intentional baseline updates.
- `VerifyPreflight` completes with zero new failures attributable to feature 139.
- SkiaViewer tests pass, or GL/window-system limitations are recorded as environment limitations.
- Rendering.Harness offscreen emits pass evidence, or records a clean headless/GL skip with `run.json` status.

## Evidence to Record During Implementation

Record the final command results in this file or the implementation readiness notes:

- Feature 139 focused test result
- Existing parity/audit oracle results
- Existing work-reduction metric oracle results
- Surface check result
- SkiaViewer and Rendering.Harness gate results
- Broad preflight result or any environment/pre-existing limitation, including command, observed status,
  environment facts, and why it is not attributable to feature 139
- Confirmation that later-phase semantics remained out of scope

## Feature 139 Assembly Owner

The final current-node assembly owner is `ControlInternals.assembleCurrentNode` in
`src/Controls/Control.fs`, declared in `src/Controls/Control.fsi`. It accepts a node's own paint, evaluated
box, and already-assembled child results, then returns the node's `InFlowScene` and `OverlayScene`.

Routed call sites:

- `Control.renderTree` immediate paint recursion
- `RetainedRender.init` first-frame build
- `RetainedRender.step` fresh rebuild for inserted or replaced nodes
- `RetainedRender.step` carry rebuild for shifted kept nodes
- `RetainedRender.step` changed-node update rebuild
- `RetainedRender.step` cache/replay emit walk

Later-phase exclusions for this feature remain explicit: no modifier algebra, no portals, no public IR changes,
no intrinsic layout protocol changes, no text shaping changes, no compositor changes, and no portable protocol
changes.

## Validation Results - 2026-06-17

Focused and compatibility results:

| Command | Status | Evidence |
|---|---:|---|
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139` | PASS | 8 passed, 0 failed. Direct assembly contract, immediate/retained visible parity, source ownership guard, and scope fence passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature137` | PASS | 9 passed, 0 failed. Existing clipping and overlay behavior remains green. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091` | PASS | 14 passed, 0 failed. Retained identity/parity/work-reduction checks passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092` | PASS | 9 passed, 0 failed. Chained retained parity and first-frame checks passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_PictureCache` | PASS | 5 passed, 0 failed. Includes Feature139 clipped/offset cache-disabled parity fixture. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_MemoCache` | PASS | 5 passed, 0 failed. Memo cache parity and effectiveness checks passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_TextCache` | PASS | 5 passed, 0 failed. Text cache parity and key-completeness checks passed. |
| `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Audit` | PASS | 4 passed, 0 failed. Incremental layout audit remains green. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature096` | PASS | 19 passed, 0 failed. Existing runtime visual-state/work-reduction checks passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature097` | PASS | 5 passed, 0 failed. Existing incremental layout wiring checks passed. |
| `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` | PASS | 768 passed, 1 skipped, 0 failed. Full Controls suite passed. |
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` | PASS | 97 passed, 0 failed. Release SkiaViewer tests passed. |
| `dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature139-harness` | PASS | Wrote `artifacts/feature139-harness/T1/run.json`; status `passed`, proof level `offscreen-pixels`, X11 display `:1`, direct GL renderer `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`. |

Validation limitations and pre-existing repository gaps:

| Command | Status | Evidence |
|---|---:|---|
| `./fake.sh build -t PackageSurfaceCheck` | BLOCKED | Repo root has no `./fake.sh` (`/usr/sbin/bash: line 1: ./fake.sh: No such file or directory`). Only template fake scripts exist under `template/base/`. Not attributable to Feature139. |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface` | PRE-EXISTING FAIL | 4 passed, 8 failed due missing `readiness/surface-baselines/*.txt`, missing `scripts/controls-prelude.fsx`, and an empty package surface report. These required baseline/script artifacts are absent before Feature139 changes and are not touched by this refactor. |
| `./fake.sh build -t ControlsRenderingCheck` | BLOCKED | Repo root has no `./fake.sh`. Not attributable to Feature139. |
| `./fake.sh build -t VerifyPreflight` | BLOCKED | Repo root has no `./fake.sh`. Not attributable to Feature139. |

Scope confirmations:

- `git diff -- src/Scene/Scene.fsi src/SkiaViewer/SceneRenderer.fs tests/surface-baselines/FS.GG.UI.Scene.txt readiness/surface-baselines/FS.GG.UI.Scene.txt` produced no diff.
- Feature139 changed no public Scene IR file and no SkiaViewer renderer file.
- The final diff was reviewed against `contracts/assembly-compatibility.md`: current-node assembly lives in one owner, all retained scene assembly call sites route through it, and later-phase semantics remain out of scope.
