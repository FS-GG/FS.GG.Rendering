# Feature Specification: Bridge the Old Repository (Migration Stage R7)

**Feature Branch**: `007-bridge-old-repository`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next part of ff.gg"

## Context

This is the next increment of the FS.GG.Rendering migration. The migration is staged R1 → R8
in the active rendering implementation plan. R1 (fresh repo), R2 (product shape), R3
(validation set), R4 (source import), R5 (test harness), and R6 (CI cadence wiring,
feature `005-ci-cadence-wiring`) are done; feature `006` audited the imported mechanisms and
found them sound. The next uncompleted stage is **R7 — Bridge the old repository**.

Rendering is now usable here: the product source builds, the default local tier passes, CI
runs the validation set at its declared cadences, and the imported mechanisms have been
independently verified. What is missing is the **handoff**. The sibling repository
`EHotwagner/FS-Skia-UI` is where this code came from; it has been informally archived (its
README carries an "archived for now" note), but it still *describes itself as the active
framework* — it advertises Vulkan, a governed evidence workflow, and live NuGet packages,
with **no pointer to the repository that now owns the product**. A newcomer landing on the old
repo, or a consumer of the old `FS.Skia.UI.*` packages, has no documented path to the new home.
Conversely, this repository records its import provenance in `PROVENANCE.md` but does not yet
state, as a first-class document, that it is now the canonical home and what the relationship
to the archived source is.

R7 closes that gap. It is a **documentation and handoff stage**, not a code stage: it makes the
old→new relationship explicit, durable, and reviewable so that all future rendering work happens
here and the old repository is unambiguously a provenance/archive endpoint. It does **not**
rename anything — package and template identity stay `FS.Skia.UI.*`; deciding a rebrand is the
separate Stage R8 (`docs/product/decisions/0001-package-identity.md`). R7 records the *current*
identity mapping and hands the rename matrix to R8.

R7 also respects a hard boundary: the source repository is **archived (read-only)** and lives
outside this working tree. This feature therefore produces its deliverables **in this
repository**, and for the changes that belong on the old repo or the org profile (`FS-GG/.github`),
it produces **copy-ready handoff content plus a recorded action** rather than editing repositories
this feature does not own. Whoever holds write access (un-archiving the old repo if needed)
applies them as a follow-up; the audit trail of *what* must be applied lives here.

"Users" here are: a developer or consumer who arrives at the **old** repository or its packages
and needs to find the live product; a maintainer who needs an unambiguous rule for *where new
work goes*; and a future reader who needs to trust that the imported code's lineage is recorded.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A bridge document declares the canonical home (Priority: P1)

As someone who lands on the archived `FS-Skia-UI` repository (or one of its `FS.Skia.UI.*`
packages), I want a clear, prominent bridge notice that tells me the rendering product now lives
in `FS.GG.Rendering`, what moved, and where to go for current work — so I am never stranded on a
dead repository with no forward pointer.

**Why this priority**: The whole point of R7 is the handoff. If nothing tells a visitor where the
product went, the migration is invisible to everyone outside the maintainer's head. A single,
discoverable bridge notice is the minimum viable handoff and delivers value on its own, even
before the deeper provenance and archive work.

**Independent Test**: Review the bridge document (in this repo) and its copy-ready old-repo notice;
confirm that, from the old repository's entry point, a reader can reach the new home, learn what
was imported, and learn the directional policy (new work happens in the rendering repo) without
prior context.

**Acceptance Scenarios**:

1. **Given** the bridge deliverable, **When** a reader opens it, **Then** it names this
   repository as the canonical home for the rendering product, states the source repository and
   the imported commit, and links the import provenance already recorded in `PROVENANCE.md`.
2. **Given** the archived old repository's current README (which still presents itself as the
   active framework), **When** the bridge work is complete, **Then** a copy-ready deprecation/redirect
   notice for the old README exists, pointing to the new home, and is recorded as an action to apply
   to the old repo (with its read-only/archived status acknowledged).
