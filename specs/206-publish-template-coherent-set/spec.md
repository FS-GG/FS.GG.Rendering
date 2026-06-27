# Feature Specification: Publish FS.GG.UI.Template Carrying the Lifecycle Parameter & Tag the Coherent Set

**Feature Branch**: `206-publish-template-coherent-set`

**Created**: 2026-06-27

**Status**: Draft

**Input**: User description: "next Rendering item on the project coordination board" → P1 · rendering board item **"Publish FS.GG.UI.Template carrying the new parameter; tag the coherent set."**

## Context

The P1 Rendering epic on the org `Coordination` board — *"Make `fs-gg-ui` emit Spec Kit only
when asked (lifecycle-agnostic template)"* — has delivered its two in-repo changes:

- **204 (`template-lifecycle-symbol`)** added the `lifecycle` choice symbol (`spec-kit|sdd|none`)
  to `.template.config/template.json`, with the `spec-kit` default emitting byte-identical output.
- **205 (`scaffold-git-init-chmod`)** made generation **side-effect-free by default** (removed the
  auto-running git-init/chmod post-actions; replaced the `skipGitInit` opt-out with an `initGit`
  opt-in) and published the generation contract the scaffold path fulfils.

Both changes live only in this repository's working tree and template manifest. **No consumer can
obtain them**: the published `FS.GG.UI.Template` package is still `0.1.17-preview.1` (pre-lifecycle,
pre-side-effect-free), there is no named immutable snapshot that includes the new template, and the
`fs-gg-ui-template` row in the cross-repo registry does not yet record a coherent release carrying
these surfaces. The board therefore marks this publish item as the gate that turns the epic's work
into something downstream repos (`templates`, `sdd`) and `dotnet new fs-gg-ui` users can actually
install — and it is the precondition for the open cross-repo request that asks
`templates`/`sdd` → `rendering` to adopt the lifecycle symbol.

This feature closes that delivery loop. It is the **`fs-gg-ui-template` analogue of feature 204's
`fs-skia-ui-version` coherence work**: publish the template package at a new version that carries the
lifecycle parameter and side-effect-free generation, tag a reproducible coherent snapshot, and bring
the cross-repo record (registry row + compatibility projection + tracking/request issues) into
agreement with that published reality. Per the Ant Design source-of-truth note this feature emits no
UI; it is a packaging/release-coherence deliverable.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A downstream consumer installs the template and scaffolds with the new lifecycle behavior (Priority: P1)

A developer in a consuming context (the `templates`/`sdd` scaffold orchestrator, or a direct
`dotnet new fs-gg-ui` user) installs the **published** template and scaffolds a product. They can
choose a `lifecycle` (`spec-kit`, `sdd`, or `none`); the default `spec-kit` reproduces today's
output byte-for-byte; and generation completes **without** spawning git/chmod processes or creating
a repository unless they explicitly opt in.

**Why this priority**: This is the outcome the board item delivers. Until a package carrying these
surfaces is on the feed, every downstream repo and CLI user is stuck on the pre-204/205 template
regardless of what this repository's working tree contains. Everything else in this feature exists to
make and prove this installable.

**Independent Test**: Install the published (or locally packed) template package, then
`dotnet new fs-gg-ui` with each `lifecycle` value and with no git flag. Confirm the template version
on the feed carries the `lifecycle` symbol and the side-effect-free default, the `spec-kit` default
output matches the prior byte-identical baseline, and generation creates no `.git` and runs no
process unless `--initGit true` is passed.

**Acceptance Scenarios**:

1. **Given** a clean package cache, **When** a consumer installs the template by id, **Then** the
   resolved package version is the newly published one (greater than `0.1.17-preview.1`) and its
   manifest exposes the `lifecycle` choice symbol and the `initGit` opt-in (no `skipGitInit`).
2. **Given** the published template, **When** a product is scaffolded with `lifecycle=spec-kit` (the
   default), **Then** the emitted file set is byte-identical to the prior published baseline for that
   profile.
3. **Given** the published template, **When** a product is scaffolded with no git flag in a headless
   context, **Then** generation returns promptly, starts no process, and creates no repository.
4. **Given** the published template, **When** a product is scaffolded with `lifecycle=sdd` and
   `lifecycle=none`, **Then** each emits its corresponding Spec-Kit-present / Spec-Kit-absent file set.

---

