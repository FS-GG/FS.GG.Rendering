# Phase 1 — Data Model: Rebrand Package Identity (R8)

The "entities" here are identity artifacts and their states, not runtime data. R8 transitions each
from its `FS.Skia.UI.*` form to its `FS.GG.UI.*` form (or, for governance/notice artifacts, from
*deferred / retained / not-applied* to *accepted / rebranded / recorded*).

## Entity: Package-identity decision (`0001`) — the governance keystone

- **Fields**: status; date; decision text; old→new mapping; publish-before-deprecate rule;
  new starting version; old-ID freeze note; rationale; revisit conditions.
- **State transition**: `deferred` → **`accepted` (rebrand to `FS.GG.UI.*`)**.
- **Validation** (FR-001, SC-001): status is no longer *deferred*; names the complete old→new map
  for all 10 modules + template; states publish-before-deprecate; records starting version
  `0.1.0-preview.1` and that old IDs freeze; rationale + revisit conditions intact.
- **Authorizes**: every downstream file change in R8.

## Entity: Runtime package identity (×10 modules)

Modules: `Color, Scene, Layout, Input, KeyboardInput, SkiaViewer, Elmish, Controls,
Controls.Elmish, Testing`.

- **Four facets (move together)**:
  1. **Package ID** — `<PackageId>` in `src/<M>/*.fsproj`.
  2. **Root namespace** — `namespace FS.Skia.UI.<M>` in every `.fs`/`.fsi` (+ `open` in consumers).
  3. **Assembly name** — `<AssemblyName>` in the fsproj.
  4. **Identity metadata** — `<Title>` and brand text in `<Description>`.
- **State transition**: `FS.Skia.UI.<M>` → `FS.GG.UI.<M>` on all four facets simultaneously.
- **Version**: `<Version>` reset to `0.1.0-preview.1` (new lineage).
- **Validation** (FR-002, SC-002): each module reads `FS.GG.UI.<M>` on all four facets; product
  search finds zero `FS.Skia.UI.*` brand-prefix tokens in product source (except recorded
  descriptive usage).
- **Invariant — coherence**: a facet renamed without its siblings is a **failure**, not an
  intermediate success (spec Edge Case "partial/incoherent rename").
- **Invariant — descriptive preservation**: the `SkiaViewer` name and `SkiaSharp`/`Skia` references
  are NOT brand tokens and are preserved (FR-006).
- **Runtime kebab literals**: two `.fs` bodies carry the kebab brand as a runtime string —
  `src/Elmish/AnimationTick.fs` (subscription id `"fs-skia-ui"`) and `src/SkiaViewer/SkiaViewer.fs`
  (temp dir `"fs-skia-ui-runtime"`). These are brand tokens (→ `fs-gg-ui` / `fs-gg-ui-runtime`), are
  not on a `namespace` line, and the AnimationTick literal is mirrored by an assertion in
  `tests/Elmish.Tests/AnimationTickTests.fs` that moves in lockstep.

## Entity: Template identity

- **Fields**: template `identity`; `name`; `shortName`; `packagePrefix` default; classifications;
  template-package ID; template-package fsproj **file name**; verbatim `api-surface/**` FQ names;
  skill-folder names; generated-config brand references.
- **State transition**:
  - `FS.Skia.UI.Template` → `FS.GG.UI.Template` (identity + `<PackageId>` + file rename).
  - `fs-skia-ui` → `fs-gg-ui` (short name; `release.yml` `dotnet new` invocation follows).
  - `template/**/fs-skia-*` skill folders → `fs-gg-*` (dir rename + `template.json` `source` paths +
    generated configs + docs cross-refs).
  - `api-surface/**/*.fsi` FQ `FS.Skia.UI.*` → `FS.GG.UI.*`.
- **Validation** (FR-003, SC-004): generated project references only `FS.GG.UI.*`; template package
  ID is `FS.GG.UI.Template`; restore + build succeed (generated-consumer contract holds); zero
  `FS.Skia.UI.*` in generated output.

## Entity: Surface-area baselines (×9 of 10 modules)

- **Fields**: baseline file name; fully-qualified type/member lines.
- **Coverage note**: only **9** of the 10 runtime modules are baseline-tracked. **Color** is
  intentionally excluded from `scripts/refresh-surface-baselines.fsx` (it has a public `.fsi` surface —
  `Contrast.fsi`, `Palettes.fsi` — but no committed baseline). Color's invariance is verified by `.fsi`
  inspection, not a baseline diff.
- **State transition**: `tests/surface-baselines/FS.Skia.UI.<M>.txt` → `FS.GG.UI.<M>.txt`
  (file rename); every FQ line re-prefixed.
- **Validation** (FR-005, SC-005): the 9 baselines regenerated via
  `scripts/refresh-surface-baselines.fsx`, normalized old↔new diff is **prefix-only** — zero
  added/removed/retyped members; Color confirmed prefix-only by `.fsi` inspection. See
  `contracts/surface-invariance.md`.

## Entity: Replacement packages

- **Fields**: ten `FS.GG.UI.*` module packages + `FS.GG.UI.Template`, at `0.1.0-preview.1`,
  in `~/.local/share/nuget-local/`.
- **State**: **produced/available** (exist on the local feed) — precondition for any deprecation.
- **Validation** (FR-007, SC-006): the new packages exist **before** any old-ID deprecation is
  actioned (publish-before-deprecate).

## Entity: Deprecation notice / recorded action

- **Fields**: old ID → new ID map (per package); forward-pointer text; target (public feed);
  status (NOT yet applied); apply checklist.
- **State**: **recorded, not applied** — copy-ready content owned outside this tree.
- **Validation** (FR-008/009, SC-006/007): maps every old ID to its replacement; old IDs deprecated
  **(not deleted/unlisted)**; explicitly marked not-yet-applied; never described as applied here.

## Entity: Provenance & bridge records

- **Members**: `docs/bridge/package-identity-migration.md`, `docs/bridge/old-repo-redirect.md`
  (Block B), `PROVENANCE.md`, `README.md`, decision `0002`.
- **State transition**: each "retained / unchanged / no rename" claim → the **rebranded reality**,
  with the import-time mapping correctly **scoped as history** (not deleted).
- **Validation** (FR-010/011, SC-008): zero stale "identity unchanged" claims present as current
  truth; old→new mapping recorded once authoritatively; every in-repo cross-reference still resolves
  (no dead links).

## Relationships

```
decision 0001 (accepted) ──authorizes──▶ runtime identity ×10 ──packs to──▶ replacement packages
        │                                      │                                   │
        │                                      ▼                                   ▼
        ├──authorizes──▶ template identity   surface baselines          deprecation notice (recorded)
        │                                      │                                   │
        └──recorded in──▶ provenance & bridge ◀┴───────── old→new map ─────────────┘
```
