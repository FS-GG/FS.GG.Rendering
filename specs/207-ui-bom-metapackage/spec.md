# Feature Specification: Optional FS.GG.UI BOM / Metapackage Pinning the Coherent Package Set

**Feature Branch**: `207-ui-bom-metapackage`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" — resolved to **P5 · rendering — Optional FS.GG.UI BOM/metapackage pinning the 16-package set**, a child of the P5 epic *"Make the FsSkiaUiVersion staleness bug class structurally impossible"* on the FS-GG Coordination board (Workstream: Versioning).

## Context

The FS.GG.UI framework publishes a set of NuGet packages (the coherent **16-package set**:
`FS.GG.UI.Build`, `.Scene`, `.Canvas`, `.Controls`, `.Controls.Elmish`, `.DesignSystem`,
`.Diagnostics`, `.Elmish`, `.KeyboardInput`, `.Layout`, `.SkiaViewer`, `.Symbology`,
`.Symbology.Render`, `.Testing`, `.Themes.AntDesign`, `.Themes.Default` — the exact membership
being whatever the published release snapshot contains; see plan.md for the verified set). The
bare `FS.GG.UI` ID is *not* one of these members — it is reserved for the new BOM/metapackage this
feature ships. For a product to work, **every**
`FS.GG.UI.*` package a consumer references must resolve to the **same** version: the Scene/IR
contracts, the renderer, the controls, and the governance engine are co-versioned and drift
across versions (feature `204-fs-skia-ui-version-coherence` exists precisely because a mixed
set broke downstream consumers).

Today coherence is held together by **convention**: the `fs-gg-ui` template centralizes the
version into a single `FsSkiaUiVersion` property in its `Directory.Packages.props`, every
member pin references that property, and feature 204 added a tagged, reproducible snapshot.
That makes coherence *easy* inside a template-generated product, but it does not make a stale
or mixed set *impossible*. A consumer who hand-edits pins, adds a member package outside
central management, copies a subset into a non-template project, or upgrades one package
without the others can still assemble a partial/mismatched set — and the failure (NU1605
downgrade, an API-drift compile error, or worse, a silent wrong-version resolve) shows up far
from the cause. The version is single-sourced *per project by discipline*, not *per
distributed artifact by construction*.

This feature ships the structural fix the P5 epic calls for: an **optional Bill-of-Materials
(BOM) / metapackage** published alongside the framework set. A consumer references **one**
package at **one** version and thereby pins the entire coherent set to the matching version —
with no per-package alignment to get wrong. Because the BOM's version corresponds 1:1 to the
release snapshot it pins, "which versions go together" stops being tribal knowledge and
becomes a published, verifiable artifact. It is **optional**: the existing per-package /
`FsSkiaUiVersion` pinning keeps working unchanged; the BOM is an additional, stronger pinning
surface consumers may adopt.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A consumer pins the whole coherent set with one reference (Priority: P1)

A developer in a consuming project wants to use FS.GG.UI without curating sixteen individually
versioned package references. They add a single reference — the FS.GG.UI BOM/metapackage — at
one version, restore, and obtain every framework package they need at that single coherent
version. There is no second version literal to keep in sync and no opportunity to forget a
member or pick a mismatched one.

**Why this priority**: This is the core value and the whole reason the item exists — collapse
"keep 16 pins aligned" into "reference one thing." If this does not work, nothing else in the
feature matters. It is independently demonstrable on its own.

**Independent Test**: In a clean consuming project that references only the BOM/metapackage at
version X, run restore and build. Every `FS.GG.UI.*` package the project uses resolves to
version X, with no missing-package (NU1101) or version-conflict (NU1605/NU1608) errors, and
code compiles against that version's APIs.

**Acceptance Scenarios**:

