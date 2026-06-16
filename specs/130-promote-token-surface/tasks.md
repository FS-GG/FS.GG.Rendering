---
description: "Task list for Feature 130 — Promote Token, Policy & Resolver Surface to Public (Workstream F5)"
---

# Tasks: Promote Token, Policy & Resolver Surface to Public — Workstream F, Phase F5

**Input**: Design documents from `/specs/130-promote-token-surface/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: INCLUDED. Per Constitution Principle V ("test evidence is mandatory") and FR-008/SC-001, public-path
test evidence is a required deliverable of this feature (`tests/Controls.Tests/Feature130PublicSurfaceTests.fs`).
Per Principle I the public-path tests are authored **fail-before** (symbols not yet public) / **pass-after**.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) to enable independent implementation
and verification of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each description

## Path Conventions

Single F# solution `FS.GG.Rendering.slnx` at repository root: package sources under `src/`, tests under `tests/`,
generator scripts under `scripts/`, committed gate baselines under `tests/surface-baselines/`, docs under `docs/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish a known-good pre-promotion baseline so all parity/neutrality claims are provable by diff.

- [X] T001 Confirm working tree is on branch `130-promote-token-surface` and `dotnet build -c Release FS.GG.Rendering.slnx` is clean (0 warnings / 0 errors) — record this as the pre-F5 build baseline (quickstart V1 reference).
- [X] T002 [P] Capture the pre-F5 full-suite pass/skip counts by running `dotnet test FS.GG.Rendering.slnx` and recording the totals (the additive-only invariant INV-7/SC-006 is checked against these numbers later).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Snapshot the exact pre-promotion artifacts whose byte-identity later tasks must prove. These MUST be captured **before** any `internal`-removal or regeneration edit, or the parity evidence is lost.

**⚠️ CRITICAL**: No user-story promotion edits may begin until these snapshots exist.

- [X] T003 Snapshot the current `src/DesignSystem/DesignTokensExt.fs` token *values* (e.g. `git stash`-clean copy or recorded `git show HEAD:src/DesignSystem/DesignTokensExt.fs`) to serve as the byte-parity reference for INV-2 (only the `module` line may differ post-promotion).
- [X] T004 Confirm the existing Feature129 neutral-resolution oracle (`tests/Controls.Tests/Feature129*` resolver parity) is green and identify it as the render-neutrality oracle that `StyleResolver.resolveDefault` must stay byte-identical against (INV-3) after promotion.

**Checkpoint**: Pre-promotion baseline + parity oracles captured — promotion work can begin.

---

## Phase 3: User Story 1 - Theme & component authors build on a stable public design surface (Priority: P1) 🎯 MVP

**Goal**: Promote the chosen subset (`StyleResolver` in full + the entire generated `DesignTokensExt` taxonomy) to a public, documented, `.fsi`-declared contract — surface-only, zero token-value delta, byte-identical neutral output — reachable from an assembly with **no** `InternalsVisibleTo` grant.

**Independent Test**: From `tests/Controls.Tests` with the IVT grants removed, reference the promoted token taxonomy and resolver, assemble/resolve a style, and confirm it compiles and resolves with public-only access; representative token values equal their known literals; `resolveDefault` is byte-identical to the Feature129 neutral oracle.

### Tests for User Story 1 (write FIRST — must FAIL before implementation) ⚠️

- [X] T005 [US1] Create `tests/Controls.Tests/Feature130PublicSurfaceTests.fs` with a public-path consumption test that references `FS.GG.UI.DesignSystem.StyleResolver` (`resolve`/`resolveDefault`/`baseStyleFor`/`neutralPolicy`) and reads `FS.GG.UI.DesignSystem.DesignTokensExt.*` leaf values — proving public access (INV-4/SC-001). Authored to FAIL-before (symbols still internal).
- [X] T006 [US1] In `tests/Controls.Tests/Feature130PublicSurfaceTests.fs`, add a token-value-parity test asserting representative promoted `DesignTokensExt.*` values (Seed/Map/Alias/Component/Space/Density/Type/Elevation) equal their known literals (INV-2/SC-003).
- [X] T007 [US1] In `tests/Controls.Tests/Feature130PublicSurfaceTests.fs`, add a neutral-render-parity test asserting `StyleResolver.resolveDefault` output is byte-identical to the Feature129 neutral oracle across the full `{kind} × {intent} × {state}` cross-product (INV-3/SC-003).
- [X] T008 [US1] Register `Feature130PublicSurfaceTests.fs` in `tests/Controls.Tests/Controls.Tests.fsproj` `<Compile>` order; run `dotnet test FS.GG.Rendering.slnx --filter "130"` and confirm the new tests **FAIL/do not compile** against the still-internal surface (fail-before evidence).

