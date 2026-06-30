# Phase 0 Research: Consumer Skill Catalog Currency

All NEEDS CLARIFICATION from Technical Context are resolved below. Each decision records what was
chosen, why, and the alternatives rejected. Findings R0–R4 are grounded in code reads and directory
listings; R1's produced-surface ground truth is **explicitly deferred to a live scaffold run** per
the plan's standing assumption.

## R0 — Root cause: the catalog is orphaned, not merely stale

**Finding**: `template/base/docs/skillist-reference.md` claims to be "GENERATED from the live
SkillRegistry + Audit.ownsVocabulary … regenerate with `./fake.sh build -t
RefreshSurfaceBaselines` … Currency-checked by `TargetMetadataDrift`." **None of that machinery
exists:**

- No root `fake.sh`/`build.fsx`; the only `RefreshSurfaceBaselines`-intent script is
  `scripts/refresh-surface-baselines.fsx`, which reflects built **DLLs** into
  `readiness/surface-baselines/*.txt` and never touches `skillist-reference.md`.
- No `SkillRegistry` type and no `Audit.ownsVocabulary` function exist (the real `Audit` module,
  `src/Build/Evidence.fs`, is the evidence-graph audit).
- No `TargetMetadataDrift` code exists; the real `MetadataDrift` finding
  (`tools/Rendering.Harness/SkillParity.fs:777`) compares a **wrapper's** `name:` to its canonical
  `SKILL.md` `name:` — wrapper parity, not catalog content.
- Grep finds **zero writers** of `skillist-reference.md`; existing tests
  (`Feature219EmitFrameworkSkillsTests`, `Feature204LifecycleTemplateTests`,
  `validate-lifecycle-template.fsx`) assert only **emission gating** (present under `spec-kit`,
  absent under `sdd`/`none`), never row content.

**Decision**: Treat the file as a **hand-maintained doc with a false provenance header**. The fix
must (a) correct content, (b) replace the fictional provenance with a truthful statement, and
(c) introduce the *first real* currency check. The "regenerate it" language in issue #36 is honored
by either making generation real (R2 option B) or by an enforced hand-maintained contract (R2
option A).

**Alternatives rejected**: "Just edit the stale rows" — rejected: leaves the false header and no
guard, so it drifts again (exactly how it got here). "Find and re-run the existing generator" —
rejected: no generator exists.

## R1 — Ground truth of the produced skill surface (LIVE RUN REQUIRED)

**Question**: Which skill ids does a produced package actually carry, per profile × lifecycle? The
catalog must list exactly those, with paths the consumer actually has
(`.agents/skills/<id>/SKILL.md`), not framework-repo paths (`src/<Pkg>/skill/SKILL.md`).

**What is known from config** (`.template.config/template.json`):

- Product skills are wired per **profile** to both `.agents/skills/<id>/` and `.claude/skills/<id>/`:
  - `app || headless-scene || governed || sample-pack || game`: `fs-gg-scene`, `fs-gg-symbology`
  - `app || sample-pack || game`: `fs-gg-skiaviewer`, `fs-gg-elmish`
  - `app || game`: `fs-gg-keyboard-input`, `fs-gg-ui-widgets`
  - `governed`: `fs-gg-testing`
- The catalog (`docs/skillist-reference.md`) is emitted by a single `copyOnly` source gated by
  `lifecycle == "spec-kit"`; under `sdd`/`none` it is absent.
- `speckit-*` command skills ship under the `spec-kit` lifecycle workspace.
- `fsdocs-*`, `fsharp-*`, and the 8 defunct `fs-gg-*` ids are wired by **nothing** and exist as no
  directory — they never ship under any profile/lifecycle.

**Decision**: The catalog's resolvable set = **the union of profile-wired product skills**
(`fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`, `fs-gg-symbology`,
`fs-gg-testing`, `fs-gg-ui-widgets`) **plus the `speckit-*` command skills** (catalog is
spec-kit-gated, so those co-ship). Everything else currently listed is dropped. **This set is a
hypothesis until confirmed by an actual scaffold** — `/speckit-tasks` schedules the live run as the
first Foundational task. The catalog is a single static file, so it must list the **superset that
can ship** (annotating profile-specificity where it matters), since it cannot be regenerated
per-profile at scaffold time.

**Open sub-question for the live run**: whether the catalog should additionally narrow rows by
profile (it is one static file) or list the full shippable union with a profile note. Default:
**list the union with a one-line profile note**; revisit only if the scaffold shows a profile that
ships a skill not in the union.

**Alternatives rejected**: Authoring rows from the framework repo's `src/**/skill` layout —
rejected: those paths do not exist in the consumer's package. Per-profile generated catalogs at
scaffold time — rejected: out of proportion; the template emits one static doc and the union+note
is sufficient and testable.

