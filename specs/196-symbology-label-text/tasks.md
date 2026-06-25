---
description: "Task list for Symbology Label / Glyph Text Channel (feature 196)"
---

# Tasks: Symbology Label / Glyph Text Channel

**Input**: Design documents from `/specs/196-symbology-label-text/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/symbology-label-api.md](./contracts/symbology-label-api.md), [quickstart.md](./quickstart.md)

**Tests**: INCLUDED. This is a **Tier 1** change; constitution Principle V mandates fail-before/pass-after test evidence, and every user story in the spec carries an Independent Test. Test tasks are written before the implementation they cover.

**Organization**: Tasks are grouped by user story (P1 → P2 → P3) so each is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish have no story label)
- All paths are repository-relative from `/home/developer/projects/FS.GG.Rendering/`

> **Why no "live app smoke" / "root-cause map" here.** The standing Foundational requirement targets *defect fixes against a running app*. This feature is **greenfield-additive pure scene logic** with **no GL/raster/IO** in the library, so there is no defect to reproduce and no app state to drive. The plan's analogue (and the substitute used below) is an **early FSI/test smoke** (T007) plus the **render-bridge tofu raster smoke** (T008) — both run *before* the user-story build-out and are the real confirmation the channel works, not this document's narrative.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture the pre-change baseline and confirm the affected projects build.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project so pre-existing reds are known up front and not mistaken for regressions at merge. Use the discovery-based runner (it globs `*.Tests.fsproj`, including `tests/Package.Tests` and `samples/**/*.Tests` which the solution omits).

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/196-symbology-label-text/readiness/baseline.md` (runs EVERY test project; record the full red/green set — pre-existing reds, e.g. the carried `Package.Tests`/`ControlsGallery.Tests` reds noted in spec 195, are flagged here, not at merge)
- [X] T002 Confirm the affected projects build clean before edits: `dotnet build src/Symbology/Symbology.fsproj -c Debug` and `dotnet build src/Symbology.Render/Symbology.Render.fsproj -c Debug`
- [X] T003 Capture the current label-free goldens to protect: record the `token defaultToken` / `gallery` / `filmstrip` canonical-byte SHAs already pinned in `tests/Symbology.Tests/DeterminismTests.fs` (these MUST stay byte-unchanged — FR-002/SC-003)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Draft the public seam (`.fsi`) first, make the package compile with the new field defaulted, and prove the surface is usable before any story build-out.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

- [X] T004 Draft the public-surface seam FIRST: add `Label: string option` as the final field of the `Token` record in `src/Symbology/Symbology.fsi`, with the doc-comment from [contracts/symbology-label-api.md](./contracts/symbology-label-api.md); leave every existing `val`/type signature UNCHANGED
- [X] T005 Set `Label = None` in `defaultToken` in `src/Symbology/Symbology.fs`; add a no-op internal `labelNode` stub (returns `Scene.group []`) wired into `drawSymbol`/`drawBadge`/`drawRing` so the package compiles with the new field but emits no label yet (keeps label-free output byte-identical at this checkpoint)
- [X] T006 Build the solution and regenerate-check the surface seam compiles: `dotnet build FS.GG.Rendering.slnx -c Debug` succeeds with the new field present
- [X] T007 **Early FSI/test smoke (STANDING substitute for the live smoke)**: load the public surface in FSI per [quickstart.md](./quickstart.md) and confirm, on a hand-built `Token`, that (a) `token`/`badge`/`ring`/`render g` all run end-to-end and return a non-empty `Scene`, (b) a `Label = None` token's `SceneCodec.export(...).CanonicalBytes` equals the pre-feature bytes, and (c) a degenerate (`R <= 0`) labelled token returns the placeholder without throwing — BEFORE building US1
- [X] T008 **Render-bridge raster smoke**: rasterise a hand-built token through `Symbology.Render.toPng` (which installs the real measurer via `SkiaViewer.Fonts.installMeasurementSeam`) and confirm a non-blank PNG is written — establishes the tofu test harness in `tests/Symbology.Render.Tests/` before the label node exists

**Checkpoint**: Surface seam drafted, package compiles label-free byte-identical, render harness confirmed — user-story implementation can begin.

---

## Phase 3: User Story 1 — Put a legible identity label on a symbol (Priority: P1) 🎯 MVP

