---
description: "Task list for Showcase Rendering Defect Fixes"
---

# Tasks: Showcase Rendering Defect Fixes

**Input**: Design documents from `/specs/136-showcase-render-fixes/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Test tasks ARE included — Principle V ("Test evidence is mandatory") and the plan/research
(R8) require a behavior gate per defect class that **fails on today's renderer and passes after**. Write
each test first and confirm it fails before implementing the matching fix.

**Organization**: Tasks are grouped by user story (spec.md priorities P1/P1/P2/P3). Internal phased
delivery from plan.md (P-A…P-F) is mapped onto the user stories below.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: `US1`–`US4` for user-story phases; Setup/Foundational/Polish carry no story label
- Exact file paths are included in each task

## Path Conventions

Framework libraries under `src/`; framework tests under `tests/`; consumer sample under
`samples/AntShowcase/`. Line references (e.g. `Control.fs:1901`) come from research.md and are confirmed
investigation anchors — re-confirm before editing.

The framework test projects referenced below **already exist** in the solution
(`tests/SkiaViewer.Tests/`, `tests/Scene.Tests/`, `tests/Controls.Tests/`, `tests/Layout.Tests/`,
`tests/Rendering.Harness/`) — add test files to them rather than creating new projects.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Bring the bundled font assets and the empirical probe into the repo before any renderer work.

- [X] T001 Acquire the standard font face files (NotoSans-Regular/Bold, NotoSansMono-Regular, Inter-Regular/Bold, JetBrainsMono-Regular, DejaVuSans/-Bold, DejaVuSansMono) into `src/SkiaViewer/assets/fonts/` (data-model §1)
- [X] T002 [P] Record each font face's license (OFL/free) in `PROVENANCE.md` (root) per the no-new-NuGet constraint (plan Complexity Tracking)
- [X] T003 Declare every `src/SkiaViewer/assets/fonts/*.ttf` as `<EmbeddedResource>` in `src/SkiaViewer/SkiaViewer.fsproj` (plan Technical Context)
- [X] T004 [P] Build a standalone font probe (P-A, per repo "probe-driven render debugging") to confirm (a) whether `SKTypeface.Default` has glyph coverage in the headless sandbox and (b) that `SKTypeface.FromStream` loads an embedded face in the GL screenshot path; record findings under `specs/136-showcase-render-fixes/research.md` (R1 Open verification)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Primitives shared by more than one user story.

**⚠️ CRITICAL**: Complete before the user-story phases that depend on them (clipping → US2/US3/US4).

- [X] T005 Confirm/expose a rect-clip primitive (`RectClip` + `Scene.clipped`) in `src/Scene/Scene.fsi` and `src/Scene/Scene.fs`, used by both chart clipping (US3) and container clipping (US2/US4); add to `.fsi` if not already public
- [X] T006 [P] Add shared test assertion helpers — drawn-bounds overlap detection, rendered-glyph extraction from a scene, and an antLight≡antDark theme-pair runner — in `tests/Rendering.Harness/` (used by US1–US4 tests)

**Checkpoint**: Foundation ready — user stories can begin.

---

## Phase 3: User Story 1 - Every character and label renders legibly (Priority: P1) 🎯 MVP

**Goal**: All text resolves through bundled fonts with a disclosed per-character fallback chain; box
sizing matches drawn advances so nothing truncates. Fixes wrong-glyph + truncated-text defect classes
(P-A + P-B).

**Independent Test**: Render every page (both themes); confirm each authored string appears in full with
no substituted glyphs and no mid-word truncation — specifically `@`, `—`, `#`, `▸`, `·`, and
`Stable`/`Upload`/`Refresh`/numeric-input labels.

### Tests for User Story 1 ⚠️ (write against the US1 `.fsi` surface — T011/T015 — first; must FAIL until the `.fs` bodies land)

> **Principle I**: author the font-registry `.fsi` (T011) and measurement-seam `.fsi` (T015) *first* (signatures only, bodies deferred), then write these tests against that surface. The tests still fail until the `.fs` bodies (T012–T017) land, satisfying Principle V.

- [X] T007 [P] [US1] Glyph-correctness test (`@` in `ada@example.com` → `@` not `7`; `—`/`#`/`▸`/`·` authored-or-deliberate; mixed case preserved `Stable` not `STABLE`) in `tests/SkiaViewer.Tests/`
- [X] T008 [P] [US1] Measure/advance agreement test — the advance used to size a text box equals the advance used to draw it; `Stable`/`Upload`/`Refresh`/numeric labels render with no clip — in `tests/Scene.Tests/`
- [X] T009 [P] [US1] Determinism test — two same-seed headless text renders are byte-identical (host-independent) — in `tests/SkiaViewer.Tests/`
- [X] T010 [P] [US1] Fallback-disclosure test — `Substituted`/`Tofu` outcomes emit a structured diagnostic and aggregate into the evidence record; no wildcard/plausible-wrong glyph is ever produced — in `tests/SkiaViewer.Tests/`
- [X] T010A [P] [US1] Overflow-affordance test — a label/value authored *wider than its fixed box* renders with wrapping or an explicit ellipsis (`…`) and never silently drops characters (FR-002, US1-AS4, Edge "Longest labels"); covers the case that **remains after** measure/draw reconciliation (content genuinely exceeds the box) — in `tests/Controls.Tests/`

### Implementation for User Story 1

- [X] T011 [US1] Declare the font-registry surface (request `{Family;Weight;Size}`, resolve→cached `SKFont`, fallback chain, `FallbackResolution` = `Authored`/`Substituted`/`Tofu`) in `src/SkiaViewer/Fonts.fsi` (data-model §2–§3)
- [X] T012 [US1] Implement `Fonts.fs` registry: load embedded faces via `SKTypeface.FromStream` from manifest resources, cache by `(family, weight, size)`, fixed fallback order (Noto Sans→Inter→DejaVu Sans; Noto Sans Mono→JetBrains Mono→DejaVu Sans Mono); fail loudly on missing asset (Principle VI) — `src/SkiaViewer/Fonts.fs`
- [X] T013 [US1] Implement the per-character fallback chain and deliberate substitutes (`—`→`–`/`-`, `▸`→`>`, `·`→`•`) with disclosure diagnostics in `src/SkiaViewer/Fonts.fs` (depends on T011, T012)
- [X] T014 [US1] Rewrite `drawTextWithFallback` (`src/SkiaViewer/SceneRenderer.fs:242`) to resolve real typefaces via the registry, take advances from real metrics, drop the force-uppercase, and demote the 5×7 `vectorGlyphPattern` to the final disclosed-tofu renderer only (depends on T012, T013)
- [X] T015 [US1] Add the real-metrics measurer (`SKFont.MeasureText`) and surface the measurement seam in `src/SkiaViewer/SkiaViewer.fsi` + `src/SkiaViewer/SkiaViewer.fs` (depends on T012)
- [X] T016 [US1] Calibrate `Scene.measureText` heuristic to the default family and inject the real-font measurer into the box-sizing path (keep pure default for pure callers) in `src/Scene/Scene.fsi` + `src/Scene/Scene.fs` (depends on T015)
- [X] T016A [US1] Implement the grow/wrap/ellipsis overflow affordance for text whose real-metric advance **still** exceeds its box after reconciliation (FR-002, US1-AS4): wrap within the box or render a trailing ellipsis (`…`), never silent truncation — in `src/Controls/Control.fs` (text/label geometry) (depends on T016)
- [X] T017 [US1] Aggregate fallback/tofu disclosures into the per-page evidence record (counts + affected code points) surfaced through `src/SkiaViewer/SkiaViewer.fs` (depends on T013)

**Checkpoint**: US1 independently testable — text legible and untruncated on all pages, both themes (MVP).

---

## Phase 4: User Story 2 - Controls and chrome never overlap (Priority: P1)

**Goal**: Transient surfaces float above flow at true z-order; regions and sibling controls occupy
disjoint, clipped areas. Fixes overlay-overprint + control-overlap + region-overlap (P-D + P-E framework
parts).

**Independent Test**: Render every page; confirm no two sibling controls' and no two layout regions'
drawn areas overlap; open dropdowns/menus paint distinctly above neighbours.

### Tests for User Story 2 ⚠️ (write against the overlay `.fsi` — T021 — first; must FAIL on today's renderer)

> **Principle I**: author the overlay-pass `.fsi` entry (T021) *first* (signature only), then write these tests against it; they still fail until the `.fs` body (T022–T024) lands.

- [ ] T018 [P] [US2] Overlay z-order test — an open combo/auto-complete/date-picker/menu surface paints above in-flow siblings, items never overprint, and `nearestAuthored` hit-test returns the topmost overlay — in `tests/Controls.Tests/`
- [ ] T019 [P] [US2] Region non-overlap test — app bar / nav rail / content / feedback / status drawn rects are mutually disjoint — in `tests/Layout.Tests/`
- [ ] T020 [P] [US2] Container-clipping test — no child paints past its parent's right/bottom edge; nav-rail labels stay within the rail width — in `tests/Controls.Tests/`

### Implementation for User Story 2

- [ ] T021 [US2] Declare the overlay-pass public entry on `src/Controls/Control.fsi` (built on existing `Overlay` container at `Control.fsi:506`)
- [ ] T022 [US2] Implement the deferred overlay pass in `Control.renderTree` (final scene = `inFlow @ overlay`, overlays paint last at true coords) in `src/Controls/Control.fs` (depends on T021)
- [ ] T023 [US2] Route transient surfaces (menu/context-menu open list, combo-box, auto-complete, date-picker/time-picker calendar) into the overlay layer instead of in-flow in `src/Controls/Control.fs` (and the relevant `Widgets/`), referencing `Control.fs:1099,1643` (depends on T022)
- [ ] T024 [US2] Make `nearestAuthored` consult the overlay group before in-flow (z-order hit-test) in `src/Controls/Control.fs` (depends on T022)
- [ ] T025 [US2] Clip each container's children to container bounds (`Scene.clipped`) in `paintNode` (`Control.fs:~2035-2040`) in `src/Controls/Control.fs` (depends on T005)
- [ ] T026 [US2] Make the flex main-axis split honour explicit basis/weight instead of uniform division (`Layout.fs:~272`) in `src/Layout/Layout.fs` (+ `src/Layout/Layout.fsi` if surface changes)

**Checkpoint**: US1 + US2 both independently testable — nothing overprints; regions disjoint.

---

## Phase 5: User Story 3 - Composite controls show their expected structure (Priority: P2)

**Goal**: data-grid renders as a table; menu/combo rows are distinct; descriptions stay in box; QR
populated; charts clipped and degenerate-data-safe. Fixes composite-structure class (P-C).

**Independent Test**: Render the hosting pages; confirm data-grid columns side-by-side with aligned
header/body cells, menu items distinct, descriptions aligned within box, QR non-empty, charts in-box.

### Tests for User Story 3 ⚠️ (write first, must FAIL on today's renderer)

- [ ] T027 [P] [US3] data-grid table-structure test — columns side-by-side; header cell N horizontally aligned with body cell N — in `tests/Controls.Tests/`
- [ ] T028 [P] [US3] menu/combo distinct-row test — each item occupies a distinct y-band, none share a baseline — in `tests/Controls.Tests/`
- [ ] T029 [P] [US3] descriptions-in-box + qr-code-populated test — bottom descriptions item within `box.Y + box.Height`; non-empty payload yields a visible non-empty QR grid even when the box is small — in `tests/Controls.Tests/`
- [ ] T030 [P] [US3] chart in-box + degenerate-data test — chart body stays within its box; `n=0`/NaN/Inf data renders an empty (non-crashing, non-overrunning) chart — in `tests/Controls.Tests/`

### Implementation for User Story 3

- [ ] T031 [US3] Map `data-grid-row`/`data-grid-header` to `Row` in `directionOf` (`Control.fs:1901-1911`) and share a column-width track so header/body cells align, in `src/Controls/Control.fs` (and `src/Controls/DataGrid.fs` if geometry lives there)
- [ ] T032 [P] [US3] `rowsGeom`: `rowH = max(minRowHeight, box.Height / n)` so items never collapse onto a shared baseline (`Control.fs:898-914`) in `src/Controls/Control.fs`
- [ ] T033 [P] [US3] Make `descriptionsGeom` respect `box.Height` (scale spacing or truncate-with-affordance, never past the box) replacing fixed `16 + i*22` (`Control.fs:1420-1426`) in `src/Controls/Control.fs`
- [ ] T034 [P] [US3] Enforce a minimum QR module-grid size and clip to box in `qrCodeGeom` (`Control.fs:1457-1464`) in `src/Controls/Control.fs`
- [ ] T035 [P] [US3] Wrap chart **render** geometry in `Scene.clipped (RectClip box)` and guard degenerate data (`n=0`/NaN/Inf) in the chart-geometry section of `src/Controls/Control.fs` (the `lineGeom`/`barGeom`/`pieGeom`/`areaGeom`/`columnGeom`/`radarGeom`/… functions, `Control.fs:~496-700`, anchor `Control.fs:513-645`; dispatch at `Control.fs:1676-1691`). NOTE: `src/Controls/Charts.fs` / `Charts2.fs` are *authoring* modules, **not** render geometry — do not edit them for clipping. (depends on T005)

**Checkpoint**: US1 + US2 + US3 all independently functional.

---

## Phase 6: User Story 4 - Pages stay within the window and scroll when long (Priority: P3)

**Goal**: `ScrollViewer` is a real clipping viewport; the sample Shell budgets region sizes, nav width,
and content scroll. Fixes unbounded-content class (P-E layout + the only sample-level remediation).

**Independent Test**: Render each page at the default window size; confirm no control paints outside the
content region's bounds and a page taller than the viewport exposes a scroll affordance with all controls
reachable.

### Tests for User Story 4 ⚠️ (write first, must FAIL on today's renderer)

- [ ] T036 [P] [US4] ScrollViewer viewport test — content is clipped to the box, a scroll offset + affordance exist, taller content is clipped (scrollable) not spilled (`Control.fs:1292-1297`) — in `tests/Controls.Tests/`
- [ ] T037 [P] [US4] Bounded-page test — a page taller than the content region paints nothing outside it; status strip and feedback text render fully within the window — in `tests/Controls.Tests/`

### Implementation for User Story 4

- [ ] T038 [US4] Turn `ScrollViewer` into a real clipping viewport (clip content to box + expose scroll offset + affordance) in `src/Controls/Control.fs` (+ `src/Layout/Layout.fs`/`.fsi` if viewport metrics are surfaced) (depends on T005)
- [ ] T039 [US4] SAMPLE fix — in `samples/AntShowcase/AntShowcase.Core/Shell.fs` assign explicit region sizes (app bar / feedback / status height, fixed nav-rail width) and wire content flex-grow + scroll (depends on T026, T038)

**Checkpoint**: All four user stories independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns (P-F — Re-baseline + Verification, FR-012/FR-013)

**Purpose**: Re-establish baselines as intended correctness fixes with disclosure, then re-verify all 19
pages.

- [ ] T040 Update surface-area baselines for every public module whose `.fsi` gained surface — the per-module `.txt` files under `tests/surface-baselines/` (`FS.GG.UI.SkiaViewer.txt`, `FS.GG.UI.Controls.txt`, `FS.GG.UI.Scene.txt`, `FS.GG.UI.Layout.txt`), regenerated via `scripts/refresh-surface-baselines.fsx`. `tests/surface-baselines/` is the **canonical surface-area baseline home** (NOT `readiness/`, which holds only the rendered-output/parity drift gate)
- [ ] T041 Re-establish G1 Controls Gallery and G2 Sample Apps golden evidence (rebaseline-ledger.md "Baselines expected to change")
- [ ] T042 Re-baseline the rendered-output drift gate under `readiness/`
- [ ] T043 Fill `specs/136-showcase-render-fixes/contracts/rebaseline-ledger.md` — one row per changed baseline (id, FR/defect cause, before/after, intended-confirmation) and the SC-006 framework-vs-sample split record
- [ ] T044 Address the latent drift-gate holes from memory `surface-baseline-gaps` if touched: (a) the unguarded `FS.GG.UI.Color` surface baseline, and (b) the absent `readiness/surface-baselines/` rendered-output gate — **distinct** from the existing API-surface baselines at `tests/surface-baselines/` (see T040). Close them or record as out-of-scope follow-ups in the ledger
- [ ] T045 Re-capture all 19 showcase pages in both themes (`cd samples/AntShowcase && dotnet run --project AntShowcase.App -c Release -- evidence --seed 1`) and confirm zero instances of the seven defect classes (SC-001..SC-005); record GL screenshots or a disclosed no-GL degrade (never a fabricated pass)
- [ ] T046 [P] Run `specs/136-showcase-render-fixes/quickstart.md` validation end-to-end
- [ ] T047 [P] Update docs and public-API compatibility/migration note for the new font-registry, overlay-pass, and measurement-seam surface

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: after Setup; T005 (clip primitive) blocks US2/US3/US4 clipping tasks.
- **User Stories (Phase 3–6)**: after Foundational. US1 and US2 are both P1; US3 (P2) and US4 (P3) follow.
  US4's `Shell.fs` task (T039) depends on US2's flex fix (T026) and US4's viewport (T038).
- **Polish (Phase 7)**: after all desired user stories — re-baselining must reflect final output.

### User Story Dependencies

- **US1 (P1)**: depends only on Setup/Foundational. Independently testable. MVP.
- **US2 (P1)**: depends on Foundational (T005 for T025). Independent of US1.
- **US3 (P2)**: depends on Foundational (T005 for T035). Independent of US1/US2.
- **US4 (P3)**: depends on Foundational (T005) and integrates US2's flex fix in the Shell task.

### Within Each User Story

- **Principle I order**: for each story the new public surface is sketched in its `.fsi` **first** (signature only, bodies deferred), *then* the semantic tests are written against that surface and MUST fail, *then* the `.fs` bodies are implemented. Concretely: **T011 before T007–T010A** (font registry), **T015 before T008** (measurement seam), **T021 before T018–T020** (overlay pass). This satisfies Principle I (sketch FSI → semantic tests → implement) and Principle V (tests fail first).
- Surface (`.fsi`) before bodies (`.fs`) within each story (T011 before T012; T021 before T022).
- Registry/measurer before the call sites that consume them.

### Parallel Opportunities

- T002 and T004 run alongside T001/T003 in Setup.
- All US1 tests (T007–T010A) run in parallel; same for US2 (T018–T020), US3 (T027–T030), US4 (T036–T037).
- US3 body tasks T032/T033/T034/T035 touch distinct geometry and can run in parallel after T031.
- Once Foundational completes, US1–US4 can be staffed in parallel by different developers.

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (write first, confirm they FAIL):
Task: "Glyph-correctness test in tests/SkiaViewer.Tests/"          # T007
Task: "Measure/advance agreement test in tests/Scene.Tests/"        # T008
Task: "Determinism (byte-identical) test in tests/SkiaViewer.Tests/"# T009
Task: "Fallback-disclosure test in tests/SkiaViewer.Tests/"         # T010
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → 4. STOP & VALIDATE text legibility on all
   19 pages, both themes → 5. demo. This alone removes the most damaging (wrong-glyph + truncation) class.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → legible text (MVP) → re-capture a sample page to confirm.
3. US2 → no overprint / disjoint regions.
4. US3 → composite structure correct.
5. US4 → bounded + scrolling.
6. Phase 7 → re-baseline G1/G2/drift, fill ledger, re-capture all 19 pages, confirm zero defects.

### Parallel Team Strategy

After Foundational: Dev A → US1; Dev B → US2; Dev C → US3; Dev D → US4 (coordinating the Shell task with
US2's flex change). Converge on Phase 7 together.

---

## Notes

- [P] = different files, no incomplete-task dependency.
- This is a **Tier 1** change: framework output changes are deliberate; never silently overwrite a
  baseline — re-establish and disclose (FR-012/SC-007).
- Determinism is paramount: text must resolve through bundled assets, never `SKTypeface.Default`.
- Every fix must hold identically under antLight and antDark (theme-invariance).
- Confirm each test fails on today's renderer before writing the fix; commit after each task or logical group.
