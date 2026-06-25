---
description: "Task list for Symbology Rich-Text Label Runs (per-run colour / weight / size)"
---

# Tasks: Symbology Rich-Text Label Runs (per-run colour / weight / size)

**Input**: Design documents from `/specs/198-symbology-rich-text-label/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/symbology-rich-text-label-api.md ✓, quickstart.md ✓

**Tests**: Tests ARE in scope — the spec mandates a fail-before/pass-after Expecto battery (plan "Testing"), a render-bridge tofu test, a linter-invariance test, and value-preserving fixture migration of every existing label golden. Test tasks below are therefore REQUIRED, not optional.

**Organization**: Tasks are grouped by user story (P1 → P2 → P3) so each story is independently implementable and testable. The core types + layout live in the Foundational phase because the retype of `Token.Label` is a single shared seam every story (and every existing test) compiles against.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup / Foundational / Polish carry no story label)
- Exact file paths are included in each task

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). The change is confined to the existing pure
package `src/Symbology/` + its `.fsi`, extended tests in `tests/Symbology.Tests/` and
`tests/Symbology.Render.Tests/`, the regenerated symbology surface baseline under
`readiness/surface-baselines/`, and the mirrored `fs-gg-symbology` skill docs.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Pin the pre-feature truth before any byte moves, so layered zero-drift (FR-002/SC-003) is provable.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project via the
> discovery-based runner so pre-existing reds are known up front and not mistaken for regressions at
> merge. `dotnet test FS.GG.Rendering.slnx` deliberately omits `tests/Package.Tests` (release-only
> public-surface gate) and the `samples/**/*.Tests` projects — exactly where stale surface baselines /
> sample pins hide. Use the glob-based runner.

- [x] T001 Establish the no-regression baseline: run `dotnet fsi scripts/baseline-tests.fsx --out specs/198-symbology-rich-text-label/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not at merge)
- [x] T002 [P] Capture the pinned pre-feature label goldens that must stay byte-identical: record the canonical-bytes hashes for `Label = None` (`0dda10bd…`), `Label = Some "HMR-7"` (`6710215b…`), and the multi-line `ALPHA\nBRAVO` (`b41c9626…`) cases from `tests/Symbology.Tests/DeterminismTests.fs` into `specs/198-symbology-rich-text-label/readiness/baseline.md` so the post-migration byte-equality is checkable
- [x] T003 [P] Snapshot the current symbology surface baselines (`readiness/surface-baselines/FS.GG.UI.Symbology.txt` and `FS.GG.UI.Symbology.Render.txt`) and confirm every OTHER package baseline is clean in `git status -- readiness/surface-baselines/`, so FR-015 "only the symbology baseline moves" is verifiable after regen

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared typed channel + inline-run layout + structural zero-drift dispatch + the value-preserving fixture migration. Until `Token.Label` is retyped and every existing fixture migrated, NOTHING in the solution compiles, so this phase blocks all three stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Early smoke run (STANDING, do not omit).** This is greenfield-additive, not a defect fix — there
> is no running-app bug to reproduce, so the analogue of the live-smoke mandate is an **FSI/test smoke
> through the public surface** (plan "Standing assumption"). It runs the moment the types +
> `richLabelNodes` exist and BEFORE US1/US2/US3 build-out. Treat that smoke — not this plan's narrative —
> as confirmation the channel works.

### Public surface first (Constitution I — `.fsi` before `.fs`)

- [x] T004 Edit `src/Symbology/Symbology.fsi` to ADD `type LabelRun = { Text: string; Color: Color option; Weight: int option; Scale: float option }` and `[<RequireQualifiedAccess>] type LabelText = Plain of string | Rich of LabelRun list`, RETYPE the existing `Token.Label` field from `string option` to `LabelText option`, and ADD the convenience ctors `val plainLabel: string -> LabelText`, `val run: string -> LabelRun`, `val richLabel: LabelRun list -> LabelText` (per contracts §A; declares all new public types in the `.fsi`, Constitution II)