3. **Given** a consumer of an old `FS.Skia.UI.*` package, **When** they follow the bridge, **Then**
   they find a statement of the current package-identity status (retained as `FS.Skia.UI.*` for now)
   and that any rename is a separate, later decision — so they are not led to expect a rebrand that
   has not happened.

---

### User Story 2 - Provenance and import-path lineage are first-class (Priority: P1)

As a maintainer or auditor, I want the source commit and import-path provenance to be captured as
a durable, complete record tied to the bridge — so anyone can trace any file here back to where it
came from in the old repository, and the lineage survives the old repo going fully cold.

**Why this priority**: Provenance is the part of the bridge that must outlive the archived repo. The
repo already has a `PROVENANCE.md`; R7's job is to make sure it is *complete and bridge-grade*
(source commit, path map, adaptations, exclusions) and that the bridge document references it as
the authoritative lineage rather than restating it. Trustworthy lineage is independently valuable
even if no one ever visits the old repo.

**Independent Test**: Cross-check the provenance record against the imported tree; confirm the source
commit is pinned, every imported top-level path maps to its source path, and every deliberate
adaptation/exclusion is recorded — with no imported area left unaccounted for.

**Acceptance Scenarios**:

1. **Given** the provenance record, **When** an auditor picks any imported source/test/template
   path, **Then** they can find its origin path and the pinned source commit.
2. **Given** a deliberate divergence from the source (governance excluded, ownership metadata
   adapted, solution format changed, identity retained), **When** the auditor reviews provenance,
   **Then** that divergence is recorded with its rationale.
3. **Given** the bridge document, **When** it refers to lineage, **Then** it points to the single
   provenance record rather than maintaining a second, drift-prone copy.

---

### User Story 3 - Package and template migration notes (current identity, rename deferred) (Priority: P2)

As a maintainer or a downstream consumer, I want a clear note on what happens to package and
template identities across the migration — that they are **retained** as `FS.Skia.UI.*` today, with
any rename handled as a separate decision — so no one mistakes "moved repositories" for "renamed
packages," and so R8 has a clean starting point if a rebrand is later chosen.

**Why this priority**: The plan calls for "package/template migration notes if identities changed."
Identities have **not** changed at R7, so the correct deliverable is a note that records the
*retained* mapping and the deferral, preventing a common confusion. It is P2 because it depends on
the bridge framing (Story 1) but sharpens it for package consumers.

**Independent Test**: Read the migration note and confirm it lists each retained package/template
identity, states they are unchanged by the repository move, and points to the R8 rebrand decision
record for any future rename — without itself deciding a rename.

**Acceptance Scenarios**:

1. **Given** the migration note, **When** a reader checks a package or the template identity, **Then**
   it shows the identity is retained (`FS.Skia.UI.*`) and unaffected by the repository move.
2. **Given** the migration note, **When** a reader asks whether a rename will happen, **Then** it
   points to the R8 decision (`docs/product/decisions/0001-package-identity.md`) as the place that
   choice is made, and does not pre-empt it.
3. **Given** a future rebrand at R8, **When** that work begins, **Then** the R7 note already provides
   the current-identity baseline the rename matrix builds on.

---

### User Story 4 - Archive note and directional policy (Priority: P2)

As a maintainer, I want a recorded archive note for the old repository's specs, reports, and
readiness artifacts, plus an explicit directional policy — new product work opens **here**; the old
repository receives only bridge, archive, provenance, or emergency migration fixes — so the
boundary between the two repositories is unambiguous and stays that way.

**Why this priority**: Without a stated rule, work can drift back to the old repo or duplicate across
both. The archive note tells readers that the old repo's historical artifacts are read-only history,
and the directional policy is the durable governance outcome of the whole migration. P2 because it
formalizes the boundary the earlier stories imply.

**Independent Test**: Read the archive note and directional policy; confirm it (a) marks the old
repo's specs/reports/readiness artifacts as archive-only history, and (b) states the one-way rule for
where new work and which kinds of changes go to each repository — matching the plan's R7 exit criteria.

**Acceptance Scenarios**:

1. **Given** the archive note, **When** a reader looks for the old repo's historical specs/reports/
   readiness artifacts, **Then** they are described as archive-only and not a second source of truth.
