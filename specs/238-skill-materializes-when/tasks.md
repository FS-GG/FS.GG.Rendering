---
description: "Task list — record per-skill materializes-when/supplied-by on the product skill-manifest"
---

# Tasks: record per-skill materialization conditions on the product skill-manifest

**Input**: Design documents from `specs/238-skill-materializes-when/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/skill-manifest.schema.md, quickstart.md

**Tests**: INCLUDED — the constitution (Principle V) makes test evidence mandatory, and this feature's
whole point is a machine-checkable honesty guard. The test is written FIRST and must FAIL before the
generator change.

**Organization**: one user story (the honest manifest). No runtime/app surface exists, so the
template's "early live smoke run" standing clause is satisfied by the **deterministic gate**
(`--check` + the template.json-equivalence test) — disclosed in T004.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[US1]**: the single user story — an honest, condition-aware manifest

---

## Phase 1: Setup

- [X] T001 Record the no-regression baseline across EVERY test project: `dotnet fsi scripts/baseline-tests.fsx --out specs/238-skill-materializes-when/readiness/baseline.md` (globs `*.Tests.fsproj` so `tests/Package.Tests` — which owns the manifest gates — and the `samples/**` consumers are included; Feature231/204/219 are expected green pre-change, any pre-existing reds are flagged here not at merge).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: pin the exact condition map and the pre-change gate state before touching the producer.

- [X] T002 Build the id → `template.json` body-source condition map (the "root-cause map"): read `.template.config/template.json` `sources[]` and confirm all 12 verbatim conditions in `data-model.md` are current, INCLUDING the `fs-gg-project` special case (its gate is the whole-tree `source: "template/base/.agents/"` row, condition `(lifecycle == "spec-kit")`, distinct from its per-skill `supplied-by` dir). Correct `data-model.md`/`research.md` if any string drifted from the live file.
- [X] T003 Confirm the additive-shape seam matches reality: verify `contracts/skill-manifest.schema.md` against the current `template/skill-manifest/skill-manifest.json` (four base keys present, `schemaVersion: 1`) and confirm `Feature231SkillManifestTests` reads only `{id,scope,sha256,resolvablePath}` (so additive keys keep it green).
- [X] T004 **Deterministic gate baseline (live-smoke substitute — disclose why).** No running app surface exists for this manifest change; the honest pre-change evidence is `dotnet fsi scripts/generate-skill-manifest.fsx --check` (up-to-date) + `dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature231"` (green). Record both in the readiness baseline; note in the task output that the Feature-175 "run the real app" clause is N/A here and why.

**Checkpoint**: condition map verified verbatim; Feature231 green; contract seam confirmed additive.

---

## Phase 3: User Story 1 — honest, condition-aware manifest (Priority: P1) 🎯 MVP

**Goal**: every manifest entry records `materializes-when` (verbatim `template.json` condition) and
`supplied-by` (provider source dir); `fs-gg-project` is recorded as `spec-kit`-lane-only so its
sdd-lane absence is legitimate, not a `[missing]` supply failure.

**Independent Test**: `dotnet fsi scripts/generate-skill-manifest.fsx --check` is clean and
`Feature238*` is green, asserting each `materializes-when` equals the live template.json condition,
each `supplied-by` matches the catalog source dir, and `fs-gg-project` evaluates false under
`lifecycle=sdd` / true under `lifecycle=spec-kit` — while `Feature231/204/219` stay green.

### Test first (write, then watch it FAIL)

- [X] T005 [US1] Add `tests/Package.Tests/Feature238SkillMaterializesWhenTests.fs` (Expecto, `System.Text.Json`) asserting, over the parsed manifest: (a) every entry has non-empty `materializes-when` + `supplied-by`; (b) `materializes-when` == the verbatim `condition` of that skill's `template.json` body source — reuse the id→source mapping pattern from Feature231's "catalog coherent with emission rows" test, with `fs-gg-project` → the `template/base/.agents/` row; (c) `supplied-by` == `dirname(canonicalSource(id)) + "/"` (mirror Feature231's `canonicalSources` list); (d) a minimal condition evaluator over the `==` / `&&` / `||` / quoted-literal / `true` grammar proves `fs-gg-project` is **false** for `{lifecycle=sdd}` and **true** for `{lifecycle=spec-kit}` (SC-002/SC-005).
- [X] T006 [US1] Register the new file in `tests/Package.Tests/Package.Tests.fsproj` — a `<Compile Include="Feature238SkillMaterializesWhenTests.fs" />` line immediately after the `Feature231SkillManifestTests.fs` entry and before `Tests.fs`. Run `dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature238"` and CONFIRM IT FAILS (manifest has no `materializes-when`/`supplied-by` yet).

### Implementation

- [X] T007 [US1] Extend `scripts/generate-skill-manifest.fsx`: give each catalog row its verbatim `materializes-when` string (the T002 map) and derive `supplied-by = dirname(source) + "/"`; emit both keys in every entry's JSON object (stable key order, after `resolvablePath`); keep `schemaVersion: 1` and the sort-by-id ordering.
- [X] T008 [US1] Regenerate the manifest: `dotnet fsi scripts/generate-skill-manifest.fsx` → "wrote … (12 skills)"; then `--check` → "up to date". Confirm `git diff template/skill-manifest/skill-manifest.json` is **additive-only** (no change to any `id/scope/sha256/resolvablePath/schemaVersion` value — SC-003).
- [X] T009 [US1] Run `dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature238"` → green (the discriminating guard now passes).

**Checkpoint**: MVP complete — the manifest is honest and self-guarding; `fs-gg-project` recorded as spec-kit-only.

---

## Phase 4: Polish & Cross-Cutting

- [X] T010 [P] No-emission-regression proof: `dotnet test tests/Package.Tests --filter "FullyQualifiedName~Feature231|FullyQualifiedName~Feature204|FullyQualifiedName~Feature219"` → all green (same skills materialize in the same lanes; SC-004).
- [X] T011 [P] Docs: add a one-line note to the manifest/schema reference (grep `skill-manifest` under `docs/` — e.g. `docs/product/decisions/0014-*` / `docs/product/README.md`) documenting the two additive fields and the `fs-gg-project` honesty record; link `contracts/skill-manifest.schema.md`. Defer the local ADR-0017 mirror (`docs/product/decisions/0017-*`) — the org ADR is not yet committed (research R5 / cross-repo note).
- [X] T012 Run the full `quickstart.md` end-to-end and record the readiness evidence under `specs/238-skill-materializes-when/readiness/`.
- [ ] T013 Cross-repo close-out: post `## Response` on `FS-GG/FS.GG.Rendering#71` (owner decision = record-honestly; link spec/plan + the honest manifest record; note companions `.github#164` for `registry/skills.yml`+gate and `FS.GG.SDD#53` for the sdd process manifest); move Coordination board item #71 `In progress → In review` (→ `Done` on merge), and check the P1-Rendering box on epic `.github#163`.

---

## Dependencies & Execution Order

- **Setup (T001)** → no deps.
- **Foundational (T002–T004)** → after T001; BLOCKS US1. T002 feeds T005 and T007.
- **US1 (T005–T009)** → after Foundational. Strict order: T005 (write test) → T006 (register + confirm RED) → T007 (generator) → T008 (regenerate) → T009 (confirm GREEN). Test-before-implementation is mandatory (constitution V).
- **Polish (T010–T013)** → after US1 green. T010/T011 are [P] (different surfaces); T012 then T013 last (close-out needs everything green + committed).

### Parallel opportunities

- T010 and T011 touch different artifacts (test run vs docs) → parallel.
- Everything else is a tight serial chain (single generator, single manifest, single test file); no intra-story parallelism.

---

## Implementation Strategy

**MVP = Phase 1 + 2 + 3.** After T009 the feature is functionally done and self-guarding: the manifest
is honest, `--check` is clean, and the drift test binds it to `template.json`. Phase 4 proves
no-regression, updates docs, and closes the cross-repo loop.

## Notes

- The whole change is 3 artifacts: the generator, its regenerated JSON output, and one new test file
  (+ its `.fsproj` registration). No `src/`, no `.fsi`, no template emission-row edits.
- Tier 1 (contract shape) but the contract is JSON, not `.fsi` → no public-surface baseline task.
- Commit after each logical group; keep the `skill-manifest.json` diff additive-only.
