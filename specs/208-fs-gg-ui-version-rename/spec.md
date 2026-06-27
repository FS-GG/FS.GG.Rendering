# Feature Specification: Rename fs-skia-ui Version Machinery to fs-gg-ui

**Feature Branch**: `208-fs-gg-ui-version-rename`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → resolved to FS.GG.Rendering issue [#3](https://github.com/FS-GG/FS.GG.Rendering/issues/3) — "[versioning] Rename fs-skia-ui-* version machinery to fs-gg-ui-* (clean break)", per ADR-0003.

## Overview

The `FS.GG.UI.*` packages are pinned and snapshotted by machinery still named after the old
`fs-skia-ui` identity — a leftover from the FS.Skia.UI → FS.GG.UI package rebrand (Feature 008).
The product packages, the org, and the registry all say `fs-gg-ui`, but three consumer- and
tooling-visible surfaces still say `fs-skia-ui`:

1. the single-source version property `FsSkiaUiVersion` that every generated product carries,
2. the snapshot/reproducibility tag namespace `fs-skia-ui/v<V>`, and
3. the registry contract ids `fs-skia-ui-version` and `fs-skia-ui-bom`.

This feature renames all three to the `fs-gg-ui` root as a **clean break with no
backward-compatibility aliases**, so the naming is coherent end-to-end and the legacy identity
stops surfacing to product authors and tooling.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Coherent version property in generated products (Priority: P1)

A product author generates a new FS.GG.UI product (or opens an existing one) and looks for the
one place that controls which version of the FS.GG.UI packages they depend on. They find a single
property named `FsGgUiVersion` — matching the `FS.GG.UI.*` packages it pins and the `fs-gg-ui`
name used everywhere else — with no trace of the old `fs-skia-ui` name. Changing that one value
and restoring produces a green build.

**Why this priority**: This is the consumer-visible breaking change and the core of the rename.
Until the property is renamed, every generated product carries the legacy identity in the file
authors edit most often. A green restore+build against the renamed property is the definition of
done for the whole feature.

**Independent Test**: Generate a product from the template, confirm `Directory.Packages.props`
exposes exactly one `FsGgUiVersion` property (and no `FsSkiaUiVersion`), every `FS.GG.UI.*` pin
references `$(FsGgUiVersion)`, and the product restores and builds green.

**Acceptance Scenarios**:

1. **Given** a freshly generated product, **When** the author inspects `Directory.Packages.props`,
   **Then** the single version literal is named `FsGgUiVersion` and no `FsSkiaUiVersion` token
   remains anywhere in the generated tree.
2. **Given** a generated product, **When** the author changes the `FsGgUiVersion` value to a valid
   published version and restores, **Then** all `FS.GG.UI.*` packages resolve to that version and
   the product builds green.
3. **Given** the template's single-source-version invariant test, **When** it runs against a
   generated product, **Then** it asserts the invariant against `FsGgUiVersion` and passes.

---

### User Story 2 - Reproducible snapshot lookups under the new tag namespace (Priority: P2)

Someone reproducing or auditing a published coherent set looks up the snapshot tag for a given
version and finds it under the `fs-gg-ui/v<V>` namespace. The old `fs-skia-ui/v<V>` tags no longer
exist, so there is one unambiguous place to look.

**Why this priority**: Reproducibility and audit depend on the snapshot tag being findable under
the current identity. It is decoupled from the per-product property (P1) but still part of making
the `fs-gg-ui` root authoritative.

**Independent Test**: Confirm the coherent snapshots are tagged `fs-gg-ui/v0.1.51-preview.1` and
`fs-gg-ui/v0.1.50-preview.1`, that the legacy `fs-skia-ui/v*` tags are gone, and that a
reproducibility lookup resolves to the same commit it did before the rename.

**Acceptance Scenarios**:

1. **Given** the repository tags, **When** a reproducibility lookup queries the `fs-gg-ui/v<V>`
   namespace, **Then** it finds the snapshot for each previously published coherent version.
2. **Given** the repository tags, **When** anything queries the legacy `fs-skia-ui/v*` namespace,
   **Then** no tags are returned (clean break).

---

### User Story 3 - No stale `fs-skia-ui` references in docs and provenance (Priority: P3)

A reader of the template READMEs, upgrade guide, provenance record, or per-library READMEs never
encounters the old `fs-skia-ui` / `FsSkiaUiVersion` name. Every reference points at the `fs-gg-ui`
root, so documentation matches what the product files and tooling actually use.

**Why this priority**: Documentation coherence matters for trust and onboarding but does not block
a product from building. It is the lowest-risk slice and can land last.

**Independent Test**: Search the shipped docs/READMEs/PROVENANCE surfaces for `fs-skia-ui` and
`FsSkiaUiVersion`; the only remaining matches are in historical `specs/` records, never in
currently-shipped guidance.

**Acceptance Scenarios**:

1. **Given** the shipped template and per-library documentation, **When** a reader searches for the
   legacy name, **Then** no current guidance references `fs-skia-ui` or `FsSkiaUiVersion`.
2. **Given** the upgrade guide, **When** an author follows it to bump versions, **Then** it
   instructs them to edit `FsGgUiVersion`.

---

### Edge Cases

- **Existing products on the old property**: A product generated before this feature still uses
  `FsSkiaUiVersion`. Because this is a clean break, such products do not auto-migrate; the upgrade
  guidance must tell authors to rename the property when they move to the new template version.
- **Mixed/stale references after a partial rename**: If any pin still references the old
  `$(FsSkiaUiVersion)` while the property is renamed, restore fails fast (undefined property). The
  single-source invariant test must catch this rather than letting it ship.
- **Cross-repo leakage**: A vendored or scaffold reference to `FsSkiaUiVersion` / `fs-skia-ui/*` in
  Templates or SDD would break those consumers. The feature must verify none remain and file a
  cross-repo request if one is found.
- **Historical spec records**: Past `specs/` directories legitimately reference the old name as
  history and MUST NOT be rewritten.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The single-source version property MUST be renamed from `FsSkiaUiVersion` to
  `FsGgUiVersion` in the template base (`Directory.Packages.props`, `build.fsx`, and the generated
  `.template.config` tree), with every `FS.GG.UI.*` pin referencing `$(FsGgUiVersion)`.
- **FR-002**: There MUST remain exactly one version literal per generated product; the rename MUST
  NOT introduce a second source of the FS.GG.UI version.
- **FR-003**: The template's single-source-version invariant test (and any governance test
  asserting it) MUST assert against `FsGgUiVersion` and pass.
