---
description: "Task list for Per-Feature Data-Table Refactor (Code-Health Phase 4)"
---

# Tasks: Per-Feature Data-Table Refactor

**Input**: Design documents from `/specs/181-feature-data-table-refactor/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: This is a behavior-preserving refactor. No new product behavior is added, so there are **no
fail-first TDD tasks**. The existing suites plus a **regenerate-and-diff byte oracle** (quickstart Step 2)
are the acceptance evidence. US3 is itself a test-side collapse; its tasks edit test files but the assertion
*coverage* must stay equivalent (FR-005, SC-004). A small catalog-shape assertion is added to
`Rendering.Harness.Tests` to lock C-FD-1/3/4.

**Organization**: Tasks grouped by user story (renderer → CLI → tests), each independently shippable and
gated on the same single byte-stability baseline.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete-task dependency)
- **[Story]**: US1, US2, US3 (Setup/Foundational/Polish carry no story label)
- Exact file paths are included in each task

## Catalog scope (used throughout)

The catalog is the **12 non-contiguous** features: **148, 149, 152, 153, 154, 155, 156, 157, 158, 159, 160,
161** (150/151 absent). Variant coverage is non-uniform (research R2). Features **146, 147, 150, 151** have
`Feature###CompatibilityLedgerTests.fs` files but are **NOT** in the catalog — they are out of scope for the
US3 collapse and MUST be left untouched.

---

## Phase 1: Setup & Baseline Capture (Shared Infrastructure)

**Purpose**: Stand up the byte oracle. Per plan.md, the standing "early live smoke run" clause is resolved
**N/A** (pure structural refactor of an internal tool with no defect/root-cause hypothesis); **baseline
capture is the substitute first task** and the gate for every later story.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test project via
> the discovery runner — the solution deliberately omits `tests/Package.Tests` (release-only public-surface
> gate) and `samples/**/*.Tests` (package-feed consumers). Use `scripts/baseline-tests.fsx`, which globs
> `*.Tests.fsproj` so nothing silently drops out.

- [X] T001 Create the byte-oracle tree: `specs/181-feature-data-table-refactor/readiness/baseline/` and `.../readiness/post-change/` (quickstart Step 0).
- [X] T002 Establish the no-regression test baseline: `dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/181-feature-data-table-refactor/readiness/baseline/tests.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; known reds `tests/Package.Tests` + `samples/ControlsGallery` stale-feed are recorded as baseline-not-regression, not discovered at merge).
- [X] T003 Capture the pre-edit readiness artifacts + command output: for each catalog feature (148 149 152 153 154 155 156 157 158 159 160 161) run every per-feature harness command (compositor-readiness / compositor-performance / compositor-damage / … `--feature NNN`) into a throwaway tree, then archive all generated `specs/###-*/readiness/**` plus per-command stdout/stderr/`echo $?` under `specs/181-feature-data-table-refactor/readiness/baseline/` (quickstart Step 0b). This is the byte oracle for US1/US2.
- [X] T004 [P] Record the exact per-feature command matrix actually invoked in T003 into `specs/181-feature-data-table-refactor/readiness/baseline/command-matrix.md` (the canonical list re-run for every post-change diff, so baseline and post-change are captured identically).

---

## Phase 2: Foundational (Blocking Prerequisites) — the descriptor catalog

**Purpose**: Author the single source of truth (`FeatureCatalog`) that all three stories read. **No user
story work may begin until this phase is complete** (data-model, contracts/feature-descriptor).

> **Live-smoke clause: N/A (resolved in plan.md).** This refactor carries no root-cause hypothesis to
> validate against a running app; the equivalent risk gate is the byte oracle captured in Phase 1. Do not
> add a live-app smoke task.