1. **Given** a project whose only FS.GG.UI dependency declaration is a reference to the BOM/metapackage at version X, **When** it restores, **Then** every `FS.GG.UI.*` package in the resolved graph is at version X with no missing-package or version-conflict errors.
2. **Given** that same project, **When** it builds against the resolved set, **Then** it compiles with no errors caused by inter-package version drift.
3. **Given** the BOM/metapackage version X, **When** the set it pins is enumerated, **Then** it lists every member of the coherent published set at exactly version X (no member omitted, none at a different version).

---

### User Story 2 - A stale or mixed set becomes impossible (or loudly detected) (Priority: P2)

A maintainer relies on the BOM to make the staleness bug class structurally impossible: a
consumer who has adopted the BOM cannot end up with one member at the coherent version and
another silently downgraded to a stale version. If something tries to pull a member at a
version different from the one the BOM pins, restore/build fails fast with a clear conflict
rather than resolving to a wrong, mixed set.

**Why this priority**: This is the epic's actual goal ("structurally impossible"). Pinning
conveniently (US1) is necessary but not sufficient; the contract only holds if deviation is
caught, not silently absorbed. P2 because US1 delivers the headline value and can be
demonstrated first, but the durability promise depends on this.

**Independent Test**: In a project referencing the BOM at version X, additionally force a
member package to a different version (a stale or newer one). Restore/build surfaces a version
conflict (or otherwise refuses to produce a mixed set), rather than quietly resolving members
to differing versions.

**Acceptance Scenarios**:

1. **Given** a project referencing the BOM at version X, **When** a member package is also referenced at version Y ≠ X, **Then** restore or build reports a version conflict and does not silently produce a graph mixing X and Y.
2. **Given** a project that has adopted the BOM, **When** the resolved package graph is inspected, **Then** no `FS.GG.UI.*` member resolves to a version other than the one the BOM pins.
3. **Given** a new FS.GG.UI member package is published in a later coherent snapshot, **When** the BOM for that snapshot is produced, **Then** the new member is included (the BOM membership stays in lockstep with the published set, so adopters do not silently miss a package).

---

### User Story 3 - The BOM version corresponds 1:1 to a recorded coherent snapshot (Priority: P3)

A maintainer (and any downstream repo) can treat the BOM version as the name of a coherent
snapshot: the BOM at version X is published as part of the same release snapshot as the
`FS.GG.UI.*` packages it pins at X, it carries the same channel semantics (`-preview.N`
preview vs bare `x.y.z` stable), and the cross-repo registry records the BOM as part of the
coherent set so other repos can discover and depend on it.

**Why this priority**: A BOM whose version does not map to a reproducible published snapshot
would just relocate the staleness problem. P3 because it records/publishes the coherence that
US1/US2 establish rather than producing the consumer-facing behavior; it must not be declared
before US1/US2 hold.

**Independent Test**: Locate the published BOM at version X, confirm it was published in the
same snapshot/tag as the member packages at X, restore it from a clean cache twice and confirm
the resolved set is identical, and confirm the cross-repo registry references the BOM as part
of the coherent FS.GG.UI set.

**Acceptance Scenarios**:

1. **Given** the BOM at version X, **When** its publication is examined, **Then** it was published as part of the same coherent snapshot as the `FS.GG.UI.*` members it pins at X, with matching channel semantics.
2. **Given** the published BOM, **When** it is restored from a clean state two independent times, **Then** the resolved member set is identical (reproducible).
3. **Given** US1 and US2 verified, **When** the cross-repo compatibility registry (`FS-GG/.github`) is updated, **Then** it records the BOM/metapackage as part of the coherent FS.GG.UI set (e.g. under or alongside the `fs-skia-ui-version` contract).

---

### Edge Cases

