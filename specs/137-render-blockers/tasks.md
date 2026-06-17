---
description: "Task list for Render Blockers — Clipping, Overlay & Scroll"
---

# Tasks: Render Blockers — Clipping, Overlay & Scroll

**Input**: Design documents from `/specs/137-render-blockers/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Test tasks ARE included — Principle V ("Test evidence is mandatory") plus the existing
`Audit_PictureCache` parity trio is the **decisive gate** for the blocker (it must stay/return green WITH
clipping enabled). Write each new behavior test against the surface (`.fsi`) first and confirm it fails on
today's renderer before implementing the matching body.

**Organization**: Tasks are grouped by user story (spec.md priorities P1/P1/P2/P3). The internal phased
delivery (P-A…P-D) maps onto the stories below.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: `US1`–`US4`; Setup/Foundational/Polish carry no story label
- Exact file paths are included in each task

## Path Conventions

Framework libraries under `src/`; framework tests under `tests/`; consumer sample under
`samples/AntShowcase/`. The shared `Rendering.Harness/TestAssertions.fs` helper (linked into the test
projects, from feature 136) provides `renderedText`/`drawnBounds`/`rectsOverlap`/`containedIn`. Line anchors
(e.g. `RetainedRender.fs:1269`) come from research.md and were confirmed during investigation — re-confirm
before editing.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Pin the regression oracle and the exact composition-site inventory before any change.

- [X] T001 Record the pre-change baseline: run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Debug --filter "Picture cache"` and confirm the three `Audit_PictureCache` tests (`cache-on ≡ cache-off`, present-but-dead, effectiveness) pass today (no clipping); note them in `specs/137-render-blockers/research.md` as the gate. Confirm the **six** composition sites exist: `Control.renderTree` `paint`, the four `RetainedRender` `build`/`buildFresh`/`carry`/child-insert-fallback `let subtree` sites, and the `RetainedRender.assemble` emit walk (`RetainedRender.fs:~1269`).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The single shared composition rule every user story builds on.

**⚠️ CRITICAL**: Complete before US1–US3 (clipping/overlay/scroll all depend on it).

- [X] T002 Declare `composeContainerScene` (signature only, body deferred) in `module internal ControlInternals` in `src/Controls/Control.fsi` (data-model §1): `box: Rect option -> own: Scene list -> childScenes: Scene list -> Scene list`
- [X] T003 Implement `composeContainerScene` in `src/Controls/Control.fs` (`ControlInternals`): `Some b, (_::_) -> own @ [ Scene.clipped (RectClip b) (Scene.group childScenes) ]`; otherwise `own @ childScenes` (leaf / box-less = flat, byte-identical to pre-137) (depends on T002)

**Checkpoint**: Foundation ready — the shared clip rule exists and compiles.

---

## Phase 3: User Story 1 - Children never paint past their container, retained path still correct (Priority: P1) 🎯 MVP

**Goal**: Clip every container's children to its bounds via the shared rule at ALL six sites, fixing
control/region-overlap + spill — while keeping full ≡ retained and `cache-on ≡ cache-off` (the feature-136
blocker). Maps to P-A.

**Independent Test**: Render the showcase pages via the full and retained paths and confirm (a) no child's
drawn rect exceeds its container's bounds and (b) the two paths are byte-identical, with the
`Audit_PictureCache` trio green WITH clipping enabled.

### Tests for User Story 1 ⚠️ (write against the `composeContainerScene` surface first; must FAIL until all six sites are wired)

- [X] T004 [P] [US1] Container-bounds non-overflow test — a child laid out wider/taller than its container renders with its drawn area confined to the container box (a `ClipNode` to the container bounds wraps the children) — in `tests/Controls.Tests/`
- [X] T005 [P] [US1] Full ≡ retained parity on a clipped tree test — `Control.renderTree` and `RetainedRender.step` produce byte-identical scenes for a container-with-children tree (paint-order flatten) — in `tests/Controls.Tests/`

> The existing `tests/Controls.Tests/Audit_PictureCache.fs` trio is the **regression gate**: it passes today (no clip), will FAIL while clipping is only half-wired (e.g. before T008), and MUST pass once all six sites route through `composeContainerScene`.

### Implementation for User Story 1

- [X] T006 [P] [US1] Route `Control.renderTree`'s `paint` recursion through `composeContainerScene` (compose own + children clipped to the node box) in `src/Controls/Control.fs` (depends on T003)
- [X] T007 [P] [US1] Route the four `RetainedRender` `let subtree = own @ (children ... SubtreeScene)` sites (`build`, `buildFresh`, `carry`, child-insert/replace fallback) through `composeContainerScene` in `src/Controls/RetainedRender.fs` (depends on T003)
- [X] T008 [US1] Route the `RetainedRender.assemble` emit walk (`own @ (n.Children |> List.collect assemble)`, `RetainedRender.fs:~1269`) through `composeContainerScene` — **the feature-136 miss** that broke `cache-on ≡ cache-off` — in `src/Controls/RetainedRender.fs` (depends on T007)
- [X] T009 [US1] Verify: run `Audit_PictureCache` + T004/T005 and the full `Controls.Tests` suite; confirm `cache-on ≡ cache-off`, hits=3, misses=0, full ≡ retained, and zero regressions, all WITH clipping enabled (depends on T006, T007, T008)