- [X] T005 Lock the byte-stability evidence contract and baseline-not-regression set: record the Phase-1 red/green set and the command matrix as the acceptance gate in `specs/181-feature-data-table-refactor/readiness/baseline/README.md` (cites SC-002/SC-004/SC-006; mirrors feature-180 evidence).
- [X] T006 Inventory the per-feature constant quintets and aliases at HEAD: grep `tools/Rendering.Harness/Compositor.fs` for `feature###ReadinessDirectory|…ParityDirectory|…TimingDirectory|…CompatibilityLedgerPath|…ValidationSummaryPath` and `tools/Rendering.Harness/Cli.fs` for `isFeature###`, recording each feature's exact byte-string constants + accepted aliases into `specs/181-feature-data-table-refactor/readiness/baseline/constant-inventory.md` (the C-FD-2 / C-FD-4 oracle for derived paths and alias sets).
- [X] T007 Author `tools/Rendering.Harness/FeatureCatalog.fsi`: declare `ReportVariant` DU (ValidationSummary, CompatibilityLedger, PackageValidation, RegressionValidation, UnsupportedHost, Timing, LiveProof, Parity, ProofSet, Reuse, Snapshot), `FeatureConfig`, `FeatureDescriptor`, the `FeatureDescriptor` path/`supports`/`tryByAlias` helpers, and `val catalog : FeatureDescriptor list` — per contracts/feature-descriptor.md (internal-tool surface only; FR-008).
- [X] T008 Implement `tools/Rendering.Harness/FeatureCatalog.fs`: the 12-entry catalog in `Compositor.fs` declaration order (C-FD-1), `Variants` sets per research R2, `CliAliases` ⊇ {`"NNN"`,`"featureNNN"`,Slug} (C-FD-4), and the derived path helpers via `Path.Combine` reproducing the T006 byte strings exactly (C-FD-2/C-FD-5). `ValidationSummary`+`CompatibilityLedger` in every descriptor (C-FD-3). **`RequiredHeaders`** MAY be authored empty/partial here and is filled per-feature by T024 (US3) — the US3 token inventory is the authoritative source (see U2 seam). **`FeatureConfig`** is populated per the data-model "≥2 features share *and* routing reduces net lines" rule (data-model.md:102); a scalar used by exactly one feature stays inline in that feature's explicit body, not in `FeatureConfig` (FR-007).
- [X] T009 Wire `FeatureCatalog.fsi`/`.fs` into `tools/Rendering.Harness/Rendering.Harness.fsproj` include order **before** `Compositor.fsi`/`.fs` so both `Compositor` and `Cli` can consume it (research R6); `dotnet build FS.GG.Rendering.slnx -c Release` green.
- [X] T010 Add a catalog-shape assertion to `tests/Rendering.Harness.Tests/` (new `FeatureCatalogTests.fs`, registered in the test fsproj include order): assert catalog count = 12 and exact id set, universal-variant presence (C-FD-3), and `tryByAlias "158"/"feature158"/"158-compositor-performance"` all resolve to feature 158 (C-FD-4). Run `tests/Rendering.Harness.Tests` green.

**Checkpoint**: `FeatureCatalog` builds, is consumable by `Compositor`/`Cli`, and its shape is locked by a test. User stories can now proceed.

---

## Phase 3: User Story 1 — Add a feature with a single data entry (Priority: P1) 🎯 MVP

**Goal**: Route the structurally-repeated report variants in `Compositor.fs` through a generic,
descriptor-driven renderer and derive the constant quintets from the catalog — zero observable output change.

**Independent Test**: Append a hypothetical 13th descriptor and confirm it renders all its standard variants
through the generic path with **zero** new `renderFeature…` functions (SC-001); byte-diff of regenerated
`readiness/**` vs baseline is empty (SC-002). Revert the probe.

### Implementation for User Story 1

