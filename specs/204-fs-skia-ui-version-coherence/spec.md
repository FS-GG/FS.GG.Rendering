# Feature Specification: Restore fs-skia-ui-version Cross-Repo Coherence

**Feature Branch**: `204-fs-skia-ui-version-coherence`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "Read cross-repo request FS-GG/FS.GG.Rendering#1 — the `fs-gg-ui` template drifted from FS.GG.UI framework HEAD. The request is OPEN and labeled cross-repo/cross-repo:request/blocked: the template pins `FsSkiaUiVersion=0.1.0-preview.1` while the framework HEAD ships a refactored Scene API (VisualTextInspection/LayoutEvidenceReport split, Rect vs Rect option), there are no git tags so no coherent release snapshot exists, and the `fs-skia-ui-version` registry row is `coherent: false` linking to that issue. Resolve the request and bring the contract back to coherent."

## Context

The `fs-skia-ui-version` contract governs the single version that the `fs-gg-ui` template
(hosted in this repository at `template/base/`) pins for every `FS.GG.UI.*` package via the
`FsSkiaUiVersion` property. Cross-repo request **FS-GG/FS.GG.Rendering#1** (filed from
FS.GG.Templates, blocking `dotnet new fs-gg-ui` consumers) reports that this contract is
incoherent: the seed product code targeted a stale Scene API, no tagged release snapshot
existed, and the compatibility registry records the row as `coherent: false`.

Prior feature `201-refresh-template-scene-api` refreshed the template's seed product code to
the current Scene API and bumped the local pin to `0.1.49-preview.1` (all four template
profiles green against a locally packed feed). That closed the *code-drift* half of the
request. This feature closes the **coherence loop** that the request actually tracks: a
consumer must be able to scaffold and build a working product against a reproducible,
consistent package set, and the cross-repo record (registry row + the request issue itself)
must be brought into agreement with that reality.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A downstream consumer scaffolds and builds a working product (Priority: P1)

A developer in a consuming repository (e.g. via `dotnet new fs-gg-ui` / `fs-gg-fullstack`)
scaffolds a product from the `fs-gg-ui` template and builds it. Every `FS.GG.UI.*` package
resolves to one consistent version, the seed product code compiles against that version's
Scene API, and the product produces its scene/evidence output — with no manual fixes for
API drift or missing/conflicting packages.

**Why this priority**: This is the outcome the cross-repo request is blocking. If a freshly
scaffolded product cannot restore and build, the contract is incoherent regardless of what
any registry says. Everything else in this feature exists to make and prove this true.

**Independent Test**: Scaffold the template at each supported profile, restore against the
pinned package set, and build. Restore reports no missing-package (NU1101) or version-conflict
errors, and build reports no Scene-API compile errors; the product emits its expected output.

**Acceptance Scenarios**:

1. **Given** the template at its pinned `FsSkiaUiVersion`, **When** a product is scaffolded and `restore` runs against the package set that version resolves to, **Then** every `FS.GG.UI.*` package resolves to a single consistent version with no missing-package or version-conflict errors.
2. **Given** a scaffolded product, **When** it is built, **Then** the seed product code compiles with no errors caused by Scene API mismatch and the product emits its expected scene/evidence output.
3. **Given** all supported template profiles, **When** each is scaffolded and built, **Then** all succeed under the same single pinned version.

---

### User Story 2 - A reproducible, pin-able release snapshot exists (Priority: P2)

A maintainer (and any downstream consumer) can pin the template against a **named, immutable
snapshot** of the `FS.GG.UI.*` packages rather than a moving HEAD. The version the template
pins corresponds to a recorded coherent snapshot, so two restores at different times resolve
to byte-for-byte the same package set.

**Why this priority**: The request explicitly calls out "there are no git tags, so no coherent
release snapshot exists to pin against." A pin that points at shifting HEAD is not a contract —
the next framework change silently re-breaks every consumer. A reproducible snapshot is what
makes the coherence durable rather than momentary. P2 because US1 can be demonstrated against a
local feed first, but the contract is not truly coherent until the pinned version is reproducible.

