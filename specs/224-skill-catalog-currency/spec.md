# Feature Specification: Consumer Skill Catalog Currency

**Feature Branch**: `224-skill-catalog-currency`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "start the next Rendering owned item on the coordination board." → Coordination board epic #34 child **#36** — *[cross-repo] Consumer skill docs reference a defunct skill taxonomy; wire the currency check*.

**Change Classification**: Tier 1 (contracted change) — modifies consumer-facing content shipped under the `fs-gg-ui-template` package contract and adds a repo-owned currency/parity check. Cross-repo: feeds the downstream pin bump (FS-GG/FS.GG.Templates#8) and rides the Feature 220 republish vehicle (#33).

## User Scenarios & Testing *(mandatory)*

A scaffolded FS.GG.UI product ships with a small set of consumer-facing skill documents under
`docs/` (originating from `template/base/docs/`). Today those documents advertise a skill
taxonomy that no longer matches what the package actually delivers: the catalog lists ids and
file paths for skills that are not present in a scaffolded product, and other docs cross-link to
those non-existent skills. A product author following the docs is sent to skills that do not
exist. The package's own currency machinery claims to guard these docs but does not catch the
drift.

The seven skills a scaffolded product **actually** receives are `fs-gg-elmish`,
`fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`, `fs-gg-testing`,
and `fs-gg-ui-widgets` (authored under `template/product-skills/fs-gg-*` and delivered into the
produced package at `.agents/skills/<id>/` and `.claude/skills/<id>/`). The shipped catalog
instead lists framework-internal ids whose only plausible homes are `src/*/skill/*` framework-repo
paths the consumer never receives, or `.agents/skills/<id>/` directories that no real skill backs.

### User Story 1 - Catalog lists only the skills a product actually ships (Priority: P1)

A product author opens the shipped skill catalog (`docs/skillist-reference.md`) to discover
which skills are available to them. Every id and path the catalog lists resolves to a skill that
is actually present in their scaffolded product; no entry points at a skill that does not ship.

**Why this priority**: This is the primary consumer-facing defect — the catalog is the front
door to the skill surface, and right now it advertises a defunct taxonomy (renamed/merged ids,
plus framework-only `fsdocs-*`/`fsharp-*` ids) that misleads on first contact.

**Independent Test**: Scaffold a fresh product, open the generated catalog, and confirm every
listed id resolves to a `SKILL.md` present in that product; confirm no defunct or unshipped id
remains. Delivers value standalone even if the other stories are deferred.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded product, **When** the author reads the shipped skill catalog,
   **Then** it lists exactly the skills that ship with that product and nothing else.
2. **Given** the catalog before this change (listing `fs-gg-controls-host`,
   `fs-gg-typed-controls`, `fs-gg-viewer-host`, `fs-gg-design-tokens`, `fs-gg-evidence-mode`,
   `fs-gg-reconciliation`, `fs-gg-layout-readability`, `fs-gg-template-update`, and the
   `fsdocs-*`/`fsharp-*` families), **When** the catalog is regenerated, **Then** those ids are
   gone (or, if any cross-repo id is intentionally retained, the catalog states why).
3. **Given** the catalog after this change, **When** each listed path is followed, **Then** it
   points at a resolvable skill in the package (no `.agents/skills/*` or other unvendored path).

---

### User Story 2 - Cross-references point at skills that actually ship (Priority: P2)

A product author reading the scaffold map (`docs/scaffold-map.md`) follows its "see the X skill"
pointers. Each pointer names a skill that exists in their product.

**Why this priority**: Broken in-doc cross-references (e.g. "see the `fs-gg-typed-controls`
skill's consumer note", "the authority for this seam is the `fs-gg-controls-host` skill") send
authors to skills that never ship. It is consumer-facing but narrower than the catalog itself.

**Independent Test**: Grep the shipped scaffold map for skill references and confirm each names a
skill present in the package; follow each reference to a real skill.

**Acceptance Scenarios**:

1. **Given** the scaffold map, **When** the author follows a "see the … skill" reference,
   **Then** the referenced skill exists in the scaffolded product.
2. **Given** a reference that previously named a merged/renamed skill, **When** the map is
   updated, **Then** it names the current shipping skill that absorbed that responsibility
   (e.g. `fs-gg-ui-widgets`, `fs-gg-skiaviewer`).

---

### User Story 3 - Drift cannot silently recur (Priority: P3)

A maintainer renames, merges, or removes a product skill, or edits a shipped doc to reference a
skill id. A repo-owned check fails whenever a skill id referenced by the shipped consumer docs
has no resolvable skill in the package, before the package is published.

**Why this priority**: This is the durable fix. The current docs claim to be "currency-checked"
yet the defunct ids survived, so a one-time regeneration would drift again without an enforced
gate. It depends on Stories 1–2 having defined what "resolvable" means for the catalog.

**Independent Test**: Introduce a deliberately dangling skill id into a shipped doc (or remove a
referenced skill) and confirm the check fails with an actionable message naming the offending id;
revert and confirm it passes.

**Acceptance Scenarios**:

1. **Given** the shipped consumer docs, **When** the currency/parity check runs and every
   referenced skill id resolves to a packaged skill, **Then** the check passes.
2. **Given** a doc that references a skill id with no resolvable skill in the package, **When**
   the check runs, **Then** it fails and names the unresolvable id and the doc that references it.
3. **Given** a maintainer regenerates the catalog from the live registry, **When** they run the
   documented refresh, **Then** the regenerated catalog passes the check without hand-editing.

### Edge Cases

- **Intentional cross-repo id in a product catalog**: if a non-product (e.g. SDD) skill id must
  appear in a consumer doc, the catalog/check must allow it only with an explicit, documented
  reason rather than silently.
- **Lifecycle/profile variation**: the set of shipped skills can differ by scaffold profile
  (`app`/`game`/`sdd`/`none`); the catalog and check must reflect the skills the *produced*
  package actually carries, not the framework repo's full skill set.
- **Regeneration source unavailable**: if the catalog is regenerated from a registry that is not
  present in a consumer checkout, the documented refresh path must still work for the maintainer
  who owns the package content (the consumer does not regenerate).
- **Reference forms**: a skill may be named in prose, in a table path, or as an inline code span;
  the check must recognize the reference forms the docs actually use, not only table rows.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The shipped skill catalog (`template/base/docs/skillist-reference.md`) MUST list
  exactly the skills that a scaffolded product receives, and MUST NOT list any skill id that has
  no resolvable skill in the produced package.
- **FR-002**: The catalog MUST drop the defunct/renamed ids (`fs-gg-controls-host`,
  `fs-gg-typed-controls`, `fs-gg-viewer-host`, `fs-gg-design-tokens`, `fs-gg-evidence-mode`,
  `fs-gg-reconciliation`, `fs-gg-layout-readability`, `fs-gg-template-update`) and the unvendored
  `fsdocs-*` and `fsharp-*` families, unless a specific id is intentionally retained with a
  documented justification.
- **FR-003**: Each catalog entry's resolved path MUST point at a skill present in the scaffolded
  product — a consumer-resolvable location (`.agents/skills/<id>/` or `.claude/skills/<id>/`, which
  the produced package carries) — and MUST NOT point at a framework-repo-only path the consumer
  never receives (e.g. `src/*/skill/*`), nor at a `.agents/skills/<id>/` directory that no real
  skill backs.
- **FR-004**: The shipped scaffold map (`template/base/docs/scaffold-map.md`) MUST reference only
  skills that ship with the product; merged/renamed references MUST be updated to the current
  shipping skill that owns that responsibility.
- **FR-005**: A repo-owned currency/parity check MUST fail when any skill id referenced by a
  shipped consumer doc has no resolvable skill in the produced package.
- **FR-006**: The currency/parity check failure message MUST name the unresolvable id and the doc
  that references it, so the offending reference can be located without manual searching.
- **FR-007**: The documented regeneration path for the catalog MUST produce output that passes the
  currency/parity check without hand-editing.
- **FR-008**: The change MUST be verifiable against a freshly scaffolded product (not only against
  the framework repo source tree), since the defect manifests in the consumer's package.
- **FR-009**: Any intentionally retained non-product skill id in a consumer doc MUST carry an
  explicit recorded reason that the check recognizes, rather than being silently exempt.
- **FR-010**: The change MUST coordinate with the downstream consumer per the cross-repo protocol:
  it rides the republish vehicle (#33) and feeds the Templates pin bump (FS-GG/FS.GG.Templates#8),
  and updates the cross-repo issue/registry coherence as required for a `fs-gg-ui-template`
  contract touch.

### Key Entities *(include if feature involves data)*

- **Shipped skill catalog**: the consumer-facing list of available skill ids and their resolved
  locations, regenerated from an authoritative source rather than hand-maintained.
- **Shipping product skill**: a skill actually delivered in a scaffolded product (the
  `template/product-skills/fs-gg-*` set), identified by its `name:` value.
- **Skill reference**: any mention of a skill id in a shipped consumer doc (catalog row, scaffold
  map prose, cross-link) that must resolve to a shipping skill.
- **Currency/parity check**: the repo-owned gate that validates every shipped-doc skill reference
  resolves, and fails the build/pack otherwise.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a freshly scaffolded product, 100% of skill ids listed in the shipped catalog
  resolve to a skill present in that product (zero unresolvable ids).
- **SC-002**: Zero references in the shipped scaffold map name a skill that does not ship with the
  product.
- **SC-003**: Introducing a single dangling skill reference into any shipped consumer doc causes
  the currency/parity check to fail, and removing it causes the check to pass — demonstrated by a
  deliberate regression test.
- **SC-004**: A maintainer can regenerate the catalog via the documented refresh step and the
  result passes the currency/parity check on the first run, with no hand-edits required.
- **SC-005**: The defunct id set named in FR-002 appears zero times in the shipped consumer docs
  after this change.

## Assumptions

- The authoritative set of "skills a product ships" is the `template/product-skills/fs-gg-*` set
  resolved by their `name:` values; the catalog should reflect that produced surface, not the
  framework repo's full `.agents/skills/*` + `src/*/skill/*` inventory.
- The currency/parity check is a repo-owned, narrow check (in the spirit of the constitution's
  surface-drift / package-skew checks) and does not require any external governance platform.
- This work ships as package content: it reaches consumers only once carried in a republished
  `FS.GG.UI.Template` and re-pinned downstream; it is sequenced onto the #33 republish vehicle.
- The catalog's existing "GENERATED … regenerate with RefreshSurfaceBaselines / currency-checked
  by TargetMetadataDrift" machinery is the intended home for the fix; if that machinery cannot
  reach the produced skill surface, the check is extended or relocated rather than replaced
  wholesale — the plan phase decides the exact seam.
- Per-profile skill-set variation (`app`/`game`/`sdd`/`none`) is in scope to the extent the
  produced catalog must match the profile actually generated; defining new per-profile skill sets
  is out of scope.
- Authoring net-new product skills, de-leaking framework vocabulary from skill bodies (#37), and
  adding a theming skill (#38) are sibling epic items and are out of scope here.