- [X] T011 [US1] Replace the hand-declared per-feature directory/path constant quintets in `tools/Rendering.Harness/Compositor.fs` with calls to the `FeatureCatalog.FeatureDescriptor` helpers (`readinessDirectory`/`variantDirectory`/`compatibilityLedgerPath`/`validationSummaryPath`) — no value changes (C-GR-5, constant-drift edge case).
- [X] T012 [US1] Build the per-variant collapse/exclude map: for each variant family (ValidationSummary, CompatibilityLedger, PackageValidation, RegressionValidation, UnsupportedHost, Timing, LiveProof, Parity, ProofSet, Reuse, Snapshot) decide COLLAPSE vs RETAIN per C-GR-2, recording each decision + reason in `specs/181-feature-data-table-refactor/readiness/post-change/collapse-decisions.md` (FR-007). Feature-unique bodies (159 counter/promotion, 160 throughput, 161 lane-ledger, 158 proof-probe) are pre-marked RETAIN.
- [X] T013 [US1] Implement the generic renderer entry points in `tools/Rendering.Harness/Compositor.fsi` + `.fs` for each COLLAPSE family (e.g. `renderCompatibilityLedger`, `renderValidationSummary`), driven by `FeatureDescriptor` + payload; emit byte-identical output to the per-feature bodies they replace (C-GR-1).
- [X] T014 [US1] Route every COLLAPSE family's per-feature `renderFeatureNNN<Variant>` callsites through the generic path and delete the now-dead per-feature functions; leave RETAIN bodies explicit (C-GR-3/C-GR-4, FR-002/FR-007).
- [X] T015 [US1] `dotnet build FS.GG.Rendering.slnx -c Release` green, then regenerate `readiness/**` + command output into `specs/181-feature-data-table-refactor/readiness/post-change/` (T004 matrix) and `diff -r baseline post-change` — MUST be empty (C-GR-1/C-GR-2 gate, FR-003/SC-002). Any non-empty diff → revert that family to explicit (FR-007).
- [X] T016 [US1] Verify SC-001/SC-003 mechanically: temporarily append a 13th probe descriptor, confirm full standard-variant coverage with **zero** new `renderFeature…` functions, then revert. **Pin the SC-003 baseline metric (resolves I1):** record BOTH `grep -c 'let render' tools/Rendering.Harness/Compositor.fs` and `grep -c 'let renderFeature' tools/Rendering.Harness/Compositor.fs` at HEAD and after, in the collapse-decisions note, and state explicitly that **SC-003 is measured against `let renderFeature` count** (spec's "~114" counts the broader `let render…` family; the quickstart oracle and SC-003 target the `renderFeature` subset).
- [X] T017 [US1] Run `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` + the affected `Rendering.Harness.Tests`; confirm the same red/green set as baseline (no new reds, SC-006).

**Checkpoint**: US1 independently shippable — descriptor-driven renderer, byte-identical output, MVP delivered.

---

## Phase 4: User Story 2 — Drive the CLI from the same descriptor table (Priority: P2)

**Goal**: Replace the `isFeature###` + `if/elif` dispatch chains in `Cli.fs` with a descriptor-keyed command
table and extract the shared performance/readiness runner once — byte-identical stdout/stderr/exit codes.

**Independent Test**: Run every per-feature CLI command before/after and diff stdout/stderr/exit code; all
empty (SC-002). `isFeature###`/`if-elif` chains gone from `Cli.fs`.

### Implementation for User Story 2

- [X] T018 [US2] Implement `selectFeature : string list -> FeatureDescriptor option` in `tools/Rendering.Harness/Cli.fs` reading `--feature <alias>` via `FeatureCatalog.FeatureDescriptor.tryByAlias`, replacing the 12 `isFeature###` predicates (C-CT-3). (Note: `Cli.fs` has no `.fsi`; keep signatures inside `Cli.fs`, FR-008.)
- [X] T019 [US2] Replace the `if/elif` dispatch chains (e.g. `runCompositorReadinessCmd`, performance, damage) in `tools/Rendering.Harness/Cli.fs` with a `CommandKind`→descriptor `dispatch` table over the catalog; preserve the `runLegacyCompositorReadinessCmd` fall-through for any feature not in the catalog (C-CT-4).
- [X] T020 [US2] Extract the shared performance/readiness runner body (probe host → derive profile → classify reason → write artifacts → exit code) duplicated across the ~400-line per-feature handlers into one parameterized body driven by `FeatureDescriptor` + `FeatureConfig`; keep genuinely-divergent steps as small explicit hooks (C-CT-5/C-CT-6, FR-004/FR-007). Record any retained hooks in the collapse-decisions note. **Constitution II guard (C1):** `Cli.fs` has no `.fsi`; the extracted runner and its helpers MUST NOT introduce `private`/`internal`/`public` modifiers on top-level `.fs` bindings — visibility stays implicit (no new harness `.fsi` is added for `Cli`).
- [X] T021 [US2] `dotnet build FS.GG.Rendering.slnx -c Release` green, then re-run the full T004 command matrix into `readiness/post-change/` and `diff` every command's stdout/stderr/exit code + emitted `readiness/**` against baseline — MUST be empty (C-CT-1/C-CT-2, FR-004/SC-002).
- [X] T022 [US2] Add `Rendering.Harness.Tests` assertions for alias resolution and legacy fall-through (C-CT-3/C-CT-4) in `tests/Rendering.Harness.Tests/FeatureCatalogTests.fs`; confirm `isFeature` and `elif` dispatch grep-clean in `tools/Rendering.Harness/Cli.fs`.
- [X] T023 [US2] Run `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release`; same red/green set as baseline (SC-006).

**Checkpoint**: US1 + US2 both work independently — CLI dispatched from the catalog, byte-identical command behavior.

---

## Phase 5: User Story 3 — Collapse copy-forward test families (Priority: P3)

**Goal**: Replace the per-feature `Feature###Compatibility*Tests.fs` families (catalog features only) with one
data-driven `testList` over the catalog, preserving equivalent coverage.

**Independent Test**: Run the package/compatibility suites before/after; the same coverage (by feature) passes,
per-feature files replaced by one parameterized `testList` (SC-004). US3 is independent of US1/US2.

### Implementation for User Story 3

- [X] T024 [US3] **Decide the catalog-access path first (resolves U1, research R6 open choice):** determine how `tests/Package.Tests/` reaches the catalog data — **preferred:** add a `ProjectReference` from `tests/Package.Tests/Package.Tests.fsproj` to `tools/Rendering.Harness/Rendering.Harness.fsproj` and consume `FeatureCatalog.catalog` directly; **fallback (only if the exe reference dirties the release-only Package.Tests surface/feed graph):** factor the descriptor catalog into a tiny harness-adjacent module the tests reference, or mirror the minimal `RequiredHeaders` data in the test project. Record the chosen path + rationale in `specs/181-feature-data-table-refactor/readiness/post-change/collapse-decisions.md`, and confirm it does not perturb the `Package.Tests` public-surface gate. Then inventory the in-scope test files and their asserted tokens: the catalog-feature `Feature###CompatibilityLedgerTests.fs` (148,149,152,153,154) + `Feature###CompatibilityTests.fs` (155,156,157,158,159,160,161) in `tests/Package.Tests/`; record each file's `requiredHeaders`/token expectations into the descriptor `RequiredHeaders` data (extend `tools/Rendering.Harness/FeatureCatalog.fs` — this is the authoritative fill for the T008 partial `RequiredHeaders`, U2 seam). Explicitly EXCLUDE non-catalog 146/147/150/151 ledger files (leave untouched).
- [X] T025 [US3] Author `tests/Package.Tests/CompatibilityLedgerTests.fs`: one data-driven `testList` parameterized over the catalog, asserting each feature's `RequiredHeaders`/compatibility-ledger tokens — equivalent coverage to the per-feature files (FR-005/SC-004), reusing `FS.GG.TestSupport.RepositoryRoot` for repo-relative paths (research R4).
- [X] T026 [US3] Update `tests/Package.Tests/Package.Tests.fsproj` include order: add `CompatibilityLedgerTests.fs`, remove the 12 in-scope per-feature `Feature###Compatibility*Tests.fs` entries; delete those 12 files (keep 146/147/150/151 + non-compatibility files).
- [X] T027 [US3] Run `dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/181-feature-data-table-refactor/readiness/post-change/tests.md` and diff the pass/fail set vs `baseline/tests.md`: every previously-covered catalog feature still asserted, same red/green set (SC-004/SC-006). Confirm `ls tests/Package.Tests/Feature{148,149,152,153,154}CompatibilityLedgerTests.fs tests/Package.Tests/Feature{155..161}CompatibilityTests.fs` returns none.

**Checkpoint**: All three stories independently functional; per-feature compatibility test files (catalog) = 0.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification of every success criterion and the gated SC-005 net-line measurement.

- [X] T028 Full `dotnet build FS.GG.Rendering.slnx -c Release` + `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` + `dotnet fsi scripts/baseline-tests.fsx --config Release` — capture final `post-change/` and confirm red/green identical to baseline (SC-006).
- [X] T029 Final byte-stability gate: `diff -r specs/181-feature-data-table-refactor/readiness/baseline specs/181-feature-data-table-refactor/readiness/post-change` is empty (SC-002/SC-004, quickstart Step 2).
- [X] T030 SC-005 net-line measurement (gated, do not skip): `git diff --stat main -- tools/Rendering.Harness/ tests/Package.Tests/`; record per-family net-line delta and confirm no family was collapsed at a net line cost. Any line-increasing family is reverted to explicit and recorded in the collapse-decisions note (FR-007).
- [X] T031 [P] Verify FR-008: `FS.GG.UI.*` public-surface `.fsi` baselines unchanged (no descriptor symbol leaked into any shipped `.fsi`); record in the Implementation Outcome.
- [X] T032 Write the Implementation Outcome section into `specs/181-feature-data-table-refactor/plan.md`: per-family COLLAPSE/RETAIN decisions + rationale (FR-007), `renderFeature…` count before/after (SC-003), per-feature test-file count → 0 (SC-004), net-line delta (SC-005), and the SC-001…SC-006 checklist from quickstart "Done when".
- [X] T033 [P] Capture per-phase feedback into `specs/181-feature-data-table-refactor/feedback/` via the `fs-gg-feedback-capture` skill (process friction, generalizable-code candidates, severity).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001 → T002/T003 → T004.
- **Foundational (Phase 2)**: Depends on Setup (the baseline + constant inventory feed catalog correctness). **BLOCKS all user stories.** T005/T006 → T007 → T008 → T009 → T010.
- **User Stories (Phase 3–5)**: All depend on Foundational (the catalog). US1 and US3 are mutually independent; US2 reads the catalog (Foundational) and is conceptually downstream of US1 but does not depend on US1's renderer edits — it can proceed independently once T009 lands.
- **Polish (Phase 6)**: Depends on all desired user stories complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on US2/US3. **MVP.**
- **US2 (P2)**: After Foundational. Independent of US1/US3 (shares the same byte oracle).
- **US3 (P3)**: After Foundational. Independent of US1/US2; may land in any order.

### Within Each Story

- Constants-from-catalog before renderer collapse (US1: T011 before T013/T014).
- Collapse/exclude decision before collapsing (US1: T012 before T014).
- Build green before byte-diff before test sweep (each story ends on diff + sweep).

### Parallel Opportunities

- T003 and T004 are sequential (T004 records what T003 ran); T031/T033 in Polish are [P].
- Once Foundational (T010) completes, **US1, US2, and US3 can be worked in parallel** by different developers — they touch different files (`Compositor.fs` / `Cli.fs` / `tests/Package.Tests/`) and share only the read-only baseline oracle.
- Within US3, T024 (data inventory) precedes T025/T026.

---

## Parallel Example: post-Foundational fan-out

```bash
# After T010 (catalog locked), three independent tracks:
Track US1: "Route Compositor.fs report variants through the generic renderer" (T011–T017)
Track US2: "Replace Cli.fs isFeature###/if-elif with descriptor-keyed table"  (T018–T023)
Track US3: "Collapse Package.Tests Feature###Compatibility*Tests.fs into one testList" (T024–T027)
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 Setup + baseline capture (byte oracle).
2. Phase 2 Foundational — `FeatureCatalog` (CRITICAL, blocks all stories).
3. Phase 3 US1 — descriptor + generic renderer.
4. **STOP and VALIDATE**: byte-diff empty + test set unchanged → ship MVP (SC-001/002/003).

### Incremental Delivery

1. Setup + Foundational → catalog ready.
2. US1 → byte-diff clean → ship (MVP).
3. US2 → command stdout/stderr/exit diff clean → ship.
4. US3 → equivalent test coverage → ship.
5. Polish → SC-005 measurement + Implementation Outcome.

---

## Notes

- **Byte-stability wins over line reduction** when they conflict (FR-003/SC-002). A non-empty diff means revert the offending family to explicit (FR-007).
- **SC-005 is gated, not assumed** (Phase-3/180 lesson): collapse a family only if byte-identical AND net-line-reducing; otherwise retain explicit and record why.
- `Cli.fs` has no `.fsi`; keep all CLI signature changes inside `Cli.fs`. New descriptor surface lives in `FeatureCatalog.fsi` only — no shipped `FS.GG.UI.*` `.fsi` change (FR-008).
- Non-catalog test files (146/147/150/151 ledgers) are out of scope — do not delete or alter.
- Commit after each task or logical group; each story ends green on build + test + byte-diff.