### Core layout implementation (`src/Symbology/Symbology.fs`)

- [x] T005 In `src/Symbology/Symbology.fs` add the new ctor implementations (`plainLabel`/`run`/`richLabel`) and generalise `labelFontOf` into `labelFontWith (weight: int option) (size: float)` such that the no-weight, scale-1.0 call reproduces today's `{ Family = None; Size; Weight = None }` exactly (zero drift on the plain path; plan Technical-approach §"Per-run style resolution")
- [x] T006 In `src/Symbology/Symbology.fs` add the per-run style resolver: a run resolves to `(Color, FontSpec)` at grammar base size `b` as colour = `run.Color |> Option.defaultValue labelInk`, font = `{ Family = None; Size = max 1.0 (b * (run.Scale |> Option.defaultValue 1.0)); Weight = run.Weight }` (FR-003; data-model §1)
- [x] T007 In `src/Symbology/Symbology.fs` implement `richLabelNodes`: (1) atomise runs → `(word, resolvedStyle)` stream + hard-break markers (split `Text` on `\n`/`\r\n`, then on whitespace; drop empty/whitespace runs — FR-007); (2) greedy inline break, each word measured in its own resolved font via `Scene.measureTextResolved`, packing while running line width ≤ region width, hard break forces a new line; (3) cap line count to the per-grammar budget + ellipsis the last kept line (FR-006/SC-005); (4) per line, line-height = max of each run's `lineHeightOf` at its scale, runs share a common baseline (FR-006); (5) centre each line by total measured width, emit one `Scene.glyphRunProof` node per contiguous same-style segment, first line at spec-197's first-line baseline, stacking downward; a single over-wide word with no wrap point degrades through the existing `fitLabel` shrink-toward-floor → measured ellipsis-truncate per segment (no mid-glyph clip / overflow)
- [x] T008 In `src/Symbology/Symbology.fs` wire the case-dispatch into the per-grammar label helpers (`tokenLabelNodes`/`badgeLabelNodes`/`ringLabelNodes`): `None ⇒ []`; `Some (LabelText.Plain s) ⇒` the UNCHANGED spec-197 `wrapLabel`/`labelNodes` path on `s`; `Some (LabelText.Rich runs)` where every run is default-styled (`Color=None && Weight=None && Scale∈{None,Some 1.0}`) ⇒ join run texts and route through the SAME `Plain` path; `Some (LabelText.Rich runs)` with any non-default attribute ⇒ `richLabelNodes` (structural zero-drift; emit styled nodes as per-segment siblings appended to the child list, NOT a wrapping `group`, to preserve byte-identity — plan Technical-approach §"Layered zero-drift")

### Make the solution compile again — value-preserving fixture migration

- [x] T009 Migrate every existing `Label = Some "X"` fixture to `Label = Some (LabelText.Plain "X")` (and each `\n` multi-line case to `LabelText.Plain "A\nB"`) across `tests/Symbology.Tests/ChannelPresenceTests.fs`, `DeterminismTests.fs`, `PlaceholderTests.fs`, `GalleryTests.fs`, `LegibilityTests.fs`, `LabelTests.fs`, `MultilineLabelTests.fs` (and any other `Label = Some` site surfaced by the build) — mechanical, value-preserving; existing assertions and pinned goldens (`0dda10bd…`/`6710215b…`/`b41c9626…`) MUST stay green by construction (FR-014; plan "Testing")

### Early smoke — confirm the channel works before building stories