- **FR-004**: The snapshot/reproducibility tag namespace MUST be `fs-gg-ui/v<V>`; the previously
  published coherent versions (`v0.1.51-preview.1` and `v0.1.50-preview.1`) MUST be re-tagged under
  it, pointing at the same commits.
- **FR-005**: The legacy `fs-skia-ui/v*` tags MUST be removed (clean break, no aliases).
- **FR-006**: The template version MUST be bumped, because renaming the consumer-visible property
  is a breaking change for generated products.
- **FR-007**: Currently-shipped documentation and provenance surfaces (template READMEs, upgrade
  guide, `PROVENANCE.md`, `.template.package`/`.template.config` READMEs, and per-library
  `src/**/README.md`) MUST be swept so no current guidance references `fs-skia-ui` or
  `FsSkiaUiVersion`.
- **FR-008**: Upgrade guidance MUST tell authors of pre-rename products how to migrate (rename the
  property) when adopting the new template version.
- **FR-009**: Historical `specs/` records MUST NOT be edited; the rename applies only to
  currently-shipped surfaces.
- **FR-010**: The registry contract ids `fs-skia-ui-version` and `fs-skia-ui-bom` MUST be renamed
  to `fs-gg-ui-version` and `fs-gg-ui-bom` in the cross-repo registry, and ADR-0003 moved from
  Proposed to Accepted on resolution. *(Owned by `FS-GG/.github`; tracked here as the cross-repo
  dependency that makes the contract surface coherent.)*