### User Story 2 - A reproducible, named coherent snapshot exists (Priority: P2)

A maintainer (and any downstream consumer) can pin against a **named, immutable snapshot** that
records exactly which `FS.GG.UI.Template` version — alongside the `FS.GG.UI.*` framework set it
scaffolds against — constitutes the coherent release. Two installs/restores at different times
resolve to the same template package and the same scaffolded-and-buildable framework set.

**Why this priority**: A published package without a recorded snapshot leaves "which template +
which framework set is the blessed combination" implicit. The board item explicitly pairs *publish*
with *tag the coherent set*; the tag is what makes the release reproducible and pin-able, mirroring
the `fs-skia-ui/v0.1.50-preview.1` snapshot from feature 204.

**Independent Test**: From the tagged snapshot, install the recorded template version and scaffold
each profile; confirm restore/build succeeds against the recorded framework set with no
missing-package or version-conflict errors, and that the snapshot's recorded versions match what the
template and registry assert.

**Acceptance Scenarios**:

1. **Given** the publish is complete, **When** the coherent set is tagged, **Then** the tag names the
   published template version and the framework set it is coherent with, and the working tree at the
   tag reproduces that package set.
2. **Given** the tagged snapshot, **When** a consumer scaffolds and builds each profile against the
   recorded set, **Then** all profiles restore and build with one consistent framework version and no
   conflicts.

---

### User Story 3 - The cross-repo record agrees with the published reality (Priority: P3)

The `fs-gg-ui-template` registry row, its compatibility-doc projection, and the open cross-repo
request/tracking issue are brought into agreement: the registry records a coherent release at the
published version, and the request that depends on the lifecycle symbol being shippable is unblocked
or resolved with a response pointing at the published package + tag.

**Why this priority**: Coherence is only real when the shared cross-repo record matches what was
shipped. This is required by the coordination protocol for any contract change, but it follows
publication (US1) and snapshotting (US2) — it records a truth those stories establish.

**Independent Test**: Read the registry row and compatibility projection for `fs-gg-ui-template`;
confirm they reference the published version and tag and are marked coherent, and that the linked
cross-repo request shows a response or closure citing the published package and tag.

**Acceptance Scenarios**:

1. **Given** the template is published and tagged, **When** the registry row for `fs-gg-ui-template`
   is updated, **Then** it records the published version as a coherent release and links the tracking
   issue, and the compatibility-doc projection reflects the same.
2. **Given** the cross-repo request that depends on the shippable lifecycle symbol, **When** the
   publish is recorded, **Then** the request carries a response (or closure) citing the published
   version and the coherent-set tag.

---

### Edge Cases

- **Re-publish / immutability**: What happens if a package at the chosen version already exists on the
  feed? The publish MUST fail loudly rather than silently overwrite; choose the next unused
  preview version.
- **Tag already exists**: If the intended coherent-set tag name is taken, the publish must not move an
  existing tag; it must surface the collision and select a distinct name.
- **Framework set drift**: If the framework `FS.GG.UI.*` versions the template pins are not themselves
  a coherent published snapshot (the 204 condition), this feature must record that dependency and not
  declare template coherence on top of an incoherent base.
- **Default-output drift**: If the `spec-kit` default output is not byte-identical to the prior
  published baseline, the publish is blocked until the regression is resolved (the 204/205 guarantee).
- **Partial publish**: If the package publishes but tagging or the registry update fails, the release
  is **not** coherent; the record must show in-progress, not coherent, until all three agree.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST publish a new `FS.GG.UI.Template` package version whose template manifest
  carries the `lifecycle` choice symbol (from 204) and the side-effect-free generation surface
  (`initGit` opt-in, no auto post-actions, from 205).
- **FR-002**: The published version MUST be strictly greater than the currently published
  `0.1.17-preview.1` and MUST NOT overwrite an existing published version.
- **FR-003**: The system MUST tag a named, immutable **coherent set** that records the published
  template version together with the `FS.GG.UI.*` framework version set it scaffolds against, such that
  the tagged tree reproduces that package set.
- **FR-004**: A consumer installing the template by id from the feed MUST resolve to the newly
  published version and be able to scaffold all supported profiles and lifecycle values.
- **FR-005**: The `spec-kit` (default) lifecycle output MUST remain byte-identical to the prior
  published baseline for every profile (the 204/205 non-regression guarantee); publication MUST be
  blocked if it is not.
