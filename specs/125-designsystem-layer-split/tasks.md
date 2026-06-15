---
description: "Task list for Design-System Layer Split (Workstream D, Phase D1)"
---

# Tasks: Design-System Layer Split (Workstream D, Phase D1)

**Input**: Design documents from `/specs/125-designsystem-layer-split/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: No *new* test tasks are generated. Per the spec (FR-005/FR-006/SC-001) the
behaviour-neutrality oracle is the **existing** suite passing unchanged plus render-identity
evidence — no test is added, weakened, deleted, or newly skipped. Verification tasks therefore
*run* existing suites rather than author new ones.

**Organization**: Tasks are grouped by the three user stories from spec.md. Because this is a single
high-blast-radius structural carve, the build only returns to green once each carve's consumers are
fixed; the per-story checkpoints below mark the points where the solution compiles and a story is
independently verifiable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). New library projects live under `src/`; their
**folder name == the refresh-script row slug** (`DesignSystem`, `Themes.Default`) with matching
`AssemblyName`/`PackageId` on the `FS.GG.UI.*` scheme.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up the two new project skeletons and register them everywhere a project must be
listed, so the carve has somewhere to land and the drift gate can see the new assemblies.

- [X] T001 Create `src/DesignSystem/DesignSystem.fsproj` (`PackageId`/`AssemblyName` = `FS.GG.UI.DesignSystem`, `net10.0`, `OutputType=Library`, `IsPackable=true`) with exactly one `ProjectReference` to `..\Scene\Scene.fsproj` and an empty `<Compile>` ItemGroup (entries added in Phase 3). Mirror the property/version style of `src/Controls/Controls.fsproj`.
- [X] T002 Create `src/Themes.Default/Themes.Default.fsproj` (`PackageId`/`AssemblyName` = `FS.GG.UI.Themes.Default`, `net10.0`, `OutputType=Library`, `IsPackable=true`) with exactly one `ProjectReference` to `..\DesignSystem\DesignSystem.fsproj` and an empty `<Compile>` ItemGroup (entries added in Phase 4).
- [X] T003 Add both new projects to `FS.GG.Rendering.slnx` (per R6/FR-009), preserving the existing project ordering convention.
- [X] T004 Add two rows to `scripts/refresh-surface-baselines.fsx` (around line 20): `"FS.GG.UI.DesignSystem", "DesignSystem"` and `"FS.GG.UI.Themes.Default", "Themes.Default"` so the gate derives `src/<slug>/bin/Debug/net10.0/<PackageId>.dll` for each new package (R6).

**Checkpoint**: Empty projects exist, are in the solution, and are known to the refresh script. The
solution still builds (empty projects compile; Controls is untouched so far).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-split "green oracle" that every behaviour-neutrality claim (US3) is
measured against. **No carve work may begin until this baseline is recorded.**

**⚠️ CRITICAL**: This is the reference point for FR-005/SC-001/SC-003 — without it, "identical" is
unverifiable.

- [X] T005 Establish baseline confidence: from a clean tree run `dotnet build FS.GG.Rendering.slnx -c Release` (0 errors/warnings) and `dotnet test FS.GG.Rendering.slnx -c Release`; record the passing/skipped counts (expected 18 honest `ptest`/`ptestList` skips per quickstart V5) as the pre-split oracle.
- [X] T006 Record the pre-split public surface: snapshot current `tests/surface-baselines/FS.GG.UI.Controls.txt` (and note it will shrink) as the before-side of the SC-005 "relocations only, no removals" diff.

**Checkpoint**: Pre-split build/test/surface state is captured. Carve can begin.

---

## Phase 3: User Story 1 - Depend on the design system without the whole catalog (Priority: P1) 🎯 MVP

**Goal**: The design-system primitives (token model, `Theme` record incl. new `Success`/`Warning`,
`ResolvedStyle`, `StyleVariant`, `StyleClass`, `VisualState`, `ValidationState`, and `Style.resolve`)
become a standalone `FS.GG.UI.DesignSystem` package whose only dependency is `FS.GG.UI.Scene`, and
`FS.GG.UI.Controls` consumes them rather than defining them.

**Independent Test**: `dotnet list src/DesignSystem/DesignSystem.fsproj package --include-transitive`
shows `FS.GG.UI.Scene` and **never** `FS.GG.UI.Controls`; a throwaway consumer that `open`s only
`FS.GG.UI.DesignSystem` can name `Theme`/`ResolvedStyle`/`StyleVariant`/`StyleClass`/`VisualState`
and call `Style.resolve` (quickstart V2/V3).

### Implementation for User Story 1

- [X] T007 [US1] Create `src/DesignSystem/Types.DesignSystem.fsi` under `namespace FS.GG.UI.DesignSystem`, relocating the design-system slice of `src/Controls/Types.fsi` in this exact order (R1/data-model declaration invariant): `ValidationState` → `VisualState` → `StyleVariant` (`[<RequireQualifiedAccess>]`) → `StyleClass` → `ResolvedStyle` → `Theme`. Add the two additive fields `Success: Color` and `Warning: Color` to `Theme` immediately after the existing role fields (FR-004/R7). Keep `ResolvedStyle` declared immediately before `Theme`.
- [X] T008 [US1] Create `src/DesignSystem/Types.DesignSystem.fs` implementing the relocated types in the same order, `namespace FS.GG.UI.DesignSystem`, `open FS.GG.UI.Scene` for `Color` (no behaviour change to existing definitions).
- [X] T009 [P] [US1] Move `src/Controls/DesignTokens.fsi` → `src/DesignSystem/DesignTokens.fsi` and re-namespace to `FS.GG.UI.DesignSystem` (curated `.fsi`, Principle II; values byte-identical, R5).
- [X] T010 [P] [US1] Move `src/Controls/DesignTokens.fs` → `src/DesignSystem/DesignTokens.fs` and re-namespace to `FS.GG.UI.DesignSystem` (generated module stays in DesignSystem; values unchanged, R5).
- [X] T011 [US1] Move `src/Controls/Style.fsi` → `src/DesignSystem/Style.fsi` and re-namespace to `FS.GG.UI.DesignSystem`, preserving the header comment documenting the `ResolvedStyle`-before-`Theme` field-inference dependency (R1).
- [X] T012 [US1] Move `src/Controls/Style.fs` → `src/DesignSystem/Style.fs` and re-namespace to `FS.GG.UI.DesignSystem` (the pure `Style.resolve`; unchanged logic).
- [X] T013 [US1] Populate `src/DesignSystem/DesignSystem.fsproj` `<Compile>` ItemGroup in dependency order: `Types.DesignSystem.fsi`/`.fs`, `DesignTokens.fsi`/`.fs`, `Style.fsi`/`.fs` (DesignTokens before any type that reads tokens; `Types` before `Style`).
- [X] T014 [US1] Build `src/DesignSystem/DesignSystem.fsproj` standalone (`-c Release`) and confirm it compiles against Scene only — first proof of the acyclic `DesignSystem → Scene` edge.
- [X] T015 [US1] In `src/Controls/Types.fsi` and `src/Controls/Types.fs`, **remove** the relocated design-system types (T007 list) leaving only the control-semantic types per data-model.md (`ControlId`…`ControlRenderResult<'msg>`, `Standard*`, `Known*`, `AccessibilityMetadata` & friends, etc.); add `open FS.GG.UI.DesignSystem` so `AttrValue<'msg>`'s `ThemeValue`/`StyleClassesValue`/`VisualStateValue`/`ValidationValue` cases still resolve.
- [X] T016 [US1] Edit `src/Controls/Controls.fsproj`: **keep** the (now-shrunken) `Types.fsi/.fs` `<Compile>` entries — `Types` stays in Controls; **remove** the `DesignTokens.fsi/.fs` and `Style.fsi/.fs` `<Compile>` entries (they moved to DesignSystem); and add `<ProjectReference Include="..\DesignSystem\DesignSystem.fsproj" />`. Do **not** add any reference to a theme project.
- [X] T017 [US1] Add `open FS.GG.UI.DesignSystem` to every `src/Controls/**` file the compiler flags for a now-unresolved moved type (compiler-driven, R8); where an ambiguous bare field (`Foreground`/`FontFamily`/`FontSize`) is reported, add a use-site type annotation with a one-line disclosure comment — never reorder or rename fields (R1).
- [X] T018 [US1] Verify catalog-free closure: `dotnet list src/DesignSystem/DesignSystem.fsproj package --include-transitive` shows `FS.GG.UI.Scene` and not `FS.GG.UI.Controls` (SC-002); optionally compile the quickstart V3 throwaway consumer that opens only `FS.GG.UI.DesignSystem`.

**Checkpoint**: `DesignSystem` is a real standalone package; Controls consumes it. (Full-solution
green build arrives once US2's theme module — still in Controls until Phase 4 — is also relocated;
if Controls' `Theme.fs`/`Theming.fs` still reference moved tokens via the old namespace, add the
open as part of T017 so Controls keeps building.)

---

## Phase 4: User Story 2 - Swap the default theme as its own layer (Priority: P2)

**Goal**: The default Light/Dark `Theme` value module, `ThemeMode`/`RolePalette`/`Theming`
derivation, and the DTCG token source relocate to `FS.GG.UI.Themes.Default`, depending only on
`FS.GG.UI.DesignSystem`.

**Independent Test**: `dotnet list src/Themes.Default/Themes.Default.fsproj package
--include-transitive` shows `FS.GG.UI.DesignSystem` and not `FS.GG.UI.Controls`; a consumer that
opens `FS.GG.UI.Themes.Default` obtains the same `Theme.light`/`Theme.dark` values plus the new
`Success`/`Warning` roles (quickstart V4).

### Implementation for User Story 2

- [X] T019 [US2] Move `src/Controls/Theme.fsi` → `src/Themes.Default/Theme.fsi` and re-namespace to `FS.GG.UI.Themes.Default` (the `Theme` *module*: `light`/`dark`/`withDensity`/`withAccent`/`resolve`); keep `[<CompilationRepresentation(ModuleSuffix)>]` so the same-named type/module coexist across packages (R4).
- [X] T020 [US2] Move `src/Controls/Theme.fs` → `src/Themes.Default/Theme.fs`, re-namespace, `open FS.GG.UI.DesignSystem`; set the new `Success`/`Warning` fields in `Theme.light`/`Theme.dark` from `DesignTokens.{Light,Dark}.success`/`warning` (R7) — no existing field value changes.
- [X] T021 [P] [US2] Move `src/Controls/Theming.fsi` → `src/Themes.Default/Theming.fsi`, re-namespace to `FS.GG.UI.Themes.Default` preserving the `Theming` child-namespace isolation (`ThemeMode`/`RolePalette`/`Theming.resolve`/`toTheme`) so `RolePalette` field names do not poison `Theme` inference (R1/R4/data-model).
- [X] T022 [US2] Move `src/Controls/Theming.fs` → `src/Themes.Default/Theming.fs`, re-namespace, `open FS.GG.UI.DesignSystem`; ensure `Theming.toTheme` sets the new `Success`/`Warning` fields (from light defaults or palette) so every `Theme` construction site compiles (R7).
- [X] T023 [US2] Move `src/Controls/design-tokens.tokens.json` → `src/Themes.Default/design-tokens.tokens.json` (DTCG source + generation tooling travel with the default theme, R5/spec assumption).
- [X] T024 [US2] Populate `src/Themes.Default/Themes.Default.fsproj` `<Compile>` ItemGroup in order: `Theme.fsi`/`.fs`, `Theming.fsi`/`.fs`; include the `design-tokens.tokens.json` as Content if it was packaged before.
- [X] T025 [US2] Edit `src/Controls/Controls.fsproj`: remove the `Theme.fsi/.fs` and `Theming.fsi/.fs` `<Compile>` entries and the `design-tokens.tokens.json` reference (all relocated). Confirm Controls has **no** ProjectReference to `Themes.Default` (forbidden edge, layering-contract).
- [X] T026 [US2] Re-point the `DesignTokenParityTests` drift check (`tests/Controls.Tests/DesignTokenParityTests.fs`, line ~115) and any generator path from `src/Controls/...` to the new locations (`design-tokens.tokens.json` under `src/Themes.Default/`, generated `DesignTokens.fs` under `src/DesignSystem/`) so the parity/drift check passes against the new source/output paths (R5).
- [X] T026a [US2] Update the frozen `Theme` parity oracle in `tests/Controls.Tests/DesignTokenParityTests.fs` (the `frozenLight`/`frozenDark` literals, ~lines 28-57): add the two additive `Success`/`Warning` fields, set from `DesignTokens.{Light,Dark}.success`/`warning`, so (a) the literals still compile against the widened `Theme` record (FR-004 makes `Theme` require all fields) and (b) the `Expect.equal Theme.light frozenLight` / `Theme.dark frozenDark` parity assertions stay green after T020 sets the same token-derived values on `Theme.light`/`dark` (FR-004/SC-001/SC-002). **This `frozenLight`/`frozenDark` pair is the only in-repo test/sample that builds a full `Theme` literal** — every other `Foreground =` site is a `ResolvedStyle` (verified), so no other literal gains fields and the rest of T029 remains opens-only.
- [X] T027 [US2] Build `src/Themes.Default/Themes.Default.fsproj` standalone (`-c Release`); confirm it compiles against DesignSystem only and verify closure: `dotnet list ... --include-transitive` shows `FS.GG.UI.DesignSystem`, not `FS.GG.UI.Controls` (SC-002/US2.1).
- [X] T028 [US2] Add `open FS.GG.UI.Themes.Default` to the remaining `src/Controls/**` consumers (and any other in-repo source) that reference `Theme.light`/`dark`/`withAccent`/`Theming` (compiler-driven, R8), so the full solution returns to a green Release build.

**Checkpoint**: All three packages exist with the acyclic graph (`DesignSystem → Scene`,
`Themes.Default → DesignSystem`, `Controls → DesignSystem`); `dotnet build FS.GG.Rendering.slnx -c
Release` is green (SC-006/quickstart V1).

---

## Phase 5: User Story 3 - Nothing a user or consumer can observe changed (Priority: P1)

**Goal**: Prove behaviour-neutrality and land the surface baselines, docs, decision record, and
template updates **atomically** so every quality gate is green in this same change.

**Independent Test**: full existing suite passes unchanged (same pass/skip counts as T005); gallery
render + accessibility contract identical; drift gate green with 2 new baselines + 1 regenerated
(smaller) Controls baseline committed together; decision record + module-map + template reflect the
move (quickstart V5–V8).

### Implementation for User Story 3

- [X] T029 [US3] Update the remaining in-repo consumers to add the relocation opens (compiler-driven, R8): `src/Controls.Elmish/**`, the `tests/Controls.Tests/**` and `tests/Elmish.Tests/**` suites (~80 files), `tests/SkiaViewer.Tests/**`, and the `samples/ControlsGallery/**` tree — `open FS.GG.UI.DesignSystem` and, where they use default theme values/`Theming`, `open FS.GG.UI.Themes.Default`. No semantic edits **except** the `Theme`-literal field additions handled in T026a (the lone full-`Theme` construction site; all other relocated-type references are opens-only).
- [X] T030 [US3] Regenerate surface baselines: `dotnet build FS.GG.Rendering.slnx -c Debug` then `dotnet fsi scripts/refresh-surface-baselines.fsx`, producing committed `tests/surface-baselines/FS.GG.UI.DesignSystem.txt`, `tests/surface-baselines/FS.GG.UI.Themes.Default.txt`, and the regenerated (smaller) `tests/surface-baselines/FS.GG.UI.Controls.txt`.
- [X] T031 [US3] Verify the SC-005 "relocations only, no removals" evidence: diff `FS.GG.UI.Controls.txt` against the T006 snapshot and confirm every removed row reappears (re-namespaced) in one of the two new baselines; `git status --porcelain tests/surface-baselines/` is clean after committing all three (SC-004/quickstart V7).
- [X] T032 [US3] Run the full suite `dotnet test FS.GG.Rendering.slnx -c Release` and confirm the pass/skip counts match the T005 oracle exactly — zero tests deleted, weakened, or newly skipped (FR-006/SC-001/quickstart V5).
- [X] T033 [US3] Render identity: run `dotnet test samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj -c Release` and confirm `ThemeInvarianceTests`/`PageRenderTests` pass — rendered output + accessibility contract byte-identical (SC-003/quickstart V6).
- [X] T034 [P] [US3] Create `docs/product/decisions/0003-designsystem-namespace-relocation.md` recording the namespace relocation and the no-backward-source-compat-shim rationale (pre-1.0, in-repo consumers only) (FR-008/R2/quickstart V8).
- [X] T035 [P] [US3] Update `docs/product/module-map.md`: move the design-system and theme-layer rows from "embedded in `Controls`" to "owned assembly" (`FS.GG.UI.DesignSystem` / `FS.GG.UI.Themes.Default`) (FR-011/quickstart V8).
- [X] T036 [US3] Update the `template/base` product source + regenerate its `template/base/docs/api-surface/` snapshot (notably `Controls/Theme.fsi`, `Controls/Types.fsi` move to the new packages) so the template pack/instantiate check stays green (FR-008/R8/quickstart V8).
- [X] T036a [P] [US3] Update `docs/bridge/package-identity-migration.md` (the FR-008 "bridge/migration documentation") to record the design-system / default-theme **namespace relocation** out of `FS.GG.UI.Controls` into `FS.GG.UI.DesignSystem` / `FS.GG.UI.Themes.Default`, cross-linking the new decision record `0003` (T034). This is the bridge/migration-doc half of FR-008 / acceptance US3.4 (distinct from the decision record T034 and the template T036).

**Checkpoint**: All of V1–V8 pass; the layer boundary is physical and nothing observable changed.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final atomicity and hygiene checks across the whole change.

- [X] T037 Confirm atomicity: a single commit/change contains the two new projects, the two added refresh-script rows, the two new baselines + regenerated Controls baseline, the moved sources, all consumer opens, the decision record, module-map, and template snapshot — so CI is green at this one commit (R6/FR-007).
- [X] T038 [P] Final clean-tree verification: from a fresh checkout `dotnet build FS.GG.Rendering.slnx -c Release` (0 new warnings) and `dotnet test FS.GG.Rendering.slnx -c Release` both green (quickstart "Done when").
- [X] T039 Run through quickstart V1–V8 end-to-end as the acceptance walkthrough and tick each success criterion (SC-001…SC-006).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; records the green oracle — BLOCKS all carve work.
- **User Story 1 (Phase 3)**: Depends on Foundational. The load-bearing MVP carve.
- **User Story 2 (Phase 4)**: Depends on US1 (Themes.Default references the relocated `Theme` type /
  `DesignTokens` in DesignSystem). Until US2 relocates Controls' `Theme.fs`/`Theming.fs`, the
  full-solution build is not green — so US1 and US2 together restore the green build.
- **User Story 3 (Phase 5)**: Depends on US1 **and** US2 (baselines need both packages built; the
  full suite needs the solution green). This is the verification + atomic-landing story.
- **Polish (Phase 6)**: Depends on US3.

### Story Independence Notes

This is a single structural carve, so the stories are **sequential**, not parallel: US2's package
cannot build before US1's `DesignSystem` exists, and US3's gates require both. Each story is still
*independently verifiable* at its checkpoint (US1: standalone DesignSystem closure; US2: standalone
Themes.Default closure; US3: green suite + drift gate + render identity).

### Within Each Story

- `.fsi` relocation before/with its paired `.fs` (Principle I — the `.fsi` is the carve unit).
- `DesignTokens` compiles before `Theme` (Theme reads token values).
- `Types.DesignSystem` before `Style` (Style depends on those types).
- Source relocation before `.fsproj` `<Compile>` edits before standalone build before consumer opens.

### Parallel Opportunities

- Setup: T001/T002 touch different new files (T003/T004 follow once both exist).
- US1: T009/T010 (DesignTokens move) are independent of the Types split (T007/T008).
- US2: T021 (Theming.fsi) is independent of the Theme module move (T019/T020).
- US3: T034 (decision record), T035 (module-map), and T036a (bridge/migration doc) are independent
  docs; all independent of the code-gate tasks.
- Polish: T038 is independent of T037's review.

---

## Parallel Example: User Story 1

```bash
# After the Types split (T007/T008) lands, the DesignTokens relocation can proceed alongside it:
Task: "Move src/Controls/DesignTokens.fsi → src/DesignSystem/DesignTokens.fsi (re-namespace)"   # T009
Task: "Move src/Controls/DesignTokens.fs  → src/DesignSystem/DesignTokens.fs  (re-namespace)"   # T010
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational (record the green oracle).
2. Phase 3 US1: carve `FS.GG.UI.DesignSystem` out of Controls, add the two `Theme` roles, fix
   Controls consumers.
3. **STOP and VALIDATE**: DesignSystem builds against Scene only and is catalog-free (T014/T018).
   This is the load-bearing slice that unblocks Workstreams F and D2.

### Incremental Delivery

1. Setup + Foundational → scaffolding + oracle ready.
2. US1 → standalone DesignSystem (MVP — layering rule physically true for the primitives).
3. US2 → standalone Themes.Default → full solution green build (acyclic graph proven).
4. US3 → atomic baselines + green suite + render identity + docs/decision/template.
5. Polish → atomicity + clean-tree verification.

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- The single highest-risk item is cross-assembly record-field inference (R1): keep
  `ResolvedStyle` immediately before `Theme`, prove by green build, annotate only where the compiler
  forces it.
- The single most likely CI-reddener is non-atomic baseline/solution landing (R6): T037 exists to
  guard the atomicity of the commit.
- No new behaviour, no new test framework, no new third-party dependency — purely a relocation plus
  two additive `Theme` fields nothing reads yet.
