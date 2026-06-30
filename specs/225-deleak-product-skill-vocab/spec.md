# Feature Specification: De-leak Product Skill Vocabulary

**Feature Branch**: `225-deleak-product-skill-vocab`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "start the next Rendering owned item on the coordination board" → resolved to **FS-GG/FS.GG.Rendering#37 — "Product skills leak framework-process vocabulary to consumers"** (child of epic #34, Phase P1 · Rendering; contract `fs-gg-ui-template`).

## Context

A scaffolded FS.GG.UI product ships **7 product skills** under `template/product-skills/fs-gg-*`
(`fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`,
`fs-gg-testing`, `fs-gg-ui-widgets`). These are the guidance a product author actually receives.
A consumer-skill audit (analysis run 2026-06-30) found the skill **bodies are good**, but their
framing **leaks framework-repo process** that a scaffolded product does not have. Three distinct
classes of leakage are present in the shipped files today:

1. **Framework-repo evidence process** baked into "Feature 168 … Evidence Rules" blocks —
   `fs-gg-testing/SKILL.md:53`, `fs-gg-ui-widgets/SKILL.md:111`, `fs-gg-skiaviewer/SKILL.md:53`.
   These reference framework-only artifacts: `scripts/refresh-local-feed-and-samples.fsx`, the
   "`package-feed` proof" workflow, "Framework readiness output under `specs/*/readiness/` is
   ignored until `.gitignore` allowlists it" (`fs-gg-testing/SKILL.md:59-60`), and a
   concurrent-`dotnet test`/`BaseOutputPath` rule (`fs-gg-testing/SKILL.md:63`).
2. **Dangling `specs/<feature>/feedback/` references** in the identical "Persistent problems"
   block of **every** product skill (`fs-gg-elmish:65`, `fs-gg-keyboard-input:101`,
   `fs-gg-scene:82`, `fs-gg-skiaviewer:106`, `fs-gg-testing:89`, `fs-gg-ui-widgets:150`,
   `fs-gg-symbology:200`). That path only exists under the **spec-kit** lifecycle; under an
   `app`/`game`/`sdd`/`none` scaffold it points nowhere.
3. **Framework feature-number stamps** bleeding into consumer prose — the "Feature 168" headers
   above, and heaviest in `fs-gg-symbology` ("feature 199", "feature 200", "spec-196 baseline").
   A product author does not need the framework's feature history.

