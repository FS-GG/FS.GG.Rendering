# Feature Specification: product skill-manifest + single standalone materialize; drop dev-surface vendoring (ADR-0014 P2)

**Feature Branch**: `231-skill-manifest-materialize`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Implement ADR-0014 P2 for the fs-gg-ui template (issue FS-GG/FS.GG.Rendering#43, parent epic FS-GG/.github#110): product skill-manifest + single standalone materialize; drop dev-surface vendoring. R2.1 product manifest boundary; R2.2 one standalone materialize + content-parity test vs FS.GG.Contracts >= 1.4.0; R2.3 scope the `replaces:\"product\"` token; R2.4 no-dangling-route guard."

## Context

ADR-0014 (extends ADR-0011) requires every scaffolded product's three agent-skill roots
(`.agents/skills/`, `.claude/skills/`, `.codex/skills/`) to carry the byte-identical union of
process and product skills, produced by **one** shared, content-addressed algorithm. The
2026-07-01 four-repo audit found this repo's `fs-gg-ui` template violates that in three ways:

- **F3 (dangling wrappers)**: the template vendors the repo's own `.agents/skills/` developer
  surface wholesale into products. Every repo-root `fs-gg-*` entry there is a ~12-line wrapper
  routing to repo-internal paths (`../../../src/**`, `../../../template/**`,
  `../../../.claude/**`) that do not exist in a scaffolded product, so a spec-kit product ships
  ~13 dangling skill wrappers (the `fs-gg-product-*` alias layer plus framework wrappers such as
  `fs-gg-diagnostics`, `fs-gg-design-system`, `fs-gg-ant-design`, and profile-mismatched
  wrappers such as `fs-gg-samples`/`fs-gg-testing` outside their profiles).
- **F1 (standalone half — implementation multiplicity)**: Feature 230 mirrors the union into
  `.claude/` and `.codex/` via 24 hand-written per-skill `template.json` twin sources plus two
  blanket copies — hand-maintained fan-out that ADR-0014 replaces with one manifest-driven
  materialize step invoking the shared `mirror`/`verify` algorithm now published in
  `FS.GG.Contracts` 1.4.0 (live on the org feed; the former upstream blocker is delivered).
- **F5 (token leak)**: the template's lowercase name substitution rewrites the ordinary English
  word "product" wherever it appears in substituted files, corrupting skill prose in generated
  products.

This feature is roadmap phase **P2** (issue #43): fix all three in the template, guarded by
release gates, and release a coherent template set. The upstream contract (`skill-manifest`
schema v1, `AGENT_SKILL_ROOTS`, `Fsgg.SkillMirror`) is published; downstream phases P3/P4
(Templates composition gate, re-pin, enforcing flip) consume this feature's output.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Standalone spec-kit product is self-contained with three identical skill roots (Priority: P1)

A developer scaffolds a product from the `fs-gg-ui` template in the standalone spec-kit lane
(no orchestrator). The product they receive contains, in each of the three agent-skill roots,
exactly the union of the spec-kit process skills and the profile-selected product skills — every
skill body self-contained (no references to paths that only exist in the template's source
repository), all three roots byte-identical, produced by one mechanism.

**Why this priority**: This is the exit criterion of roadmap P2 and the user-visible defect
today (a scaffolded product offers ~13 skills that are broken on arrival, and the three roots
are only coincidentally aligned by 36 hand-written mirror rows).

**Independent Test**: Scaffold a standalone spec-kit product for each profile; enumerate the
three skill roots; assert set-equality and byte-equality across roots, assert zero skill bodies
referencing paths absent from the product tree, and assert the dev-surface-only skills
(`fs-gg-product-*` aliases, framework wrappers, repo-authoring skills) are absent.

**Acceptance Scenarios**:

1. **Given** a standalone spec-kit scaffold (default profile), **When** the product's skill
   roots are enumerated after the product's documented materialize step has run, **Then**
   `.agents/skills/`, `.claude/skills/`, and `.codex/skills/` contain the same skill ids with
   byte-identical `SKILL.md` bodies, and each body's digest matches the shipped skill-manifest.
2. **Given** the same scaffold, **When** every emitted skill body's path references are
   resolved against the product tree, **Then** none dangle (zero references to
   `src/**`-of-the-template, `template/**`, or other repo-internal paths absent in the product).
3. **Given** scaffolds across all five profiles, **When** the emitted skill-id sets are
   compared to the profile's declared product-skill selection plus the process set, **Then**
   they match exactly — no `fs-gg-product-*` aliases, no framework wrappers, no repo-only
   authoring skills.
4. **Given** a scaffold with the sample-pack profile or the feedback option, **When** roots are
   enumerated, **Then** the optional skills (`fs-gg-samples`, `fs-gg-feedback-capture`) appear
   in all three roots (and nowhere when not selected).

---

### User Story 2 - Orchestrated (sdd/none) products stay confined and unchanged (Priority: P2)

A consumer scaffolding through the orchestrator (`lifecycle=sdd`) or with no lifecycle
(`lifecycle=none`) receives a product whose provider skills live only in `.agents/skills/`
(the orchestrator remains the mirror authority for the other roots), with no dev-surface leak
and no spec-kit lifecycle content — exactly as today, so the closed Templates#47 chain stays
closed.

**Why this priority**: Regression protection for the orchestrated lane; ADR-0011/0014 assign
mirror authority to the orchestrator there, and this feature must not re-introduce the #47
class (provider writing SDD-owned trees).

**Independent Test**: Scaffold `lifecycle=sdd` and `lifecycle=none` products; assert
`.claude/`+`.codex/` carry zero product skills, `.agents/skills/` carries exactly the
profile-selected canonical product skills, and no dangling wrapper ships.

**Acceptance Scenarios**:

1. **Given** an `sdd` or `none` scaffold (any profile), **When** roots are enumerated, **Then**
   `.claude/skills/` and `.codex/skills/` contain zero product skills and `.agents/skills/`
   contains exactly the profile's canonical product skills — with self-contained bodies.
2. **Given** an `sdd` scaffold, **When** the emitted set is compared to today's (Feature 230)
   `sdd` output, **Then** the product-skill placement is unchanged (no new roots written, no
   new skill ids introduced or removed) except that formerly-leaked dev-surface content, if
   any, is gone.

---

### User Story 3 - The product's skill set is declared, content-addressed, and machine-checkable (Priority: P2)

A downstream consumer (the orchestrator, the Templates composition gate, or a product's own
tooling) reads one declarative skill-manifest shipped with/in the product — listing every
provider skill id, its scope, and the digest of its canonical body — instead of scanning
directories or trusting per-source template strings, and can verify the materialized roots
against it.

**Why this priority**: The manifest is the P2 contract deliverable that P3 (composition-gate
assertion) and P4 (enforcing flip) build on; without it the byte-identical-union property
remains unverifiable downstream.

**Independent Test**: Validate the shipped manifest against the published `skill-manifest`
schema (v1); recompute each listed body digest and compare; run the verify step against a
scaffolded product's roots and assert zero drift; corrupt one root copy and assert the drift is
reported.

**Acceptance Scenarios**:

1. **Given** the template's shipped skill-manifest, **When** validated against the published
   schema shape, **Then** it parses with `schemaVersion` 1 and every entry carries an id, a
   `product` scope, and a digest matching its canonical body.
2. **Given** a materialized standalone product, **When** the verify step runs over its three
   roots against the manifest-declared union, **Then** it reports zero drift.
3. **Given** a product where one root's copy of one skill is altered or deleted, **When** the
   verify step runs, **Then** it reports that skill as drifted (missing root or divergent).

---

### User Story 4 - One algorithm in both lanes, kept equal by a gate (Priority: P3)

A maintainer changing the skill fan-out touches one mechanism: the standalone materialize step
vendors the shared library's algorithm, and a repo gate proves the vendored copy behaves
identically to the published `FS.GG.Contracts` library, so the two lanes cannot silently drift
(the roadmap §6 risk).

**Why this priority**: Sustainability of the fix; prevents the recreation of F1 inside this
repo.

**Independent Test**: Run the content-parity gate: it exercises the vendored algorithm and the
published library (pinned ≥ 1.4.0) over the same inputs (mirror plans and verify verdicts,
including drift cases) and fails on any behavioral difference.

**Acceptance Scenarios**:

1. **Given** the vendored materialize algorithm and the published library, **When** the parity
   gate runs mirror/verify over representative and adversarial inputs (empty union, multi-root,
   missing copies, divergent bodies, hash mismatches, path retargeting), **Then** outputs are
   identical.
2. **Given** a deliberate local modification to the vendored algorithm's behavior, **When** the
   parity gate runs, **Then** it fails.

---

### User Story 5 - Skill prose keeps the word "product" (Priority: P3)

A developer reading a generated product's skills (and any other substituted prose) sees the
English word "product" intact; only intentional name-substitution sites are rewritten by the
product's chosen name.

**Why this priority**: Cosmetic but shipped-text corruption (F5); cheap to fix alongside the
skill emission rework.

**Independent Test**: Scaffold with a distinctive product name (e.g. `Zebra`); assert generated
skill bodies and docs contain no `zebra` where the source said "product" as an ordinary word,
while intentional substitution sites still render the product name.

**Acceptance Scenarios**:

1. **Given** a scaffold named with a distinctive token, **When** emitted skill bodies are
   scanned, **Then** ordinary-word "product" occurrences from the canonical sources survive
   verbatim (no lowercase-name rewriting inside skill prose).
2. **Given** the same scaffold, **When** files that intentionally embed the lowercase product
   name are inspected, **Then** they still receive the substitution (no regression of intended
   renames).

---

### Edge Cases

- Profile-conditional skills: `fs-gg-testing` (governed only), `fs-gg-samples` (sample-pack
  only), `fs-gg-feedback-capture` (feedback option only) must be in the manifest-declared union
  only when selected — the union the verify step checks is scaffold-parameter-dependent.
- A skill body legitimately mentioning a path-looking token that is not a product path (e.g.
  an example URL or a `docs/...` file the product does ship) must not false-positive the
  no-dangling-route guard; conversely relative `../` escapes must always be caught.
- The materialize step must be idempotent (re-running yields byte-identical roots, no
  duplicate writes) and safe on both Unix and Windows path conventions.
- If the materialize step cannot run in a consumer's environment, the product must still be
  usable and the gap visible: the primary root (`.agents/skills/`) is populated by generation
  itself, and the product documents how to (re-)materialize the remaining roots.
- The repo's own development surface (aliases, wrappers, authoring skills in the repo's
  `.agents/skills/`) must keep working for repo contributors — removal is from *product
  output*, not from the repository.
- `.codex/skills/` remains SDD-owned in orchestrated scaffolds; the standalone materialize must
  never run in `sdd`/`none` lifecycles.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001 (manifest)**: The template MUST publish a declarative skill-manifest conforming to
  the published `skill-manifest` schema (v1), declaring every provider-shipped skill with id,
  `product` scope, and the digest of its canonical body; the manifest MUST be shipped so that a
  scaffolded product (and downstream tooling) can read it, and MUST reflect the
  scaffold-time selection (profile / feedback conditionality) or declare the full catalog with
  selection derivable — such that the materialized union is exactly determinable from it.
- **FR-002 (dev-surface boundary)**: Product output MUST NOT include the repo's developer skill
  surface: no `fs-gg-product-*` aliases, no framework wrappers routing to repo-internal paths,
  no repo-authoring/dev-only skills. The spec-kit process skills (`speckit-*`) and the base
  project skill MUST continue to ship in the spec-kit lane; the canonical profile-gated product
  skills MUST continue to ship in all lifecycles.
- **FR-003 (one standalone materialize)**: The `.claude/skills/` and `.codex/skills/` roots of
  a standalone spec-kit product MUST be produced by exactly one materialize step that consumes
  the manifest-declared union from the primary root and fans it out via (a vendored copy of)
  the shared published algorithm — replacing all 24 per-skill twin sources and the blanket
  `.claude`/`.codex` copies. No per-skill, per-root hand-written mirror rows may remain.
- **FR-004 (lane confinement)**: The materialize step MUST run only in the standalone spec-kit
  lane. Under `sdd`/`none`, the template MUST write product skills to `.agents/skills/` only
  (byte-identical placement to today).
- **FR-005 (verification)**: A verify capability MUST exist that checks, for every skill in the
  materialized union: presence in each root, byte-identity across roots, and digest match
  against the manifest — and reports drift per skill. The repo's release gates MUST run it
  against live-scaffolded products (all profiles, all lifecycles) and fail on drift.
- **FR-006 (content parity)**: A repo gate MUST prove the vendored materialize/verify algorithm
  is behaviorally identical to the published shared library (pinned at ≥ the version that
  introduced it) over representative and adversarial inputs, failing on any divergence.
- **FR-007 (no-dangling-route guard)**: The release gates MUST reject any emitted skill whose
  body references a filesystem path absent from the generated product tree (covering all
  profiles/lifecycles in which that skill ships).
- **FR-008 (token scoping)**: The lowercase product-name substitution MUST NOT rewrite the
  ordinary English word "product" in emitted skill bodies or other prose not intended as a
  substitution site; intentional lowercase-name substitution sites MUST keep working.
- **FR-009 (release)**: The change MUST ship as a coherent template set: template version bump,
  release gates green, and the published template producing the User Story 1 outcome — ready
  for the downstream re-pin (roadmap P4, out of scope here).
- **FR-010 (repo surface intact)**: The repository's own `.agents/skills/` developer surface
  and skill-parity checks MUST remain functional for contributors (changes are to product
  emission, not to repo authoring).

### Key Entities

- **Skill manifest**: the declarative contract — schema version + entries `{id, scope, digest,
  body-or-path}` — listing the provider's product-scope skills; source of truth for the
  fan-out and verification.
- **Skill union**: the scaffold-parameter-dependent set of skills a given product must carry in
  every root — process skills (spec-kit lane) ∪ selected product skills.
- **Agent-skill roots**: the declared root set (`.claude`, `.codex`, `.agents`) under which
  each union skill materializes at `<root>/skills/<id>/SKILL.md`.
- **Materialize step**: the single standalone-lane mechanism that fans the union from the
  primary root into the remaining roots using the shared algorithm.
- **Drift report**: per-skill verification outcome (missing roots, cross-root divergence,
  digest mismatch) produced by the verify capability.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A standalone spec-kit scaffold (every profile) yields three skill roots with
  identical skill-id sets and byte-identical bodies, each matching its manifest digest —
  verified by gates over live scaffolds, zero drift.
- **SC-002**: Zero dangling skill routes in scaffolded products (all profiles × lifecycles);
  the ~13 known dangling wrappers are gone.
- **SC-003**: The number of hand-written per-root mirror rows in the template drops from 26
  (24 twins + 2 blanket copies) to 0; exactly one materialize mechanism produces the
  non-primary roots.
- **SC-004**: `sdd`/`none` scaffolds are placement-identical to today for product skills
  (`.agents/skills/` only; zero product skills in `.claude`/`.codex`).
- **SC-005**: The parity gate demonstrates vendored-vs-library behavioral equality and fails
  when the vendored copy is perturbed.
- **SC-006**: A scaffold named with a distinctive token emits skill prose with zero
  unintended rewrites of the word "product", while intended substitution sites still rename.
- **SC-007**: A coherent template set (version-bumped) passes all release gates, ready for the
  downstream P3/P4 phases.

## Assumptions

- `FS.GG.Contracts` ≥ 1.4.0 (the `skill-manifest` schema v1, the declared root set, and the
  `mirror`/`verify` algorithm) is live on the org feed and is the authority this feature vendors
  and parity-tests against; its shapes are stable for this feature's duration.
- The orchestrated lane's mirror authority (fsgg-sdd ≥ 0.5.0) continues to fan provider
  `.agents/skills/` writes into the other roots; this feature does not change orchestrated-lane
  mirroring, only what the provider emits.
- "Publish the manifest" is satisfied by shipping it with the template/product (consumable by
  downstream gates); registering it in the org registry is P3/P4 work.
- The downstream composition-gate assertion (P3) and re-pin/enforcing flip (P4) are out of
  scope; this feature only needs to leave the template set released and verifiable.
- The repo's existing lifecycle/emission gates (Features 204/219/230 tests and the lifecycle
  validator script) are the natural home for the new guards and will be reworked rather than
  duplicated; Feature 230's twin-matrix expectations are superseded by this feature.
- Skill-manifest conditionality: the manifest may declare the full product-skill catalog with
  per-skill applicability, since scaffold-time parameters (profile, feedback) determine the
  union; the verify step evaluates the union for the concrete scaffold.
