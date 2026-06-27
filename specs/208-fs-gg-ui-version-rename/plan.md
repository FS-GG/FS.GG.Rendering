# Implementation Plan: Rename fs-skia-ui Version Machinery to fs-gg-ui

**Branch**: `208-fs-gg-ui-version-rename` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/208-fs-gg-ui-version-rename/spec.md`

## Summary

Three consumer- and tooling-visible surfaces still carry the pre-rebrand `fs-skia-ui` identity left
over from the `FS.Skia.UI.* → FS.GG.UI.*` package rename (Feature 008). This feature renames all
three to the `fs-gg-ui` root as a **clean break with no backward-compatibility aliases**:

1. **The single-source version property** `FsSkiaUiVersion → FsGgUiVersion` — the one CPM property
   every generated product edits to pin the whole `FS.GG.UI.*` set. Lives in the template base
   (`template/base/Directory.Packages.props` literal + 11 `$(…)` pins, `template/base/build.fsx`
   runtime regex, the `GovernanceTests` single-source invariant) and is echoed in shipped READMEs.
2. **The snapshot/reproducibility tag namespace** `fs-skia-ui/v<V> → fs-gg-ui/v<V>` — re-tag the two
   published coherent snapshots (`v0.1.50-preview.1`, `v0.1.51-preview.1`) at their **same commits**
   and delete the legacy tags.
3. **The registry contract ids** `fs-skia-ui-version`/`fs-skia-ui-bom → fs-gg-ui-version`/`fs-gg-ui-bom`
   in the cross-repo registry (`FS-GG/.github`), with ADR-0003 moved Proposed → Accepted.

The property rename is the one **breaking** change for generated products, so the template version is
bumped and the upgrade guide tells pre-rename authors to rename the property when they adopt it.
This is a **naming/identity change to versioning machinery only** — no runtime/rendering behavior
changes and the coherent package set is unchanged.

> **Standing assumption — the rename is unverified until a product is generated from the bumped
> template and restored+built.** A green text-grep ("no `FsSkiaUiVersion` remains") is **not**
> evidence that the renamed property actually drives a coherent restore: a half-renamed tree (literal
> renamed but a pin still `$(FsSkiaUiVersion)`) fails restore fast on an *undefined property*, which a
> grep alone won't surface. The only trustworthy signal is a real *generate → restore → build* run
> against the freshly packed/bumped template, plus a real `git tag` lookup under the new namespace.
> `/speckit-tasks` MUST schedule that live generate+restore+build (and the tag re-point + lookup) as
> the **first Foundational step**, before the doc sweep and before any cross-repo registry write.

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`). The change is a **rename across MSBuild/CPM
properties, git tags, docs, and a cross-repo registry** — not F# library surface. The only `.fs`
touched is the template's `GovernanceTests.fs` single-source invariant (assertion string + comments);
no `src/**` `.fs`/`.fsi` public surface changes.

**Primary Dependencies**: The 16 published, co-versioned `FS.GG.UI.*` member packages on the local
feed at `~/.local/share/nuget-local/`. The template's CPM pins the 11 of those consumed by a
generated product through the single property under rename (the remaining members + BOM aren't
referenced by the generated tree).
Tooling: `dotnet restore`/`build`, `git tag`, `gh` + the `cross-repo-coordination` skill for the
registry/ADR work in `FS-GG/.github`.

**Storage**: N/A — no application data. Artifacts are MSBuild props, an `.fsx`, a test, READMEs, git
tags, and a sibling-repo registry record.

**Testing**: The template's `template/base/tests/Product.Tests/GovernanceTests.fs` single-source
invariant (currently asserts `build.fsx` resolves the engine from `FsSkiaUiVersion`) flips to
`FsGgUiVersion` and must pass against a generated product. Verification is the live
generate→restore→build of a product from the bumped template (Expecto `Product.Tests` + a clean
`dotnet build`), the exactly-one-`FsGgUiVersion`/zero-`FsSkiaUiVersion` grep over the *generated*
tree, and a `git tag -l 'fs-gg-ui/v*'` / `'fs-skia-ui/v*'` lookup.

**Target Platform**: Linux desktop / headless. Restore+build proves the rename; no GL context needed.

**Project Type**: Release-engineering / template + docs rename + a cross-repo coordination
deliverable. No new product feature, control, or API.

**Performance Goals**: N/A — naming/coherence task.