- [x] T010 **Early FSI smoke run** (quickstart "Early FSI smoke"): through the PUBLIC surface (`Symbology.token`/`render` + `SceneCodec`), confirm in one go — (a) `Some (LabelText.Plain "HMR-7")` canonical bytes equal the pinned `6710215b…` and `Label = None` equals `0dda10bd…`; (b) `Some (LabelText.Rich [ Symbology.run "HMR-7" ])` is byte-identical to `Some (LabelText.Plain "HMR-7")`; (c) a two-run styled label emits ≥2 glyph-run nodes differing in colour/weight/size; (d) an over-wide styled run wraps/shrinks and an over-budget styled label caps + ellipsises; (e) `Rich []` / all-whitespace runs ⇒ no node + no throw, and a degenerate `R = 0.0` styled token ⇒ placeholder + no throw — record the observed result in `specs/198-symbology-rich-text-label/readiness/`

**Checkpoint**: Solution compiles, every pre-feature golden is green post-migration, and the early smoke confirms zero-drift + styled-channel + fit + safe-degenerate behaviours — user stories can now proceed.

> **Fail-first evidence (Constitution I/V).** The core layout (`richLabelNodes`, T007) lands in this
> phase BEFORE the per-story styled-run tests (T011–T020) because the plan's mandated early smoke (T010)
> needs the type + algorithm to exist to be exercised through FSI. T010 (the FSI smoke through the public
> surface) + the migrated pinned goldens (T009) are therefore the **fail-first evidence** for the
> Foundational core: a wrong `richLabelNodes`/dispatch fails T010 and/or drifts a pinned golden. Each
> per-story test (T011+) is still authored to assert its NEW behaviour and must be red against a stub /
> incomplete `richLabelNodes` before the story's tuning task (T015/T021/T022) closes it.

---

## Phase 3: User Story 1 - Author a label with emphasis (styled runs) (Priority: P1) 🎯 MVP

**Goal**: A short ordered sequence of styled runs (per-run colour/weight/size) renders in reading order in every grammar with real glyphs; a plain label is byte-identical to spec 197 and a no-label token byte-identical to the pre-feature symbol — rich styling is purely additive and opt-in.

**Independent Test**: Build `Token`s carrying (a) no label, (b) a plain label, (c) ≥2 styled runs differing in colour/weight/size; render each per grammar through the render bridge and confirm (1) each styled run draws in its own colour/weight/size with real glyphs (non-tofu); (2) plain ≡ spec-197 bytes and no-label ≡ pre-feature bytes; (3) same characters / different styling ⇒ different bytes.

### Tests for User Story 1 (write FIRST, ensure they FAIL before/over the impl) ⚠️

- [x] T011 [P] [US1] In `tests/Symbology.Tests/ChannelPresenceTests.fs` add: same characters split into styled runs vs `LabelText.Plain` ⇒ differing canonical bytes (style is a channel — B5/SC-002), and a ≥2-run styled label emits ≥2 glyph-run nodes in reading order (B4)
- [x] T012 [P] [US1] Create `tests/Symbology.Tests/RichLabelTests.fs` with the zero-drift battery: `Some (LabelText.Rich [ Symbology.run "X" ])` (all-default) is byte-identical to `Some (LabelText.Plain "X")` (B3); per-run colour/weight/size presence is observable in the scene; an author-supplied non-default `Color` survives unchanged into the emitted glyph-run node — used as-is, never re-mapped or rejected at runtime (B14/FR-013); register the new file in `tests/Symbology.Tests/Symbology.Tests.fsproj` (compile order before `Program.fs`)
- [x] T013 [P] [US1] In `tests/Symbology.Tests/DeterminismTests.fs` add a styled render-twice in-process byte-equality assertion plus a NEW pinned styled cross-process golden for a representative two-run `Token` (B10/SC-004)
- [x] T014 [P] [US1] Extend `tests/Symbology.Render.Tests/RenderLabelTests.fs`: rasterise a STYLED (multi-run) labelled token through `Render.toPng` under the real measurer and assert every run is non-tofu (`TofuCount = 0`) and the board is non-blank (FR-005/SC-002/B4)

### Implementation for User Story 1

