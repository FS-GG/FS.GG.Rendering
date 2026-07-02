---
description: "Task list for feature 232 — unify control-id schemes onto Key ?? path"
---

# Tasks: unify control-id schemes onto `Key ?? path`

**Input**: Design documents from `/specs/232-unify-control-id-schemes/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/fsi-surface-deltas.md, quickstart.md
**Tests**: INCLUDED (Constitution Principle V mandates fail-before/pass-after evidence; plan is tests-first).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallelizable (different files, no dependency on an incomplete task)
- **[Story]**: US1 (unkeyed keyboard dispatch), US2 (hover/press/scroll stamp), US3 (widget real ids), US4 (diagnostics/`.fsi`)
- Exact file paths included.

## Move-group reminder (research.md)

- **Group A** (Focus traversal): `Focus.order` + `ControlsElmish` 1220 (traverse) + 1537 + `routeFocusedKey` filter — land together.
- **Group B** (visual-state/ring): `applyRuntimeVisualState` + `finalVisualState`/`targetedWalk` + `ControlsElmish` 1394 — land together.
- **Independent**: `applyScrollOffsets`. **Non-domain**: `ControlsElmish` 969 label.
- Enabler for A/B host seams: the internal path-aware `RetainedId → Key ?? path` resolver.

---

## Phase 1: Setup (baseline)

- [X] T001 Establish a comprehensive test baseline: run **every** test project (`dotnet test` over the solution) on branch `232-unify-control-id-schemes` and record the full red/green set in `specs/232-unify-control-id-schemes/notes-baseline.md`, so pre-existing failures are not mistaken for regressions at merge.
- [X] T002 Confirm the `path` derivation SSOT in `src/Controls/Control.fs` (`eventBindingsOf`/`collectBoundsWith`: root `"0"`, child *i* → `path + "." + string i`) and note the exact expression to reuse verbatim in every newly path-threaded walk. Record in `notes-baseline.md`.

---

## Phase 2: Foundational (blocking prerequisites)

### Root-cause map + EARLY BEHAVIORAL SMOKE (do not skip — plan standing assumption)

- [X] T003 Write the root-cause map to `specs/232-unify-control-id-schemes/notes-rootcause.md` pinning each scheme-B site to `file:line` (Focus.fs:52-53; ControlRuntime.fs:271,292,369; ControlsElmish.fs:969,1220,1394,1537) and its move-group (research.md).
- [X] T004 **Early behavioral smoke (before any fix)** — add temporary reproduction tests (or an fsx script) that exercise the REAL seams on `main`'s behavior and CONFIRM the symptoms: (a) an **unkeyed** focusable `Button` with an activation binding, focused, routed an activation key via `routeFocusedKey`, produces **zero** messages; (b) two **unkeyed** same-kind controls with `HoveredControl = path-of-second` run through `ControlRuntime.applyRuntimeVisualState` leave the intended node unstamped. File under `tests/Controls.Elmish.Tests/` (marked with the feature id). If a symptom does NOT reproduce, STOP and revise research.md before proceeding.

### Enabler — path-aware retained resolver

- [X] T005 Add an `internal` path-aware resolver in `src/Controls/RetainedRender.fs` (e.g. `retainedCanonicalId : RetainedId -> RetainedRender<'msg> -> ControlId option` and/or `tryFindNodeWithPath`) returning a node's full-tree `Key ?? path`, mirroring the walk in `authoredControlIds` (`RetainedRender.fs:1596-1610`). Keep it out of `RetainedRender.fsi` (internal; `InternalsVisibleTo`).
- [X] T006 [P] Unit-test the resolver in `tests/Controls.Tests/` — for a mixed keyed/unkeyed tree, each `RetainedId` resolves to the same `Key ?? path` that `Control.eventBindingsOf`/`boundIdsOf` mint for that node.

**Checkpoint**: symptoms confirmed; resolver available to the Elmish host seams.

---

## Phase 3: User Story 1 — unkeyed focused control activates by keyboard (P1) · Group A + filter

**Goal**: an unkeyed focused control dispatches its activation binding on keypress; unkeyed same-kind siblings are distinct focus stops.
**Independent test**: focus an unkeyed focusable control, route an activation key → expected message produced; `Focus.order` gives two unkeyed same-kind siblings distinct ids.

### Tests (fail before)