**Goal**: A `Token` carrying `Some label` renders the label — drawn with real glyphs (tofu-free) — in every grammar (Token/Badge/Ring); a `Label = None` token renders byte-identically to today.

**Independent Test**: Build tokens with and without a label, render each in each grammar through the headless bridge; confirm (a) a labelled raster contains the label's glyphs rendered non-tofu, (b) an unlabelled scene is byte-identical to the pre-feature symbol, (c) two tokens differing only in label produce observably different output.

### Tests for User Story 1 (write first; must FAIL before T012 — they go green once `labelNode` emits the glyph run; T013–T015 then refine per-grammar placement)

- [X] T009 [P] [US1] In `tests/Symbology.Tests/DeterminismTests.fs`: assert a labelled token rendered twice is byte-equal (`SceneCodec.export(...).CanonicalBytes`); **pin a labelled-token canonical-byte SHA** as the cross-process determinism proxy (SC-004 "separate process" — a fixed SHA computed in a prior process trips on any process-dependent drift, the same proxy used for the label-free goldens per research.md R7); and assert the existing `token`/`gallery`/`filmstrip` `Label=None` golden SHAs are **unchanged** (FR-002/FR-008/SC-003/SC-004)
- [X] T010 [P] [US1] In `tests/Symbology.Tests/ChannelPresenceTests.fs`: assert two tokens differing **only in `Label`** produce differing canonical bytes, in `token`, `badge`, and `ring` (C-03; US1 acceptance #3)
- [X] T011 [P] [US1] In `tests/Symbology.Render.Tests/` (extend the harness from T008): rasterise a labelled token through `Render.toPng` under the installed measurer and assert the label's glyph run is **non-tofu** (`Missing = false` for covered glyphs); a roster of distinct labels is mutually distinguishable (FR-004/SC-002)

### Implementation for User Story 1

- [X] T012 [US1] In `src/Symbology/Symbology.fs`: implement the internal `labelNode` helper — emit the label as `Scene.glyphRunProof` at a basic per-grammar placement, returning `Scene.group []` (no node) when `Label = None` (replaces the T005 stub; the empty/whitespace and full-fit refinements land in US2)
- [X] T013 [US1] In `src/Symbology/Symbology.fs`: site the label region in `drawSymbol` (Token grammar) — screen-aligned caption below the body belly, beneath the health arc, clear of the sigil/shield (per [data-model.md](./data-model.md))
- [X] T014 [US1] In `src/Symbology/Symbology.fs`: site the label region in `drawBadge` — screen-aligned caption band along the bottom inner edge of the frame, clear of the speed pips/health bar/heading pip
- [X] T015 [US1] In `src/Symbology/Symbology.fs`: site the label region in `drawRing` — screen-aligned caption beneath the sigil inside the inner disc, clear of the outer ring/health arc/speed beads/heading needle
- [X] T016 [US1] Run US1 tests green: `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` and `dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj`; confirm T009 goldens still green (zero drift)

**Checkpoint**: A label renders tofu-free in all three grammars; unlabelled output is byte-identical. MVP is demonstrable.

---

## Phase 4: User Story 2 — Label fits the symbol without clipping or overflow (Priority: P2)

**Goal**: An overlong label is fitted (shrink and/or ellipsis-truncate) to its region using real text measurement so it never overflows the footprint, clips mid-glyph, or overlaps another channel; empty/whitespace and degenerate-with-label are safe.

**Independent Test**: Render a token with a label far wider than its region; confirm the fitted label measures within the region footprint and is not cut mid-glyph; confirm an empty/whitespace label produces no label and no exception; confirm a degenerate (`R <= 0`) token with a label produces the placeholder.

### Tests for User Story 2 (write first; must FAIL before T020–T022)

- [X] T017 [P] [US2] Create `tests/Symbology.Tests/LabelTests.fs` and register it in `tests/Symbology.Tests/Symbology.Tests.fsproj`: assert (a) an overlong label, once fitted, measures (`Scene.measureTextResolved`) **≤ the grammar's region width** and the fitted text is a clean prefix + ellipsis (no mid-glyph cut), in each grammar (FR-005/SC-005), (b) an empty/whitespace-only label emits **no** label node and raises **no** exception (FR-006), and (c) **pure-fallback path (no measurer installed)** — a labelled token built in this pure test project (no real measurer) emits a label glyph-run node and does **not** throw, confirming the library is measurer-optional (FR-009/C-09)
- [X] T018 [P] [US2] In `tests/Symbology.Tests/PlaceholderTests.fs`: assert a degenerate (`R <= 0`) token **with** a `Some label` renders the existing visible placeholder and never throws — placeholder rule wins over the label (FR-007/SC-005)

### Implementation for User Story 2

- [X] T019 [US2] In `src/Symbology/Symbology.fs`: implement the internal `fitLabel` helper — trim the string; return "no label" for empty/whitespace; else measure with `Scene.measureTextResolved`, **shrink the font toward a floor**, then **ellipsis-truncate at a measured glyph boundary** (re-measuring the candidate incl. the ellipsis) so the result is always within the region width (FR-005; research.md R3)
- [X] T020 [US2] In `src/Symbology/Symbology.fs`: route every grammar's `labelNode` through `fitLabel` so empty/whitespace ⇒ no node and overlong ⇒ fitted (FR-005/FR-006)
- [X] T021 [US2] In `src/Symbology/Symbology.fs`: ensure the degenerate (`R <= 0`) placeholder path takes precedence over `labelNode` in `drawSymbol`/`drawBadge`/`drawRing` (no label drawn on a placeholder; no throw) (FR-007)
- [X] T022 [US2] Run US2 tests green: `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj`; re-confirm US1 tests and the T009 goldens remain green

**Checkpoint**: Labels fit trustworthily; empty/degenerate inputs are safe. US1 + US2 both pass independently.

---

## Phase 5: User Story 3 — Labels on review boards, governed unchanged (Priority: P3)

**Goal**: A labelled roster renders reproducibly on a review board (gallery/filmstrip) in a selected grammar; the legibility linter's verdict stays grammar-independent and unchanged by labels; the design-loop skill documents the label channel and passes skill-parity.

**Independent Test**: Render a labelled roster as a gallery in each grammar (reproducible per grammar); confirm the linter's report for the roster is identical regardless of grammar and unchanged by label presence; confirm the skill documents the label channel and passes the skill-parity check.

### Tests for User Story 3 (write first; must FAIL/where applicable before T026–T027)

- [X] T023 [P] [US3] In `tests/Symbology.Tests/GalleryTests.fs`: assert a labelled roster rendered via `galleryIn g` is byte-reproducible per grammar (render twice, equal canonical bytes) for `g ∈ {Token, Badge, Ring}` (FR-010)
- [X] T024 [P] [US3] In `tests/Symbology.Tests/LegibilityTests.fs`: assert a fixed roster's `Legibility.score` `Report` is **identical** with and without labels, and identical across all three grammars — label presence does not change pre-attentive-channel governance (FR-011/SC-006)

### Implementation for User Story 3

- [X] T025 [US3] Confirm (and test-pin) that `gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn` need **no signature change** — they already thread the whole `Token`, so a labelled roster flows through unchanged (FR-010/FR-012); no edit expected beyond the tests in T023
- [X] T026 [US3] Edit the canonical `src/Symbology/skill/SKILL.md`: add a label section — the label is an **opt-in inspection-detail identity channel** (when to use it; requires the real measurer for tofu-free output; keep strings short; complements, never replaces, the vector sigil; not in the legibility capacity table) (FR-015)
- [X] T027 [P] [US3] Mirror the label section into `template/product-skills/fs-gg-symbology/SKILL.md` (the `.claude/` and `.agents/` skill trees inherit via their pointer wrappers)
- [X] T028 [US3] Run skill-parity: `dotnet fsi scripts/check-agent-skill-parity.fsx` → critical=0, high=0 (SC-007); run `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` for US3 tests green

**Checkpoint**: Boards render labelled rosters reproducibly; linter unchanged; skill documented and in parity. All three stories pass independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Tier-1 surface baseline, full-suite validation, and feedback capture.

- [X] T029 Regenerate the surface baseline: `dotnet build FS.GG.Rendering.slnx -c Debug` then `dotnet fsi scripts/refresh-surface-baselines.fsx`; `git diff readiness/surface-baselines/` MUST show **only** `FS.GG.UI.Symbology.txt` changed (the `Token` record gains the `Label` field), with **zero drift** on every other baseline (FR-013/SC-007). Also confirm `git diff src/Symbology/Symbology.fsproj` shows **no new `<PackageReference>`/`<ProjectReference>`** (and no new font asset) — the label consumes only the already-referenced `FS.GG.UI.Scene` text vocabulary, making the "pure scene-only, no new GL/raster/IO/font dependency" guarantee observable in the diff (FR-014)
- [X] T030 Run the full quickstart validation ([quickstart.md](./quickstart.md)) and record evidence in `specs/196-symbology-label-text/readiness/quickstart-validation.md`; confirm the `token`/`gallery`/`filmstrip` goldens, the render-bridge tofu test, and all batteries are green, with no new reds beyond the T001 baseline
- [X] T031 Re-run the comprehensive test baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against T001 — confirm the only deltas are the new/extended green symbology tests; no regressions in other projects
- [X] T032 [P] Capture per-phase fs-gg-ui / Spec Kit feedback into `specs/196-symbology-label-text/feedback/` via the `fs-gg-feedback-capture` skill (process friction, generalizable-code candidates, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup; **blocks all user stories** (the `.fsi` seam + compiling default + smoke must land first).
- **User Stories (Phase 3–5)**: all depend on Foundational. US2 depends on US1 (fit refines the basic placement); US3 depends on US1 (boards/linter need a labelled token to render) but not on US2.
- **Polish (Phase 6)**: depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: after Foundational. The MVP — basic legible label in every grammar, opt-in zero-drift.
- **US2 (P2)**: after US1 (refines `labelNode` with `fitLabel`; same file).
- **US3 (P3)**: after US1 (boards + linter invariance + skill docs); independent of US2.

### Within Each User Story

- Tests are written first and must FAIL before the implementation they cover.
- `labelNode`/`fitLabel` (helpers) before per-grammar siting that calls them.
- All `src/Symbology/Symbology.fs` edits are in **one file** → sequential (not `[P]`); cross-file tasks (separate test files, the mirror SKILL.md) are `[P]`.

### Parallel Opportunities

- US1 tests T009/T010/T011 are in three different files → `[P]` together.
- US2 tests T017/T018 are in two different files → `[P]` together.
- US3 tests T023/T024 are in two different files → `[P]` together; T027 (mirror skill) is `[P]` with T026's follow-up.
- T032 (feedback) is `[P]` with the rest of Polish.

---

## Parallel Example: User Story 1

```bash
# Launch the three US1 test tasks together (different files, no shared edits):
Task: "T009 DeterminismTests — labelled byte-stable + Label=None goldens unchanged"
Task: "T010 ChannelPresenceTests — label-differ bytes differ across grammars"
Task: "T011 Symbology.Render.Tests — labelled raster is non-tofu under the installed measurer"

# Then implement the Symbology.fs edits SEQUENTIALLY (same file):
#   T012 labelNode → T013 Token siting → T014 Badge siting → T015 Ring siting → T016 run green
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (baseline + goldens captured).
2. Phase 2: Foundational (`.fsi` seam, compiling `Label=None` default, FSI smoke, render harness).
3. Phase 3: US1 — basic legible label in every grammar, tofu-free, opt-in byte-identical.
4. **STOP & VALIDATE**: US1 tests + render-bridge tofu test green; goldens unchanged.
5. Demo: a disambiguated roster (e.g. eight infantry variants with callsigns).

### Incremental Delivery

1. Setup + Foundational → seam ready.
2. US1 → tofu-free label in all grammars (MVP).
3. US2 → trustworthy fit (shrink/ellipsis), safe empty/degenerate.
4. US3 → boards reproducible, linter unchanged, skill documented.
5. Polish → surface baseline regenerated (zero drift elsewhere), full-suite validation, feedback.

---

## Notes

- `[P]` = different files, no dependency on an incomplete task.
- All `src/Symbology/Symbology.fs` label edits share one file → keep them sequential.
- Verify each test FAILS before implementing the code it covers (constitution V).
- The label-free goldens (`token`/`gallery`/`filmstrip` SHAs) are the zero-drift tripwire — never regenerate them to "fix" a red; a red there means a real drift (FR-002).
- Tofu-free is verified by **real** rasterisation through `Symbology.Render` (T011), never asserted from pure unit tests alone.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
