# Tasks: product skill-manifest + single standalone materialize; drop dev-surface vendoring (ADR-0014 P2)

**Input**: Design documents from `/specs/231-skill-manifest-materialize/`
**Prerequisites**: plan.md, research.md, data-model.md, contracts/skill-manifest-and-materialize.md, quickstart.md

**Tests**: Included — this feature IS gate work (Constitution V: fail-before/pass-after), so
gate tasks precede/accompany each behavior change.

## Phase 1: Setup

- [X] T001 Add `FS.GG.Contracts` `1.4.0` central pin to `Directory.Packages.props` (test-only dependency; justification research.md §New dependency) and verify restore resolves from nuget.org
- [X] T002 [P] Record the pre-change live baseline: run `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` on the unmodified tree; save the emitted report + a scaffold tree listing (spec-kit app profile: which `.agents/skills/` dirs exist, which are 12-line wrappers) to `specs/231-skill-manifest-materialize/readiness/baseline.md` — this confirms the F3 hypothesis live (plan.md standing assumption)

## Phase 2: Foundational (blocking prerequisites)

- [X] T003 Name-neutralize the canonical skill bodies: rephrase `template/product-skills/fs-gg-testing/SKILL.md` (`src/Product/Product.fsproj` → name-agnostic), `template/product-skills/fs-gg-layout/SKILL.md` (`Product.LayoutEvidence`, `Product/Program.fs` → name-agnostic), and audit `template/base/.agents/skills/fs-gg-project/SKILL.md` + all other canonical bodies (`template/product-skills/*/SKILL.md`, `template/fragments/samples/skill/SKILL.md`, `template/feedback/skill/SKILL.md`) for remaining `Product` rename-token path references and repo-internal path references (R4, R7 precondition)
- [X] T004 Create `scripts/generate-skill-manifest.fsx`: computes lowercase-hex SHA256 over each canonical `SKILL.md` (12-entry catalog per data-model.md) and writes `template/skill-manifest/skill-manifest.json` (schema v1, sorted by id, `resolvablePath` entries, no inline body); run it to produce the manifest (R3)
- [X] T005 Create `template/lifecycle/skill-mirror-vendored.fs`: module `FsGg.Vendored.SkillMirror` transliterating `Fsgg.SkillMirror` 1.4.0 (providerSourceRoot, sha256, skillPath, skillIdOfPath, mirrorTargetRoots, retargetSkillPath, MirrorWrite, mirror, ExpectedSkill/ActualCopy/SkillDrift + local SkillScope, verify) plus vendored `agentSkillRoots = [".claude"; ".codex"; ".agents"]` (C2)
- [X] T006 Create `template/lifecycle/materialize-skill-roots.fsx`: `#load`s the vendored module; implements C3 (enumerate `.agents/skills/**` → retarget into `.claude`/`.codex` with skip-if-byte-identical writes; load `.agents/skills/skill-manifest.json`; expected = present∩manifest digest-checked ∪ present∖manifest empty-digest; verify; per-skill drift lines; `--enforce` exit semantics; `--product-root`; no-op when `.agents/skills/` absent)

## Phase 3: User Story 1 — standalone spec-kit product: three identical, self-contained roots (P1) 🎯 MVP

**Goal**: spec-kit scaffold + first build ⇒ three byte-identical union roots, zero dangling wrappers, one mechanism.

**Independent Test**: quickstart.md §3 (manual end-to-end) and §2 report lines for spec-kit profiles.