- [x] T015 [US1] Verify/finish the styled reading-order emission in `richLabelNodes` (`src/Symbology/Symbology.fs`) so T011–T014 pass: each contiguous same-style segment becomes one `Scene.glyphRunProof` carrying per-glyph `Missing`/`FallbackMode`, runs laid left-to-right at a shared baseline; confirm the all-default-run join (T008) yields byte-identical output to the plain path for T012
- [x] T016 [US1] Confirm the pure-fallback path (no measurer installed) still emits the per-run styled text nodes deterministically and never throws (FR-010/B11) — assert in `tests/Symbology.Tests/RichLabelTests.fs`

**Checkpoint**: US1 is independently demonstrable — styled runs render per grammar tofu-free, plain/no-label stay byte-identical, styling changes the bytes.

---

## Phase 4: User Story 2 - Styled runs fit the region without clipping or overflow (Priority: P2)

**Goal**: Runs are fitted per run in their own style — wrap at measured boundaries, line-height = tallest run on common baseline, line count capped to the region, surplus ellipsised — never clipping mid-glyph, never overflowing the footprint, never overlapping the sigil/health/other channels; empty/whitespace/empty-run and degenerate-token-with-label are safe.

**Independent Test**: Render a `Token` whose styled runs are far wider/taller/more numerous than its region; confirm the block stays within the label footprint (measured per run), no run cut mid-glyph, line count capped, surplus truncated not overflowed, mixed sizes set each line height to its tallest run, no overlap with adjacent channels; confirm all-empty/whitespace runs ⇒ no label + no exception.

### Tests for User Story 2 (write FIRST) ⚠️

- [x] T017 [P] [US2] In `tests/Symbology.Tests/RichLabelTests.fs` add the fit battery: an over-wide styled run wraps/shrinks within the region with no mid-glyph clip (B6); an over-budget styled label caps to the per-grammar budget (Token ≤ 3, Badge ≤ 2, Ring ≤ 2) with a trailing ellipsis on the last kept line (B6/SC-005); assert every emitted segment's measured extent ≤ the region width/height
- [x] T018 [P] [US2] In `tests/Symbology.Tests/RichLabelTests.fs` add the mixed-size line battery: a line mixing run sizes/weights has height = the tallest run with runs sharing a common baseline and no vertical overlap (B7/FR-006)
- [x] T019 [P] [US2] In `tests/Symbology.Tests/RichLabelTests.fs` add the safe-degenerate battery: `Rich []`, `Rich` of all-empty/whitespace runs, and `Plain ""`/whitespace ⇒ no label node + no exception (B8/FR-007); interior empty/whitespace runs normalise without drawn gaps
- [x] T020 [P] [US2] In `tests/Symbology.Tests/PlaceholderTests.fs` add: a degenerate `Token` (`R <= 0`) carrying a styled `Rich` label ⇒ the existing visible placeholder + no exception (placeholder wins — B9/FR-008)

### Implementation for User Story 2

- [x] T021 [US2] Tune the cap + ellipsis + per-line max-height logic in `richLabelNodes` (`src/Symbology/Symbology.fs`) against the per-grammar budgets/regions in data-model §5 so T017/T018 pass: bound the drawn line count, ellipsis the last kept line via the existing measured truncate, and size each line to its tallest run on a common baseline (FR-006/SC-005)
- [x] T022 [US2] Confirm the empty/whitespace/empty-run normalisation and the `R <= 0` placeholder precedence in the dispatch (`src/Symbology/Symbology.fs`) so T019/T020 pass — the placeholder branch (`Symbology.fs` degenerate path) is reached before any label work and neither path throws (FR-007/FR-008)

**Checkpoint**: US1 + US2 both work — styled labels render AND stay safely inside the footprint with bounded, non-overlapping, non-clipping layout.

---

## Phase 5: User Story 3 - Styled labels on review boards, governed unchanged (Priority: P3)

**Goal**: Styled-label rosters render reproducibly on gallery/filmstrip boards in any grammar with no signature change; the legibility linter stays grammar-independent and treats the label (plain or styled) as inspection-detail; the skill documents rich-text runs and passes parity.

