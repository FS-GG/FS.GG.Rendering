# Phase 1 Data Model: Rename fs-skia-ui Version Machinery to fs-gg-ui

This feature renames three identity-bearing entities. There is no application data; the "entities"
are the contract surfaces being re-rooted from `fs-skia-ui` to `fs-gg-ui`, plus the immutable
boundary that must not be touched.

## Entity 1 — FS.GG.UI version property (single-source CPM pin)

The one Central Package Management property a generated product edits to choose its `FS.GG.UI.*`
package version.

| Field | Before | After |
|-------|--------|-------|
| Property name | `FsSkiaUiVersion` | `FsGgUiVersion` |
| Declaration site | `template/base/Directory.Packages.props` (`<FsSkiaUiVersion>0.1.50-preview.1</…>`) | `<FsGgUiVersion>…</FsGgUiVersion>` |
| Referencing pins | 13 × `Version="$(FsSkiaUiVersion)"` (every `FS.GG.UI.*`) | 13 × `Version="$(FsGgUiVersion)"` |
| Runtime resolver | `template/base/build.fsx` regex `<FsSkiaUiVersion>([^<]+)</FsSkiaUiVersion>` | `<FsGgUiVersion>([^<]+)</FsGgUiVersion>` |
| Invariant test | `GovernanceTests.fs` asserts `build` contains `"FsSkiaUiVersion"` | asserts `"FsGgUiVersion"` |
| Doc echoes | `.template.config/generated/README.md`, `.template.package/README.md`, plus the swept READMEs | renamed |

- **Cardinality invariant (FR-002)**: exactly **one** literal of this property per generated product,
  before and after. The rename MUST NOT add a second FS.GG.UI version literal.
- **Validation rules**:
  - `grep -c "<FsGgUiVersion>" Directory.Packages.props` == 1 in a generated product (SC-001).
  - Zero occurrences of `FsSkiaUiVersion` anywhere in the generated tree (SC-001).
  - Every `FS.GG.UI.*` `PackageVersion` reads `$(FsGgUiVersion)`.
  - `build.fsx` resolves the engine from `FsGgUiVersion` (else `failwithf`, loud).
- **State transition**: must be renamed **atomically** (literal + all pins + resolver + invariant in
  one commit). A half-renamed state is invalid: a pin reading the undefined `$(FsSkiaUiVersion)`
  fails restore fast (Edge Case). See research R2.

## Entity 2 — Coherent snapshot tag (reproducibility/audit marker)

A git tag marking the commit that produced a published coherent 16-package (+BOM) set.

| Field | Before | After |
|-------|--------|-------|
| Namespace | `fs-skia-ui/v<V>` | `fs-gg-ui/v<V>` |
| Members in scope | `v0.1.50-preview.1` → commit `57be86c`; `v0.1.51-preview.1` → commit `d9f4c81` | same commits, new namespace |
| Tag kind | annotated (carries snapshot subject) | annotated (subject preserved) |
| Legacy tags | present | **deleted** (FR-005) |

- **Validation rules**:
  - `git rev-list -n1 fs-gg-ui/v<V>` == the pre-rename commit for each version (FR-004 / SC-003).
  - `git tag -l 'fs-gg-ui/v*'` lists exactly the two; `git tag -l 'fs-skia-ui/v*'` returns nothing
    (FR-005 / SC-003).
- **Not in scope**: the `fs-gg-ui-template/v0.1.50-preview.1` tag is a *different* (template) namespace
  and is untouched.

## Entity 3 — Registry contract id (cross-repo contract surface)

The cross-repo registry identifier for the version-pinning and BOM surfaces, owned by `FS-GG/.github`.

| Field | Before | After |
|-------|--------|-------|
| Version contract id | `fs-skia-ui-version` | `fs-gg-ui-version` |
| BOM contract id | `fs-skia-ui-bom` | `fs-gg-ui-bom` |
| Sites | `registry/dependencies.yml`, `docs/registry/compatibility.md` (sibling repo) | renamed |
| Governing ADR | ADR-0003 **Proposed** | ADR-0003 **Accepted** |

- **Validation rules (SC-005)**: all three surfaces (property, tag namespace, registry ids) use the
  `fs-gg-ui` root; the registry projection is updated; ADR-0003 is Accepted.
- **Gating**: the registry write is made only **after** Entities 1 & 2 are verified in this repo
  (FR-010 / research R6), via `gh` + `cross-repo-coordination` — never as files in this repo.
- **Downstream check (FR-011)**: confirm Templates/SDD carry no `FsSkiaUiVersion` / `fs-skia-ui/*`
  reference; if any found, raise a cross-repo request (it is otherwise a verify-only no-op).

## Immutable boundary (MUST NOT change)

These reference the old name legitimately and are **out of scope** (FR-009):

- `specs/**` — historical feature records.
- `docs/product/decisions/0001-package-identity.md` — records the prior `FS.Skia.UI → FS.GG.UI` /
  `dotnet new fs-skia-ui → fs-gg-ui` rebrand; provenance, not this version machinery.
- `docs/audit/mechanism-inventory.md`, `docs/bridge/package-identity-migration.md` — historical
  package-identity provenance.
- `src/**/*.fs`, `src/**/*.fsi` — no public F# surface change; the coherent package set is unchanged.
