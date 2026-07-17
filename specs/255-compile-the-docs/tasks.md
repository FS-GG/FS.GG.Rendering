# Tasks: Compile the docs instead of parsing them

**Input**: Design documents from `specs/255-compile-the-docs/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md (all present)

**Tests**: Included — this feature *is* test/CI tooling, and the constitution (V) makes test evidence
mandatory. The harness's own assertions are the deliverable, so test tasks are first-class here.

**Organization**: By user story (P1 → P2 → P3). **Note on independence**: unlike a typical feature, the
stories here are *sequenced*, not independent — US2 and US3 delete machinery that US1 must first be proven
to replace (research.md D5). US1 is the MVP and the enabler; US2/US3 must not start until US1 is green in CI
and SC-006 (no coverage loss vs. the historical cases) is demonstrated.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (setup, foundational, polish carry no story label)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up the harness project and the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING).** T002 runs EVERY test project (solution + Package.Tests +
> samples) via the discovery runner so pre-existing reds are known now, not discovered at merge — critical
> here because this feature *deletes tests*, and a pre-existing red must not be mistaken for a deletion
> regression.

- [ ] T001 Create the harness project `tests/DocFences.Tests/DocFences.Tests.fsproj` (xUnit, `net10.0`, references `tests/TestSupport`) and add it to `FS.GG.Rendering.slnx` so the gate's slnx loop picks it up
- [ ] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/255-compile-the-docs/readiness/baseline.md` (runs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set)
- [ ] T003 [P] Confirm the pinned packages resolve: read the live `$(FsGgUiVersion)` from `Directory.Build.local.props` and confirm `FS.GG.UI.*` restore from the local nupkg feed; record the release-pending-waiver behavior the harness must honor (research.md D4, [[fsgg-release-window-pin-probes]])

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The extraction + preamble + opt-out design and the early end-to-end proof that the compiler
actually replaces the extractors — BEFORE any machinery is deleted.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Early live proof (STANDING — the plan's "live smoke run").** For this feature the "real running app"
> is the harness itself against real docs. T005 stands up the minimal end-to-end path on a handful of REAL
> fences and proves **green on a known-good fence and red on a deliberately-unreleased symbol**, mapped to
> the right doc+line, before anyone trusts the assumption that P1 subsumes the extractors. Treat "the
> compiler catches what the regex caught" as an UNVERIFIED assumption until this passes.

- [ ] T004 Build the corpus/fence map: enumerate the three corpora (`template/product-skills/**/*.md`, `template/base/docs/api-surface/**/*.fsi` `///`, scaffold sources under `template/base/src` + `template/fragments` `///`) and extract F# fences via `FS.GG.TestSupport.MarkdownFences` only; assert no corpus silently yields zero fences (FR-001)
- [ ] T005 **Early live proof**: minimal end-to-end run in `tests/DocFences.Tests/DocFencesCompileTests.fs` — generate a pinned project from ~5 real fences, one restore + `dotnet build`, assert GREEN on a known-good fence and RED (with doc+line) on an injected unreleased symbol. Record evidence under `specs/255-compile-the-docs/readiness/` before proceeding
- [ ] T006 [P] Define the per-corpus preamble format + in-fence `open`-directive (research.md D2) as reviewed config in the repo; document the default `open` set per corpus
- [ ] T007 [P] Define the per-fence `SkipWithReason` opt-out directive (research.md D3) that replaces the ledger — local, greppable, reason-carrying (FR-005)
- [ ] T008 Draft the harness helper seams first (`.fsi` for any `TestSupport` helper): fence → `CompilationUnit` assembly, the generated `FenceProject`, and the `SymbolManifest` emitter (data-model.md); per constitution I (sketch signatures before bodies)

**Checkpoint**: Corpus map built, green+red proven end to end on real fences, preamble/opt-out formats
fixed, seams drafted — US1 implementation can proceed.

---

## Phase 3: User Story 1 - The harness holds the line (Priority: P1) 🎯 MVP

**Goal**: Every F# fence in every shipped doc compiles against the pinned packages on every PR; an
unreleased symbol fails the build with a doc+line diagnostic.

**Independent Test**: quickstart.md steps 1–3 and 8 — green over the whole corpus; red on an injected
unreleased symbol; a legitimate partial snippet still compiles; the historical cases (#550/#591/#592/#598/
#619) reconstructed as fences all go red.

### Tests for User Story 1 ⚠️ (write first, ensure they fail before implementation)

- [ ] T010 [P] [US1] Green-path test: the full corpus assembles and builds against the pin, in `tests/DocFences.Tests/DocFencesCompileTests.fs`
- [ ] T011 [P] [US1] Red-path test: an injected unreleased symbol fails the build and the failure names the doc+line (SC-002), in `DocFencesCompileTests.fs`
- [ ] T012 [P] [US1] Partial-snippet test: a fence relying only on the corpus preamble compiles without being made self-contained (FR-004, guards #664 fail-closed), in `DocFencesCompileTests.fs`
- [ ] T013 [P] [US1] Skip-opt-out test: a `SkipWithReason` fence is excluded, its reason reported, and NO ledger line is added (FR-005), in `DocFencesCompileTests.fs`
- [ ] T014 [P] [US1] No-regression test: each historical case #550/#591/#592/#598/#619 reconstructed as a fence goes red where its retired extractor did (SC-006), in `DocFencesCompileTests.fs`

### Implementation for User Story 1

- [ ] T015 [US1] Implement fence → `CompilationUnit` assembly (corpus preamble + fence `ExtraOpens`, unique module name encoding doc+line) in `tests/DocFences.Tests/` (data-model.md CompilationUnit)
- [ ] T016 [US1] Implement `FenceProject` generation + single pinned restore + `dotnet build` (reuse the `runProbeBuild` project-generation approach; ONE restore amortized over all fences) in `tests/DocFences.Tests/`
- [ ] T017 [US1] Map compiler diagnostics back through `Origin` to `{Doc, Line, Diagnostic}` so a failure is clickable (FR-003, constitution VI) in `tests/DocFences.Tests/`
- [ ] T018 [P] [US1] Classify non-F# fences: excluded from the compile set but counted for coverage accounting, never silently dropped (edge case) in `tests/DocFences.Tests/`
- [ ] T019 [US1] Emit the per-fence `SymbolManifest` (resolved pinned symbols) — built now, consumed by US3 (data-model.md SymbolManifest, D6) in `tests/DocFences.Tests/`
- [ ] T020 [US1] Confirm CI: `DocFences.Tests` runs via the gate's slnx loop on a PR; add a named step in `.github/workflows/gate.yml` ONLY if a distinct restore/setup is required (plan Structure Decision)

**Checkpoint**: US1 green in CI, all US1 tests pass, SC-006 demonstrated. The line is now held by the
compiler. **Only now may US2/US3 begin.** Leave the old extractors running for this transition (D5).

---

## Phase 4: User Story 2 - One fence engine, one `.fsi` reader, one symbol oracle (Priority: P2)

**Goal**: Delete the machinery the compiler subsumes; leave exactly one fence engine, one `.fsi` reader, one
symbol oracle. **Depends on US1 (Phase 3) green in CI.**

**Independent Test**: quickstart.md step 5 — the retired symbols are gone by `grep`; the remaining suites
are green; `MarkdownFences` / `SurfaceSignature` / the PE-metadata walk are the sole survivors.

- [ ] T021 [P] [US2] Fold the third fence reader in `scripts/check-symbology-skill-parity.fsx` onto `MarkdownFences` (FR-007)
- [ ] T022 [P] [US2] Fold the five duplicate `val` regexes onto `SurfaceSignature` — callers at `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs:144`, `tests/Package.Tests/SurfaceDocCoverageTests.fs:81`, `tests/**/ApiSurfaceMirrorTests.fs:238`, `tests/**/Issue496FSharpCoreShadowingTests.fs:132` (FR-007)
- [ ] T023 [US2] Delete the four fence-side extractors (`skillFenceSymbols`, `mirrorValSymbols`, `mirrorDocCommentSymbols`, `scaffoldSourceDocCommentSymbols`) from `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs` — gated on SC-006 still green (FR-006)
- [ ] T024 [US2] Delete the compile-probe oracle (`runProbeBuild` / `runNameofProbe`) and the `oracleVersion = "0.9.0"` hardcode; the harness reads the live pin (FR-006, FR-009) in `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs`
- [ ] T025 [US2] Expose the retained PE/metadata oracle (`readSurfaceAt`) behind ONE API, serving only the prose residue (`skillProseSymbols`), and confirm it is the only symbol oracle left (FR-008) in `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs`
- [ ] T026 [US2] Re-run the full baseline (`scripts/baseline-tests.fsx`) and diff against T002 — assert no net coverage loss (SC-006), in `specs/255-compile-the-docs/readiness/`

**Checkpoint**: Exactly one fence engine, one `.fsi` reader, one symbol oracle (SC-003); all suites green.

---

## Phase 5: User Story 3 - Empty ledger, homonym-proof S-DOC (Priority: P3)

**Goal**: The ledger holds no suppressions; S-DOC "cited" means "appears in a fence that compiled against
the pin", dissolving the same-language-homonym class. **Depends on US1 (Phase 3); the `SymbolManifest`
(T019) is its input.**

**Independent Test**: quickstart.md steps 6–7 — the ledger is empty; a local `let describe` that never
cites `Scene.describe` does not credit it.

- [ ] T027 [US3] Rebase S-DOC coverage: redefine "cited" as membership in a fence's `SymbolManifest` (D6) in `tests/Package.Tests/SurfaceDocCoverageTests.fs` (FR-011)
- [ ] T028 [P] [US3] Add the homonym regression test: a fence defining `let describe` locally, never citing `Scene.describe`, must not credit it (SC-005) in `tests/Package.Tests/SurfaceDocCoverageTests.fs`
- [ ] T029 [US3] Empty `tests/Build.Tests/pinned-api-doc-ledger.txt`: the one remaining line (`Physics.circleCircleManifold`) must either compile or be fixed at root — NOT re-suppressed (FR-010)
- [ ] T030 [US3] Confirm zero suppression lines (`grep -cvE '^\s*(#|$)' tests/Build.Tests/pinned-api-doc-ledger.txt` → 0) and the gate stays green (SC-004)

**Checkpoint**: Ledger empty; homonym class gone; the compiler, not a ledger, holds the line.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T031 [P] Run the full quickstart.md validation (steps 1–8) and record evidence under `specs/255-compile-the-docs/readiness/`
- [ ] T032 [P] Document the harness in `tests/DocFences.Tests/` (how a doc author adds a preamble open / a `SkipWithReason`, where a failure points) and update any skill/README that referenced the retired ledger
- [ ] T033 Final baseline diff vs. T002 across every test project; confirm the ~4,200-line reduction landed with no red introduced
- [ ] T034 Update epic #695: cross-link this feature; confirm the "Done when" (one engine / one reader / one oracle, empty ledger, fences compile on every PR) is met before closing

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup; BLOCKS all stories. The early live proof (T005) is the gate.
- **US1 (Phase 3)**: depends on Foundational. The MVP and enabler.
- **US2 (Phase 4)** and **US3 (Phase 5)**: depend on **US1 green in CI** (research.md D5) — NOT independent
  of US1. US2 and US3 are independent of *each other* and can run in parallel once US1 holds.
- **Polish (Phase 6)**: depends on US2 + US3.

### Sequencing rationale (why this differs from the template's independent-stories default)

The epic's whole complaint is heuristics replaced by heuristics with new holes. Deleting the old gate before
the new one is proven risks a coverage gap in a defect class that has shipped five times. So US1 must hold
the line (SC-006) before US2/US3 remove anything. Belt-and-suspenders for one transition is deliberate.

### Parallel Opportunities

- T003 alongside T001/T002 in Setup.
- T006/T007 in parallel in Foundational; T008 alongside once formats are fixed.
- US1 tests T010–T014 in parallel (all in the one test file but independent cases); T018 alongside T015–T017.
- Once US1 holds: **US2 and US3 run in parallel** (different files — `TemplateConsumesPinnedApiTests.fs` /
  `scripts/*.fsx` for US2; `SurfaceDocCoverageTests.fs` / the ledger for US3).

---

## Implementation Strategy

### MVP (US1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational incl. the **early live proof** (T005) → 3. Phase 3 US1 →
4. **STOP and VALIDATE**: US1 green in CI, SC-006 demonstrated. This alone delivers value — the compiler
holds the doc-vs-pin line — even with the old extractors still present.

### Incremental delivery

US1 (hold the line, old gate still there) → US2 (delete the duplication, gated on SC-006) → US3 (empty the
ledger, dissolve the homonym class). Each ships independently once US1 is proven.

---

## Notes

- [P] = different files / independent, no dependency on an incomplete task.
- Every deletion in US2/US3 is gated on the baseline diff (T026, T033) showing no coverage loss.
- Commit after each task or logical group; keep each PR reviewable (a deletion PR should show the harness
  test that now covers what was deleted).
- This maps to the T1–T6 breakdown discussed on #695: T1→US1, T2/T3→US2, T4→US2, T5/T6→US3.