These are **package-content** fixes. They change only the shipped skill prose (and add a guard so
the de-leaked state cannot silently regress); they do **not** change any skill's behavior or
capability, do **not** add or remove skills, and do **not** touch the consumer skill catalog
(`skillist-reference.md` / `scaffold-map.md`, corrected separately by sibling #36 / Feature 224).
They reach consumers only once shipped in a republished `FS.GG.UI.Template` and re-pinned
downstream (FS-GG/FS.GG.Templates#8).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Evidence guidance speaks the product author's language (Priority: P1)

A developer scaffolds a product and opens the testing / widgets / viewer skills to learn how to
capture evidence. They should read guidance about **what evidence to record and where in their own
product**, not instructions that assume the framework repo's feed-refresh scripts, package-feed
proof workflow, `.gitignore` allowlisting of `specs/*/readiness/`, or concurrent-test
`BaseOutputPath` rules — none of which exist in their scaffolded product.

**Why this priority**: This is the worst leak (named "worst" in the audit). It actively misdirects
an author toward framework-only tooling they cannot run, undermining trust in the whole skill set.

**Independent Test**: Open the three skills carrying "Feature 168 … Evidence Rules" blocks; confirm
the evidence guidance describes product-local actions and contains no reference to
`scripts/refresh-local-feed-and-samples.fsx`, "package-feed" proof, `specs/*/readiness/`,
`.gitignore` allowlisting, or `BaseOutputPath` — while still telling the author what evidence to
record and where.

**Acceptance Scenarios**:

1. **Given** the shipped `fs-gg-testing` skill, **When** an author reads the evidence section,
   **Then** it states what evidence to record and a product-local place to record it, with **no**
   framework-repo script path, package-feed/feed-refresh workflow, `specs/*/readiness/`,
   `.gitignore`, or `BaseOutputPath` reference.
2. **Given** the `fs-gg-ui-widgets` and `fs-gg-skiaviewer` skills, **When** an author reads their
   evidence sections, **Then** the same holds — product language, no framework-repo artifacts.
3. **Given** the de-leaked skills, **When** the evidence guidance is read end-to-end, **Then** the
   *intent* (record visual/readback/control evidence; verify before claiming done) is preserved —
   the leak is removed without removing the lesson.

---

### User Story 2 - Feedback / persistent-problems guidance fits the author's lifecycle (Priority: P2)

A developer working in a non-spec-kit product (lifecycle `app`/`game`/`sdd`/`none`) reads a skill's
"Persistent problems" block and is told to record findings under `specs/<feature>/feedback/`. That
folder does not exist in their project, so the instruction dead-ends. The guidance must point to a
location that exists for them.

**Why this priority**: This affects **all 7** skills (the broadest consumer-facing defect), but it
misdirects rather than actively breaks, so it ranks below US1.

**Independent Test**: Inspect the "Persistent problems" block in each of the 7 skills; confirm the
recommended place to record findings is either lifecycle-conditional or a location that exists
regardless of lifecycle (e.g., the skill's own durable-lessons section / a product-local docs
location), with no unconditional dependence on `specs/<feature>/feedback/`.

**Acceptance Scenarios**:

1. **Given** any of the 7 product skills under a non-spec-kit lifecycle, **When** an author reads
   the persistent-problems / feedback guidance, **Then** the recommended record location resolves
   to a real place in their project.
2. **Given** a spec-kit-lifecycle product, **When** an author reads the same guidance, **Then** the
   spec-kit feedback path is still offered (the fix is additive/conditional, not a removal of the
   spec-kit path).

---

### User Story 3 - No framework feature-number stamps in consumer prose (Priority: P3)

A product author reading any skill should not encounter the framework's internal feature/spec
numbers ("Feature 168", "feature 199", "feature 200", "spec-196 baseline"), which carry no meaning
in their project and read as unfinished internal notes.

**Why this priority**: Cosmetic/clarity — confusing but not misdirecting. Lowest of the three.

**Independent Test**: Search every shipped product skill body for framework feature/spec-number
stamps; confirm none remain in consumer-facing prose (section headings and inline references), while
the substantive guidance each stamp introduced is preserved.

**Acceptance Scenarios**:

1. **Given** the `fs-gg-symbology` skill, **When** an author reads it, **Then** the capability prose
   ("rich-text layout", "auto-label", "label-bound motion") remains but the "feature 199 / feature
   200 / spec-196" stamps are gone.
2. **Given** the evidence blocks across skills, **When** an author reads their headings, **Then**
   they are titled in product language (e.g., "Evidence Rules") with no "Feature 168" prefix.

---

### Edge Cases

- **A stamp/path is load-bearing** (the prose only makes sense with the number). Resolution:
  rewrite the sentence to carry the lesson without the number; never delete the lesson to remove the
  stamp.
- **A spec-kit-only path is genuinely useful under spec-kit.** The fix must remain conditional, not
  delete the spec-kit guidance — spec-kit authors keep it.
- **New leakage is introduced after this fix** (a future skill edit re-adds a feature stamp or
  framework path). Resolution: a regression guard fails the build so it cannot ship (see FR-007).
- **A skill not in scope today gains the leaky boilerplate later.** The guard must scan the whole
  shipped product-skill set, not a fixed list of 7 files.
- **Canonical-vs-vendored drift.** Several product skills are vendored copies of canonical
  framework skills; de-leaking the shipped copy must not reintroduce wrapper/canonical parity
  failures.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The shipped evidence guidance in `fs-gg-testing`, `fs-gg-ui-widgets`, and
  `fs-gg-skiaviewer` MUST describe **what evidence to record and where**, in language that applies
  to a scaffolded product, with no reference to framework-repo-only artifacts:
  `scripts/refresh-local-feed-and-samples.fsx`, the "package-feed" proof workflow,
  `specs/*/readiness/`, `.gitignore` allowlisting, or concurrent-test `BaseOutputPath` rules.
- **FR-002**: The persistent-problems / feedback guidance in all 7 product skills MUST recommend a
  record location that exists under the author's lifecycle. The `specs/<feature>/feedback/` path
  MUST appear only as a spec-kit-conditional option, never as the unconditional instruction.
- **FR-003**: Consumer-facing product-skill prose MUST NOT contain framework feature/spec-number
  stamps (e.g., "Feature 168", "feature 199/200", "spec-196"). Section headings carrying such a
  stamp MUST be retitled in product language.
- **FR-004**: Every de-leak edit MUST **preserve the underlying guidance** — the lesson, evidence
  intent, or capability each leaky block conveyed remains; only the framework-specific framing is
  removed or generalized.
- **FR-005**: The change MUST be confined to product-skill content (the shipped
  `template/product-skills/fs-gg-*/SKILL.md` bodies) plus the guard in FR-007. The **only** permitted
  edit outside that set is the matching reframe of a **canonical source skill body** when, and only
  when, the wrapper-vs-canonical parity check (FR-006) requires body-coverage to match — the same
  reframe, body only, never front-matter. It MUST NOT alter any skill's declared capability, add or
  remove skills, or modify the consumer skill catalog docs (`skillist-reference.md` / `scaffold-map.md`)
  owned by sibling #36.
- **FR-006**: The change MUST NOT introduce wrapper-vs-canonical skill-parity failures; where a
  product skill is a vendored copy of a canonical framework skill, parity MUST hold after the edit
  (consistent with the existing skill-parity check).
- **FR-007**: A repo-owned, reachable check MUST enforce the de-leaked state so it cannot silently
  regress: it scans the **whole shipped product-skill set** and fails when a skill body reintroduces
  any of — a framework feature/spec-number stamp; a framework-repo script/feed-proof path; a
  `specs/*/readiness/` reference (banned outright, no conditional form); or an **unconditional**
  `specs/<feature>/feedback/` reference (the `feedback/` path is the only conditional case — it passes
  only when its enclosing paragraph carries a spec-kit gating phrase). The failure message MUST name
  the offending skill, the matched leak, and where it occurs.
- **FR-008**: The corrected skills reach consumers only as **package content** in a republished
  `FS.GG.UI.Template` coherent set; the feature MUST record the delivery dependency (republish +
  downstream pin bump FS-GG/FS.GG.Templates#8) without itself owning the publish.
- **FR-009**: Cross-repo coherence — on completion, the originating board item (#37) and its parent
  epic (#34) MUST be updated to reflect delivery, per the coordination protocol.

### Key Entities *(include if feature involves data)*

- **Product skill**: a shipped `template/product-skills/fs-gg-*/SKILL.md` an author receives on
  scaffold. Attributes: id/name, body prose, optional vendored-from-canonical lineage.
- **Leak token**: a string in product prose that only makes sense in the framework repo — a
  framework feature/spec-number stamp, a framework-repo script/feed-proof path, or a
  lifecycle-specific `specs/...` path used unconditionally.
- **Lifecycle**: the scaffold lifecycle of the produced product (`spec-kit`, `app`, `game`, `sdd`,
  `none`); determines which paths (e.g., `specs/<feature>/feedback/`) actually exist.
- **Currency/leak guard**: the repo-owned check that scans the shipped skill set for leak tokens and
  fails the build on a match.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 0 of the shipped product skills reference framework-repo-only evidence artifacts
  (feed-refresh script, package-feed proof, `specs/*/readiness/`, `.gitignore` allowlist,
  `BaseOutputPath`) — down from 3 today.
- **SC-002**: 0 of the 7 shipped product skills carry an **unconditional** `specs/<feature>/feedback/`
  instruction — down from 7 today; under a non-spec-kit lifecycle every "where to record findings"
  instruction resolves to a real location.
- **SC-003**: 0 framework feature/spec-number stamps remain in consumer-facing skill prose — down
  from the current set (3 "Feature 168" headers + ≥4 symbology stamps).
- **SC-004**: A reviewer can confirm, for every de-leaked block, that the original lesson/capability
  is still present (no guidance lost) — 100% of edits are reframing, not removal.
- **SC-005**: The regression guard fails on an injected leak token (any of the three classes) and
  passes on the corrected skill set — demonstrated failing-before / passing-after.
- **SC-006**: The existing wrapper-vs-canonical skill-parity check remains green after the edits.

## Assumptions

- **Scope is #37 only.** Delivery of the symbology skill (#35) and the catalog currency check (#36)
  are already done/in-review; the new theming skill (#38) is a separate later item. This feature
  does not pull them in.
- **Product skills ship across lifecycles.** Per the resolution of #30 (framework skills now
  vendored on non-spec-kit scaffold paths) and Feature 219 (skills follow the product, not the
  lifecycle), the 7 product skills reach `app`/`game` (and SDD-composed) products — so their prose
  must not assume spec-kit. The fix makes lifecycle-specific paths conditional rather than removing
  them.
- **Non-spec-kit feedback location default.** When `specs/<feature>/feedback/` does not exist, the
  reasonable record location is the skill's own durable-lessons section and/or a product-local docs
  location; the guidance is phrased conditionally ("if your project uses Spec Kit … otherwise …").
- **Guard placement reuses existing discovery.** The regression guard reuses the repo's existing
  skill-discovery surface (the same enumeration that powers skill-parity and the #36 currency check)
  rather than re-implementing skill scanning; exact home/shape is a planning decision.
- **No public API surface change is expected.** If the guard nonetheless adds a public function to a
  framework tool, its `.fsi` and the surface-area baseline are updated in the same change (ordinary
  Tier 1 procedure).
- **Delivery rides a future republish.** The corrected content lands under a coherent
  `FS.GG.UI.Template` republish tag and the downstream pin bump; this feature produces the content
  and guard, not the publish itself.