**Independent Test**: Identify the snapshot the pin refers to, restore the template against it
twice (or from a clean cache), and confirm the resolved package set is identical and matches the
recorded snapshot.

**Acceptance Scenarios**:

1. **Given** the pinned `FsSkiaUiVersion`, **When** the snapshot it refers to is located, **Then** it is a single coherent set in which all `FS.GG.UI.*` packages carry that same version.
2. **Given** the recorded snapshot, **When** the template is restored against it from a clean state, **Then** the resolved package set is reproducible (identical across restores) with no reliance on un-snapshotted HEAD.

---

### User Story 3 - The cross-repo record reflects coherence and the request is resolved (Priority: P3)

A maintainer of any FS-GG repo can look at the compatibility registry and the request issue and
see an accurate, current picture: the `fs-skia-ui-version` row is `coherent: true`, the request
**FS-GG/FS.GG.Rendering#1** carries a `## Response` documenting how it was resolved (which option
was taken and the verifying evidence) and is closed, and the requester can confirm the
resolution.

**Why this priority**: The registry and the open issue are the cross-repo "single source of truth"
for whether this contract is safe to depend on. Leaving them stale — `coherent: false` with an
open blocked request — after the underlying problem is fixed misleads every other repo into
believing they are still blocked. P3 because it records and communicates the resolution achieved
by US1/US2 rather than producing it; it must not be flipped before US1/US2 actually hold.

**Independent Test**: Read the `fs-skia-ui-version` row in the registry projection and the state
of issue #1; confirm the row is coherent, the issue has a `## Response` and is closed, and the
two agree with the verified build/snapshot evidence.

**Acceptance Scenarios**:

1. **Given** US1 and US2 verified, **When** the `fs-skia-ui-version` registry row (`registry/dependencies.yml` + the `docs/registry/compatibility.md` projection in `FS-GG/.github`) is updated, **Then** it reads `coherent: true` and references the resolving change.
2. **Given** the resolution, **When** request **FS-GG/FS.GG.Rendering#1** is updated, **Then** it carries a comment beginning `## Response` that names the option taken and links the verifying evidence, and the issue is closed.
3. **Given** the registry still reports `coherent: false`, **When** the resolution is attempted, **Then** the registry/issue are NOT flipped to coherent unless US1 and US2 acceptance scenarios pass (no premature closure).

---

### Edge Cases

- **The pinned version cannot be reproducibly resolved** (no published/tagged snapshot, only a local feed): coherence MUST NOT be declared; the contract stays `coherent: false` and the request stays open until a reproducible snapshot exists (this is the gap US2 closes).
- **A profile builds but another does not**: the contract is incoherent until *all* supported profiles build under the single pin; partial success does not justify flipping the registry.
- **Framework HEAD advances past the pinned snapshot during this work**: the pin must continue to reference the recorded snapshot, not the new HEAD; re-drift against later HEAD is a separate future request, not a reason to leave this one open.
- **More than one `FS.GG.UI.*` version literal appears** in the template (pin, docs, or seed code): violates the single-source-of-version invariant and means the package set is not coherent.
- **Registry and issue disagree** (one flipped, the other not): both must be updated together so the cross-repo record stays internally consistent.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A product scaffolded from the `fs-gg-ui` template MUST restore with every `FS.GG.UI.*` package resolving to one consistent version (no missing-package or version-conflict errors).
- **FR-002**: A scaffolded product MUST build with no compile errors caused by Scene API drift, across every supported template profile, under the single pinned `FsSkiaUiVersion`.
- **FR-003**: The version the template pins MUST correspond to a reproducible, immutable coherent snapshot of the `FS.GG.UI.*` packages (not a moving HEAD), such that restores at different times resolve to the same package set.
- **FR-004**: The template MUST keep `FsSkiaUiVersion` as the single source of the FS.GG.UI version — no second version literal in package pins, bundled docs, or seed code.
- **FR-005**: The `fs-skia-ui-version` row in the cross-repo registry (`registry/dependencies.yml` and the `docs/registry/compatibility.md` projection in `FS-GG/.github`) MUST be updated to `coherent: true` once FR-001–FR-003 are verified, and MUST reference the resolving change.
- **FR-006**: Cross-repo request **FS-GG/FS.GG.Rendering#1** MUST receive a comment beginning `## Response` that states which resolution option was taken and links the verifying evidence, and MUST be closed.
- **FR-007**: The registry row and the request issue MUST NOT be marked coherent/resolved unless and until FR-001–FR-003 hold (no premature closure); if a reproducible snapshot cannot be produced, the contract MUST remain `coherent: false` and the request open with the blocker recorded.
- **FR-008**: The resolution MUST be verifiable by re-running the template's existing checks (governance/single-version invariant + generated-product build/evidence) and seeing them pass under the pinned version, with no stale reference to the previously pinned `0.1.0-preview.1`.