- **Profile footprint**: the coherent set spans packages a given product profile does not need (e.g. a headless/governed profile does not need the viewer or controls). A single full-set metapackage that *pulls* every member would over-include for slim profiles. Resolution: the BOM is **optional** and the default is the existing per-package/`FsSkiaUiVersion` pinning for minimal footprint; the BOM targets consumers who want the whole set pinned simply. Whether the BOM *transitively pulls* the set or only *constrains versions of packages already referenced* is the key design choice (see Assumptions) and must be stated, because it determines footprint.
- **A new member package is added to the framework**: the BOM's membership list is itself a single source that MUST be updated in lockstep; a BOM that omits a newly published member would let adopters silently miss it (covered by US2 scenario 3).
- **A member package is removed/renamed in a later snapshot**: the BOM for that snapshot must drop it; an adopter upgrading must not be left pinning a package that no longer exists.
- **Preview vs stable mismatch**: the BOM version's channel must match the members it pins (a `-preview` BOM must not pin stable members or vice versa).
- **Consumer mixes BOM adoption with manual central management**: the two pinning surfaces must not silently disagree; if both are present they must agree on the version or fail (US2 scenario 1).
- **Restore against a feed that lacks the BOM but has the members**: adopting consumers get a clear missing-package signal for the BOM, not a partial silent set.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The project MUST publish a BOM/metapackage artifact that pins the coherent FS.GG.UI package set such that a consumer referencing it at a single version obtains every member of that set at that same version.
- **FR-002**: A consumer referencing only the BOM/metapackage at version X MUST be able to restore and build with every `FS.GG.UI.*` package resolving to version X — no missing-package and no version-conflict errors, and no second FS.GG.UI version literal required in the consuming project.
- **FR-003**: The BOM MUST cover the full coherent published set (every member of the snapshot it represents); its membership list MUST be single-sourced and stay in lockstep with the published set so no member is silently omitted when the set changes.
- **FR-004**: When a consumer that has adopted the BOM at version X is also exposed to a member package at a different version Y, restore/build MUST surface a conflict and MUST NOT silently produce a graph mixing versions (the staleness/mixed-set failure becomes loud, not silent).
- **FR-005**: The BOM version MUST carry the same channel semantics as the members it pins (`-preview.N` ⇒ preview channel, bare `x.y.z` ⇒ stable) and MUST correspond 1:1 to a reproducible, recorded coherent snapshot of the member packages at that version.
- **FR-006**: The BOM MUST be published as part of the same release snapshot/tag as the member packages it pins, and restoring it from a clean state MUST be reproducible (identical resolved set across restores).
- **FR-007**: Adoption of the BOM MUST be optional and additive: the existing per-package / `FsSkiaUiVersion` central-pinning mechanism MUST continue to work unchanged for consumers and for the `fs-gg-ui` template (no forced migration as part of this feature).
- **FR-008**: Once FR-001–FR-006 are verified, the cross-repo compatibility registry (`FS-GG/.github`) MUST record the BOM/metapackage as part of the coherent FS.GG.UI set so other FS-GG repos can discover and depend on it; this record MUST NOT be made before the behavior holds.
- **FR-009**: The single-source-of-FS.GG.UI-version invariant MUST be preserved end to end: the BOM does not introduce a second place where the version can drift; producing the BOM derives its version from the same coherent snapshot, not a hand-maintained duplicate.

### Key Entities

