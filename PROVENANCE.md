# Provenance

**Source repository**: `EHotwagner/FS-Skia-UI`
**Source commit**: `f759f399` (2026-06-14)
**Imported at**: migration Stage R4 (feature `003-import-selected-source`)

The product source in this repository was imported from FS-Skia-UI, bounded by the R2 module
map (`docs/product/module-map.md`, `import-from-source`) and the R3 validation set
(`docs/validation/validation-set.md`, `import-now`).

This file is the **authoritative lineage** referenced by the Stage R7 bridge
(`docs/bridge/README.md`); update imported scope here, not in the bridge.

## Path map

| Source path | Repo path |
|---|---|
| `src/{Scene,Color,Layout,Input,KeyboardInput,SkiaViewer,Elmish,Controls,Controls.Elmish,Testing}` | `src/<same>` |
| `tests/{Color,Scene,Layout,Input,KeyboardInput,Elmish,SkiaViewer,Controls,Testing,Lib,Smoke}.Tests` | `tests/<same>` |
| `tests/Package.Tests` | `tests/Package.Tests` (on disk; release-only, not in solution — wired at R6) |
| `readiness/surface-baselines/*.txt` | `tests/surface-baselines/` |
| `scripts/refresh-surface-baselines.fsx` | `scripts/` |
| `template`, `.template.config`, `.template.package` | `template/`, `.template.config/`, `.template.package/` |
| `docs/FS.GG/{design-and-controls,rendering-project}.md` | `docs/imported/` |
| `Directory.Build.props`, `Directory.Packages.props` | repo root (ownership metadata adapted) |

## Adaptations

- **Governance excluded** (FR-006): `src/SkillSupport`, `build/Governance`
  (`FS.Skia.UI.Build` evidence-graph/merge-gate engine), `tests/Governance.Tests`,
  `tests/SkillSupport.Tests` were not imported. `tests/Parity.Tests` and
  `tests/ControlsPreview.Harness` were not imported (R3: rewrite-pending/deferred to R5).
- **Controls.Tests governance decoupling**: removed `CatalogTests.fs` and the
  `build/Governance/FS.Skia.UI.Build.fsproj` reference (the Feature-066 typed-catalog drift
  suite is governance-engine material, not a rendering-product test).
- **Repo-root marker**: 8 test files searched for `FS-Skia-UI.sln`; rewritten to this repo's
  solution `FS.GG.Rendering.slnx`.
- **Ownership metadata**: `Authors`/`Company`/repository URLs in `Directory.Build.props`
  changed to FS.GG at import (R4). **Package identity rebranded at Stage R8**: at import the
  projects kept the source identity `FS.Skia.UI.<Module>`; **Stage R8 rebranded that identity to
  `FS.GG.UI.<Module>`** (`<PackageId>`/`<AssemblyName>`/root namespace) as one coherent matrix, with
  the template moving to `FS.GG.UI.Template`. The imported files still trace to their source paths and
  commit (above); their import-time `FS.Skia.UI.*` identifiers now read `FS.GG.UI.*` in-tree. The full
  old→new map and acceptance record are in
  [`docs/product/decisions/0001-package-identity.md`](docs/product/decisions/0001-package-identity.md)
  and [`docs/bridge/package-identity-migration.md`](docs/bridge/package-identity-migration.md).
  (The excluded source-repo `FS.Skia.UI.Build` governance engine named below is **not** part of this
  repo and is referenced by its original source identity as history.)
- **Vulkan**: the source already presents through OpenGL (no Vulkan backend). The public
  `ViewerBackendPreference.Vulkan` case is **retained** as a graceful "unsupported, presents
  through OpenGL" result (parallel to `Software`, Principle VI). No Vulkan dependency exists.
- **Template**: imported with the top-level governance profile (`profiles/governed.yml`) and
  the `fragments/full-governance` fragment removed. Residual governance references inside
  `template/base/**` and full generated-product (template) validation are finalized at Stage
  R6 (release/template validation); the template is content, not part of the solution build.
- **Skipped tests**: 18 tests skipped with rationale — see `SKIPPED-TESTS.md` (perf-corpus/
  baseline → Stage R5 harness; one FSI-transcript check → excluded old-repo readiness artifact).
- **Build format**: solution authored as `FS.GG.Rendering.slnx` (the .NET 10 XML solution
  format that `dotnet new sln` now emits).

## Repo-authored (not imported)

These areas exist in the working tree but were **built in this repository**, not imported, so they
intentionally have no import path-map row:

- `tests/Rendering.Harness`, `tests/Rendering.Harness.Tests` — the tiered evidence CLI and its unit
  tests, authored at Stage R5 (feature `004-rendering-harness`).
- `FS.GG.Rendering.slnx` — the solution, authored here (see *Build format* under Adaptations).
- `specs/**`, `docs/**` other than `docs/imported/` — this repository's own Spec Kit + product docs.

## Named gaps

None. Every imported top-level area (`src/`, `tests/` suites, `template/`, `.template.config/`,
`.template.package/`, `docs/imported/`, `tests/surface-baselines/`, `scripts/`, and the root build
metadata) is accounted for by a Path-map row, an Adaptation, or the *Repo-authored* list above.

## Excluded (left in the source archive)

`build/**` (FAKE/governance build front-end), `readiness/` (historical), `docs/testSpecs`,
`samples/`, `Container/`, `Mailbox/`, and all old feature-workflow `specs/**` artifacts.