- [X] T007 [US1] Rework `.template.config/template.json` per the data-model.md source-row delta: narrow the blanket `.agents/skills/` → `.agents/skills/` row with `"include": ["speckit-*/**"]`; DELETE the two blanket `.claude/`/`.codex/` rows and all 24 per-skill `.claude`/`.codex` twin rows; add `copyOnly: ["**/*"]` to the 9 product-skill rows and the samples/feedback skill rows (dropping their non-`.agents` twins); ADD the ungated manifest row (`template/skill-manifest/` → `.agents/skills/`, copyOnly) and the spec-kit-gated materialize row (`template/lifecycle/` → `.specify/scripts/fs-gg/`, copyOnly); update affected `comment` fields (supersede Feature 230 wording)
- [X] T008 [US1] Add the `FsGgMaterializeSkillRoots` target to `template/base/Directory.Build.props`: `BeforeTargets="Build"`, existence-conditioned on the emitted fsx, `Inputs` = manifest + `.agents/skills/**` files, `Outputs` = `.specify/.fs-gg/skill-roots.stamp`, `Exec dotnet fsi <script>` + `Touch` stamp (R1); ensure the stamp path is ignored by the product's `.gitignore` if one is emitted
- [X] T009 [US1] Live-verify the mechanism by hand before gate rework: `dotnet new install .` a fresh scaffold (spec-kit, app), run `dotnet build` (or the fsx directly), and confirm three-root byte-identity, wrapper absence, manifest digest match, idempotent re-run; record transcript in `specs/231-skill-manifest-materialize/readiness/live-materialize.md`
- [X] T010 [US1] Rework `tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs` to the new emission table (fail-before/pass-after: assert include-narrowed speckit row, 9 copyOnly `.agents`-only product rows, zero `.claude`/`.codex` skill targets, manifest + materialize rows present and correctly gated)
- [X] T011 [US1] Rework `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`: drop Feature 230 twin/GV-floor expectations; keep sdd/none invariants; assert the materialize row is spec-kit-gated workspace; keep gated-condition audits coherent with the new rows
- [X] T012 [US1] Rework `scripts/validate-lifecycle-template.fsx`: verdict core re-derives the new classifier (include-narrowed speckit source, manifest/materialize rows, no twins); live loop — after each spec-kit scaffold, run `dotnet fsi <product>/.specify/scripts/fs-gg/materialize-skill-roots.fsx --enforce`, then assert `.agents ≡ .claude ≡ .codex` (full-directory byte compare incl. extra files like `fs-gg-symbology/reference.fsx`), manifest-digest match, and wrapper absence; report lines per quickstart.md §2; regenerate `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` with `provenance: live`
- [X] T013 [US1] Audit/rework `tests/Package.Tests/Feature217ProductNameTemplateTests.fs`, `Feature224SkillCatalogCurrencyTests.fs`, `Feature225ProductSkillVocabularyTests.fs` for assertions encoding the superseded shape (substituted skill bodies, twin rows, wrapper vendoring); re-derive to the new emission contract

## Phase 4: User Story 2 — sdd/none placement unchanged (P2)

**Goal**: orchestrated-lane regression protection.

**Independent Test**: live loop `sdd|none/<p>: claude-product-skills=0 codex-product-skills=0`; manifest present under `.agents/skills/`.

- [X] T014 [US2] Extend the T012 live loop's sdd/none assertions: product skills in `.agents/skills/` only, `skill-manifest.json` present there (new ungated row), materialize script + `.specify/` absent, zero `.claude`/`.codex` product skills, no dangling wrapper dirs; verdict-core equivalents in Feature204 tests

## Phase 5: User Story 3 — declared, content-addressed, machine-checkable set (P2)

**Goal**: the manifest is valid, fresh, and the verify step catches drift.

**Independent Test**: quickstart.md §1 Feature231 manifest tests; §3 `--enforce` run; corrupt-a-root red case.

- [X] T015 [P] [US3] Create `tests/Package.Tests/Feature231SkillManifestTests.fs` (manifest half): schema-shape validation of `template/skill-manifest/skill-manifest.json` (schemaVersion 1, sorted, product scope, resolvablePath form), digest freshness vs canonical bodies, catalog ↔ `template.json` product-skill-row coherence (every catalogued id has an emission row and vice versa); wire into `tests/Package.Tests/Package.Tests.fsproj` compile order
- [X] T016 [US3] Add drift-detection coverage: unit tests driving the vendored `verify` (missing root, divergent body, hash mismatch, clean) + a live-loop red-case (T012 loop corrupts one mirrored copy in a throwaway scaffold, asserts `--enforce` exits non-zero and names the skill)