**Independent Test**: Render a styled-label roster as a gallery per grammar (reproducible); confirm the linter's report is identical across grammars and unchanged versus the same roster with plain labels; confirm the skill documents the channel and passes the parity check.

### Tests for User Story 3 (write FIRST) ⚠️

- [x] T023 [P] [US3] In `tests/Symbology.Tests/GalleryTests.fs` add: a styled-label roster rendered via `render`/`galleryIn`/`filmstripIn` in each grammar is byte-reproducible under a fixed provider and every unit's runs are drawn (B12/FR-011/SC-001) — no signature change to the board/motion entry points
- [x] T024 [P] [US3] In `tests/Symbology.Tests/LegibilityTests.fs` add the linter-invariance assertion: `Legibility.score`/`scoreAnimated` on a roster with styled labels equals the verdict on the same roster with plain labels, and is identical across grammars — styled-label presence does not change pre-attentive governance (B13/FR-012/SC-006)

### Implementation for User Story 3

- [x] T025 [US3] Confirm `render`/`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` keep their existing signatures (they thread the whole `Token`) so a styled roster renders per grammar by construction — no edit expected; if any signature touch is required, STOP (it violates FR-011) and reconcile against the contract
- [x] T026 [US3] Author the rich-text section CANONICALLY in `src/Symbology/skill/SKILL.md`: opt-in inspection-detail channel; attributes colour/weight/size; keep to a few short runs + restrained palette; do NOT impersonate faction/state pre-attentive encodings; requires the real measurer for tofu-free output; how surplus runs/width degrade; complements (never replaces) the vector sigil (FR-017)
- [x] T027 [US3] Mirror the rich-text section into `template/product-skills/fs-gg-symbology/SKILL.md` (adapted copy) and confirm `.claude/skills/fs-gg-symbology/` and `.agents/skills/fs-gg-symbology/` pointer wrappers inherit it; run `dotnet fsi scripts/check-agent-skill-parity.fsx` and confirm `critical=0 high=0` (FR-017/SC-007)

**Checkpoint**: All three stories independently functional — authoring, fit, and board/linter/skill governance.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Regenerate the moved surface baseline, prove zero drift elsewhere, and run the full validation pass.

- [x] T028 Regenerate the symbology surface baseline per the repo's surface-baseline workflow so `readiness/surface-baselines/FS.GG.UI.Symbology.txt` captures `LabelRun`, `LabelText`, the retyped `Token.Label`, and the new ctors; confirm via `git status -- readiness/surface-baselines/` that ONLY `FS.GG.UI.Symbology.*` moved and every other package baseline is unchanged (FR-015/SC-007)
- [x] T029 [P] Run the full test pass: `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` and `dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` — every migrated battery green, `RichLabelTests.fs` + the styled render-bridge tofu test + the linter-invariance assertion pass
- [x] T030 [P] Re-run the comprehensive baseline runner `dotnet fsi scripts/baseline-tests.fsx --out specs/198-symbology-rich-text-label/readiness/baseline-after.md` and diff against the Phase-1 baseline — no new reds outside the intended surface-baseline move (Package.Tests public-surface gate green after T028); ALSO confirm no new third-party dependency and no new font file were introduced (FR-016): `git status` + the `src/Symbology/Symbology.fsproj` diff show only the intended Symbology source/test/baseline/skill changes
- [x] T031 Walk quickstart.md "Per-Success-Criterion validation" (SC-001…SC-007) and record the evidence pointers (gallery scenes per grammar, tofu/presence assertions, byte-equal goldens, cross-process golden, fit/cap/empty/placeholder cases, linter-invariance, surface diff + parity report) in `specs/198-symbology-rich-text-label/readiness/`
- [x] T032 [P] Capture per-phase fs-gg feedback (process friction, generalizable-code candidates, severity) into `specs/198-symbology-rich-text-label/feedback/` per the `fs-gg-feedback-capture` skill

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the `Token.Label` retype (T004) + fixture migration (T009) are what make the solution compile, and `richLabelNodes` (T007) is what every story exercises.
- **User Stories (Phase 3–5)**: All depend on Foundational completion. Once T010 (early smoke) is green they may proceed in parallel (if staffed) or sequentially P1 → P2 → P3.
- **Polish (Phase 6)**: Depends on all desired stories. T028 (baseline regen) must precede T029/T030 so the public-surface gate sees the intended surface.