2. **Given** the directional policy, **When** a maintainer asks "where does new rendering work go?",
   **Then** the answer is unambiguously this repository.
3. **Given** the directional policy, **When** someone proposes a non-bridge change to the old repo,
   **Then** the policy identifies it as out of bounds (only bridge/archive/provenance/emergency fixes
   belong there), and governance experiments are explicitly excluded from rendering stabilization.

---

### Edge Cases

- **Old repository is archived / read-only**: the changes that belong on the old repo (README
  redirect, deprecation guidance) cannot be assumed to be directly writable. The feature must
  deliver copy-ready content plus a recorded action, and must not claim the old repo was edited when
  it was not. (Honesty-of-claim carries over from the audit and Constitution Principle VI.)
- **Provenance gap**: if any imported path or deliberate exclusion is missing from the provenance
  record, the bridge is incomplete; the gap must be closed (or recorded as a known, named gap), not
  papered over.
- **Identity confusion**: a reader must not infer from "the repo moved" that "the packages were
  renamed." The migration note must actively prevent this, because the packages are deliberately
  unchanged at R7.
- **Rebrand bleed**: R7 must not start renaming packages, namespaces, or the template — that is R8.
  Any rename content beyond recording the current mapping and pointing to R8 is out of scope.
- **Dangling links**: bridge/provenance/archive documents that cross-reference each other (or
  `PROVENANCE.md`, the decision records, the org profile) must point to targets that exist, so the
  handoff does not contain dead pointers.
- **Stale self-description on the old repo**: the old README still advertises Vulkan and a governed
  workflow that no longer describe the live product; the redirect notice must supersede that framing
  rather than leaving a visitor to believe the stale description.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST produce a **bridge document in this repository** that names
  `FS.GG.Rendering` as the canonical home for the rendering product, identifies the source
  repository `EHotwagner/FS-Skia-UI`, and summarizes what was imported and the relationship between
  the two repositories.
- **FR-002**: The bridge document MUST reference the existing import provenance (source commit and
  import-path map in `PROVENANCE.md`) as the single authoritative lineage record, rather than
  duplicating it.
- **FR-003**: The feature MUST ensure the provenance record is **bridge-grade and complete**: the
  pinned source commit, a path map covering every imported top-level area (source, tests, template,
  docs, build metadata), and every deliberate adaptation and exclusion with rationale. Any
  unaccounted area MUST be added or recorded as a named gap.
- **FR-004**: The feature MUST produce a **copy-ready redirect/deprecation notice for the old
  repository** (README banner and package-page guidance) that points visitors to the new home and
  supersedes the old repo's stale self-description — delivered as content plus a recorded action to
  apply, acknowledging the old repo's archived/read-only status.
- **FR-005**: The feature MUST produce a **package/template migration note** stating that package and
  template identities are **retained** as `FS.Skia.UI.*`, are unaffected by the repository move, and
  that any rename is deferred to Stage R8 — pointing to `docs/product/decisions/0001-package-identity.md`.
- **FR-006**: The migration note MUST NOT decide or begin a rebrand; it records only the current
  identity mapping and the deferral.
- **FR-007**: The feature MUST produce an **archive note** marking the old repository's specs,
  reports, and readiness artifacts as archive-only history and not a second source of truth.
- **FR-008**: The feature MUST state a **directional policy**: new rendering product work opens in this
  repository; the old repository receives only bridge, archive, provenance, or emergency migration
  fixes; governance experiments are not mixed into rendering work.
- **FR-009**: Every bridge/provenance/archive deliverable MUST cross-reference its related documents
  (this repo's `PROVENANCE.md`, the package-identity decision, the org profile / `.github` migration
  docs) with **no dead links** to in-repo targets.
- **FR-010**: The feature MUST NOT change product runtime behavior, build configuration, package
  identity, namespaces, or the template — it adds and refines documentation only.
- **FR-011**: Any change that belongs to a repository this feature does not own (the archived old
  repo, the org `.github`) MUST be delivered as copy-ready content with a recorded action, and MUST
  NOT be reported as already applied when it has not been. (Constitution Principle VI: no
  overclaiming.)