## Phase 6: User Story 4 — one algorithm, parity-gated (P3)

**Goal**: vendored copy provably equals `FS.GG.Contracts` 1.4.0.

**Independent Test**: quickstart.md §4.

- [X] T017 [US4] Add the parity half of `Feature231SkillManifestTests.fs`: `<Compile Include="../../template/lifecycle/skill-mirror-vendored.fs">` (or link) + `PackageReference FS.GG.Contracts` in `tests/Package.Tests/Package.Tests.fsproj`; property/table tests asserting vendored ≡ library over sha256/skillPath/skillIdOfPath/mirrorTargetRoots/retargetSkillPath/mirror/verify (empty union, multi-root, `\\` paths, non-skill paths, missing/divergent/hash-mismatch cases) and vendored roots ≡ `Fsgg.Schemas.agentSkillRoots`; document the perturbation red-case (change vendored behavior locally ⇒ gate fails) in the test header

## Phase 7: User Story 5 — skill prose keeps the word "product" (P3)

**Goal**: F5 fixed for skill emission; intended renames intact.

**Independent Test**: quickstart.md §3 grep; live-loop assertion.

- [X] T018 [US5] Add live-loop + verdict-core assertions (T012 script + Feature231 tests): scaffold named with distinctive token ⇒ emitted `SKILL.md` bytes ≡ canonical bytes (verbatim emission, subsumes the word-"product" check); spot-assert one intentional rename site outside skills still substitutes (e.g. `Product.slnx`/`src/<Name>/` present)

## Phase 8: R2.4 guard + polish & release readiness

- [X] T019 Add the no-dangling-route guard (R7) to `Feature231SkillManifestTests.fs` (env-free core: extract path-like references from every emittable skill body incl. `speckit-*`, resolve against the template's declared emission set per profile×lifecycle; `<`/`*` placeholders skipped; `../` escapes always flagged) and to the T012 live loop (resolve against real scaffold trees); confirm `fs-gg-testing` → `docs/effects-boundary.md` resolves
- [X] T020 Documentation: update `template/base/docs/scaffold-map.md` (+ generated agent-context docs if they enumerate skill roots) to document the manifest, the materialize-on-first-build behavior, and the `sdd`/`none` confinement; note the deferred F5-outside-skills follow-up and the deferred `docs/skillist-reference.md` per-id scoping (bounded deferrals per Constitution)
- [X] T021 Bump `.template.package/FS.GG.UI.Template.fsproj` `0.1.60-preview.1` → `0.1.61-preview.1`; verify `FsGgUiVersion` untouched; run the full release-gate set (quickstart.md §1: `dotnet test tests/Package.Tests -c Release`, validator core) and the env-gated live loop; confirm all green
- [X] T022 Run `/speckit-analyze`-style self-check across spec/plan/tasks coherence; capture final feature readiness evidence under `specs/231-skill-manifest-materialize/readiness/` (gate transcripts, live report excerpt)

## Dependencies & execution order

- Phase 1 → Phase 2 → T007..T009 (US1 core) → gates T010..T013.
- US2 (T014) depends on T012's loop skeleton. US3 (T015 [P] parallel with T016 after T004/T006).
  US4 (T017) needs T001+T005. US5 (T018) needs T007. T019 needs T003+T007; T020..T022 last.
- Parallel opportunities: T002 ∥ T001; T005 ∥ T004 ∥ T003; T015 ∥ T017 ∥ T018 once their inputs exist.

## Implementation strategy

MVP = Phases 1–3 (US1): the mechanism + reworked primary gates. Then US2/US3 assertions
harden both lanes, US4/US5 close the parity/prose contracts, Phase 8 ships the guard, docs,
version bump, and release-readiness evidence.