- [X] T007 [P] [US1] Test in `tests/Controls.Tests/` (Feature 108-adjacent): `Focus.order` stop id for an unkeyed control equals `Control.eventBindingsOf`/`boundIdsOf` id (`Key ?? path`), and two unkeyed same-kind siblings get **distinct** stop ids (0 collisions). (Maps SC-003.)
- [X] T008 [P] [US1] Test in `tests/Controls.Elmish.Tests/`: `routeFocusedKey` on an **unkeyed** focused control with an activation binding produces the activation message exactly once; keyed control behavior unchanged (regression). (Maps SC-001.)

### Implementation (Group A — land together)

- [X] T009 [US1] `src/Controls/Focus.fs`: replace `controlId c = Key ?? Kind` (`:52-53`) with an **indexed** walk threading `path` (root `"0"`, child *i* → `path + "." + string i`), minting `FocusStop.Control = Key ?? path`. Ensure the index/path advances even across focusable subtrees the walk does not descend (switch the `for … do` at `:73-75` to an indexed traversal).
- [X] T010 [US1] `src/Controls.Elmish/ControlsElmish.fs` `routeFocusedKey` (`~1210-1271`): derive the focused node's full-tree `Key ?? path` via the T005 resolver; feed it to `Focus.traverse`. Replace the node-re-rooted `eventBindingsOf node.Control |> filter (b.ControlId = nodeId)` (`:1235-1236`) with a filter over the **full-tree** `eventBindingsOf r.Root.Control` by that id (fixes unkeyed dispatch; keyed unchanged). Update the `Activate`/`resolveNavIntent` `ControlId = Some nodeId` to the full-tree id.
- [X] T011 [US1] `src/Controls.Elmish/ControlsElmish.fs` `retainedIdOfControl` (`:1535-1544`): match each node's `Key ?? path` (resolver) against the traverse `controlId`, not `Key ?? Kind`.
- [X] T012 [US1] Update `src/Controls/Focus.fsi` doc text for `order`/`FocusStop.Control` to the unified `Key ?? path` scheme (no signature change). Run US1 tests → green.

**Checkpoint**: US1 independently testable and green; keyboard activation works for unkeyed controls.

---

## Phase 4: User Story 2 — hover/press/scroll land on the right node (P1) · Group B + scroll

**Goal**: runtime-derived hover/press/focus-ring state and scroll offsets are stamped on the exact node the pointer/model resolves to (unkeyed included); at-rest byte-identity preserved.
**Independent test**: set `HoveredControl` to an unkeyed node's path → only that node stamped Hover; unkeyed `scroll-viewer` scrolls.

### Tests (fail before)

- [X] T013 [P] [US2] Test in `tests/Controls.Tests/` (Feature 096/112-adjacent): with two unkeyed same-kind controls and `HoveredControl = path-of-second`, `applyRuntimeVisualState` stamps Hover on the second only; the first is byte-identical to un-bridged. At-rest (nothing hovered/pressed) → whole tree byte-identical. (Maps SC-002, SC-005.)
- [X] T014 [P] [US2] Test in `tests/Controls.Tests/`: targeted walk (`applyRuntimeVisualStateTargeted`) touched-node count for a keyed tree is unchanged vs. `main` (subtree reuse preserved). (Maps SC-005.)
- [X] T015 [P] [US2] Test in `tests/Controls.Tests/` (Feature 175-adjacent): an **unkeyed** `scroll-viewer` with an offset keyed by its path is shifted by `applyScrollOffsets`. (Maps SC-002.)
- [X] T016 [P] [US2] Test in `tests/Controls.Elmish.Tests/`: the focus **ring** (deriveVisualState `FocusedControl` branch) stamps an unkeyed focused control after `focusedControlId` re-points to `Key ?? path`.

### Implementation (Group B — land together; scroll independent)