- **FR-011**: There MUST be confirmation that no vendored or scaffold reference to
  `FsSkiaUiVersion` or `fs-skia-ui/*` remains in the Templates or SDD consumers; any found MUST be
  raised as a cross-repo request.

### Key Entities *(include if feature involves data)*

- **FS.GG.UI version property**: The single CPM property a generated product edits to choose its
  FS.GG.UI package version. Renamed `FsSkiaUiVersion` → `FsGgUiVersion`.
- **Coherent snapshot tag**: A git tag marking the commit that produced a published coherent
  16-package set, used for reproducibility/audit. Namespace renamed `fs-skia-ui/v<V>` →
  `fs-gg-ui/v<V>`.
- **Registry contract id**: The cross-repo registry identifier for the version-pinning and BOM
  surfaces. Renamed `fs-skia-ui-version`/`fs-skia-ui-bom` → `fs-gg-ui-version`/`fs-gg-ui-bom`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product generated from the bumped template exposes exactly one version property,
  named `FsGgUiVersion`, and zero occurrences of `FsSkiaUiVersion` anywhere in its generated tree.
- **SC-002**: A generated product restores and builds green driven solely by `FsGgUiVersion`.
- **SC-003**: Reproducibility lookups for every previously published coherent version succeed under
  `fs-gg-ui/v<V>` and resolve to the same commit as before; the `fs-skia-ui/v*` namespace returns
  zero tags.
- **SC-004**: A search of currently-shipped docs/READMEs/PROVENANCE returns zero `fs-skia-ui` /
  `FsSkiaUiVersion` matches (matches remain only in historical `specs/`).
- **SC-005**: All three contract surfaces (property, tag namespace, registry ids) use the
  `fs-gg-ui` root, the registry projection is updated, and ADR-0003 is Accepted.

## Assumptions

- **Clean break, confirmed by ADR-0003 and issue #3**: No compatibility aliases are provided for
  the old property name or tag namespace; pre-rename products migrate by editing one property.
- **Two coherent versions to re-tag**: Only `v0.1.51-preview.1` and `v0.1.50-preview.1` currently
  exist under `fs-skia-ui/v*` and are in scope for re-tagging; earlier history is not.
- **Template version bump**: A single preview increment of the template version is sufficient to
  signal the breaking property rename; the exact number is a release detail decided at
  implementation time.
- **Registry/ADR work lands in `.github`**: The contract-id rename and the ADR flip are executed in
  `FS-GG/.github` as a coordinated `contract-change` (this item carries the `contract-change` and
  `cross-repo` labels); the Rendering repo owns the property/tag/docs surfaces.
- **Templates/SDD are expected clean**: Per the post-Feature-205 scaffold direction, no vendored
  copy of the property is expected downstream; verification is a check, with a cross-repo request as
  the fallback if a reference is found.
- **No code behavior changes**: This is a naming/identity change to versioning machinery only; it
  does not alter runtime rendering behavior or which package versions are coherent.

## Dependencies

- **Cross-repo (`FS-GG/.github`)**: Registry contract-id rename (`registry/dependencies.yml` +
  `docs/registry/compatibility.md`) and ADR-0003 acceptance — required for SC-005.
- **Cross-repo (Templates / SDD)**: Confirmation of no lingering `FsSkiaUiVersion` / `fs-skia-ui/*`
  references — required for FR-011.
- **ADR-0003** (`FS-GG/.github` `docs/adr/0003-rename-fs-skia-ui-version-machinery-to-fs-gg-ui.md`):
  the governing decision for this rename.
