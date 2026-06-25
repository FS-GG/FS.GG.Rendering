---
description: "Task list for Symbology Full Rich-Text Layout (paragraph layout + decoration/slant/tracking)"
---

# Tasks: Symbology Full Rich-Text Layout (paragraph layout + decoration/slant/tracking)

**Input**: Design documents from `/specs/199-rich-text-layout/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/symbology-rich-text-layout-api.md ✓, quickstart.md ✓, constitution.md ✓

**Tests**: INCLUDED — the constitution (Principle I: Spec → FSI → Semantic Tests → Implementation; Principle V: Test Evidence Mandatory) and the plan require fail-before/pass-after semantic tests over the public surface. Test tasks are first-class and ordered before implementation within each story.

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- All paths are repo-relative from `/home/developer/projects/FS.GG.Rendering/`

## Path Conventions

This is a multi-project F# solution (`FS.GG.Rendering.slnx`). The change is internal to the existing
`src/Symbology/` library + its curated `.fsi`, plus additive tests in existing test projects and the
mirrored skill trees. No new project, no new sample, no new font file (FR-018/FR-019).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build graph and pin the no-regression baseline before any change.

- [X] T001 Confirm the affected projects build clean on the branch (no edits yet): `dotnet build src/Symbology/Symbology.fsproj`, `dotnet build src/Symbology.Render/Symbology.Render.fsproj`, `dotnet build tests/Symbology.Tests/Symbology.Tests.fsproj`, `dotnet build tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` (no new project is added — change lands in the existing `Symbology.fsproj` per plan Structure Decision)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/199-rich-text-layout/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds incl. any stale surface/sample pins are flagged HERE, not discovered at merge)
- [X] T003 Record the pre-change symbology surface baseline snapshot for later diff: `git status -- readiness/surface-baselines/` is clean and note current `readiness/surface-baselines/FS.GG.UI.Symbology.txt` content as the comparison point (only this file may move; `FS.GG.UI.Symbology.Render.txt` and all others MUST stay byte-stable — FR-017/SC-007)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Draft the public surface first (Constitution I — FSI before implementation), make the tree
compile against it with zero-drift defaults, and prove the channel works with an early smoke BEFORE building
out any user story.

**⚠️ CRITICAL**: No user-story work (US1/US2/US3) can begin until this phase is complete.

> **⚠️ Early FSI/test smoke (STANDING, do not omit).** This is a greenfield-additive completion, not a defect
> fix, so there is no root-cause map; per plan §"Standing assumption", the analogue of the live-smoke mandate
> is an **early FSI/public-surface smoke** (T007 below). Treat that smoke — and later the render-bridge tofu
> test (T026/T040) — NOT this plan's narrative, as the confirmation the channel actually works. Pull it
> forward; do not defer evidence to the per-story checkpoints.

- [X] T004 Draft the public-surface delta in `src/Symbology/Symbology.fsi` FIRST (Constitution I/II, FR-017, contract §A): add 4 optional fields to `LabelRun` (`Italic: bool option`, `Underline: bool option`, `Strike: bool option`, `Tracking: float option`); add `type LabelAlign = Leading | Center | Trailing | Justify`; add `type LabelParagraph = { Runs: LabelRun list; Align: LabelAlign }`; add `LabelText.Laid of LabelParagraph list` case; add ctors `val paragraph: LabelRun list -> LabelParagraph`, `val align: LabelAlign -> LabelRun list -> LabelParagraph`, `val laidLabel: LabelParagraph list -> LabelText`. Keep `Plain`/`Rich`, `plainLabel`/`run`/`richLabel`, `token`/`render`/`gallery`/`galleryIn`/`filmstrip`/`filmstripIn`/`animate`/`animateIn`/`badge`/`ring` and all other signatures byte-stable
- [X] T005 Make `src/Symbology/Symbology.fs` compile against the new `.fsi` with zero-drift defaults: add the 4 `None`-defaulted fields to the `LabelRun` record + extend `run`/`richLabel` ctors to default them `None`; add `LabelAlign`/`LabelParagraph`/`Laid`; implement ctors `paragraph` (`Align = Center`), `align`, `laidLabel`; widen `isDefaultRun` (`Symbology.fs:434`) to ALSO require `Italic`/`Underline`/`Strike` unset-or-false and `Tracking` unset-or-`0.0`; add a temporary `labelDispatch` arm for `Laid` that reduces to the existing `Rich`/`Plain` path (placeholder — real `laidLabelNodes` lands in US2) so the case is total and zero-drift
- [X] T006 [P] Update every raw `LabelRun` record literal (no `with`-copy) in `src/Symbology/` and `tests/Symbology.Tests/` to include the four new `None` fields — value-preserving, additive only (fixtures built via `Symbology.run` + `with`-copy are unaffected; only literal constructions need the new fields). Confirm all four projects still build
- [X] T007 **Early FSI/public-surface smoke** (the live-smoke analogue; quickstart §"Early FSI smoke"): through the PUBLIC surface (`Symbology.token`/`render` + `SceneCodec.export(...).CanonicalBytes`) confirm BEFORE any story — (1) `Some (Plain "HMR-7")` still equals the pinned spec-197 golden and `Label = None` equals the pre-feature golden; (2) `Some (Rich [ Symbology.run "HMR-7" ])` all-default is byte-identical to that `Plain` (B3); (3) `Some (laidLabel [ paragraph [ Symbology.run "HMR-7" ] ])` single `Center` all-default paragraph is byte-identical to that `Rich`/`Plain` (B4 — default = 198 flow); and that constructing a styled/aligned token does not throw. Record the FSI session output as evidence
- [X] T008 [P] Scaffold the new paragraph-layout battery file `tests/Symbology.Tests/LaidLabelTests.fs` (empty Expecto `testList "LaidLabel"`) and register it in `tests/Symbology.Tests/Symbology.Tests.fsproj` (compile order before `Program.fs`); confirm the project still builds and the (empty) list is discovered
- [X] T009 Regenerate the symbology surface baseline now the `.fsi` is final-shape (FR-017): run the repo surface-baseline workflow, then `git status -- readiness/surface-baselines/` MUST show ONLY `FS.GG.UI.Symbology.txt` changed (new `LabelRun` fields, `LabelAlign`, `LabelParagraph`, `Laid`, ctors) and `FS.GG.UI.Symbology.Render.txt` + every other baseline byte-stable. Final verification is re-run in Polish (T042)

**Checkpoint**: Public surface drafted + baseline regenerated, tree compiles, early smoke proves layered
zero-drift (B1–B4) and no-throw construction — user stories can now begin (US1 first; US2 after US1's run
emission exists; US3 after US1+US2).

---

## Phase 3: User Story 1 - Full per-run typography (italic / underline / strike / tracking) (Priority: P1) 🎯 MVP

**Goal**: Each `LabelRun` can carry `Italic` (synthetic slant), `Underline`, `Strike`, and `Tracking`
(letter-spacing) on top of colour/weight/size, drawn in reading order with real glyphs (tofu-free) in every
grammar, riding the existing `LabelText.Rich` path. A run that sets none of the new attributes is
byte-identical to spec 198.

**Independent Test**: Render `Token`s carrying (a) no label, (b) a 198-era styled label with no new attrs,
(c) a label whose runs set italic/underline/strike/tracking — in each grammar through the render bridge;
confirm decorated runs draw their slant/decoration/tracking with real glyphs (non-tofu), the no-new-attr
label is byte-identical to spec 198, and same-chars-different-decoration produce different bytes.

### Tests for User Story 1 (write FIRST, ensure they FAIL before T016–T022)

> **NOTE**: Each new assertion must be at least as strong as what it adds (Constitution V — no existing
> assertion weakened or deleted). All-default runs MUST stay byte-identical to the pinned 198 goldens.

- [X] T010 [P] [US1] In `tests/Symbology.Tests/RichLabelTests.fs` add the per-run typography presence battery: a run with `Italic = Some true` (then `Underline`, `Strike`, `Tracking = Some t`) produces DIFFERENT `SceneCodec` canonical bytes from the same characters with the attribute unset, in each grammar; neither raises (B5/B6, SC-002)
- [X] T011 [P] [US1] In `tests/Symbology.Tests/RichLabelTests.fs` add the all-default ≡ 198 byte-identity assertion: a `Rich` run that sets the new attrs to `None`/`Some false`/`Some 0.0` is byte-identical to the equivalent spec-198 run and to the joined `Plain` (B3, SC-003) — extend, do not weaken, the existing 198 identity test
- [X] T012 [P] [US1] In `tests/Symbology.Tests/RichLabelTests.fs` add the tracking-in-measurement assertion: a run with `Tracking = Some t` (t>0) widens the measured run width so wrap/fit treat it as wider (e.g. content that fit on one line at t=0 wraps or shrinks at t>0), and tracking never pushes a drawn segment past the region (B12, FR-007)
- [X] T013 [P] [US1] In `tests/Symbology.Tests/RichLabelTests.fs` add the decoration-follows-wrapped-geometry assertion: an underlined/struck run that wraps gets a decoration `line` on EACH drawn line fragment, each clamped to that fragment's fitted extent (never past the region or a clipped glyph) (B11, FR-008)
- [X] T014 [P] [US1] In `tests/Symbology.Tests/ChannelPresenceTests.fs` add: two `Token`s whose labels carry the SAME characters but differ only in a new attribute (italic / tracking) yield differing canonical bytes (the attribute is a channel) (B6, SC-002)
- [X] T015 [P] [US1] In `tests/Symbology.Render.Tests/RenderLabelTests.fs` add the decorated-run tofu test: rasterise (via `Render.toPng` under the real `SkiaViewer.Fonts` measurer) a labelled token whose runs set italic/underline/strike/tracking; assert EVERY run is non-tofu (`TofuCount = 0`) and the board is non-blank (B5/FR-006, SC-002)

### Implementation for User Story 1

- [X] T016 [US1] Extend `RunStyle`/`resolveStyle` (`src/Symbology/Symbology.fs:444`) with the four resolved attributes — defaults upright / no-underline / no-strike / `Tracking = 0.0` (`Option.defaultValue`) (data-model §1)
- [X] T017 [US1] Add a tracking-aware width helper used by break/justify/fit (`trackedWidth = baseWidth + tracking*(glyphs-1)`) and fold it into the existing `richLabelNodes` measurement so letter-spacing affects wrap/fit (`src/Symbology/Symbology.fs`) — makes T012 pass
- [X] T018 [US1] In `richLabelNodes` per-segment emission (`src/Symbology/Symbology.fs:539`) add tracking draw: when `tracking ≠ 0`, emit one `Scene.glyphRunProof` per character advanced by `charWidth + tracking` (per-glyph positioning, NOT per-glyph styling — FR-019); when `tracking = 0`/unset, keep the existing single-node emission (zero drift)
- [X] T019 [US1] In `richLabelNodes` add synthetic slant: when `Italic` set, wrap the segment's glyph node in `Scene.withPerspective` with a baseline-pivoted horizontal shear (`M11=1; M12=slant; M13=-slant*baselineY; M22=1; M33=1`, slant ≈ 0.21); when unset, NO wrapper node (zero drift) — real glyphs preserved (tofu-free)
- [X] T020 [US1] In `richLabelNodes` add underline/strike: when set, append a `Scene.line` (`Paint.stroke` in the run's colour, thickness from run size) spanning each drawn fragment's fitted width — underline below baseline, strike at mid-x-height — clamped to the fitted extent, per drawn line fragment; when unset, NO line node (zero drift) — makes T013 pass
- [X] T021 [US1] Verify the all-default run still hits NONE of the new branches and emits the exact 198 node (re-run T011); confirm the pinned 198/197/196/pre-feature goldens stay green (`dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj`)
- [X] T022 [US1] In `tests/Symbology.Tests/DeterminismTests.fs` add a decorated-run render-twice byte-equal assertion (in-process) for an italic/underlined/tracked `Rich` label (B15, FR-011) — covers determinism of the new emission before US2's cross-process golden

**Checkpoint**: US1 fully functional and independently testable — runs carry italic/underline/strike/tracking,
decorated runs are tofu-free at the render edge (T015), all-default runs are byte-identical to 198 (T011/T021),
and same-chars-different-decoration differ (T014). This is the MVP — the larger half of the deferred 198
FR-018 typography gap.

---

## Phase 4: User Story 2 - Paragraph layout: alignment, justification, explicit breaks (Priority: P2)

**Goal**: A new `LabelText.Laid of LabelParagraph list` carries explicit paragraphs, each with its own
`LabelAlign` (Leading/Center/Trailing/Justify). Alignment operates inside the existing per-grammar region
reusing the 197/198 fit machinery (wrap/cap/ellipsis/max-height line/common baseline). Default `Center`
single all-default paragraph lays out byte-identically to spec 198.

**Independent Test**: Render `Token`s whose labels set each alignment and explicit paragraph/line breaks,
including over-region content; confirm each alignment places drawn lines correctly (centre centred, trailing
right, justify fills width with last paragraph line un-justified), the block stays in the footprint
(capped/ellipsised, no mid-glyph clip, no overflow), and default alignment is byte-identical to spec 198.

**Depends on**: US1 (the decoration/slant/tracking per-segment emission `laidLabelNodes` reuses).

### Tests for User Story 2 (write FIRST, ensure they FAIL before T030–T033)

- [X] T023 [P] [US2] In `tests/Symbology.Tests/LaidLabelTests.fs` add alignment-placement assertions: `Center` centres lines (matches 198 placement), `Leading` left-sites, `Trailing` right-sites within the per-grammar region span (`centerX ± regionWidth/2`); each differs in bytes from the others (B7, FR-001)
- [X] T024 [P] [US2] In `tests/Symbology.Tests/LaidLabelTests.fs` add justify assertions: a `Justify` paragraph wrapping to ≥2 lines distributes inter-word space so each wrapped line fills the region width, the LAST line of the paragraph is left un-justified, and a single-token line falls back to the base alignment (no glyph stretch, no mid-glyph clip) (B8, FR-007/FR-008)
- [X] T025 [P] [US2] In `tests/Symbology.Tests/LaidLabelTests.fs` add explicit-structure + default-equivalence assertions: explicit paragraphs/line-breaks produce the authored structure with paragraphs differing in alignment (B9); and a single `Center` all-default-run paragraph is byte-identical to the equivalent `Rich`/`Plain` label (B4, SC-003 — extend the smoke into a pinned test)
- [X] T026 [P] [US2] In `tests/Symbology.Tests/LaidLabelTests.fs` add fit/cap assertions under EVERY alignment: over-numerous/over-wide content caps the drawn line count to the per-grammar budget (Token≤3, Badge≤2, Ring≤2) with a trailing ellipsis on the last drawn line, every drawn segment ≤ the region, mixed-size lines sized to the tallest run on a common baseline, no overflow (B10, FR-007/SC-005)
- [X] T027 [P] [US2] In `tests/Symbology.Tests/LaidLabelTests.fs` add empty/whitespace handling: `Laid []`, `Laid` of all-empty/whitespace paragraphs/runs ⇒ no label node, no exception, regardless of alignment/decoration (B13, FR-009)
- [X] T028 [P] [US2] In `tests/Symbology.Tests/PlaceholderTests.fs` add a degenerate-token-with-laid-out-label case: `R <= 0` carrying a `Laid` aligned/decorated label ⇒ visible placeholder, no exception (placeholder rule wins) (B14, FR-010)
- [X] T029 [P] [US2] In `tests/Symbology.Tests/DeterminismTests.fs` add a laid-out render-twice byte-equal assertion AND a NEW pinned laid-out cross-process golden (justified, multi-paragraph, decorated) — existing 198/197/196 goldens UNCHANGED; AND a **pure-fallback** assertion (B16): with NO real measurer installed (the default `Symbology.Tests` path) a `Laid`/decorated token still yields a deterministic scene carrying the recorded alignment/decoration and never throws (B15/B16, FR-011/FR-012/SC-004)

### Implementation for User Story 2

- [X] T030 [US2] Add an `alignPlace` helper in `src/Symbology/Symbology.fs`: pure fold over measured line widths computing each line's start `x` from `paragraph.Align` over the region span — `Leading`→left edge, `Center`→`centerX - total/2` (198 verbatim), `Trailing`→`right - total`, `Justify`→distribute `(regionWidth - total)` evenly across inter-word gaps (advance by `segW + spaceW + extraPerGap`); last-paragraph-line and single-token (≤1 gap) lines fall back to the paragraph's base alignment (FR-008) — makes T023/T024 pass
- [X] T031 [US2] Add `laidLabelNodes` in `src/Symbology/Symbology.fs`: per paragraph run the 197/198 pipeline (atomise tracking-aware → greedy break → per-line shrink-to-floor + ellipsis fit (`fitLabelW`, floor `0.62×`) → cap to the per-grammar budget SHARED across paragraphs → per-line max-height/common-baseline), place each line via `alignPlace`, emit per-segment reusing the US1 decoration/slant/tracking emission; ellipsise the last kept line; first line of the first paragraph at the spec-197/198 first-line baseline, paragraphs stack downward by running line height (data-model §7) — makes T025/T026 pass
- [X] T032 [US2] Replace the temporary `Laid` arm in `labelDispatch` (`src/Symbology/Symbology.fs:588`) with the real dispatch: `Laid [{ Runs; Align = Center }]` single-paragraph all-default ⇒ reduce to the `Rich`/`Plain` path (byte-identical to 198, B4); `Laid paras` with any non-default alignment / >1 paragraph / styled run ⇒ `laidLabelNodes`; `Laid []`/all-empty ⇒ `[]` (B13) — makes T023–T027 pass and keeps T025's default-equivalence green
- [X] T033 [US2] Re-run the full `tests/Symbology.Tests/Symbology.Tests.fsproj` battery: confirm the pinned 198/197/196/pre-feature goldens are still byte-stable (layered zero drift via the structural reductions, SC-003) and the new cross-process golden (T029) reproduces

**Checkpoint**: US2 fully functional and independently testable — paragraphs align (incl. justify with
un-justified last line), explicit breaks produce authored structure, content is capped/ellipsised/fitted under
every alignment, degenerate + empty cases are safe, and default `Center` single-paragraph is byte-identical to
198. US1 + US2 together deliver both halves of the deferred 198 FR-018.

---

## Phase 5: User Story 3 - Laid-out labels on review boards, governed unchanged (Priority: P3)

**Goal**: Laid-out/decorated labels render reproducibly on gallery/filmstrip boards in every grammar with no
board/motion signature change; the legibility linter stays grammar-independent and treats the label as
inspection-detail (governance unchanged); the design-loop skill documents full rich-text layout and passes
the parity check.

**Independent Test**: Render a fully-laid-out roster as a gallery in each grammar (reproducible per grammar);
confirm the linter's report is grammar-independent and unchanged in pre-attentive governance vs the same
roster with 198-era labels; confirm the skill documents the capabilities and passes parity.

**Depends on**: US1 + US2 (boards render whatever the dispatch produces).

### Tests for User Story 3 (write FIRST, ensure they FAIL/are-pending before T037–T040)

- [X] T034 [P] [US3] In `tests/Symbology.Tests/GalleryTests.fs` add: a fully-laid-out/decorated roster rendered via `galleryIn` (and `render`) draws every unit's label with its alignment/decoration in the selected grammar and is byte-reproducible per grammar under a fixed provider, with no signature change to the board entry points; AND assert author-supplied `Color`/`Align`/decoration are used **as-is** — never silently re-mapped or rejected at runtime (B17/B19, FR-013/FR-015/SC-001)
- [X] T035 [P] [US3] In `tests/Symbology.Tests/LegibilityTests.fs` add the linter-invariance assertion: `Legibility.score`/`scoreAnimated` over a roster WITH laid-out/decorated labels equals the verdict over the SAME roster with 198-era styled labels, and is identical across grammars — layout/decoration does NOT change pre-attentive governance (B18, FR-014/SC-006)
- [X] T036 [P] [US3] In `tests/Symbology.Render.Tests/RenderLabelTests.fs` add the full-layout render-bridge tofu test — **EXTENDS T015's decorated-run case with paragraph layout + justification rather than repeating it**: rasterise a LAID-OUT (justified, multi-paragraph) + DECORATED (italic/underline/strike/tracking) labelled token through `Render.toPng` under the real measurer; assert EVERY run is non-tofu (`TofuCount = 0`) and the board is non-blank (FR-006, SC-002)

### Implementation for User Story 3

- [X] T037 [US3] Confirm `Legibility.fs`/`Legibility.fsi` are UNCHANGED (they do not read `Token.Label`); make T035 green by relying on the existing grammar-independent verdict — if the test reveals any `Label` coupling, that is a defect to fix in `src/Symbology/Legibility.fs` (plan asserts none exists post-198)
- [X] T038 [US3] Author the full-rich-text section canonically in `src/Symbology/skill/SKILL.md` (FR-020): when to use alignment/justify/explicit breaks/decoration; the run attrs (italic/underline/strike/tracking on top of colour/weight/size); that they require the real measurer for tofu-free output; keep paragraphs short + a restrained alignment/decoration set; don't impersonate faction/state pre-attentive encodings or crowd the region / over-justify; how surplus degrades (cap+ellipsis); complements (never replaces) the vector sigil
- [X] T039 [P] [US3] Mirror the SKILL.md update to the skill trees: `.claude/skills/fs-gg-symbology/SKILL.md`, `.agents/skills/fs-gg-symbology/SKILL.md`, and `template/product-skills/fs-gg-symbology/SKILL.md` (adapted copy) — keep `reference.fsx` in sync if it enumerates label capabilities
- [X] T040 [US3] Run skill parity: `dotnet fsi scripts/check-agent-skill-parity.fsx` ⇒ expect `critical=0 high=0` (FR-020/SC-007); fix any drift between canonical and mirrored copies

**Checkpoint**: All three stories independently functional — laid-out rosters render reproducibly on boards in
every grammar, the linter's governance is provably unchanged, and the skill is documented + parity-clean.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification across all stories, surface/baseline integrity, and quickstart sign-off.

- [X] T041 Run the full feature test sweep: `dotnet test tests/Symbology.Tests/Symbology.Tests.fsproj` and `dotnet test tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` — all existing + new batteries green
- [X] T042 Final surface-baseline integrity check (FR-017/SC-007): `git status -- readiness/surface-baselines/` shows ONLY `FS.GG.UI.Symbology.txt` moved (new fields/types/case/ctors) and EVERY other baseline byte-stable — re-confirms T009 after all edits
- [X] T043 Re-run the comprehensive no-regression baseline and diff against T002: `dotnet fsi scripts/baseline-tests.fsx --out specs/199-rich-text-layout/readiness/baseline-after.md` — no NEW reds vs the pre-change baseline (incl. `Package.Tests` + `samples/**`)
- [X] T044 Execute quickstart.md per-Success-Criterion validation (SC-001…SC-007) and record evidence: gallery scenes per grammar, render-bridge tofu + presence, layered zero-drift goldens, cross-process golden, fit/justify/decoration/empty/placeholder cases, linter invariance, baseline + parity reports
- [X] T045 [P] Final review of `src/Symbology/Symbology.fs` for idiomatic simplicity (Constitution III): pure folds, no `mutable`, `Option.defaultValue` defaults, internal helpers omitted from `.fsi`; confirm no new scene primitive / font file / GPU path was introduced (FR-018/FR-019)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. The `.fsi` (T004) is drafted before any implementation (Constitution I); the early smoke (T007) gates the stories.
- **User Stories (Phase 3–5)**: All depend on Foundational. Ordered by priority and by code reuse:
  - **US1 (P1)** — the per-run emission; the MVP.
  - **US2 (P2)** — `laidLabelNodes` reuses US1's per-segment decoration/slant/tracking emission ⇒ start after US1's emission (T018–T020) exists.
  - **US3 (P3)** — boards/linter/skill render whatever US1+US2 produce ⇒ after US2 (linter test T035 + skill T038 can be drafted earlier in parallel as they touch no shared source).
- **Polish (Phase 6)**: Depends on all desired stories.

### Within Each User Story

- Tests (T010–T015 / T023–T029 / T034–T036) are written and MUST FAIL before the implementation tasks in that story (Constitution I/V).
- In US1: `resolveStyle` (T016) → tracking-aware measure (T017) → emission branches (T018–T020) → zero-drift re-verify (T021).
- In US2: `alignPlace` (T030) → `laidLabelNodes` (T031) → dispatch (T032) → re-verify (T033).
- Implementation tasks editing the SAME file (`Symbology.fs`) run sequentially within a story; test tasks in different files are `[P]`.

### Parallel Opportunities

- **Setup**: T001/T002 sequential (baseline after build); T003 independent.
- **Foundational**: T004 first (others depend on the `.fsi`); T006 `[P]` (literals) and T008 `[P]` (test scaffold) can run alongside each other after T005.
- **US1 tests**: T010–T015 all `[P]` (T010–T013 share `RichLabelTests.fs` — coordinate as one edit or sequence them; T014/T015 are separate files, truly parallel).
- **US2 tests**: T023–T027 share `LaidLabelTests.fs` (sequence or single edit); T028 (`PlaceholderTests.fs`) and T029 (`DeterminismTests.fs`) are `[P]`.
- **US3**: T034/T035/T036 `[P]` (three different test files); T039 `[P]` (mirror trees) after T038.
- **Cross-story**: once Foundational is done, US3's skill doc (T038/T039) and linter test (T035) can be drafted in parallel with US1/US2 implementation (no shared source).

---

## Parallel Example: User Story 1 tests

```bash
# Different files — launch together (write FIRST, expect FAIL):
Task: "ChannelPresenceTests.fs — same chars, different attr ⇒ differing bytes (T014)"
Task: "RenderLabelTests.fs — decorated-run tofu test, every run non-tofu (T015)"
# RichLabelTests.fs additions (T010–T013) touch one file — sequence or do as one edit.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 (Setup) + Phase 2 (Foundational) — including the **early FSI/public-surface smoke (T007)** that proves layered zero-drift before any story.
2. Complete Phase 3 (US1): per-run italic/underline/strike/tracking riding the `Rich` path.
3. **STOP and VALIDATE**: decorated runs tofu-free (T015), all-default ≡ 198 (T011/T021), same-chars-differ (T014).
4. This is a shippable increment — the larger half of the deferred 198 FR-018 typography gap.

### Incremental Delivery

1. Setup + Foundational → surface drafted, baseline regenerated, smoke green.
2. US1 → run typography → validate independently → demo (MVP).
3. US2 → paragraph layout / alignment / justify → validate independently → demo.
4. US3 → boards + linter invariance + skill → validate independently → demo.
5. Polish → full sweep, baseline integrity, quickstart sign-off.

### Notes

- `[P]` = different files, no dependency on incomplete tasks.
- `[Story]` maps each task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry none.
- Verify story tests FAIL before implementing (Constitution I/V); never weaken or delete an existing assertion (Constitution V).
- Layered zero-drift is the hard constraint (FR-004/SC-003): keep the pinned 198/197/196/pre-feature goldens green at every step.
- Commit after each task or logical group; stop at any checkpoint to validate the story independently.