## R2 — Seam: hand-maintained-under-check vs generated

**Decision**: **Option A — hand-maintained-under-check, with an honest header** is the primary
recommendation; a tiny generator (Option B) is an acceptable upgrade if it proves trivial against
`SkillParity` discovery.

- **Option A (recommended)**: Correct the catalog by hand to the R1 set; change the header to state
  the file is hand-maintained and **enforced by the currency check** (R3). Lowest mechanism,
  smallest surface, honest.
- **Option B (optional)**: Add `scripts/refresh-skill-catalog.fsx` that enumerates the produced
  surface via `SkillParity.discoverDefaultSurfaces`/`readEntry` and emits the rows; the currency
  check then asserts the committed file equals regeneration. Makes "GENERATED" true and fully
  drift-proof, at the cost of one more script and deciding the produced-vs-framework path mapping in
  code.

**Rationale**: FR-005/FR-007 demand a check that fails on drift and a refresh path that passes the
check — both are satisfied by Option A (the "refresh path" is "edit to satisfy the check") and more
strongly by Option B. The constitution favors the plainest thing that pays for itself (Principle
III); Option A ships the durable guard with the least code. The choice is finalized in
`/speckit-tasks` after the R1 live run shows how mechanical the row set is.

**Alternatives rejected**: Resurrecting the named `RefreshSurfaceBaselines`/`TargetMetadataDrift`
targets — rejected: they never existed; reviving fictional names adds confusion.

## R3 — Where the currency check lives, and how it extracts references

**Decision**: House the check as a **new Expecto test in `tests/Package.Tests/`**
(`Feature224SkillCatalogCurrencyTests.fs`), consuming the existing
`SkillParity` discovery to build the resolvable-skill set. Prefer **no new public surface**; if a
small reference-extraction helper is warranted as public API on `SkillParity`, update
`SkillParity.fsi` and the surface-area baseline in the same commit (Principle II).

**Reference extraction** must recognize the forms the docs actually use:

- `skillist-reference.md`: markdown table rows — the `` `id` `` in the first column and the path in
  the second column.
- `scaffold-map.md`: inline-code skill mentions in prose (`` `fs-gg-...` `` followed by "skill"),
  e.g. lines 131/140/149/153.

**Resolution rule**: a referenced id resolves iff `SkillParity` discovery finds a `SKILL.md` whose
`name:` equals that id within the produced-surface roots (`.agents/skills`, `.claude/skills`,
`template/product-skills`, `src/**/skill`, and the `speckit-*` command surface). The check fails
listing each unresolvable `(id, doc, line)` (FR-006).

**Rationale**: `Package.Tests` is where the other catalog/emission assertions already live
(`Feature219`, `Feature204`), so the new content check sits beside its siblings and runs in the same
pack lane. Reusing `SkillParity` avoids a second skill-enumeration implementation.

**Alternatives rejected**: A standalone `.fsx` not wired into a test project — rejected: would not
run in the existing test/pack gate, so it could be skipped silently. Extending the `MetadataDrift`
finding in `SkillParity` to cover catalogs — rejected as the *primary* home: that finding is about
wrapper/canonical parity and overloading it muddies its meaning; the doc-content check is a distinct
concern.

## R4 — Cross-repo sequencing & registry coherence

**Decision**: This is a **Tier 1** `fs-gg-ui-template` content change that ships only via a
republished `FS.GG.UI.Template`. It **rides the Feature 220 republish vehicle (#33)** (same coherent
tag as 219/220/223) and **feeds the downstream pin bump FS-GG/FS.GG.Templates#8**. On resolution:
update the `fs-gg-ui-template` cross-repo contract/registry coherence per the
cross-repo-coordination protocol, comment the result on issue #36, and confirm the Templates pin.

**Rationale**: Identical delivery mechanism to sibling epic-#34 child #35 (Feature 223), which set
the precedent of carrying package-content skill fixes on the republish and updating the
`fs-gg-ui-template` contract entry.

**Alternatives rejected**: Shipping as a standalone release — rejected: epic #34 explicitly
sequences its children onto one coherent republish to land under a single tag.

## Resolved unknowns summary

| Unknown | Resolution |
|---|---|
| Does a generator/registry produce the catalog? | No — orphaned hand-maintained doc; header is fictional (R0). |
| What should the catalog list? | The profile-wired product skills + `speckit-*`, as the shippable union; **confirm by live scaffold** (R1). |
| Generate or hand-maintain? | Hand-maintain-under-check (Option A) recommended; tiny generator optional (R2). |
| Where does the check live / what does it parse? | New `Package.Tests` test reusing `SkillParity` discovery; parses table rows + prose code-span refs (R3). |
| Delivery & coherence? | Tier 1; rides #33, feeds Templates#8, update registry + close #36 (R4). |
