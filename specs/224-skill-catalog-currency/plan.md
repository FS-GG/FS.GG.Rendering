# Implementation Plan: Consumer Skill Catalog Currency

**Branch**: `224-skill-catalog-currency` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/224-skill-catalog-currency/spec.md`

## Summary

The consumer-facing skill catalog `template/base/docs/skillist-reference.md` advertises a skill
taxonomy that no longer exists. Investigation (see [research.md](./research.md)) found the defect is
deeper than "stale rows":

- **The "GENERATED from the live SkillRegistry … regenerate with `RefreshSurfaceBaselines` …
  currency-checked by `TargetMetadataDrift`" header is fiction.** None of those targets, scripts,
  or a `SkillRegistry`/`ownsVocabulary` type exist in the repo. The catalog is an **orphaned,
  hand-maintained** file; no code writes it and no check validates its rows.
- **All 8 defunct `fs-gg-*` ids** (`controls-host`, `typed-controls`, `viewer-host`,
  `design-tokens`, `evidence-mode`, `reconciliation`, `layout-readability`, `template-update`) and
  the entire `fsdocs-*` / `fsharp-*` families it lists **resolve to no `SKILL.md` anywhere** — they
  are not in `.agents/skills/`, not vendored, and never ship.
- **`scaffold-map.md`** names 5 skills in prose; 3 (`fs-gg-typed-controls`, `fs-gg-controls-host`,
  `fs-gg-viewer-host`) are not in the vendored product-skill set and dangle for a consumer.
- The **real** skill enumerator is `tools/Rendering.Harness/SkillParity.fs`
  (`discoverDefaultSurfaces` / `filesForSurface` / `readEntry`); it scans `.agents/.claude`,
  `src/**/skill`, `template/**` and reads `name:` frontmatter — but it is wired only to
  **wrapper-vs-canonical** parity (`MetadataDrift`), never to catalog content.

**Technical approach** (three coherent slices, mirroring the spec's P1/P2/P3):

1. **Correct the catalog content + honest provenance** — rewrite `skillist-reference.md` so every
   listed id resolves to a real skill the produced package carries, drop the defunct/unvendored
   ids, and replace the false `RefreshSurfaceBaselines`/`TargetMetadataDrift` provenance with a
   truthful header (the file is enforced by the new currency check below; whether it is
   hand-maintained-under-check or emitted by a tiny generator is decided in research R2).
2. **Repoint `scaffold-map.md`** prose references to skills that actually ship.
3. **Add a reachable currency check** — a repo-owned test that extracts every skill-id reference
   from the shipped consumer docs and fails when any does not resolve to a real `SKILL.md`,
   **reusing `SkillParity`'s existing discovery** so it cannot drift again.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The findings above are verified by *reading* code and *listing* directories, not by *scaffolding
> a product*. `/speckit-tasks` MUST schedule an **early live verification** in the Foundational
> phase (before any catalog edit): scaffold a product at one spec-kit and one non-spec-kit profile
> and enumerate its actual `.agents/skills/` + `.claude/skills/` + emitted `docs/` to capture the
> **ground-truth produced skill surface** the catalog must match. Do not author catalog rows from
> the framework-repo layout; author them from what the produced package actually carries.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo standard).

**Primary Dependencies**: `dotnet new` template engine
(`/.template.config/template.json`); Expecto (tests); the in-repo `Rendering.Harness`
skill-parity tool (`tools/Rendering.Harness/SkillParity.fs`) for skill discovery. No new
dependencies.

**Storage**: N/A — file/manifest content only (the two `template/base/docs/*.md` docs; test code;
optionally a small generator script under `scripts/`).

**Testing**: Expecto. New/changed tests in `tests/Package.Tests/` (doc-content currency) and/or
`tests/Rendering.Harness.Tests/` (if the check rides `SkillParity`). The existing env-gated
lifecycle validator (`scripts/validate-lifecycle-template.fsx`, run via
`FS_GG_RUN_LIFECYCLE_VALIDATION=1`) is the real-scaffold evidence path for the standing-assumption
live run.

**Target Platform**: Produced FS.GG.UI product packages (`FS.GG.UI.Template`); the check runs in
the framework repo's test/pack lane (Linux/CI).

**Project Type**: F# library + `dotnet new` template product (desktop-app framework). Single repo.

**Performance Goals**: N/A — the currency check is a build-time correctness gate, not a hot path.

**Constraints**: Tier 1 contracted change to the `fs-gg-ui-template` package surface. The catalog
is **spec-kit-lifecycle-gated** (Feature 219): present under `spec-kit`, absent under `sdd`/`none`
— the fix must keep that gating intact while correcting content. Ships as package content only;
reaches consumers via the #33 republish and the FS-GG/FS.GG.Templates#8 pin bump.

**Scale/Scope**: 2 shipped docs (`skillist-reference.md` ~90 lines, `scaffold-map.md` 158 lines),
one new check, optionally one small generator. 7 vendored product skills + `speckit-*` command
skills as the in-scope resolvable surface.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ | Spec done; the new check's discovery surface is exercised through the packed/loaded `SkillParity` API and asserted by an Expecto test before/after content fix. |
| II. Visibility in `.fsi` | ✅ (conditional) | If the check adds any public function to `Rendering.Harness` (`SkillParity.fs`), its `.fsi` (`SkillParity.fsi`) **and** the surface-area baseline MUST be updated in the same change. If the check is a self-contained test/script with no new public surface, no `.fsi` change is needed. |
| III. Idiomatic Simplicity | ✅ | Reuse existing `discoverDefaultSurfaces`/`readEntry`; plain reference-extraction + set-membership. No SRTP/reflection/new CE. |
| IV. Elmish/MVU boundary | ✅ N/A | Pure validation over file content; no stateful/I-O workflow. |
| V. Test Evidence Mandatory | ✅ | A failing-before/passing-after test: introduce a dangling id → check reds; corrected catalog → green. Real evidence via an actual scaffold (live run), not a fixture, for the produced-surface check. |
| VI. Observability & Safe Failure | ✅ | Check failure message names the unresolvable id **and** the doc/line that references it (FR-006). |
| Change Classification | ✅ Tier 1 | Touches the `fs-gg-ui-template` consumer surface and adds a gate; requires doc/content update, test evidence, and (if public surface added) `.fsi` + baseline updates. Cross-repo coherence per FR-010. |

**Result: PASS** (no unjustified violations). The only conditional is II — handled by treating an
added public `SkillParity` function as a baseline-updating change; the plan prefers placing the
check where it adds the least new public surface (see research R3).

## Project Structure

### Documentation (this feature)

```text
specs/224-skill-catalog-currency/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — seam decisions (generator vs check, check home, produced-surface ground truth)
├── data-model.md        # Phase 1 — entities: catalog, skill reference, resolvable skill set, check finding
├── quickstart.md        # Phase 1 — runnable validation (scaffold + run check; dangling-id regression)
├── contracts/
│   └── catalog-currency-check.md   # Phase 1 — the currency-check contract (inputs, resolution rule, failure shape)
└── checklists/
    └── requirements.md  # Spec quality checklist (from /speckit-specify)
```

### Source Code (repository root)

```text
.template.config/
└── template.json                       # (read-only ref) per-profile product-skill wiring (:254-320);
                                         #   catalog copyOnly source, spec-kit-gated (:242-251). NOT edited
                                         #   unless research R1 shows gating itself is wrong.

template/base/docs/
├── skillist-reference.md               # EDIT — correct rows to the produced skill surface; honest header
└── scaffold-map.md                     # EDIT — repoint dangling skill prose refs (:131, :140, :149)

tools/Rendering.Harness/
├── SkillParity.fs / .fsi               # REUSE discovery; extend ONLY if the check lives here (then update .fsi + baseline)

scripts/
└── (optional) refresh-skill-catalog.fsx  # OPTIONAL generator if research R2 chooses "generated, not hand-maintained"

tests/
├── Package.Tests/
│   └── Feature224SkillCatalogCurrencyTests.fs   # NEW — catalog/scaffold-map content currency + dangling-id regression
└── Rendering.Harness.Tests/
    └── (alt home for the check if it rides SkillParity)
```

**Structure Decision**: Single-repo F# framework + `dotnet new` template. The change is localized
to two shipped docs under `template/base/docs/`, one new test (home decided in research R3), and an
optional small generator under `scripts/`. The authoritative skill enumerator
(`tools/Rendering.Harness/SkillParity.fs`) is **reused, not re-implemented**. No edit to
`.template.config/template.json` is planned unless the live run (R1) shows the catalog's emission
gating is itself wrong (it is not, per Feature 219).

## Complexity Tracking

> No constitution violations require justification. The single conditional (Principle II, adding a
> public `SkillParity` function) is avoided where possible by housing the check as a test that
> consumes existing discovery; if a public helper is genuinely warranted, its `.fsi` and
> surface-area baseline are updated in the same commit, which is ordinary Tier 1 procedure, not a
> complexity exception.