### Implementation for User Story 1

- [X] T009 [P] [US1] Create hand-curated `src/DesignSystem/StyleResolver.fsi` declaring exactly the five public members — `type IntentPolicy = { ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }`, `val baseStyleFor`, `val neutralPolicy`, `val resolve`, `val resolveDefault` — with doc comments (FR-010), matching `contracts/public-surface-contract.md` byte-for-byte; no helper leaks.
- [X] T010 [P] [US1] Extend `scripts/generate-design-tokens.fsx` to (a) emit `src/DesignSystem/DesignTokensExt.fsi` via a second recursive DTCG walk emitting `val name : type` per leaf **plus a `///` doc comment on the top-level module and each nested layer/sub-module** (`Seed`, `Map.Light/Dark`, `Alias.*`, `Component.*`, `Space`, `Density`, `Type.*`, `Elevation`) describing the layer — per-leaf doc comments are NOT emitted (FR-010, module-granularity), (b) emit the `.fs` `module DesignTokensExt` **without** the `internal` modifier, and (c) extend `--check` (drift mode) to verify BOTH committed files against freshly generated output (R3, INV-2).
- [X] T011 [US1] Regenerate the taxonomy by running `dotnet fsi scripts/generate-design-tokens.fsx`, producing updated `src/DesignSystem/DesignTokensExt.fs` (internal removed) and new `src/DesignSystem/DesignTokensExt.fsi`; verify `git diff src/DesignSystem/DesignTokensExt.fs` shows only the `module` line losing `internal` (values unchanged vs T003 snapshot). (depends on T010)
- [X] T012 [US1] Remove the `internal` access modifier from the `module StyleResolver` declaration in `src/DesignSystem/StyleResolver.fs`; leave the body unchanged (no `public`/`private`/`internal` on any binding — Principle II). (pairs with T009)
- [X] T013 [US1] Edit `src/DesignSystem/DesignSystem.fsproj`: add `<Compile Include="DesignTokensExt.fsi" />` immediately before `DesignTokensExt.fs` and `<Compile Include="StyleResolver.fsi" />` immediately before `StyleResolver.fs` (after `Style.fs`), per the contract compile order. (depends on T009, T010)
- [X] T014 [US1] Remove the now-redundant `InternalsVisibleTo` grants for `FS.GG.UI.Controls` and `FS.GG.UI.Controls.Tests` from `src/DesignSystem/DesignSystem.fsproj` (the promoted symbols are public; IVT is invisible to the surface gate). (depends on T013)
- [X] T015 [US1] Build clean: `dotnet build -c Release FS.GG.Rendering.slnx` → 0 warnings / 0 errors (confirms `.fsi` files match `.fs`, removed modifiers compile, and removed IVT grants did not break any consumer — quickstart V1, INV-6). Also confirm **no new dependency was introduced** (FR-011): `git diff src/DesignSystem/DesignSystem.fsproj` shows only the two `<Compile>` `.fsi` additions and the IVT removals — no new `<PackageReference>`/`<ProjectReference>` in any product/test assembly, and `System.Text.Json` remains confined to the generator script. (depends on T011, T012, T013, T014)
- [X] T016 [US1] Run `dotnet fsi scripts/generate-design-tokens.fsx --check` and confirm no drift over BOTH `DesignTokensExt.fs` AND `DesignTokensExt.fsi` (committed == generated — quickstart V2, INV-2). (depends on T011)
- [X] T017 [US1] Run `dotnet test FS.GG.Rendering.slnx --filter "130"` and confirm the T005–T007 tests now PASS against the public surface (pass-after evidence — quickstart V4, INV-3/4). (depends on T015)

**Checkpoint**: The public surface exists, compiles, is consumable with no IVT, token values are byte-identical, and neutral output is unchanged. **MVP complete — US1 is independently shippable.**

---

## Phase 4: User Story 2 - The promotion is deliberate, gated, and recorded (Priority: P2)

**Goal**: Regenerate the public-surface baseline in the same change so CI stays green, prove the only added rows are exactly the deliberately-promoted symbols, and author decision record `0004` that two-way-agrees with the baseline diff.

