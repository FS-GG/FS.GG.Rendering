---
description: "Task list for Ant-derived design-token taxonomy (Workstream F, Phase F1)"
---

# Tasks: Ant-derived design-token taxonomy (Workstream F, Phase F1)

**Input**: Design documents from `/specs/126-ant-token-taxonomy/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: Test tasks ARE generated — the contracts define a drift/idempotency test, a layer-coverage
test, and neutrality checks, and Principle V requires test evidence. No *existing* test is removed,
weakened, or skipped; the existing suite + gallery render-identity are the behaviour-neutrality oracle.

**Change classification**: **Tier 2 (internal/additive)**. The new taxonomy lands in
`module internal DesignTokensExt` (no `.fsi`) → zero public-surface-baseline delta, unchanged public
`Theme` record, byte-identical render. The existing public `DesignTokens` module is untouched.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). New tokens go in the existing
`FS.GG.UI.DesignSystem` project (generated internal module). The DTCG source lives in
`FS.GG.UI.Themes.Default`. The generator is a `scripts/*.fsx` run via `dotnet fsi` (not compiled into
any assembly).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up the generator skeleton, the internal-module compile slot + internals access, and
the test file slot, so the carve has somewhere to land.

- [X] T001 [P] Create `scripts/generate-design-tokens.fsx` skeleton: a `dotnet fsi` script with a `--check` flag and a default write mode; reads `src/Themes.Default/design-tokens.tokens.json` via `System.Text.Json` (BCL, script-time only — not compiled into any assembly, R3); deterministic emit of `src/DesignSystem/DesignTokensExt.fs`. Generation logic filled in T011 (skeleton can emit an empty marked module for now).
- [X] T002 Edit `src/DesignSystem/DesignSystem.fsproj`: add `<Compile Include="DesignTokensExt.fs" />` immediately after the `DesignTokens.fs` entry, and add `<InternalsVisibleTo Include="Controls.Tests" />`. Create a minimal placeholder `src/DesignSystem/DesignTokensExt.fs` (`module internal DesignTokensExt` with the generated header + `open FS.GG.UI.Scene`) so the project compiles before T012 generates it.
- [X] T003 Register the new test file in `tests/Controls.Tests/Controls.Tests.fsproj`: add `<Compile Include="Feature126TokenTaxonomyTests.fs" />` and create a placeholder `tests/Controls.Tests/Feature126TokenTaxonomyTests.fs` (`module Feature126TokenTaxonomyTests`, empty `testList`).

**Checkpoint**: Solution still builds (placeholder internal module + empty test compile); generator
script exists.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-change "green oracle" every neutrality claim (US2) is measured against.
**No verification claim is meaningful without it.**

**⚠️ CRITICAL**: This is the reference point for FR-004/FR-008/SC-002/SC-003.

- [X] T004 Establish baseline confidence: from a clean tree run `dotnet build FS.GG.Rendering.slnx -c Release` (0 errors/warnings) and `dotnet test FS.GG.Rendering.slnx -c Release`; record the passing/skipped counts per suite as the pre-change oracle (expected: Controls.Tests 472P/1S, Elmish.Tests 153P/17S, plus the smaller suites — confirm at run time).
- [X] T005 Snapshot `tests/surface-baselines/` (copy or md5 the directory) as the zero-delta reference for SC-002/N-1.

**Checkpoint**: Pre-change build/test/surface state captured. Token work can begin.

---

## Phase 3: User Story 1 - The full layered vocabulary, generated (Priority: P1) 🎯 MVP

**Goal**: The DTCG source and a generated `module internal DesignTokensExt` express the Ant-derived
taxonomy (seed → map → alias → component) plus spacing/density/type-scale/elevation, nameable by
in-repo code and tests.

**Independent Test**: Name a token from every group — a `Seed`, a `Map.Light` and a `Map.Dark`, an
`Alias.Light`/`Alias.Dark`, a `Component.Button`, a `Space`, a `Density`, a `Type`, and an
`Elevation` — via `InternalsVisibleTo`, and each resolves to its expected value; `Map`/`Alias` mode
parity holds (quickstart V4).

### Implementation for User Story 1

- [X] T006 [US1] Extend `src/Themes.Default/design-tokens.tokens.json` with the **`seed`** group (data-model Group 1 / `contracts/token-taxonomy-contract.md` schema), keeping the existing `light`/`dark` primitive blocks **byte-identical**.
- [X] T007 [P] [US1] Add the **`map.light`** and **`map.dark`** groups to the DTCG source (data-model Group 2). Every key MUST exist in both modes (mode parity, V2).
- [X] T008 [P] [US1] Add the **`alias.light`** and **`alias.dark`** groups (data-model Group 3; dotted keys e.g. `text.default`; both modes).
- [X] T009 [P] [US1] Add the **`component.<family>`** groups for `button`/`input`/`table`/`tabs`/`menu` (data-model Group 4).
- [X] T010 [P] [US1] Add the supplementary groups **`space`** (4/8/16/24/32), **`density`** (Comfortable=1.0/Middle/Compact), **`type`** (display…small + lineHeight), **`elevation`** (none/low/medium/high) (data-model Group 5; FR-002).
- [X] T011 [US1] Implement the generator in `scripts/generate-design-tokens.fsx`: parse the DTCG groups; map dotted keys → camelCase F# identifiers (`text.default` → `textDefault`); emit `module internal DesignTokensExt` with sub-modules (`Seed`, `Map.Light`/`Map.Dark`, `Alias.Light`/`Alias.Dark`, `Component.<Family>`, `Space`, `Density`, `Type`, `Elevation`); colors → `Colors.rgba`, dimension/number → `float`, shadow → `string`; deterministic ordering; `// GENERATED — do not edit` header + source reference. MUST fail loudly on a malformed leaf, a mode-parity gap, or an unknown `$type`.
- [X] T012 [US1] Run `dotnet fsi scripts/generate-design-tokens.fsx` to produce `src/DesignSystem/DesignTokensExt.fs`; build `src/DesignSystem/DesignSystem.fsproj -c Release` green (the generated internal module compiles against Scene only).
- [X] T013 [US1] Author the layer-coverage tests in `tests/Controls.Tests/Feature126TokenTaxonomyTests.fs` (contract C-1/C-2): name one token from every group and assert its expected value; assert `Map`/`Alias` mode parity for a sampled key. Reaches the internal module via the T002 `InternalsVisibleTo`.
- [X] T014 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "126"`; layer-coverage tests green (SC-001/SC-005/FR-006/FR-007).

**Checkpoint**: The taxonomy exists, is generated, and is nameable from every layer. MVP delivered.

---

## Phase 4: User Story 2 - Nothing observable changes (Priority: P1)

**Goal**: Prove the enrichment is invisible — zero public-surface delta, existing values byte-identical,
public `Theme` unchanged, rendered output identical, suite counts identical.

**Independent Test**: Regenerate surface baselines → zero diff; `DesignTokenParityTests` green; gallery
render-identity green; full suite pass/skip counts match the T004 oracle (quickstart V2/V3/V6).

### Implementation for User Story 2

- [X] T015 [US2] Verify zero public-surface delta (N-1/FR-005): `dotnet build FS.GG.Rendering.slnx -c Debug` then `dotnet fsi scripts/refresh-surface-baselines.fsx`; assert `git status --porcelain tests/surface-baselines/` is empty (no baseline changed vs the T005 snapshot — the internal module adds no public surface).
- [X] T016 [US2] Verify existing values + `Theme` shape unchanged (N-2/FR-004/FR-011): run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "design-token"`; `DesignTokenParityTests` green (existing Light/Dark primitives byte-identical; full `Theme` literals still compile unchanged).
- [X] T017 [US2] Render identity (N-3/FR-008): run `dotnet test samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj -c Release`; `ThemeInvarianceTests`/`PageRenderTests` green. No repack is needed — the public package surface is unchanged, so the gallery (a package consumer) sees nothing different.
- [X] T018 [US2] Suite parity (N-4/SC-003): run `dotnet test FS.GG.Rendering.slnx -c Release` and confirm the pass/skip counts match the T004 oracle exactly — zero tests deleted, weakened, or newly skipped.

**Checkpoint**: Behaviour- and contract-neutrality proven; the enrichment is invisible.

---

## Phase 5: User Story 3 - Generated and drift-checked (Priority: P2)

**Goal**: The taxonomy stays generated from the source with a real drift gate; hand-edits are caught.

**Independent Test**: Regenerate → no diff (gate green); tamper the source → `--check` fails until
regenerated; the artifact is marked generated (quickstart V1/V5).

### Implementation for User Story 3

- [X] T019 [US3] Add the drift/idempotency test to `tests/Controls.Tests/Feature126TokenTaxonomyTests.fs` (contract D-1/D-2): invoke the generator as a subprocess (`dotnet fsi scripts/generate-design-tokens.fsx --check`) and assert exit 0 (committed `DesignTokensExt.fs` is byte-identical to freshly generated output); assert the file carries the generated header + source reference. The test does **not** reference a JSON parser (D-3).
- [X] T020 [US3] Verify the drift gate catches staleness (quickstart V5): edit a value in the DTCG source, run `--check` (expect non-zero), regenerate (expect restored), then `git checkout --` both files. Record as evidence (manual/scripted).
- [X] T021 [US3] Confirm no JSON-parser dependency leaked (D-3/R3): assert `src/DesignSystem/DesignSystem.fsproj` and `tests/Controls.Tests/Controls.Tests.fsproj` reference no `System.Text.Json`/`Newtonsoft.Json`/`FSharp.Data` package, and the `DesignTokenParityTests` forbidden-package guard on `Controls.fsproj` stays green.

**Checkpoint**: Generation + drift discipline in place; the larger token set is guarded.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final accuracy and clean-tree checks.

- [X] T022 [P] Update the DTCG source `$description` and add a one-line pointer to `scripts/generate-design-tokens.fsx` (the new generator) so the "generated from this source" claim is accurate; ensure the generated-file header names the real generator (not the non-existent `fake.sh`).
- [X] T023 Run the quickstart V1–V7 end-to-end as the acceptance walkthrough and tick each success criterion (SC-001…SC-006).
- [X] T024 Final clean-tree verification: `dotnet build FS.GG.Rendering.slnx -c Release` (0 new warnings) and `dotnet test FS.GG.Rendering.slnx -c Release` both green, pass/skip counts matching the T004 oracle.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup compiling; records the green oracle — BLOCKS US2/US3 verification.
- **User Story 1 (Phase 3)**: Depends on Foundational. The MVP — builds the source + generator + internal module.
- **User Story 2 (Phase 4)**: Depends on US1 (tokens must exist to prove they changed nothing).
- **User Story 3 (Phase 5)**: Depends on US1 (the generator + committed artifact must exist to drift-check).
- **Polish (Phase 6)**: Depends on US1–US3.

### Story Independence Notes

US1 is the substance; US2 and US3 are verification stories over US1's output. Each is independently
verifiable at its checkpoint (US1: name every layer; US2: zero delta + render identity; US3: drift
gate catches staleness).

### Within User Story 1

- The DTCG-source group additions (T006 seed; T007 map; T008 alias; T009 component; T010 supplementary)
  edit the **same** JSON file — T006 first (it anchors the file's new section), then **T007–T010 are
  logically [P]** (independent groups) but, because they touch one file, apply sequentially.
- Generator (T011) after the source groups; generate (T012) after the generator; tests (T013/T014) last.

### Parallel Opportunities

- Setup: T001 (generator skeleton) is independent of T002/T003 (fsproj slots).
- US1: T007–T010 are independent token groups (same file — sequence the writes).
- US3: T019 (drift test) and T021 (dependency check) are independent.
- Polish: T022 is independent of T023/T024.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational (record the green oracle).
2. Phase 3 US1: extend the DTCG source, build the generator, generate the internal module, prove every
   layer is nameable.
3. **STOP and VALIDATE**: T014 green = the vocabulary exists and is generated. This is the deliverable
   that unblocks F2/F4 and D2.

### Incremental Delivery

1. Setup + Foundational → scaffolding + oracle.
2. US1 → generated internal taxonomy (MVP).
3. US2 → neutrality proven (zero surface delta, render identity, suite parity).
4. US3 → drift gate real (regenerate-then-compare; staleness caught).
5. Polish → accuracy + clean-tree verification.

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- The single highest-value guard is **internal placement** (no `.fsi`) — it is what keeps the change
  Tier 2 and the surface gate green; never add a public `.fsi` for these tokens in F1 (that is F5).
- The single most likely reddener is an accidental public-surface or value change; T015/T016 exist to
  catch it, and T018 pins suite parity.
- No new product/package dependency, no new test framework, no consumer wired — purely a generated,
  internal, additive vocabulary nothing reads yet.