- **FR-012**: The bridge deliverables MUST be discoverable from this repository's entry points (e.g.,
  linked from `README.md` and/or `PROVENANCE.md`) so a reader of this repo can find the handoff.

### Key Entities

- **Bridge document**: the primary R7 deliverable in this repo; declares the canonical home, the
  source repository, what moved, and links to provenance, the migration note, the archive note, and
  the directional policy.
- **Provenance record**: the authoritative lineage (`PROVENANCE.md`) — source commit, path map,
  adaptations, exclusions; referenced (not duplicated) by the bridge.
- **Old-repo redirect notice**: copy-ready README/package deprecation-and-redirect content for the
  archived source repository, plus a recorded action to apply it.
- **Package/template migration note**: the retained-identity mapping and the R8 deferral.
- **Archive note**: the statement that the old repo's specs/reports/readiness artifacts are
  archive-only history.
- **Directional policy**: the one-way rule for where new work and which change kinds go to each
  repository.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From the **old** repository's entry point, a reader can reach the new canonical home and
  learn what was imported in at most one hop via the redirect notice (no prior context required).
- **SC-002**: 100% of imported top-level areas (source, tests, template, docs, build metadata) are
  accounted for in the provenance path map; zero imported areas are unexplained, or every exception is
  a named, recorded gap.
- **SC-003**: A reader can determine, from the migration note alone, that package/template identities
  are retained as `FS.Skia.UI.*` and unchanged by the move, and that any rename is decided at R8 —
  with zero implication that a rename has occurred.
- **SC-004**: The directional policy answers "where does new rendering work go?" and "what may change
  in the old repo?" unambiguously, matching the plan's R7 exit criteria.
- **SC-005**: Every in-repo cross-reference among the bridge, provenance, migration note, archive note,
  and directional policy resolves to an existing target — zero dead in-repo links.
- **SC-006**: Every deliverable destined for a repository this feature does not own is clearly marked
  as a recorded action with copy-ready content, and none is described as already applied — zero
  overclaims.
- **SC-007**: The feature changes zero lines of product code, build configuration, package identity,
  namespaces, or template content (documentation-only), verifiable by diff.
- **SC-008**: The bridge is discoverable from this repository's `README.md` and/or `PROVENANCE.md` in
  one hop.

## Assumptions

- **Stage identity**: "next part of ff.gg" is read as the next sequential migration stage. R1–R6 are
  complete and feature 006 audited the import; the next uncompleted stage in the R1→R8 plan is **R7 —
  Bridge the old repository**. (The earlier terse request "next phase in fs.gg" became R6/feature 005,
  establishing this reading.)
- **Documentation-only**: R7 is a handoff/documentation stage. It produces and refines Markdown
  deliverables; it writes no product code and changes no build, package, namespace, or template
  identity.
- **Old repo is archived/read-only and outside this tree**: the source repository is archived and not
  part of this working tree, so changes destined for it (and for the org `.github`) are delivered as
  copy-ready content + a recorded action, not applied here. Applying them (including any un-archiving)
  is a follow-up held by whoever owns those repositories.
- **Identity unchanged at R7**: package and template identities remain `FS.Skia.UI.*`. The
  "package/template migration notes if identities changed" deliverable is therefore a *retained-identity*
  note plus a deferral to R8 — not a rename. Rebrand decisioning is Stage R8 and out of scope here.
- **Provenance baseline exists**: `PROVENANCE.md` already records the source commit (`f759f399`) and a
  path map; R7 treats it as the lineage of record and completes/verifies it rather than starting over.
- **Audit is settled**: feature 006 concluded the imported mechanisms work as advertised; R7 does not
  re-audit. Audit follow-up items (e.g., the `renderHash` observation) are tracked separately and are
  out of scope here.
- **Scope boundary**: R7 covers the bridge/provenance/archive handoff and directional policy. Rebrand
  (R8), further CI changes (settled at R6), and any new product feature work are out of scope except
  for being pointed at by the directional policy.