- [X] T017 [US2] `src/Controls/ControlRuntime.fs`: thread `path` through `applyRuntimeVisualState` (`:270-288`), `finalVisualState` (`:290-293`), and `targetedWalk` (`:301-338`); compute node id `Key ?? path` (indexed recursion mirroring T002); `deriveVisualState model (Key ?? path)`. Preserve precedence (consumer-set non-Normal wins) and at-rest byte-identity.
- [X] T018 [US2] `src/Controls/ControlRuntime.fs` `applyScrollOffsets` (`:368-388`): thread `path`; look up `ScrollOffsets` by `Key ?? path` (data already path-keyed).
- [X] T019 [US2] `src/Controls.Elmish/ControlsElmish.fs` `focusedControlId` (`:1389-1394`): resolve `loopState.Focused: RetainedId` → node's `Key ?? path` (T005 resolver) instead of `Key ?? Kind`, so `FocusedControl` matches the re-pointed bridge.
- [X] T020 [US2] Update `src/Controls/ControlRuntime.fsi` doc text (if any references `Key ?? Kind` for the visual-state/scroll bridge) to `Key ?? path`. Run US2 tests → green.

**Checkpoint**: US2 independently testable and green; hover/press/scroll/ring correct for unkeyed controls; at-rest identity intact.

---

## Phase 5: User Story 3 — transient widgets carry the ids they declare (P2)

**Goal**: DatePicker/SplitButton trigger ids resolve to real controls (no `MissingOverlayAnchor`); focus-scope stops reference real ids.
**Independent test**: lower a DatePicker/SplitButton → declared `triggerId` present in lowered ids; overlay-anchor diagnostic clean.

### Tests (fail before)

- [X] T021 [P] [US3] Test in `tests/Controls.Tests/`: lowered `DatePicker` and `SplitButton` — the declared `triggerId` (`rootId + "-trigger"`) is carried by a real lowered control; the overlay-anchor diagnostic emits **no** `MissingOverlayAnchor`. (Maps SC-004.)
- [ ] T022 [P] [US3] Test in `tests/Controls.Tests/`: a `focusScope`-declaring widget's `Stops`/`InitialFocus`/`RecoveryTarget` all reference ids carried by real lowered controls (no fabricated `-item-N` phantom stop). (Maps SC-004.)

### Implementation

- [X] T023 [US3] `src/Controls/Widgets/Pickers.fs` (DatePicker, `~:45-70`): key the trigger `Button` with `triggerId` (`Control.withKey triggerId`), so `AnchorId`/`TriggerId` resolve.
- [X] T024 [US3] `src/Controls/Widgets/Buttons.fs` (SplitButton, `~:60-85`): key the trigger `Button` with `triggerId`.
- [ ] T025 [US3] **DEFERRED (scoped, Constitution "explicit deferral").** `focusScope`'s fabricated
  `surfaceId + "-item-N"` stops are produced by the SHARED `WidgetLowering.transientMetadata` (7 widget
  callers: Overlay, Navigation×2, Buttons, CollectionsWidgets, Pickers×2) plus a separate copy in
  `DataEntry2.fs`, and feed overlay focus-trap traversal (`OverlayState.fs:273/482/577/585`,
  `Pointer.fs:135`, `Focus.fs:233`). Deriving real stops from each overlay's lowered content is a
  distinct 8-site change to overlay-trap semantics with its own regression surface, disproportionate to
  fold into this id-unification. The issue's **explicitly named** "structurally guaranteed
  `MissingOverlayAnchor`" is the trigger→anchor link, which T023/T024 fully fix. Follow-up filed to
  unify the `focusScope` item-stops onto real content ids. `RecoveryTarget = triggerId` becomes valid
  now that the trigger is keyed (T023/T024). No `focusScope` code change in THIS feature (leaving the
  phantom stops unchanged is safer than an empty-stops behavior shift that would perturb overlay auto-focus).

**Checkpoint**: US3 independently testable and green; no phantom widget ids.

---

## Phase 6: User Story 4 — diagnostics & `.fsi` describe the unified scheme (P3)

**Goal**: the unkeyed-collapse diagnostic and doc/`.fsi` contracts describe `Key ?? path`.
**Independent test**: read the diagnostic message + `.fsi` doc comments → reference `Key ?? path`; collapse warning fires only for genuine authoring ambiguity.

### Tests

- [X] T026 [P] [US4] Test in `tests/Controls.Tests/` (diagnostics): the unkeyed same-kind collapse rule still fires for two unkeyed same-kind **interactive** siblings and its message references the unified `Key ?? path` scheme + `Control.withKey` remediation.

### Implementation