**Independent Test**: Regenerate the baseline; `git diff tests/surface-baselines/` shows ONLY `FS.GG.UI.DesignSystem.txt` changed, additions only, each line a promoted symbol; the decision record names every added row and no symbol it names is absent from the diff.

### Implementation for User Story 2

- [X] T018 [US2] Regenerate surface baselines: `dotnet fsi scripts/refresh-surface-baselines.fsx`, then inspect `git diff --stat tests/surface-baselines/` and confirm ONLY `tests/surface-baselines/FS.GG.UI.DesignSystem.txt` changed (every other `*.txt` byte-identical — INV-1). (depends on T015)
- [X] T019 [US2] Inspect `git diff tests/surface-baselines/FS.GG.UI.DesignSystem.txt` and confirm the diff is additions-only and every added line is one of: `FS.GG.UI.DesignSystem.StyleResolver`, `FS.GG.UI.DesignSystem.StyleResolver+IntentPolicy`, `FS.GG.UI.DesignSystem.DesignTokensExt`, and each `FS.GG.UI.DesignSystem.DesignTokensExt+<Sub>[+<Sub>]` nested-module type — no deletions, no unrelated rows, `Theme` row unchanged (INV-1/INV-6, SC-002/SC-007). (depends on T018)
- [X] T020 [P] [US2] Author `docs/product/decisions/0004-public-token-resolver-surface.md` enumerating: every promoted symbol (StyleResolver's five members + the full `DesignTokensExt` taxonomy), the **module-granularity framing of "chosen subset"** (the taxonomy is promoted as one unit because it is all values with no internal helpers; selectivity is exercised at the candidate-module level — FR-001/FR-009), the **module-granularity documentation choice** for the generated taxonomy (per-module doc comments, no per-leaf — FR-010), the deliberately-deferred `ColorPolicy` with rationale (R5/INV-8), the deferred `DesignTokens`↔`DesignTokensExt` unification (R4), and the stability/reversibility commitment (FR-006/SC-004).
- [X] T021 [US2] Verify two-way agreement: cross-check `0004` against the T019 baseline diff so every added baseline row is named in the record and every promoted symbol named in the record appears in the diff (no orphan surface, no undocumented promotion — SC-002/SC-007, quickstart V7). (depends on T019, T020)

**Checkpoint**: Both CI drift gates (surface baseline + token generator) are green within the change and the promotion is fully documented and traceable.

---

## Phase 5: User Story 3 - Application developers supply a custom intent policy (Priority: P3)

**Goal**: Prove the public intent-policy seam lets an external consumer make a `danger` button render a divergent style by supplying their own `IntentPolicy`, with zero control edits.

**Independent Test**: From the no-IVT test, supply a divergent `IntentPolicy` mapping `"danger"` to `theme.Danger`, resolve a `danger` button via `StyleResolver.resolve`, and confirm the result differs from `resolveDefault` — with no control code changed.

### Tests for User Story 3 (write FIRST — must FAIL before US3 is reachable) ⚠️

- [X] T022 [US3] In `tests/Controls.Tests/Feature130PublicSurfaceTests.fs`, add a divergence test: construct a public `StyleResolver.IntentPolicy` whose `ApplyIntent` maps `"danger"` to `theme.Danger`, call `StyleResolver.resolve` for a `danger` button, and assert the `ResolvedStyle` differs from `StyleResolver.resolveDefault` for the same inputs — with zero control-code edits (INV-5/SC-005). (same file as T005–T007; sequence after them)

### Implementation for User Story 3

- [X] T023 [US3] Run `dotnet test FS.GG.Rendering.slnx --filter "130"` and confirm the divergence test passes — the divergent style is reachable purely through the public seam (no IVT, no control edits — quickstart V4, INV-5). (depends on T017, T022)

**Checkpoint**: All three user stories are independently functional — public consumption, gated/recorded promotion, and external intent divergence.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation touch-ups and full-suite validation across all stories.

- [X] T024 [P] Update `docs/product/module-map.md` to note the now-public `DesignTokensExt` taxonomy and `StyleResolver` resolver/intent-policy surface.
- [X] T025 [P] Update the SPECKIT marker in `CLAUDE.md` to point at `specs/130-promote-token-surface/plan.md` (already set — confirm it is current).
- [X] T026 Run pre-existing suites against the now-public surface: `dotnet test FS.GG.Rendering.slnx --filter "126"` and `dotnet test FS.GG.Rendering.slnx --filter "129"` — both green unchanged (now reaching promoted symbols publicly; quickstart V5, INV-7). (depends on T015)
- [X] T027 Run the full suite `dotnet test FS.GG.Rendering.slnx` → 0 failures; confirm pass/skip counts equal the T002 pre-F5 baseline **plus** the additive Feature130 tests, and gallery/render-identity suites are unchanged (quickstart V6, INV-3/INV-7, SC-006). (depends on T017, T023, T026)
- [X] T028 Execute the full `quickstart.md` runbook V1–V7 end-to-end and confirm every step passes (clean build, generator drift green over both files, baseline delta == promoted symbols only, public-path/parity/neutrality/divergence tests green, prior suites green, full suite 0 failures, decision record present and two-way consistent). (depends on T021, T027)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup; MUST capture pre-promotion snapshots before any edit — BLOCKS all user stories.
- **User Story 1 (Phase 3, P1)**: Depends on Foundational. This is the promotion itself and the **blocking prerequisite** for US2 and US3 (they consume the public surface US1 creates).
- **User Story 2 (Phase 4, P2)**: Depends on US1 build landing (T015) — the baseline cannot be regenerated until the surface is public.
- **User Story 3 (Phase 5, P3)**: Depends on US1 (the public `IntentPolicy`/`resolve` seam) — T022 extends the same test file as US1's tests, so author it after T005–T007.
- **Polish (Phase 6)**: Depends on all desired stories complete.

### User Story Dependencies

- **US1 (P1)**: Independent core — the MVP. No dependency on US2/US3.
- **US2 (P2)**: Requires US1's public surface to exist (regenerates its baseline + records it). Otherwise independently verifiable by baseline diff vs decision record.
- **US3 (P3)**: Requires US1's public seam. Independently verifiable by the divergence assertion.

### Within User Story 1

- Tests (T005–T008) authored FIRST and confirmed FAIL-before.
- `.fsi` contracts (T009 StyleResolver, T010 generator emission) before regeneration/modifier removal.
- Regenerate `.fs`/`.fsi` (T011) + remove `StyleResolver` modifier (T012) before fsproj wiring (T013, T014).
- Build clean (T015) + drift check (T016) before pass-after test run (T017).

### Parallel Opportunities

- **Setup**: T002 [P] runs alongside T001.
- **US1 implementation**: T009 (StyleResolver.fsi) and T010 (generator script) are different files → [P]. They both feed T013.
- **US2**: T020 (decision record authoring) [P] can proceed alongside T018/T019 (baseline regen/inspection); they converge at T021.
- **Polish**: T024 (module-map) and T025 (CLAUDE.md) are different files → [P].
- Note: T005–T008 and T022 all touch `Feature130PublicSurfaceTests.fs` — NOT parallel with each other (same file).

---

## Parallel Example: User Story 1 implementation

```bash
# The two independent contract/tooling files can be authored in parallel:
Task: "Create hand-curated src/DesignSystem/StyleResolver.fsi (T009)"
Task: "Extend scripts/generate-design-tokens.fsx to emit DesignTokensExt.fsi + drop internal + extend --check (T010)"
# Then converge: regenerate (T011), wire fsproj (T013), remove IVT (T014), build (T015).
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → known-good pre-F5 baseline.
2. Phase 2 Foundational → capture pre-promotion value/oracle snapshots (CRITICAL — must precede edits).
3. Phase 3 US1 → author fail-before tests, add the two `.fsi` (one generated), drop `internal`, wire fsproj, remove IVT, build clean, drift green, tests pass-after.
4. **STOP and VALIDATE**: public-path consumption + value parity + neutral render parity green. US1 is a complete, shippable contract.

### Incremental Delivery

1. Setup + Foundational → baseline ready.
2. US1 → public surface consumable with no IVT (MVP).
3. US2 → regenerate baseline + author `0004`; both gates green, two-way agreement (safe + auditable).
4. US3 → divergent `IntentPolicy` reachable from the public seam (user-facing payoff).
5. Polish → docs + full-suite/quickstart validation.

### Notes

- [P] = different files, no incomplete-task dependency.
- The two CI drift gates (surface baseline via `refresh-surface-baselines.fsx`; token drift via the generator `--check`) MUST both move in this one change — never as a follow-up (INV-1/INV-2).
- Verify baseline correctness by `git diff`, not by exit code (`refresh-surface-baselines.fsx` always rewrites).
- Principle II: visibility lives in the `.fsi` — no `public`/`internal`/`private` modifier survives on any promoted top-level binding.
- Commit after each logical group; stop at any checkpoint to validate the story independently.
