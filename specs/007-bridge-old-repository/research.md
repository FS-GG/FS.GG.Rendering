# Phase 0 Research: Bridge the Old Repository (Stage R7)

Methodology decisions for the handoff. R7 has no NEEDS CLARIFICATION markers; the stage is
authoritatively defined by the source `rendering-implementation-plan.md` (Stage R7 — Bridge the old
repository) and `transition-and-boundaries.md` (Bridge policy). The decisions below resolve the
*how*, each grounded in what already exists in this repo and the org `.github`.

## D1 — Where the bridge artifacts live

- **Decision**: A new `docs/bridge/` folder holds the bridge hub, the old-repo redirect, and the
  package-identity migration note. `PROVENANCE.md` stays at repo root (it already exists there) and
  is completed in place. `README.md` gets a one-hop link into `docs/bridge/`.
- **Rationale**: `docs/` already partitions by concern (`product/`, `validation/`, `ci/`, `audit/`,
  `imported/`, `harness/`). "Bridge/handoff" is a new concern, so a sibling folder matches the
  established convention and keeps the handoff discoverable as a unit without disturbing existing
  docs (Principle III).
- **Alternatives considered**: (a) a single top-level `BRIDGE.md` — rejected: scatters from the
  `docs/` convention and gives the redirect/migration sub-docs no clean home; (b) folding everything
  into `PROVENANCE.md` — rejected: provenance is lineage, not the canonical-home declaration or the
  directional policy; conflating them weakens the single-source-of-truth role `PROVENANCE.md` plays
  (FR-002).

## D2 — Handling the archived, read-only old repository

- **Decision**: Changes destined for `EHotwagner/FS-Skia-UI` (README redirect banner, package-page
  deprecation text) are authored here as **copy-ready blocks** under an explicit
  "recorded action — NOT yet applied" header in `docs/bridge/old-repo-redirect.md`. The same pattern
  covers any org `FS-GG/.github` touch-ups. R7 does not attempt to edit, un-archive, or push to those
  repositories.
- **Rationale**: The old repo is archived (its README already carries an "archived for now" note) and
  is outside this working tree; GitHub archived repos are read-only until explicitly un-archived.
  Claiming the redirect was applied when it cannot be from here would be an overclaim (Principle VI).
  Authoring copy-ready content + a recorded action gives the owner an apply-in-one-paste deliverable
  and an honest audit trail of what remains.
- **Alternatives considered**: (a) script a cross-repo push — rejected: requires un-archiving and
  credentials this feature does not own, and would be untestable from here; (b) omit the old-repo
  content and only describe it — rejected: the visitor on the old repo is the primary R7 user
  (US1/SC-001); the deliverable must be paste-ready, not a description of one.

## D3 — Reusing vs. restating provenance

- **Decision**: The bridge hub **references** `PROVENANCE.md` for lineage and never restates the path
  map. R7's provenance work is to *verify and complete* `PROVENANCE.md` to bridge-grade: pinned source
  commit (`f759f399`), every imported top-level area present in the path map, every deliberate
  adaptation/exclusion recorded with rationale. Any genuinely unaccounted area is added or recorded as
  a **named gap** rather than silently omitted.
- **Rationale**: Two copies of lineage drift; one authoritative record is the point of provenance
  (FR-002/FR-003). The repo already has a strong `PROVENANCE.md` (commit pinned, path map, adaptations,
  exclusions) — completion/verification is cheaper and more honest than a second document.
- **Alternatives considered**: a self-contained bridge that embeds the full path map — rejected:
  guarantees future drift between the bridge and `PROVENANCE.md`.

## D4 — Identity: retained mapping, rebrand deferred

- **Decision**: `docs/bridge/package-identity-migration.md` records the **retained** identity
  (`FS.Skia.UI.*` package IDs, root namespaces, template package ID — unchanged by the repository
  move) and points to `docs/product/decisions/0001-package-identity.md` and Stage R8 for any rename.
  It decides nothing.
- **Rationale**: The plan's deliverable is "package/template migration notes *if identities changed*."
  They have **not** changed at R7, so the correct artifact is a retained-identity note that prevents
  the common "repo moved ⇒ packages renamed" confusion (Edge: identity confusion) and gives R8 a clean
  baseline. Constitution Engineering Constraints and decision `0001` both fix identity as `FS.Skia.UI.*`
  until an explicit R8 decision.
- **Alternatives considered**: pre-staging an `FS.Skia.UI.* → FS.GG.UI.*` rename matrix — rejected:
  that is R8 work; pre-empting it here is rebrand bleed (Edge: rebrand bleed) and contradicts decision
  `0001`.

## D5 — Directional policy and archive note as hub sections (not separate files)

- **Decision**: The **directional policy** (FR-008) and the **archive note** (FR-007) are named
  sections inside `docs/bridge/README.md`, not standalone files.
- **Rationale**: Both are short and are read as part of the same handoff a visitor consumes once;
  separate one-paragraph files add navigation cost for no benefit (Principle III). Sections still map
  one-to-one to their FRs/SCs and are link-anchored for cross-reference.
- **Alternatives considered**: separate `directional-policy.md` / `archive-note.md` — rejected as
  over-factoring; revisit only if either grows its own sub-structure.

## D6 — Validation approach (the evidence)

- **Decision**: Three mechanical checks, documented runnably in `quickstart.md`:
  1. **Link integrity** — every in-repo Markdown link in the bridge artifacts, `PROVENANCE.md`,
     decision `0001`, and the new `README.md` link resolves to an existing path (grep link targets →
     test each exists).
  2. **Provenance coverage** — every imported top-level area (`src/`, `tests/`, `template/`,
     `.template.config/`, `.template.package/`, imported `docs/imported/`, root build metadata) appears
     in the `PROVENANCE.md` path map or is a named gap.
  3. **No-product-change guard** — `git diff --name-only main...` lists only Markdown under the allowed
     paths; zero `src/**`, `tests/**`, `*.props`, `*.slnx`, `template/**` changes (SC-007).
- **Rationale**: R7 ships docs, so its evidence is structural integrity, completeness, and a proof of
  *non*-interference with the product — exactly the three failure modes the spec calls out (dead links,
  provenance gaps, accidental code change). Each check must fail loudly when violated (Principle V); a
  coverage check that greens with a missing path is itself a finding.
- **Alternatives considered**: a markdown-link-check NuGet/CI job — deferred: R6 already owns CI
  cadence; R7 keeps validation as a documented local pass and leaves any CI wiring as a bounded R6
  follow-up rather than expanding scope here.

## D7 — Relationship to the org `.github` material

- **Decision**: Treat the org profile (`FS-GG/.github`) as **already carrying the cross-repo split
  framing** (its profile README states the archived `FS-Skia-UI` "remains as source inventory and
  provenance only"). R7 *aligns with and links to* it from the bridge hub, and lists any small org-side
  addition (e.g., a direct bridge link) as a copy-ready recorded action — it does not duplicate the
  org's split-decision docs into this repo.
- **Rationale**: The org `.github` is the home of cross-repo decisions (`docs/index.md`, split
  decision); the product repo's bridge should point to it, not fork it (cross-repo contracts kept
  small — `transition-and-boundaries.md`).
- **Alternatives considered**: copying the org split-decision docs here — rejected: creates a second
  source of truth across repos, the exact thing the boundary rules warn against.