**Checkpoint**: US1 independently testable — children clipped to bounds, retained parity + picture cache intact (the blocker is removed). MVP.

---

## Phase 4: User Story 2 - Transient surfaces float above neighbours at true z-order (Priority: P1)

**Goal**: A deferred z-top overlay group; open menus/dropdowns/pickers paint last, escape ancestor clips, and
win hit-tests; mirrored in the retained path so parity holds. Maps to P-B.

**Independent Test**: Render a page with an open transient surface over an in-flow sibling; confirm it paints
above (z-top), is not clipped by its ancestor container, items are distinct, hit-test returns the overlay, and
an empty overlay group renders byte-identically to the pre-overlay pass.

### Tests for User Story 2 ⚠️ (write against the overlay-pass `.fsi` — T013 — first; must FAIL on today's renderer)

- [X] T010 [P] [US2] Overlay z-order + escape-clip test — an open combo/menu/date-picker surface paints after (above) an in-flow sibling and is NOT clipped by its ancestor container — in `tests/Controls.Tests/`
- [X] T011 [P] [US2] Hit-test test — `nearestAuthored`/`hitTest` at a point where an overlay overlaps an in-flow control returns the topmost overlay — in `tests/Controls.Tests/`
- [X] T012 [P] [US2] Parity test — a page with no open transient surface renders byte-identically to the pre-overlay in-flow pass, and full ≡ retained holds with an overlay present — in `tests/Controls.Tests/`

### Implementation for User Story 2

- [X] T013 [US2] Declare the overlay-pass public entry on `src/Controls/Control.fsi` (built on the existing `Overlay` container)
- [X] T014 [US2] Add the deferred overlay group to `Control.renderTree` (final scene = `inFlow @ overlay`; overlay subtrees painted last at true coords, collected OUT of the in-flow container-clip hierarchy) in `src/Controls/Control.fs` (depends on T013, T006)
- [X] T015 [US2] Route transient surfaces (menu/context-menu open list, combo-box, auto-complete, date-picker/time-picker calendar) into the overlay group when shown in `src/Controls/Control.fs` (and relevant `Widgets/`) (depends on T014)
- [X] T016 [US2] Mirror the overlay-group split in the retained emit (`RetainedRender.assemble`/`SubtreeScene`) so full ≡ retained holds with overlays present in `src/Controls/RetainedRender.fs` (depends on T014, T008)
- [X] T017 [US2] Make `nearestAuthored`/`hitTest` consult the overlay group before in-flow (topmost-overlay wins) in `src/Controls/Control.fs` (depends on T014)

**Checkpoint**: US1 + US2 testable — nothing overprints; open surfaces float above; parity preserved.

---

## Phase 5: User Story 3 - Long pages stay within the window and scroll (Priority: P2)

**Goal**: `ScrollViewer` becomes a real clipping viewport (clip content + scroll offset + affordance) on the
container-clip model. Maps to P-C.

**Independent Test**: Render a `ScrollViewer` whose content is taller than its box; confirm content is clipped
to the box, a scroll offset + affordance exist, content beyond the fold is clipped (scrollable) not spilled,
and a taller-than-region page paints nothing outside the content region.

### Tests for User Story 3 ⚠️ (write first, must FAIL on today's renderer)

- [X] T018 [P] [US3] ScrollViewer viewport test — content clipped to box, scroll offset + affordance present, content taller than the viewport is clipped (scrollable) not spilled — in `tests/Controls.Tests/`
- [X] T019 [P] [US3] Bounded-page test — a page taller than the content region paints nothing outside it; status/feedback render fully within the window — in `tests/Controls.Tests/`

### Implementation for User Story 3

- [X] T020 [US3] Make `ScrollViewer` a real clipping viewport (clip content to box + expose scroll offset + render scroll affordance; taller content clipped, not spilled) in `src/Controls/Control.fs` (depends on T003)
- [X] T021 [US3] If a viewport metric must be read back, declare it in `src/Layout/Layout.fsi` (+ `src/Controls/Control.fsi`) and surface it from the viewport geometry (depends on T020)

**Checkpoint**: US1 + US2 + US3 all independently functional.

---

## Phase 6: User Story 4 - Re-baseline & re-verify the 19 pages (Priority: P3)

**Goal**: Re-establish the changed baselines as disclosed intended changes and confirm zero of the seven
defect classes across all 19 pages. Maps to P-D. Depends on US1–US3 landing.