- [X] T027 [US4] `src/Controls/Diagnostics.fs` (`:196-220`) and `src/Controls/Diagnostics.fsi` (`:79`): re-point the unkeyed-collapse rule text from `(Key ?? Kind)` to the unified `Key ?? path` scheme; keep the diagnostic shape.
- [X] T028 [P] [US4] Sweep remaining `Key ?? Kind` doc/comment references in touched modules (`Focus`, `ControlRuntime`, widget lowering) and the free-form label at `ControlsElmish.fs:969` → `Key ?? path` (label is non-correctness per research.md; change for consistency).

**Checkpoint**: contracts coherent with behavior.

---

## Phase 7: Polish & cross-cutting

- [X] T029 Update/annotate any existing test that ENCODED the unkeyed `Key ?? Kind` behavior (search `tests/` for `?? *.Kind`/`Key ?? Kind` focus/hover assertions) to the unified scheme, with a comment citing feature 232. Keyed-control tests MUST remain unchanged (regression guard).
- [ ] T030 Remove the temporary T004 smoke repro (or fold its assertions into the permanent US1/US2 tests).
- [X] T031 Run the full solution build + entire test suite; compare against the T001 baseline — only the intended new tests flip red→green, no keyed regression. Run the public-surface / ApiCompat gate; if a doc-only snapshot diff appears, accept it with a note pointing to `contracts/fsi-surface-deltas.md`. (Maps SC-006.)
- [ ] T032 `/speckit-analyze` (optional) cross-artifact consistency check; capture per-phase feedback via `fs-gg-feedback-capture` if friction arose.

---

## Dependencies & order

- **Setup (T001-T002)** → **Foundational (T003-T006)** → stories.
- **T005 resolver** blocks the Group A/B host seams (T010, T011, T019).
- **US1 (Group A: T009-T012)** and **US2 (Group B + scroll: T017-T020)** are the two P1 MVP slices; each is internally atomic (its move-group lands together) but the two stories are independently testable.
- **US3 (T021-T025)** and **US4 (T026-T028)** are independent of US1/US2 and of each other (different files).
- **Polish (T029-T032)** last.

## Parallel opportunities

- Tests T007/T008 (US1), T013-T016 (US2), T021/T022 (US3), T026 (US4) are `[P]` (distinct test files).
- US3 (widgets) and US4 (diagnostics/docs) implementation can proceed in parallel with US1/US2 once T005 lands — they touch disjoint files.

## MVP scope

**US1 + US2** (both P1) = the correctness core: unkeyed controls dispatch on keyboard and receive
hover/press/scroll/ring correctly. US3 (phantom widget ids) and US4 (docs) complete the finding.

## Completion ledger (implementation)

- **Delivered & green** (28 tasks marked `[X]`): the full id-unification — `Focus.order`, the three
  `ControlRuntime` bridges, the path-aware `RetainedRender.retainedCanonicalId` resolver + `.fsi`, the
  four Elmish host seams (`routeFocusedKey` incl. full-tree binding filter, `focusedControlId`,
  `retainedIdOfControl`, text label), the DatePicker/SplitButton trigger keys, and the re-pointed
  diagnostics + `.fsi` docs. New tests: `Feature232IdSchemeTests` (Controls, 8) +
  `Feature232FocusDispatchTests` (Elmish, 3), all fail-before/pass-after; 3 Feature-072 parity goldens
  updated (T029).
- **T004 early smoke** realized as the permanent US1/US2 fail-before tests (unkeyed keyboard-dispatch
  drop + unkeyed hover) rather than throwaway repros → **T030 folded in** (no temp repro to remove).
- **T014 (touched-count) / T016 (focus ring)**: covered by the preserved, still-green Feature-112
  targeted-stamp parity suite and Feature-096/175 focus tests (keyed trees unchanged; at-rest identity
  asserted in `Feature232IdSchemeTests`).
- **T022 + T025 — DEFERRED (scoped)**: the `focusScope` `-item-N` phantom **focus-scope stops** (shared
  `transientMetadata`, 8 call sites, overlay-trap semantics). The issue's named structural
  `MissingOverlayAnchor` (trigger→anchor) IS fixed (T023/T024). Follow-up to unify the item-stops.
- **T032**: `/speckit-analyze` optional — skipped (single-feature scope; artifacts self-consistent).

## Format validation

All tasks are `- [ ]` checkboxes with sequential IDs, `[P]`/`[Story]` labels where applicable, and
explicit file paths. Setup/Foundational/Polish carry no story label; US1-US4 phases carry theirs.
