---
description: "Task list for Symbology Multi-line / Paragraph Label Channel"
---

# Tasks: Symbology Multi-line / Paragraph Label Channel

**Input**: Design documents from `/specs/197-symbology-multiline-label/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/symbology-multiline-label-api.md ✓

**Tests**: Test tasks ARE included — this is a Constitution-V Tier-1 behavioural change proven by fail-before / pass-after Expecto semantic tests plus a real render-bridge tofu raster (plan.md "Testing"; research.md R8/R9).

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) so each slice is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (user-story phases only)
- Exact file paths are included in every task

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). The change is **internal to the existing pure package** `src/Symbology/` (no `.fsi` edit); tests extend the existing `tests/Symbology.Tests/` and `tests/Symbology.Render.Tests/` projects. No new project, no new font files (FR-013/FR-014/FR-016).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build and capture the no-regression baseline before touching code.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T002 MUST run **every** test project via the discovery-based runner so pre-existing reds are known up front and not mistaken for regressions at merge. Do NOT hand-pick a subset: the solution run deliberately omits `tests/Package.Tests` (release-only public-surface gate) and `samples/**/*.Tests` (package-feed consumers) — exactly where Feature 175's surprises hid.

- [X] T001 Confirm branch `197-symbology-multiline-label` is checked out and the solution builds clean: `dotnet build FS.GG.Rendering.slnx -c Debug` (expect 0 warnings / 0 errors)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/197-symbology-multiline-label/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; flag any pre-existing reds here)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the zero-surface-delta seam and land the shared internal plumbing (`labelNodes`/`withLabel`/per-grammar budgets) that all three user stories build on, then smoke it.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Early smoke run (STANDING, do not omit).** T005 is the live-smoke mandate's analogue for this pure-scene feature (plan.md's standing-assumption note): once the plumbing exists, drive the **public surface** in FSI/test and confirm zero-drift + safe-degenerate **before** building out US1/US2/US3. Treat the plan's narrative as **unverified** until this smoke passes — layered byte-identity is a property to observe, not assume.

- [X] T003 Confirm **zero `.fsi` / surface delta**: verify `src/Symbology/Symbology.fsi` is unchanged (`Label : string option` already present, no new val) and `git status --porcelain readiness/surface-baselines/` is empty; record "no public surface added" per FR-013 in `specs/197-symbology-multiline-label/readiness/baseline.md`
- [X] T004 Land the shared internal plumbing in `src/Symbology/Symbology.fs`: rename/convert `labelNode` → list-returning `labelNodes : centerX -> baselineY -> regionWidth -> baseSize -> lineHeight -> budget -> label:string option -> Scene list`; change `withLabel` to take `Scene list` (`Scene.group (channelNodes @ lineNodes)`, `[]`≡no-label, `[one]`≡196 single-line); wire per-grammar line budget + line-height into `tokenLabelNode`/`badgeLabelNode`/`ringLabelNode` (Token ≤3, Badge ≤2, Ring ≤2; line-height = `TextMetrics.Height`, fallback `baseSize*1.15`); add a **stub** `wrapLabel` that returns the single trimmed line only (multi-line not yet functional — keeps one-line byte-identity, compiles)
- [X] T005 **Early smoke run**: load the public surface in FSI per `quickstart.md` §2 and confirm by construction — `Label = None` and one-line `"HMR-7"` byte-identical to their spec-196 renders, a degenerate `R<=0` + label returns the placeholder without throwing, no measurer required/installed by the pure path; record evidence before proceeding

**Checkpoint**: Surface confirmed unchanged, plumbing compiles, zero-drift + safe-degenerate observed live — user-story implementation can begin.

---

## Phase 3: User Story 1 - Author a legible multi-line identity label (Priority: P1) 🎯 MVP

**Goal**: A label carrying more than one line stacks within each grammar's label region, every line drawn tofu-free; a single-line label stays byte-identical to spec 196 and a no-label token byte-identical to the pre-feature symbol (layered zero-drift).

**Independent Test**: Build no-line / single-line / `\n`-bearing labels, render in each grammar through the render bridge; confirm (a) multi-line stacks with non-tofu lines, (b) single-line == spec-196 bytes & no-label == pre-feature bytes, (c) single-line vs `\n` produce observably different output.

### Tests for User Story 1 (write FIRST, ensure they FAIL before T011) ⚠️

- [X] T006 [P] [US1] In `tests/Symbology.Tests/DeterminismTests.fs`: assert the existing `0dda10bd…` (no-label), `6710215b…` (`"HMR-7"` one-line), gallery/filmstrip **and motion (`animate`/`animateIn`)**/badge/ring goldens stay **byte-identical** (zero-drift guards, unchanged assertions — covers FR-012 motion; if motion goldens live in `MotionTests.fs`/`FilmstripTests.fs`, assert there instead), and ADD a same-process render-twice byte-equal case **plus a NEW pinned multi-line golden that serves as the cross-process anchor** for a `\n`-bearing `Token` (pinned bytes ⇒ reproducible across separate processes, matching the `6710215b…` pattern — satisfies SC-004 same-process AND separate-process)
- [X] T007 [P] [US1] In `tests/Symbology.Tests/ChannelPresenceTests.fs`: assert a single-line label vs the same text expressed with an embedded `\n` produce **differing** canonical bytes (channel presence), neither raises
- [X] T008 [P] [US1] Create `tests/Symbology.Tests/MultilineLabelTests.fs` and register it in `tests/Symbology.Tests/Symbology.Tests.fsproj`: assert a `\n`-bearing label emits **N stacked** glyph-run nodes with the first line at spec-196's exact baseline, and a one-line-fitting label is **byte-identical** to its spec-196 single-line render (zero-drift anchor); ADD an explicit **pure-path assertion (FR-009)** — with **no measurer installed**, a multi-line-labelled `Token` still emits its line nodes deterministically and **does not throw** (the pure library never installs/requires a measurer)
- [X] T009 [P] [US1] In `tests/Symbology.Render.Tests/RenderLabelTests.fs`: rasterise a **multi-line** labelled `Token` through `Render.toPng` under the installed real measurer and assert **every** line's glyph run is non-tofu (`Missing = false` / `TofuCount = 0`) and the output is non-blank (FR-004/SC-002)

### Implementation for User Story 1

- [X] T010 [US1] In `src/Symbology/Symbology.fs`, implement the `\n`/`\r\n` split + **downward stacking** in `wrapLabel`/`labelNodes`: split on hard breaks into segments, trim, emit one `Scene.glyphRunProof` per segment via the existing `fitLabel`, first line at the 196 baseline and each subsequent line at `+ lineHeight*i`; make T006–T009 pass while every zero-drift golden stays green

**Checkpoint**: Multi-line stacking works in all three grammars, tofu-free at the edge, with no-label and one-line cases byte-identical — US1 is independently demonstrable.

---

## Phase 4: User Story 2 - Multi-line label fits the region without clipping or overflow (Priority: P2)

**Goal**: Long lines soft-wrap at measured whitespace boundaries, the drawn line count is capped per grammar, surplus degrades via a trailing `…`, and the block never clips mid-glyph or overflows into adjacent channels; empty/whitespace/blank-lines-only and degenerate-token-with-label are safe.

**Independent Test**: Render a label far wider and more numerous than its region; confirm each drawn line ≤ region width (real measurer), line count ≤ budget, surplus ellipsised not overflowed, no mid-glyph clip; an empty/whitespace/blank-lines label produces no label and no exception; a degenerate token + label yields the placeholder.

### Tests for User Story 2 (write FIRST, ensure they FAIL before T013) ⚠️

- [X] T011 [P] [US2] In `tests/Symbology.Tests/MultilineLabelTests.fs`: assert a too-wide **whitespace** label wraps to multiple lines each ≤ region width; an over-budget label is **capped** to the grammar budget with the last drawn line ending in `…`; interior blank/whitespace segments are **collapsed** (`"A\n\n\nB"` ⇒ two lines, `"\n  \n"` ⇒ no label); a single unbroken word wider than the region degrades to one fitted line (no wrap point, no overflow)
- [X] T012 [P] [US2] In `tests/Symbology.Tests/PlaceholderTests.fs`: assert a degenerate `Token` (`R <= 0`) carrying a multi-line label renders the existing **visible placeholder** and does not throw (placeholder rule wins over the label)

### Implementation for User Story 2

- [X] T013 [US2] In `src/Symbology/Symbology.fs`, complete `wrapLabel`: greedy **whitespace** word-wrap each segment to the region width (measured via `Scene.measureTextResolved` at base size, never break inside a word), **cap** to the per-grammar budget, **ellipsis** the last drawn line when content is dropped (re-fit so the ellipsised line ≤ region), and drop empty/whitespace segments deterministically; rely on the existing per-line `fitLabel` for the ≤-region / no-mid-glyph-clip guarantee — make T011–T012 pass

**Checkpoint**: The multi-line block is fitted, capped, and safe in every degenerate case while US1 behaviour stays green — US1+US2 both independently testable.

---

## Phase 5: User Story 3 - Multi-line labels on review boards, governed unchanged (Priority: P3)

**Goal**: Multi-line-labelled rosters render reproducibly on gallery/filmstrip boards (no signature change), the legibility linter's verdict is unchanged and grammar-independent, and the design-loop skill documents the channel.

**Independent Test**: Render a multi-line roster as a gallery per grammar (reproducible); confirm the linter `Report` is identical with vs without labels and grammar-independent; confirm the skill documents the multi-line label and passes the parity check.

### Tests for User Story 3 (write FIRST, ensure they FAIL before T016/T017) ⚠️

- [X] T014 [P] [US3] In `tests/Symbology.Tests/GalleryTests.fs`: assert `galleryIn`/`filmstripIn` over a **multi-line-labelled roster** renders per grammar and is byte-reproducible under a fixed measurement provider, with **no signature change** to the board/motion entry points (FR-010/SC-001)
- [X] T015 [P] [US3] In `tests/Symbology.Tests/LegibilityTests.fs`: assert `Legibility.score`/`scoreAnimated` returns an **identical `Report`** for a roster with vs without (single- or multi-line) labels, and that the verdict is grammar-independent (FR-011/SC-006)

### Implementation / Documentation for User Story 3

- [X] T016 [US3] Author the multi-line section in the canonical skill `src/Symbology/skill/SKILL.md`: opt-in inspection-detail identity channel, requires the real measurer for tofu-free output, keep to a few short lines, how surplus width/lines degrade (wrap → cap → ellipsis), complements (never replaces) the vector sigil (FR-015)
- [X] T017 [US3] Mirror the multi-line section into `template/product-skills/fs-gg-symbology/SKILL.md` (pointer wrappers under `.claude/skills/fs-gg-symbology/` and `.agents/skills/fs-gg-symbology/` inherit it); run `dotnet fsi scripts/check-agent-skill-parity.fsx` and confirm `critical=0 high=0`

**Checkpoint**: Boards reproduce multi-line rosters, governance is unchanged, and the skill is documented and parity-clean — all three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Per-SC validation and final no-regression / no-surface-drift confirmation.

- [X] T018 [P] Run the full `quickstart.md` §4 per-Success-Criterion validation (SC-001…SC-007) and record the results
- [X] T019 Confirm **zero surface-baseline drift**: `git status --porcelain readiness/surface-baselines/` is empty and record "baselines unchanged — no public surface added" per FR-013/SC-007
- [X] T020 Re-run the full no-regression baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against the T002 snapshot — confirm no project regressed (every prior green still green; new multi-line battery + render tofu test green)
- [X] T021 [P] Capture per-phase fs-gg-symbology / Spec Kit feedback into `specs/197-symbology-multiline-label/feedback/` via the `fs-gg-feedback-capture` skill

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories** — the `labelNodes`/`withLabel`/budget plumbing (T004) is the shared seam every story builds on.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 is the MVP; US2 builds on US1's stacking; US3 is additive on top.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Independent after Foundational. Delivers the core multi-line stacking + zero-drift + tofu-free MVP.
- **US2 (P2)**: Builds on US1's stacking (refines the same `wrapLabel`/`Symbology.fs`) to add wrap/cap/ellipsis/safety. Independently testable via its own battery.
- **US3 (P3)**: Additive on US1/US2 — boards/linter/skill. Independently testable.

### Within Each User Story

- Tests are written and FAIL before the implementation task in that story.
- The implementation tasks T004 → T010 → T013 all edit `src/Symbology/Symbology.fs`, so they are **sequential** (not [P]) and ordered Foundational → US1 → US2.

### Parallel Opportunities

- US1 test tasks **T006, T007, T008, T009** are all [P] — different files (DeterminismTests, ChannelPresenceTests, MultilineLabelTests, RenderLabelTests).
- US2 test tasks **T011, T012** are [P] — different files (MultilineLabelTests vs PlaceholderTests). *(Note: T008 and T011 both touch MultilineLabelTests.fs; run T008 first or coordinate so the two are not edited simultaneously.)*
- US3 test tasks **T014, T015** are [P] — GalleryTests vs LegibilityTests.
- Polish **T018, T021** are [P].

---

## Parallel Example: User Story 1

```bash
# Launch all four US1 test tasks together (distinct files), then implement T010:
Task: "DeterminismTests.fs — zero-drift goldens unchanged + new multi-line golden"   # T006
Task: "ChannelPresenceTests.fs — single-line vs \n differ"                            # T007
Task: "MultilineLabelTests.fs (new) — \n stacking + one-line byte-identity"           # T008
Task: "RenderLabelTests.fs — every multi-line line non-tofu, non-blank"               # T009
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (Setup) and Phase 2 (Foundational — including the **early smoke run** T005 that confirms zero-drift + safe-degenerate against the live public surface).
2. Complete Phase 3 (US1): multi-line stacking, tofu-free, byte-identical no-label/one-line.
3. **STOP and VALIDATE**: US1 independently — render no-line/one-line/`\n` in each grammar, confirm goldens green.
4. Demo: a designer can now show a callsign over a code.

### Incremental Delivery

1. Setup + Foundational → seam ready (zero surface drift confirmed).
2. US1 → multi-line stacking MVP → validate → demo.
3. US2 → fitted/capped/safe → validate → demo.
4. US3 → boards/linter/skill → validate → demo.
5. Polish → per-SC validation + zero-drift + no-regression confirmation.

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- All `Symbology.fs` implementation tasks (T004, T010, T013) share one file → sequential.
- Verify each story's tests FAIL before its implementation task (Constitution V: fail-before / pass-after).
- The hard constraint throughout is **layered zero-drift**: no-label and one-line-fitting labels stay byte-identical to the spec-192/196 goldens — never weaken or delete an existing assertion.
- No `.fsi` edit, no baseline regeneration, no new font files (FR-013/FR-014/FR-016).
- Commit after each task or logical group.