### Key Entities

- **`fs-skia-ui-version` contract**: The cross-repo agreement binding the template's `FsSkiaUiVersion` pin to a coherent `FS.GG.UI.*` package set; carries a `coherent` flag and a link to its tracking request in the registry.
- **`fs-gg-ui` template (`template/base/`)**: The scaffold source whose seed product code and package pins must compile and resolve against the pinned version.
- **`FS.GG.UI.*` package set / release snapshot**: The 16 framework packages that must all carry the single pinned version as a reproducible, immutable snapshot.
- **Cross-repo request FS-GG/FS.GG.Rendering#1**: The GitHub issue tracking this incoherence; its state (open/closed) and `## Response` are part of the deliverable.
- **Compatibility registry (`FS-GG/.github`)**: `registry/dependencies.yml` and its `docs/registry/compatibility.md` projection, the source of truth for whether the contract is coherent.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A product scaffolded from the template restores and builds successfully on every supported profile (100% of profiles) under a single pinned version, with zero missing-package, version-conflict, or Scene-API compile errors.
- **SC-002**: Restoring the pinned template from a clean state two independent times yields the identical resolved `FS.GG.UI.*` package set (reproducible snapshot).
- **SC-003**: Exactly one `FS.GG.UI` version literal exists across the template's pins, docs, and seed code, and it is not `0.1.0-preview.1`.
- **SC-004**: The `fs-skia-ui-version` registry row reads `coherent: true` and references the resolving change; request #1 shows a `## Response` and is in the closed state — and both are consistent with the verified build/snapshot evidence.
- **SC-005**: No FS-GG repo reading the registry or issue #1 is left with a stale "blocked / coherent: false" signal for this contract after the feature completes.

## Assumptions

- The Scene-API code-drift half of the request is already addressed by feature `201-refresh-template-scene-api` (seed product code refreshed; pin bumped to `0.1.49-preview.1`, four profiles green against a local feed); this feature builds on that rather than redoing it.
- "Supported profiles" are the template's existing profile set (e.g. `headless-scene`/`governed`, `app`/`sample-pack`) as exercised by feature 201; this feature does not add or remove profiles.
- A "reproducible snapshot" is satisfied by whatever immutable mechanism the project adopts (a git tag of the framework source, a versioned published package set, or an equivalent recorded snapshot) — the spec requires reproducibility, not a specific mechanism.
- The compatibility registry and the request issue live in `FS-GG/.github` and `FS-GG/FS.GG.Rendering` respectively and are mutated through the GitHub-native cross-repo coordination protocol, not through files in this repository.
- The cross-repo coordination protocol (file/respond/resolve, registry coherence rule) defined in `FS-GG/.github` → `docs/coordination/README.md` is authoritative for the steps in User Story 3.

## Dependencies

- Write access (or a coordinated owner) for the `FS-GG/.github` registry and for closing/commenting on **FS-GG/FS.GG.Rendering#1**.
- The verified output of feature `201-refresh-template-scene-api` (refreshed seed code + bumped pin) as the starting state.
- A means of producing/recording a reproducible coherent `FS.GG.UI.*` snapshot at the pinned version (tag or published package set) to satisfy US2/FR-003.

## Out of Scope

- Adding new template profiles, controls, or product features beyond what is needed to restore coherence.
- Any further Scene-API refactor of the framework itself (the framework HEAD API is taken as given).
- Future re-drift if framework HEAD advances past the pinned snapshot — that is a new cross-repo request, not part of this resolution.
