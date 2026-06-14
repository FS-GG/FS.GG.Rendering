# Implementation Plan: Import Selected Source (Migration Stage R4)

**Branch**: `003-import-selected-source` *(git-extension before_specify hook)* | **Date**: 2026-06-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-import-selected-source/spec.md`

## Summary

Import the selected FS-Skia-UI product source into this repo as a compiling, testable
product: all R2 `import-from-source` runtime libraries, the controls + design-system/theme/
kit modules, the `dotnet new` template, and the R3 `import-now` test set — with provenance,
governance-reference removal, and the constitution's `.fsi`/baseline/identity invariants.
This is a **Tier 1** change (introduces the product's public API surface and dependencies).

**Material correction from source inspection** (see `research.md`): the source is **already
SkiaSharp-over-OpenGL** — the Vulkan→GL migration happened upstream ("feature 119"). There is
no Vulkan backend (`Silk.NET.OpenGL` 2.23.0, `GRGlInterface`, `Host/OpenGl.fs`; no
`Silk.NET.Vulkan`/`GRVkBackend`). So FR-005/SC-005 are **satisfied by the source already**;
the work is *cleanup* of vestigial `Vulkan` naming (a `ViewerBackendPreference.Vulkan` case
that already returns "Vulkan backend is no longer supported … presents through OpenGL", plus
stale comments), not a backend port.

## Technical Context

**Language/Version**: F# on .NET `net10.0`, `LangVersion` latest.

**Primary Dependencies** (central package management, `Directory.Packages.props`):
SkiaSharp `4.147.0-preview.3.1` (+ `NativeAssets.Linux/Win32`) — preview, explicitly pinned;
`Silk.NET.OpenGL`/`Input`/`Windowing` `2.23.0`; `Elmish` `5.0.2`.

**Storage**: N/A (UI runtime libraries).

**Testing**: The R3 `import-now` set — per-module unit tests + `Smoke.Tests` (GL),
`surface-baselines` + `refresh-surface-baselines.fsx`, `fsdocs` docs build, `Package.Tests`,
and the template `Product.Tests`. Default local tier = the per-module unit + smoke tests.

**Target Platform**: Linux/Windows desktop (SkiaSharp over GL); dev container provides
hardware GL (per R3 capability baseline) so GL unit/smoke tests run locally.

**Project Type**: Multi-project F# solution (runtime libraries + tests + template).

**Performance Goals**: N/A for the import (behavior preserved from source).

**Constraints**: Constitution v1.0.0 — `.fsi` per public module, no `.fs` access modifiers,
surface-area baselines, `net10.0`, SkiaSharp pinned, GL backend, `FS.Skia.UI.*` identity, no
governance runtime. Import bounded by `docs/product/module-map.md` + `docs/validation/validation-set.md`.

**Scale/Scope**: 10 runtime library projects (~28k LOC), 11 import-now test projects, 1
template; source commit `f759f399`.

**Dependency order (topological, from `ProjectReference`s)**:
`Scene` → {`Color`,`Layout`,`KeyboardInput`,`Testing`} → `SkiaViewer` (needs KeyboardInput,
Scene) → {`Input`,`Elmish`} (need SkiaViewer) → `Controls` (needs Scene,Layout,KeyboardInput)
→ `Controls.Elmish` (needs Controls,KeyboardInput,SkiaViewer). Import/build in this order.

**Package identity (resolved)**: each `src/**/*.fsproj` explicitly sets
`<PackageId>FS.Skia.UI.<Module></PackageId>` + matching `<AssemblyName>` (e.g.
`FS.Skia.UI.Color`); `Directory.Build.props` carries shared package metadata. So `FS.Skia.UI.*`
identity is per-project and preserved unchanged (FR-010). **Bonus**: `Directory.Build.props`
promotes `FS0078` to an error, so the build itself enforces "no `private`/`internal` on
top-level bindings with a companion `.fsi`" (FR-007 is build-enforced, not just swept).
**Governance to exclude**: `build/Governance/FS.Skia.UI.Build.fsproj` (compiled-F# evidence-
graph/merge-gate engine) and the template's "governed" profiles are governance machinery and
MUST NOT be imported (FR-006).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Unlike R2/R3, this is real code — the code-centric principles **apply directly**. The source
is an existing FSI-first codebase, so the import preserves rather than re-derives compliance.

| Principle | Assessment |
|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | The normal order is *inverted* here: the code already exists. Justified — this is an **import of an already-FSI-first, already-tested codebase**; every module ships its `.fsi` and the R3 `import-now` tests come with it. Future *changes* follow the normal order. **PASS (justified inversion)** |
| II. Visibility in `.fsi` | Source already has `.fsi` for every module. Import task MUST verify no `.fs` top-level access modifiers remain (strip → `.fsi` if found). **PASS w/ verify** |
| III. Idiomatic Simplicity | Imported as-is; no new cleverness introduced. **PASS** |
| IV. Elmish/MVU boundary | `Elmish` + `Controls.Elmish` imported with their MVU/effect boundaries intact. **PASS** |
| V. Test Evidence Mandatory | The import-now tests run as the verification (SC-002). Environment-blocked tests are skip-with-rationale, never weakened. **PASS** |
| VI. Observability & Safe Failure | Viewer ships `Host/Diagnostics`; GL smoke distinguishes a defect from a missing window-system. **PASS** |
| Engineering Constraints | `net10.0` ✓, SkiaSharp preview pinned ✓, `.fsi` per module ✓, surface baselines (import/regenerate) ✓, layering (`DesignTokens`/`Theme`/`Style` modules) ✓, GL backend ✓ (already GL), `FS.Skia.UI.*` identity (verify) ⚠. **PASS w/ verify** |

**Change Classification**: **Tier 1** — introduces public API surface, dependencies, and
package contracts. Full artifact chain applies (`.fsi`, baselines, test evidence, docs).

**Result**: No violations. Two verify-items (no `.fs` access modifiers; `FS.Skia.UI.*`
package-ID mapping) are tracked as import tasks, not blockers. Complexity Tracking not
required.

## Project Structure

### Documentation (this feature)

```text
specs/003-import-selected-source/
├── plan.md, spec.md, research.md, data-model.md, quickstart.md
├── contracts/
│   ├── import-manifest.schema.md     # what is imported, from where, disposition
│   ├── provenance.schema.md          # source repo + commit + copied paths
│   └── build-and-test-contract.md    # the R4 acceptance contract
└── checklists/requirements.md
```

### Source Code (repository root) — created by this feature

```text
FS.GG.Rendering.sln
Directory.Build.props          # net10.0, LangVersion, Version, Authors (adapted)
Directory.Packages.props       # SkiaSharp (pinned preview), Silk.NET.OpenGL, Elmish
src/
├── Scene/  Color/  Layout/  Input/  KeyboardInput/   # leaf + near-leaf libs
├── SkiaViewer/                                        # GL viewer/host (Host/OpenGl.fs)
├── Elmish/                                            # MVU integration
├── Controls/                                          # controls + DesignTokens/Theme/Style/kits (layered as modules)
├── Controls.Elmish/                                   # controls MVU bindings
└── Testing/                                           # test helpers
tests/
├── Color.Tests/ Scene.Tests/ Layout.Tests/ Input.Tests/ KeyboardInput.Tests/
├── Elmish.Tests/ SkiaViewer.Tests/ Controls.Tests/ Testing.Tests/ Lib.Tests/
├── Smoke.Tests/                                       # GL smoke
├── surface-baselines/ (+ refresh-surface-baselines.fsx)
└── Package.Tests/                                     # release-only
template/                                              # dotnet new template + Product.Tests
docs/                                                  # selected controls docs/ADRs (current only)
PROVENANCE.md                                          # FR-008 provenance record
```

**Structure Decision**: Mirror the source's multi-project layout (it is already FSI-first,
GL, and `net10.0`), importing in dependency order. The four UI layers are honored at the
**module** level inside `Controls` (`DesignTokens`, `Theme`, `Style`, kit modules) per
`docs/product/layering.md` — **no risky project-split** during import (preserves the single
control set; a project-level split is a later, separately-justified change). `SkillSupport`,
`Governance.Tests`, `SkillSupport.Tests`, and historical readiness logs are **not** copied.

## Complexity Tracking

No constitution violations — section intentionally empty. (The Principle I order-inversion is
inherent to importing an existing codebase and is justified above, not a complexity debt.)
