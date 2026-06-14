# Phase 1 Data Model: Import Selected Source (Stage R4)

The "data" is the import inventory and the records that accompany it. Field/format details
are in [`contracts/`](./contracts/).

## Entity: Imported runtime module

One source library copied into `src/`. Imported in dependency tiers (see `research.md`).

| Field | Description | Rule |
|---|---|---|
| Module | Source `src/**` project name | From R2 `import-from-source` set |
| Dependency tier | 1–6 (topological) | Build after each tier |
| `.fsi` present | Every public module has a curated signature | MUST (FR-007) |
| `.fs` access modifiers | none on top-level bindings | MUST be absent (FR-007) |
| Surface baseline | per-module surface-area file | imported or regenerated (FR-007) |

**Import inventory (runtime):**

| Tier | Modules |
|---|---|
| 1 | `Scene` |
| 2 | `Color`, `Layout`, `KeyboardInput`, `Testing` |
| 3 | `SkiaViewer` |
| 4 | `Input`, `Elmish` |
| 5 | `Controls` (incl. `DesignTokens`/`Theme`/`Style`/kit modules) |
| 6 | `Controls.Elmish` |

## Entity: Imported test project

A selected test project from the R3 `import-now` set, with its retained justification note.

| Field | Description | Rule |
|---|---|---|
| Project | `tests/**` name | From `docs/validation/validation-set.md` |
| Frequency | local / ci / release-only | per R3 |
| Justification note | R3 record kept alongside | MUST (FR-004) |

**Import inventory (tests):** local — `Color`/`Scene`/`Layout`/`Input`/`KeyboardInput`/
`Elmish`/`Controls`/`Testing`/`SkiaViewer`/`Lib.Tests` (runtime subset) + `Smoke.Tests`;
ci — `surface-baselines` (+ `refresh-surface-baselines.fsx`), `fsdocs` docs build;
release-only — `Package.Tests`, template `Product.Tests`.
**Excluded:** `Governance.Tests`, `SkillSupport.Tests`.

## Entity: Provenance record (`PROVENANCE.md`)

| Field | Description | Rule |
|---|---|---|
| Source repo | `EHotwagner/FS-Skia-UI` | Required |
| Source commit | `f759f399…` | Required (FR-008) |
| Path map | source path → repo path, per imported group | Required |
| Adaptations | governance removal, Vulkan-naming cleanup, identity/authors | Required |

## Entity: Surface-area baseline

The recorded public surface for a module, consumed by the API-drift check.

| Field | Description | Rule |
|---|---|---|
| Module | owning module | Required |
| Baseline file | recorded public surface | one per public module (FR-007) |
| Origin | imported as-is or regenerated | Recorded if regenerated (edge case) |

## Cross-cutting invariants

- **Compiles + tests run** (FR-011): the solution builds and the local tier passes (SC-001/002).
- **No governance runtime / no Vulkan dependency** (FR-006/005): excluded modules absent;
  vestigial `Vulkan` enum case + stale comments removed.
- **Identity preserved** (FR-010): `FS.Skia.UI.*`, `net10.0`, SkiaSharp pinned.
- **Layering** (FR-002): four layers honored at module level; one semantic control set.
