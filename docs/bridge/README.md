# Bridge — from `FS-Skia-UI` to `FS.GG.Rendering`

> Migration **Stage R7** deliverable. This is the handoff hub: it declares where the rendering
> product now lives, what moved, where new work goes, and what remains of the old repository.

## Canonical home

The rendering product's **canonical home is this repository, [`FS.GG.Rendering`](https://github.com/FS-GG/FS.GG.Rendering)**.

It was imported from the now-archived source repository
[`EHotwagner/FS-Skia-UI`](https://github.com/EHotwagner/FS-Skia-UI) at source commit
**`f759f399`** (2026-06-14), during migration Stage R4. From R7 onward, `FS.GG.Rendering` is the
single source of truth for the runtime libraries, controls, design-system/themes, viewer/host,
templates, packages, and docs. The old repository is **archive and provenance only** (see
[Directional policy](#directional-policy) and [Archive note](#archive-note) below).

## What moved

The runtime source (`src/` modules), the test suites (`tests/`), the project template
(`template/`, `.template.config/`, `.template.package/`), the imported product docs
(`docs/imported/`), the surface baselines, and the build/ownership metadata were imported from
`FS-Skia-UI` and adapted for this repository (governance machinery dropped, ownership metadata
re-pointed to FS-GG, solution authored as `.slnx`).

The complete, authoritative lineage — pinned source commit, the full source-path → repo-path map,
every deliberate adaptation, and what was deliberately left in the source archive — lives in
**[`PROVENANCE.md`](../../PROVENANCE.md)**. This hub does not restate that map; `PROVENANCE.md` is
the one record to update if imported scope ever changes.

## Identity status

Package and template identity is **retained as `FS.Skia.UI.*`** — unchanged by the repository
move. Any rename to a new identity (e.g. `FS.GG.UI.*`) is a separate, explicit decision deferred
to migration **Stage R8**; it has **not** happened. Details and the per-package mapping are in
[`package-identity-migration.md`](./package-identity-migration.md); the decision record is
[`docs/product/decisions/0001-package-identity.md`](../product/decisions/0001-package-identity.md).

## Directional policy

The boundary between the two repositories is one-way:

- **New rendering product work opens here, in `FS.GG.Rendering`.** Specs, features, fixes, docs,
  packages, and releases for the rendering product are authored and merged in this repository.
- **The old `FS-Skia-UI` repository receives only** bridge maintenance, archive/provenance
  updates, or emergency migration fixes — never new product features.
- **Governance experiments stay out of rendering stabilization.** The experimental governance
  platform is a separate concern (the planned `FS.GG.Governance` repository); rendering must never
  depend on it to build, test, document, package, or release.

This matches the FS-GG org operating rule (see the org profile,
[`FS-GG/.github`](https://github.com/FS-GG/.github)).

## Archive note

The old repository's **specs, reports, and readiness artifacts are archive-only history** — useful
provenance, not a second source of truth. Do not treat old-repo specs/`readiness/` material as
current product documentation; the current artifacts are this repository's `specs/`, `docs/`, and
the records linked above. Historical-only material (FAKE/governance build front-end, `readiness/`,
old feature-workflow `specs/**`, samples) was deliberately left in the source archive and is listed
under *Excluded* in [`PROVENANCE.md`](../../PROVENANCE.md).

## Bridge artifacts

| Document | Purpose |
|---|---|
| [`PROVENANCE.md`](../../PROVENANCE.md) | Authoritative lineage: source commit, path map, adaptations, exclusions. |
| [`old-repo-redirect.md`](./old-repo-redirect.md) | Copy-ready redirect/deprecation notice for the archived old repo + its package pages (a recorded action — not yet applied). |
| [`package-identity-migration.md`](./package-identity-migration.md) | Retained `FS.Skia.UI.*` identity mapping; rebrand deferred to R8. |
| [`docs/product/decisions/0001-package-identity.md`](../product/decisions/0001-package-identity.md) | The package-identity decision record (deferral to R8). |
| [`FS-GG/.github`](https://github.com/FS-GG/.github) | Org profile + cross-repo split/migration docs. |