- **FS.GG.UI BOM / metapackage**: The new published artifact whose single version pins the coherent FS.GG.UI member set; the consumer-facing "one reference" surface and the unit of this feature.
- **Coherent FS.GG.UI package set (16-package set)**: The co-versioned framework packages that must all resolve to one version; the BOM's membership is exactly this set for a given snapshot.
- **Release snapshot / tag**: The reproducible, immutable snapshot (from feature 204) that the BOM's version names; the source of truth for which member versions are coherent together.
- **`fs-skia-ui-version` contract / compatibility registry (`FS-GG/.github`)**: The cross-repo record of FS.GG.UI version coherence; gains an entry acknowledging the BOM as part of the coherent set.
- **`fs-gg-ui` template (`template/base/Directory.Packages.props`)**: The existing per-package pinning surface (`FsSkiaUiVersion`); unchanged by this feature but the optional future adopter of the BOM.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A clean consuming project whose only FS.GG.UI declaration is the BOM at one version restores and builds with 100% of resolved `FS.GG.UI.*` packages at that single version and zero missing-package or version-conflict errors.
- **SC-002**: A consuming project drops from N (up to 16) individually versioned FS.GG.UI references to exactly **one** FS.GG.UI version-bearing reference, with no loss of any package it previously consumed.
- **SC-003**: An attempt to force any member to a version different from the BOM's pin fails fast (restore/build error) in 100% of attempts — no mixed-version graph is silently produced. *(Verified mechanism, Feature 207 T006: the exact `[X]` bracket flags every deviation in both directions — NU1605 for `Y<X`, NU1608 for `Y>X` — which the consumer's warnings-as-errors posture, the repo / governed-template default, turns into the fail-fast restore/build error. See contract CP-3 and the research R1 amendment.)*
- **SC-004**: Restoring the published BOM from a clean cache two independent times yields an identical resolved member set (reproducible), and the BOM version's channel matches the members it pins.
- **SC-005**: The cross-repo registry records the BOM as part of the coherent FS.GG.UI set, consistent with the verified publish/restore evidence, and no second FS.GG.UI version literal is introduced anywhere by the feature.

## Assumptions

- **Mechanism (default chosen)**: the BOM is realized as a single NuGet artifact (a metapackage with exact-version dependencies on the member set, and/or a transitively-pinning version package) that, when referenced, pins/brings the coherent set. The spec requires the *behavior* (one reference ⇒ coherent set; deviation is loud); the exact NuGet mechanism is an implementation/plan decision. Profile-scoped BOMs (separate slim BOMs per template profile) are **out of scope** for this feature — a single full-set BOM is assumed.
- **Optionality**: per the board item ("Optional"), shipping and publishing the BOM is the deliverable; migrating the `fs-gg-ui` template to consume it is **optional and not required** here (a later feature may adopt it). The existing `FsSkiaUiVersion`/CPM pinning is the unchanged default.
- **Coherent set / snapshot**: the membership and the reproducible snapshot are those established by feature `204-fs-skia-ui-version-coherence` (tagged release, registry coherent); this feature reuses that snapshot rather than redefining coherence.
- **"16-package set"**: treated as the published coherent framework set; the exact count/membership is whatever the published snapshot contains and may evolve — the BOM tracks it (FR-003) rather than hard-coding a number.
- **Cross-repo record**: the compatibility registry and any cross-repo coordination live in `FS-GG/.github` and are mutated through the GitHub-native cross-repo coordination protocol, not through files in this repository.
- **Consumers**: target consumers are .NET/NuGet projects (the framework's distribution channel); the BOM uses standard NuGet pinning semantics available to those consumers.

## Dependencies

- The verified coherent snapshot/tag and `coherent: true` `fs-skia-ui-version` registry row produced by feature `204-fs-skia-ui-version-coherence` (the snapshot the BOM version names).
- The package publishing pipeline / local feed used to pack and publish the member set, so the BOM can be packed and published in the same snapshot (FR-006).
- Write access (or a coordinated owner) for the `FS-GG/.github` compatibility registry to record the BOM (FR-008).

## Out of Scope

- Profile-scoped or slim per-profile BOMs (a single full-set BOM only).
- Migrating the `fs-gg-ui` template (or any existing consumer) to consume the BOM — adoption is optional and deferred.
- Changing the member packages, their APIs, or the `FsSkiaUiVersion`/CPM mechanism itself.
- The other P5 hardening items (consumer-side lockfiles + `--locked-mode` CI, Renovate/auto-PR upstream-bump automation) — separate Coordination-board items.
- Automating future framework upstream bumps; this feature publishes the BOM for the current coherent snapshot, it does not build the bump automation.
