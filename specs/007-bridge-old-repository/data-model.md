# Phase 1 Data Model: Bridge the Old Repository (Stage R7)

R7 has no runtime data. The "entities" are the **documents** the handoff produces and the
**relationships** (cross-references) among them. This model fixes each artifact's required content,
where it physically lives, and which spec requirement and success criterion it satisfies, so the
Phase 1 contracts and the implementation tasks have an unambiguous target.

## Entity: Bridge document (hub)

- **Realized as**: `docs/bridge/README.md`
- **Purpose**: The single entry point declaring this repo as the canonical home; hosts the directional
  policy and archive note as sections; links every other bridge artifact.
- **Required fields (sections)**:
  - *Canonical home* — names `FS.GG.Rendering` as the product's home; names source repo
    `EHotwagner/FS-Skia-UI` and the pinned import commit (`f759f399`).
  - *What moved* — one-paragraph summary of imported areas; **links** `PROVENANCE.md` as the
    authoritative lineage (does not restate the path map).
  - *Directional policy* (section) — new rendering work opens here; the old repo receives only
    bridge/archive/provenance/emergency fixes; governance experiments stay out of rendering work.
  - *Archive note* (section) — the old repo's specs/reports/readiness artifacts are archive-only
    history, not a second source of truth.
  - *Identity status* — one line: identity retained as `FS.Skia.UI.*`; rename deferred to R8 →
    **links** `package-identity-migration.md`.
  - *Links* — to `old-repo-redirect.md`, `package-identity-migration.md`, `PROVENANCE.md`,
    `docs/product/decisions/0001-package-identity.md`, and the org profile
    (`https://github.com/FS-GG/.github`).
- **Satisfies**: FR-001, FR-002, FR-007, FR-008, FR-012 · SC-004, SC-008
- **Relationships**: references → Provenance record; contains → Directional policy, Archive note;
  links → Redirect notice, Migration note, decision 0001, org profile.

## Entity: Provenance record

- **Realized as**: `PROVENANCE.md` (existing, completed in place)
- **Purpose**: The authoritative lineage from source to this repo.
- **Required fields**:
  - *Source repository* + *pinned source commit* (`f759f399`, 2026-06-14).
  - *Path map* — every imported top-level area present: `src/` modules, `tests/` suites, `template/`,
    `.template.config/`, `.template.package/`, imported `docs/imported/`, root build metadata
    (`Directory.Build.props`, `Directory.Packages.props`, the `.slnx`), surface baselines, scripts.
  - *Adaptations* — each deliberate divergence with rationale (governance excluded, ownership metadata
    adapted, repo-root marker rewritten, Vulkan case retained-as-graceful, template governance profile
    removed, solution format, skipped tests).
  - *Exclusions* — what was deliberately left in the source archive.
  - *Named gaps* (if any) — any imported area not yet mapped, recorded explicitly rather than omitted.
- **Validation rule**: coverage check (quickstart) — for each imported top-level area, a path-map entry
  exists OR a named gap names it. Zero silent omissions.
- **Satisfies**: FR-003 · SC-002
- **Relationships**: referenced by → Bridge hub, Redirect notice, Migration note.

## Entity: Old-repo redirect notice

- **Realized as**: `docs/bridge/old-repo-redirect.md`
- **Purpose**: Copy-ready content for the **old** repo and its package pages, delivered with a recorded
  action because R7 cannot apply it.
- **Required fields**:
  - *Recorded-action header* — states the target (archived `EHotwagner/FS-Skia-UI` README + NuGet
    package pages), status **NOT yet applied**, and that the old repo is archived/read-only (apply
    requires un-archive by the owner).
  - *Old-repo README banner block* — copy-ready Markdown: "this product has moved to FS.GG.Rendering",
    link to the new home, one line on what moved, supersedes the stale Vulkan/governed-workflow framing.
  - *Package-page deprecation block* — copy-ready text for `FS.Skia.UI.*` package descriptions pointing
    to the new home, **without** asserting a rename (identity retained).
  - *Apply checklist* — the exact steps the owner takes (un-archive → paste banner → re-archive; update
    package descriptions).
- **Validation rule**: contains the recorded-action header and is never described elsewhere as
  "applied" (no-overclaim grep in quickstart).
- **Satisfies**: FR-004, FR-011 · SC-001, SC-006
- **Relationships**: links → Bridge hub (new home), Provenance record (what moved).

## Entity: Package/template migration note

- **Realized as**: `docs/bridge/package-identity-migration.md`
- **Purpose**: Record the retained identity mapping and the deferral; prevent identity confusion.
- **Required fields**:
  - *Retained-identity table* — each package (`FS.Skia.UI.Scene`, `.Layout`, `.Controls`, …) and the
    template package ID: status **retained, unchanged by the repository move**.
  - *Deferral statement* — any rename is decided at Stage R8; **links**
    `docs/product/decisions/0001-package-identity.md`.
  - *Non-decision disclaimer* — this note neither decides nor begins a rebrand.
- **Validation rule**: no rename instruction or new package ID appears (rebrand-bleed guard); links to
  decision 0001 resolve.
- **Satisfies**: FR-005, FR-006 · SC-003
- **Relationships**: referenced by → Bridge hub; links → decision 0001 (R8).

## Entity: Directional policy (hub section)

- **Realized as**: section of `docs/bridge/README.md`
- **Purpose**: The durable one-way boundary rule.
- **Required fields**: where new work goes (here); which change kinds the old repo may still receive
  (bridge/archive/provenance/emergency only); governance experiments excluded from rendering work.
- **Satisfies**: FR-008 · SC-004
- **Relationships**: part-of → Bridge hub; consistent-with → org profile operating rule,
  `transition-and-boundaries.md` Bridge policy.

## Entity: Archive note (hub section)

- **Realized as**: section of `docs/bridge/README.md`
- **Purpose**: Mark the old repo's historical artifacts as archive-only.
- **Required fields**: old repo's specs, reports, and readiness artifacts are archive-only history and
  not a second source of truth.
- **Satisfies**: FR-007 · SC-004
- **Relationships**: part-of → Bridge hub.

## Entity: Discoverability link

- **Realized as**: one added line in `README.md`
- **Purpose**: Make the bridge reachable in one hop from this repo's entry point.
- **Required fields**: a link to `docs/bridge/README.md` with a short label (e.g., under Status or a
  "Migration / bridge" pointer).
- **Satisfies**: FR-012 · SC-008
- **Relationships**: links → Bridge hub.

## Cross-reference integrity (applies to all entities)

Every in-repo link among the above MUST resolve to an existing target (FR-009 · SC-005). External
links (to GitHub repos/NuGet) are allowed and not path-checked. The quickstart link-integrity check
enumerates in-repo link targets and asserts each exists.

## Requirement → artifact coverage map

| FR | Artifact / field |
|---|---|
| FR-001 | Bridge hub — *Canonical home* |
| FR-002 | Bridge hub — *What moved* (references `PROVENANCE.md`, no restatement) |
| FR-003 | Provenance record — completed path map + adaptations + exclusions (+ named gaps) |
| FR-004 | Redirect notice — README banner + package block (copy-ready) |
| FR-005 | Migration note — retained-identity table + deferral |
| FR-006 | Migration note — non-decision disclaimer |
| FR-007 | Bridge hub — *Archive note* section |
| FR-008 | Bridge hub — *Directional policy* section |
| FR-009 | All — in-repo links resolve |
| FR-010 | (No product/build/identity/template change — enforced by no-change guard) |
| FR-011 | Redirect notice — recorded-action header; no "applied" claim |
| FR-012 | `README.md` — discoverability link |