- **FR-006**: Default generation from the published template MUST create no repository and spawn no
  process unless the `initGit` opt-in is passed (the 205 guarantee), verified against the published
  package, not only the working tree.
- **FR-007**: The system MUST update the `fs-gg-ui-template` registry row to record the published
  version as a coherent release, link its tracking issue, and update the compatibility-doc projection
  to match.
- **FR-008**: The system MUST bring the dependent cross-repo request (the `templates`/`sdd` → `rendering`
  lifecycle-symbol ask) into agreement — a response or closure citing the published version and the
  coherent-set tag.
- **FR-009**: The release MUST be reproducible: the recorded versions in the package, the tag, and the
  registry MUST be mutually consistent, and a from-tag rebuild MUST reproduce the published package set.
- **FR-010**: If any of publish, tag, or registry/cross-repo update cannot complete, the system MUST
  leave the cross-repo record showing the release as **not yet coherent** (in-progress), never falsely
  coherent.
- **FR-011**: The board item MUST be moved to its terminal state (Done) only once FR-001–FR-010 hold,
  and the now-satisfied "blocked by lifecycle symbol" relationship MUST be cleared.

### Key Entities *(include if feature involves data)*

- **Published template package**: The `FS.GG.UI.Template` artifact on the feed at the new version;
  attributes — package id, version, the template surfaces it carries (lifecycle symbol, `initGit`
  opt-in), the profiles it emits.
- **Coherent-set tag**: A named immutable snapshot binding the published template version to the
  framework `FS.GG.UI.*` version set it is coherent with; the reproducibility anchor.
- **Registry row (`fs-gg-ui-template`)**: The cross-repo contract record; attributes — contract id,
  coherent flag, the version/tag it points at, linked tracking issue. Projected into the
  compatibility doc.
- **Cross-repo request**: The open `templates`/`sdd` → `rendering` issue depending on the shippable
  lifecycle symbol; resolved/responded once the package and tag exist.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A consumer who installs the template by id, starting from an empty cache, resolves a
  version strictly newer than `0.1.17-preview.1` that exposes the `lifecycle` parameter and `initGit`
  opt-in — in 100% of attempts.
- **SC-002**: Scaffolding the published template at `lifecycle=spec-kit` reproduces the prior published
  baseline with **zero** byte differences across every supported profile.
- **SC-003**: Default scaffolding from the published template (no git flag) creates **zero**
  repositories and spawns **zero** processes, and completes promptly with no defensive wait.
- **SC-004**: All supported profiles, scaffolded from the coherent-set tag, restore and build against a
  single consistent framework version with zero missing-package and zero version-conflict errors.
- **SC-005**: Two installs/scaffolds from the tagged snapshot, performed at different times, resolve to
  the identical template package and framework set (byte-for-byte reproducible).
- **SC-006**: After completion, the `fs-gg-ui-template` registry row and its compatibility projection
  both record the published version as coherent and link the tracking issue, and the dependent
  cross-repo request shows a response or closure citing the published version and tag — verifiable
  without reading any code.

## Assumptions

- The publish target is the project's existing local/preview package feed and the existing tag
  convention (`fs-skia-ui/v<semver>`-style namespaced tags, as used by feature 204); a new preview
  version in the `0.1.x-preview.N` line is appropriate. The exact version number and whether the
  coherent-set tag reuses the framework tag namespace or introduces a template-scoped tag name are
  resolved during planning.
- The 204/205 in-repo changes are complete and green in this repository's working tree; this feature
  packages and ships them rather than re-deriving them.
- The framework `FS.GG.UI.*` set the template pins is already a coherent published snapshot
  (established by feature 204 at `fs-skia-ui/v0.1.50-preview.1`); template coherence is declared on top
  of that base, and a from-tag rebuild can reproduce it.
- The cross-repo registry, compatibility doc, and the dependent request live in `FS-GG/.github` and the
  target repos respectively, and are updated via the cross-repo-coordination protocol (issues +
  registry PR), not by editing another repo's files directly.
- "Tag the coherent set" means a reproducible release snapshot plus an agreeing cross-repo record, as
  in feature 204 — not merely a git tag in isolation.
- No product-runtime F# surface changes; this is a packaging/release-coherence deliverable with no
  emitted-file changes beyond what 204/205 already introduced.