**Constraints**:
- **Clean break, no aliases** (Assumptions; ADR-0003, issue #3): no compatibility shim for the old
  property name or tag namespace. A pre-rename product migrates by editing one property.
- **Single-source invariant preserved (FR-002)**: the rename MUST NOT introduce a second FS.GG.UI
  version literal. After the edit there is still exactly **one** literal (`FsGgUiVersion`) and every
  `FS.GG.UI.*` pin references `$(FsGgUiVersion)`.
- **Atomic property rename**: literal, all 11 pins, the `build.fsx` regex
  (`<FsSkiaUiVersion>([^<]+)</FsSkiaUiVersion>` → `<FsGgUiVersion>…`), and the GovernanceTests
  assertion change together in one commit; a partial rename fails restore fast (undefined property,
  Edge Case) and the invariant test must catch it rather than letting it ship.
- **Re-tag at the same commits (FR-004)**: `fs-gg-ui/v0.1.50-preview.1` → `57be86c`,
  `fs-gg-ui/v0.1.51-preview.1` → `d9f4c81` (the existing `fs-skia-ui/v*` targets), so a
  reproducibility lookup resolves to the identical commit it did before.
- **Delete legacy tags (FR-005)**: after re-tag, `git tag -l 'fs-skia-ui/v*'` returns nothing.
- **Template version bump (FR-006)**: a single preview increment signals the breaking rename; the
  exact number is a release detail at implementation time.
- **History is immutable (FR-009)**: `specs/**` records legitimately reference the old name and MUST
  NOT be rewritten. The `fs-skia-ui` strings in `docs/product/decisions/0001-package-identity.md`,
  `docs/audit/mechanism-inventory.md`, and `docs/bridge/package-identity-migration.md` document the
  **prior package rebrand** (the `dotnet new fs-skia-ui → fs-gg-ui` short-name change), not this
  version machinery — they are historical/provenance and OUT OF SCOPE.
- **Cross-repo state** lives in `FS-GG/.github` and is mutated through the GitHub-native cross-repo
  protocol (`gh` + `cross-repo-coordination`), **not** files in this repo, and **only after** the
  property/tag rename is verified (FR-010). Templates/SDD cleanliness is a *check* with a cross-repo
  request as the fallback if a reference is found (FR-011).

**Scale/Scope**: 5 property-surface files (props, build.fsx, GovernanceTests.fs,
`.template.config/generated/README.md`, `.template.package/README.md`) + 1 template version bump;
~12 shipped-doc files swept (`PROVENANCE.md`, `template/base/README.md`,
`template/base/docs/UPGRADING.md`, 9 × `src/*/README.md`); 2 git tags re-pointed + 2 deleted; 1
cross-repo registry record (2 files in a sibling repo) + 1 ADR flip. `src/**` `.fs`/`.fsi` and the
coherent package set are **unchanged**.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass (N/A shape) | No `.fs`/`.fsi` public surface is added or changed; there is nothing to draft in FSI. The "test" is the template's existing single-source GovernanceTests invariant, re-pointed to `FsGgUiVersion` and proven by a real generate→restore→build. |
| II. Visibility lives in `.fsi` | ✅ Pass (N/A) | No public module changes; no access modifiers introduced. The only `.fs` edit is an assertion string/comments in the template's `GovernanceTests.fs`. |
| III. Idiomatic Simplicity Is the Default | ✅ Pass | A pure rename + re-tag is the plainest possible mechanism. No new abstraction, operator, SRTP, reflection, or CE. |
| IV. Elmish/MVU boundary | ✅ Pass (N/A) | No stateful or I/O behavior; renaming a property/tag has no `Model`/`Msg`/`update`. |
| V. Test Evidence Is Mandatory | ✅ Pass | Verification is real: a product generated from the bumped template restores+builds green driven solely by `FsGgUiVersion`, the invariant test asserts the new name, and `git tag` lookups confirm the namespace swap. The cross-repo registry write is gated on that real evidence (FR-010). No synthetic evidence. |
| VI. Observability and Safe Failure | ✅ Pass | The clean break fails **loud and closed**: a stale `$(FsSkiaUiVersion)` pin makes restore fail fast on an undefined property; `build.fsx` already `failwithf`s if it cannot resolve the single-source property; the invariant test fails loudly if a second literal or the wrong name appears. |

**Change classification**: **Tier 1 (contracted change)** — it modifies a **consumer-visible
contract** (the single property every generated product edits) as a breaking change, renames a
**reproducibility/audit surface** (the snapshot tag namespace), and renames **cross-repo registry
contract ids**. Per Change Classification this requires the full artifact chain: spec, plan, test
evidence (the re-pointed invariant + live restore), and documentation/migration updates (FR-007/008).
There are **no `.fsi`/surface-area baselines** to touch because no `src/**` public F# surface changes.
No gate violations — **Complexity Tracking not required**.

## Project Structure

### Documentation (this feature)

```text
specs/208-fs-gg-ui-version-rename/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — clean-break decision, atomic-rename strategy, re-tag mechanism, doc-sweep boundary
├── data-model.md        # Phase 1 — the three renamed entities + the immutable-history / out-of-scope boundary
├── quickstart.md        # Phase 1 — rename → generate → restore+build → re-tag → lookup → sweep run guide
├── contracts/           # Phase 1
│   ├── version-property-rename.md   # FsSkiaUiVersion → FsGgUiVersion; single-source invariant (US1 FR-001/002/003/006)
│   ├── snapshot-tag-namespace.md    # fs-skia-ui/v<V> → fs-gg-ui/v<V>, same commits, legacy deleted (US2 FR-004/005)
│   └── registry-contract-ids.md     # fs-skia-ui-version/-bom → fs-gg-ui-version/-bom + ADR-0003 Accepted (FR-010/011)
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify, if present)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
# (A) Property surface — the single-source rename (US1, FR-001/002/003), atomic in one commit
template/base/Directory.Packages.props          # <FsSkiaUiVersion>…</FsSkiaUiVersion> literal + 11 $(FsSkiaUiVersion) pins + 2 comments (14 occurrences) → FsGgUiVersion
template/base/build.fsx                          # runtime regex <FsSkiaUiVersion>([^<]+)</…> + ~5 other usages (6 lines total) → FsGgUiVersion
template/base/tests/Product.Tests/GovernanceTests.fs  # single-source invariant: assertion string "FsSkiaUiVersion" + 2 comments → FsGgUiVersion (FR-003)
.template.config/generated/README.md             # one <FsSkiaUiVersion> mention → FsGgUiVersion
.template.package/README.md                      # one <FsSkiaUiVersion> mention → FsGgUiVersion

# Template version bump — the breaking property rename (FR-006); exact number a release detail
template/base/Directory.Build.props              # <Version> preview increment (signals the breaking rename)

# (B) Shipped-doc + provenance sweep (US3, FR-007/008) — zero FsSkiaUiVersion in current guidance
PROVENANCE.md                                    # 1 occurrence
template/base/README.md                          # 1 occurrence
template/base/docs/UPGRADING.md                  # 4 occurrences; ALSO add pre-rename migration note (FR-008)
src/Build/README.md  src/Scene/README.md  src/SkiaViewer/README.md  src/Elmish/README.md
src/KeyboardInput/README.md  src/Layout/README.md  src/Controls/README.md
src/Controls.Elmish/README.md  src/Testing/README.md   # 9 per-library READMEs, 1 each

# (C) Snapshot tag namespace — re-tag at SAME commits, delete legacy (US2, FR-004/005)
<git tag>  fs-gg-ui/v0.1.50-preview.1  → 57be86c   (was fs-skia-ui/v0.1.50-preview.1)
<git tag>  fs-gg-ui/v0.1.51-preview.1  → d9f4c81   (was fs-skia-ui/v0.1.51-preview.1)
<delete>   fs-skia-ui/v0.1.50-preview.1, fs-skia-ui/v0.1.51-preview.1

# (D) Cross-repo (sibling repo, via gh — NOT files here; gated on verified property/tag rename)
FS-GG/.github : registry/dependencies.yml + docs/registry/compatibility.md   # ids fs-skia-ui-* → fs-gg-ui-* (FR-010)
FS-GG/.github : docs/adr/0003-…-to-fs-gg-ui.md                                # Proposed → Accepted (FR-010)
# Verify-only: Templates / SDD carry no FsSkiaUiVersion / fs-skia-ui/* reference; cross-repo request if found (FR-011)

# OUT OF SCOPE — MUST NOT edit
specs/**                                          # historical records reference the old name as history (FR-009)
docs/product/decisions/0001-package-identity.md   # documents the PRIOR `dotnet new fs-skia-ui→fs-gg-ui` rebrand (history)
docs/audit/mechanism-inventory.md, docs/bridge/package-identity-migration.md  # historical package-identity provenance
fs-gg-ui-template/v0.1.50-preview.1               # a DIFFERENT (template) tag namespace, not the snapshot namespace
src/**/*.fs, src/**/*.fsi                          # no public F# surface change; coherent set unchanged
```

**Structure Decision**: Rename in place across the three surfaces, ordered so the verifiable
breaking change lands first. (A) The property rename is **atomic in one commit** — literal + all 11
pins + the `build.fsx` resolver regex + the `GovernanceTests` invariant — because a partial rename
fails restore on an undefined property; it is then proven by generating a product from the
**version-bumped** template and restoring+building green with exactly one `FsGgUiVersion` and zero
`FsSkiaUiVersion`. (C) The two coherent snapshot tags are re-created under `fs-gg-ui/v<V>` pointing at
the **same commits** and the legacy `fs-skia-ui/v*` tags deleted, verified by `git tag` lookups.
(B) The shipped-doc/provenance sweep removes every remaining `FsSkiaUiVersion`/`fs-skia-ui` from
current guidance (with a migration note added to `UPGRADING.md`), leaving matches only in immutable
`specs/**` history and the package-rebrand provenance docs. (D) Only after the in-repo rename is
verified, the registry contract-id rename and ADR-0003 acceptance are made in `FS-GG/.github` via
`gh`/`cross-repo-coordination`, and Templates/SDD are checked clean with a cross-repo request as the
fallback. `src/**` F# surface and the coherent package set are read-only.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