### User Story Dependencies

- **US1 (P1)**: Independent — needs only Foundational. The MVP slice.
- **US2 (P2)**: Independent — refines `richLabelNodes` fit/cap; builds conceptually on US1's run rendering but is separately testable.
- **US3 (P3)**: Independent — boards/linter/skill are additive on top of the channel; no behaviour change to US1/US2.

### Within Each User Story

- Tests are written/extended FIRST and must FAIL (or assert the new behaviour) before the impl task closes them.
- Core layout (`richLabelNodes`) is Foundational; each story tunes/asserts its slice of it.
- Story complete before moving to the next priority (or run in parallel if staffed).

### Parallel Opportunities

- Setup: T002, T003 in parallel.
- Foundational: T004 → T005/T006/T007 (same file `Symbology.fs`, sequence to avoid edit conflict) → T008; T009 (test files, parallelisable across files) after T004 makes the retype visible; T010 after T007/T008/T009.
- Once Foundational is done: US1, US2, US3 can run in parallel by different developers.
- All `[P]` test tasks within a story touch different files (or different test functions) and run in parallel: US1 = T011/T013/T014 (T012 creates the new file, do first); US2 = T017/T018/T019/T020; US3 = T023/T024.
- Polish: T029/T030/T032 in parallel after T028.

---

## Parallel Example: User Story 1

```bash
# After Foundational (Phase 2) is green, launch US1 tests together:
Task: "Create RichLabelTests.fs zero-drift battery (T012)"   # do first — creates the file
Task: "ChannelPresence styled-vs-plain differing bytes (T011)"
Task: "Determinism styled render-twice + cross-process golden (T013)"
Task: "Render-bridge styled tofu test in RenderLabelTests.fs (T014)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (baseline + pinned goldens + surface snapshot).
2. Phase 2: Foundational — types/`.fsi`, `richLabelNodes`, dispatch, fixture migration, and the **early FSI smoke** that confirms zero-drift + styled-channel + fit + safe-degenerate before any story build-out.
3. Phase 3: User Story 1.
4. **STOP and VALIDATE**: styled runs render tofu-free per grammar; plain/no-label byte-identical; styling changes bytes.
5. Demo the MVP.

### Incremental Delivery

1. Setup + Foundational → channel ready (smoke-confirmed).
2. US1 → test → demo (MVP: emphasis runs).
3. US2 → test → demo (fit/cap/safe).
4. US3 → test → demo (boards/linter/skill).
5. Polish → regenerate baseline, full validation pass.

---

## Notes

- `[P]` = different files / independent functions, no dependency on an incomplete task.
- `[Story]` label maps the task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry none.
- **Layered zero-drift is the hard constraint** (FR-002/SC-003): `None` ≡ pre-feature, `Plain` ≡ spec-197, all-default `Rich` ≡ `Plain` — the migration (T009) and dispatch (T008) keep every pinned golden green by construction; no existing assertion is weakened.
- Only the **symbology** surface baseline moves (FR-015); T003/T028/T030 prove zero drift elsewhere.
- **Fail-first ordering**: the Foundational core (`richLabelNodes`, T007) precedes the styled-run tests by design — T010 (FSI smoke) + the migrated goldens (T009) are the fail-first evidence for it; each new per-story test (T011+) must still be red before its tuning task (T015/T021/T022) closes it (Constitution I/V).
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