### Tests / verification for User Story 4

- [X] T022 [US4] Regenerate the touched surface-area baselines (`FS.GG.UI.Controls.txt`, and `FS.GG.UI.Layout.txt` if a metric was surfaced) via `scripts/refresh-surface-baselines.fsx`; commit only the intended overlay-pass / scroll-metric additions to `tests/surface-baselines/`
- [X] T023 [US4] Re-establish G1/G2 golden evidence and the rendered-output drift gate (`readiness/` + sample golden trees) as intended changes, and fill `specs/137-render-blockers/contracts/rebaseline-ledger.md` — one disclosed row per changed baseline (id, defect/fix cause, before/after, intended-confirmation)
- [X] T024 [US4] Re-capture all 19 showcase pages in both themes (`cd samples/AntShowcase && dotnet run --project AntShowcase.App -c Release -- evidence --seed 1`) and confirm zero instances of the seven defect classes (no spill, dropdowns above neighbours, long pages clipped+scrollable); record GL screenshots or a disclosed no-GL degrade (never a fabricated pass)
- [X] T025 [P] [US4] Run `specs/137-render-blockers/quickstart.md` validation end-to-end (cache-parity gate → build → semantic tests → re-capture → determinism diff)

**Checkpoint**: All four user stories functional; the 19 pages are clean and disclosed.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T026 [P] Update docs and the public-API compatibility/migration note for the new overlay-pass entry (and scroll-viewport metric, if surfaced) in `docs/` and any touched module `README.md`
- [ ] T027 [P] If chrome regions still need explicit sizes after framework clipping, apply compositional region sizing in `samples/AntShowcase/AntShowcase.Core/Shell.fs` (sample-only; the only sample-level remediation) — **DEFERRED (disclosed):** clipping IS needed (the shell bands rely on overflow; the app-bar's toggle button lays out ~132px in a ~32px band and is now clipped), but the outer vertical `Stack` flex-shrinks the bands and the `Attr` API exposes no flex-shrink/grow control to pin them, so a clean fix needs a framework flex authoring control or a shell redesign. Region boxes do NOT overlap (verified) — this is correct framework clipping exposing under-sized sample chrome, not a renderer defect. See `contracts/rebaseline-ledger.md`.
- [X] T028 Run the full solution test suite (`dotnet test FS.GG.Rendering.slnx -c Debug`) and confirm zero regressions across Scene/SkiaViewer/Controls/Layout

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: after Setup; `composeContainerScene` (T002→T003) blocks US1/US2/US3.
- **US1 (Phase 3)**: after Foundational. The blocker/MVP — must land with parity green before US2/US3 build on it.
- **US2 (Phase 4)**: after US1 (overlays must escape the container clips; T016 mirrors the emit fixed in T008).
- **US3 (Phase 5)**: after Foundational (reuses the clip model); independent of US2.
- **US4 (Phase 6)**: after US1–US3 (re-baselining/re-capture only honest once the fixes land).
- **Polish (Phase 7)**: after the desired user stories.

### Within / across stories

- **Principle I order**: sketch the new public surface in `.fsi` first, then write the semantic tests against
  it (they fail), then implement. Concretely: **T002 before T004/T005**; **T013 before T010–T012**;
  **T021's `.fsi` before relying on the metric**.
- **T008 is the linchpin**: the `assemble` emit-walk fix is what makes `cache-on ≡ cache-off` hold; US1 is not
  done (and US2's T016 cannot be validated) until T008 + T009 are green.

### Parallel Opportunities

- T006 (`Control.fs`) and T007 (`RetainedRender.fs`) touch different files → can run in parallel; T008 depends
  on T007 (same file).
- All tests within a story are `[P]` (T004/T005; T010–T012; T018/T019).
- US3 (T018–T021) can be staffed in parallel with US2 once US1 lands.

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → 4. STOP & VALIDATE: `Audit_PictureCache` green
   WITH clipping, no child spills, full ≡ retained. **This alone removes the feature-136 blocker** and fixes
   control/region-overlap + spill.

### Incremental Delivery

1. Foundational → shared clip rule. 2. US1 → container clipping + parity (MVP, the blocker). 3. US2 → overlay
   pass. 4. US3 → ScrollViewer viewport. 5. US4 → re-baseline + 19-page re-verify. 6. Polish.

---

## Notes

- [P] = different files, no incomplete-task dependency.
- **Tier 1** change: framework output changes are deliberate; never silently overwrite a baseline —
  re-establish and disclose (FR-010).
- The whole feature hinges on ONE shared `composeContainerScene` used at every assembly site; the six-site
  discipline is what keeps full ≡ retained and `cache-on ≡ cache-off` true by construction.
- Determinism + theme-invariance are paramount: every fix holds identically under antLight/antDark and
  preserves byte-identical same-seed evidence.
- Commit after each task or logical group.
