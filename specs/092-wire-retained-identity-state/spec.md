# Feature Specification: Wire Retained Identity State onto the Live Path (Feature 092)

**Feature Branch**: `092-wire-retained-identity-state`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification, the first of the Workstream C (C1) backfills that
follow the pattern feature 091 established and features 093/095/096 continued. Feature 091 wired the
parked keyed reconciler onto the render path and made each frame confer a **stable identity**
(`RetainedId`) on every matched node, carrying a per-identity state map (`StateByIdentity`) frame to
frame. But **091 only *carried* that state map — the live Elmish host ignored it.** Focus, in-progress
text, and per-control state therefore still resolved through the old `ControlId` `hitTest` path, which
collapses unkeyed same-kind siblings and could not survive a positional shift on the real path.

Feature 092 **wires the retained identity state into the live host**: the Elmish adapter
(`ControlsElmish`) now **reads and writes** `StateByIdentity` through the real
`resolveFocus` → `retainedHitTest` → `routeFocusedText` → `RetainedRender.step` seam, so focus and an
in-progress text edit are keyed to the node's stable `RetainedId` and **survive** an unrelated
re-render that shifts the control's position. Alongside that wiring, 092 lands four supporting
guarantees the imported source already carries: **theme becomes part of the fragment reuse key** (a
theme change between frames invalidates cached fragments and repaints faithfully); the first frame is
**painted exactly once** (`init` returns the painted scene the adapter reuses, and surfaces frame-0
diagnostics 091 reported a frame late); the work-reduction accounting **honestly splits** changed-vs-
shifted nodes under a layout shift; and it fixes two carried defects from the pre-wiring path (a
pre-filled field being wiped on its first keystroke, and a text-area hard-coded to single-line).

The implementation, the accreted `RetainedRender.fsi` surface (`init`, `step`, the
`RetainedRender<'msg>`/`RetainedInit<'msg>` records with `StateByIdentity`/`Theme`, `retainedHitTest`),
the executable suites (`Feature092RetainedRenderTests` in `Controls.Tests`,
`Feature092LiveSurvivalTests` in `Elmish.Tests`), and the captured readiness evidence under
`specs/092-wire-retained-identity-state/readiness/` **already exist** in the imported, rebranded
source. **No Spec Kit spec/plan/tasks have ever described this work.** This document backfills the
contract so the capability is governed by `Spec → .fsi → semantic tests → implementation` like any
other feature.

The whole surface is **assembly-internal** (it lives in `module internal RetainedRender` and the
`Controls.Elmish` adapter internals), exactly like the reconciler it builds on. It adds **zero**
public-surface-baseline delta. Per the constitution's vertical-slice rule, the in-assembly
Expecto/FsCheck tests (reaching the internals via `InternalsVisibleTo`) **are** the user-reachable
surface for these internal user stories.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Focus and an in-progress text edit survive an unrelated re-render through the real host seam (Priority: P1)

A user typing into a field keeps their focus **and the characters they have already typed** when an
unrelated part of the screen updates and shifts that field's position — driven through the **real**
Elmish adapter seam (`resolveFocus` + `routeFocusedText` + `RetainedRender.step`), not a hand-seeded
test fixture. The live host now reads and writes the per-identity state map keyed to the field's
stable `RetainedId`, so the state is found again under the same identity after the shift. A
rebuild-every-frame baseline (re-running `init` each frame) **loses** the state, demonstrating the
wiring is the fix.

**Why this priority**: This is the headline payoff of 092 over 091 — 091 carried the identity state
but the host ignored it, so on the *real* path focus and in-progress edits still jumped. Wiring the
host to actually use it is the MVP slice and the whole point of the feature.

**Independent Test**: Through the real adapter seam, focus an editor, type `x` (draft becomes `hix`),
insert a banner above it so its position shifts, then type `y`; confirm the draft is `hixy` (continued,
not reset) and focus survived — with **no** hand-seeded focus/text state. Run the same sequence against
a rebuild-every-frame baseline and confirm it loses the identity and the draft.

**Acceptance Scenarios**:

1. **Given** an editor focused and edited to draft `hix` through the live adapter, **When** an
   unrelated sibling is inserted above it (shifting its position), **Then** typing `y` yields draft
   `hixy` — focus and the in-progress edit survived under the same `RetainedId`.
2. **Given** the same sequence, **When** the retained structure is rebuilt every frame (no carried
   identity), **Then** the editor's identity differs across the shift and the focus/draft state is lost
   (the baseline fails the proof).
3. **Given** a control whose kind changed at the same key (a `Replace`), **When** the frame is stepped,
   **Then** the prior identity's state is dropped — no false carry onto the replacement.
4. **Given** a control that is removed in the next frame, **When** the frame is stepped, **Then** its
   per-identity state is filtered out (focus would clear), not retained as orphaned state.

---

### User Story 2 - Every field resolves to its own identity, and a pre-filled field is never wiped (Priority: P1)

Distinct fields — whether **keyed**, **unkeyed**, or wrapped in a **keyed container** — each resolve to
their **own** stable identity when hit-tested, so focusing one field never collapses onto another. A
field that already contains text keeps that text: its **first keystroke appends** rather than wiping
the pre-filled value, and a text-area honors **multi-line** mode rather than being forced to
single-line. A control wired with **more than one** change handler dispatches **every** matched handler.

**Why this priority**: This closes the concrete correctness defects the pre-wiring `ControlId` hit-test
path carried (the "090 defects"): unkeyed same-kind siblings collapsed onto a shared identity, a
pre-filled field was wiped on first keystroke, and text-areas were hard-coded single-line. These are
co-critical with US1 — focus survival is only correct if the identity it resolves to is the right one
and the value under it is preserved.

**Independent Test**: Build three fields (one keyed, one unkeyed, one keyed-container-wrapped),
hit-test each, and confirm all three resolve to **distinct** `RetainedId`s, each its own field.
Pre-fill a multi-line text-area with `line1`, focus it, send `X`, and confirm the draft is `line1X`
(appended, MultiLine) — zero characters lost. Wire a control with two change handlers and confirm both
fire on a single change.

**Acceptance Scenarios**:

1. **Given** keyed, unkeyed, and keyed-container-wrapped fields in one tree, **When** each is
   hit-tested via the retained hit-test, **Then** all three resolve to **distinct** `RetainedId`s and
   each resolves to its own field (no shared-id collapse).
2. **Given** a multi-line text-area pre-filled with `line1` and focused, **When** the first keystroke
   `X` arrives, **Then** the draft becomes `line1X` (appended in MultiLine mode), losing zero
   characters.
3. **Given** a control bound with more than one change handler, **When** a single change occurs,
   **Then** every matched handler is dispatched.
4. **Given** a hit-test point outside the root, **When** it is resolved, **Then** it returns `None` (a
   true gap, not a false match).

---

### User Story 3 - A theme change repaints faithfully; an unchanged tree under the same theme reuses everything (Priority: P2)

When the active theme changes between frames (e.g. light → dark), every cached render fragment is
invalidated and repainted, so the produced frame is **byte-identical to a full rebuild under the new
theme** (and visibly differs from a rebuild under the old theme). When the theme does **not** change and
the tree is identical, the frame **reuses everything** — no spurious repaint.

**Why this priority**: Theme is a render-affecting input that the reuse key must account for; if it
weren't in the key, a theme switch would silently keep stale colors. It is P2 because it guards
correctness of reuse rather than delivering the core focus-survival journey, and is independently
demonstrable.

**Independent Test**: Render a fixed tree under the light theme, change the theme to dark between
frames with the tree otherwise unchanged, and assert the second frame is byte-identical to a full
rebuild under dark and differs from a rebuild under light. Separately, step an identical tree with no
theme change and assert nothing is recomputed.

**Acceptance Scenarios**:

1. **Given** a tree painted under the light theme, **When** the next frame keeps the tree but switches
   the theme to dark, **Then** the wired frame is byte-identical to a full rebuild under dark and
   differs from a rebuild under light (all fragments repainted).
2. **Given** the same tree with **no** theme change, **When** the next frame is stepped, **Then**
   everything is reused — zero nodes recomputed (no spurious repaint).

---

### User Story 4 - Work reduction under a layout shift is accounted honestly (Priority: P2)

When a localized change shifts the position of an otherwise-unchanged node (e.g. inserting a sibling
**above** a fixed-size leaf, which relays the leaf out without changing it), the per-frame work is
reported **honestly**: the recomputed-node count splits into the genuinely-changed subtree plus the
relaid-out (shifted) nodes, and the total stays **strictly below** a full rebuild.

**Why this priority**: 091 proved bounded work for a change with no geometry shift; a shift forces a
relayout of the unchanged leaf, and the accounting must not hide that as "free." Honest accounting is
the correctness guard on the efficiency claim — P2 because it protects the work-reduction story rather
than the focus journey.

**Independent Test**: Insert a sibling above a fixed-size leaf and assert
`RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount` and `< BaselineNodeCount` — the
recomputed count is exactly the changed subtree plus the shifted nodes, and still less than rebuilding
everything.

**Acceptance Scenarios**:

1. **Given** a 3-node tree with a sibling inserted above a fixed-size leaf, **When** the frame is
   stepped, **Then** `RecomputedNodeCount` (2) `= ChangedSubtreeBound` (1) `+ ShiftedNodeCount` (1) and
   is strictly less than `BaselineNodeCount` (3).

---

### User Story 5 - The first frame paints exactly once and surfaces its diagnostics immediately (Priority: P3)

The first frame is **painted once**: `init` returns the painted render result the adapter reuses,
instead of the adapter also calling a full `Control.renderTree` (no double paint). Any malformed input
present in the **very first** tree — e.g. a duplicate sibling key — is surfaced as a diagnostic on
**frame 0**, not a frame late.

**Why this priority**: This is an efficiency-and-honesty refinement to the first frame: it removes a
redundant first-frame paint and closes a one-frame diagnostic lag from 091. P3 because it polishes the
initialization path rather than delivering a new user-facing journey.

**Independent Test**: Run `init` on a first frame and assert its `Render` is byte-identical to a full
rebuild and the first-frame paint count is exactly 1. Run `init` on a first frame containing a
duplicate-keyed sibling list and assert a `KeyCollision` diagnostic is surfaced on frame 0 while the
init stays total (no throw).

**Acceptance Scenarios**:

1. **Given** a first frame, **When** `init` runs, **Then** its `Render` is byte-identical to a full
   rebuild of that frame and the frame was painted exactly once (no double paint).
2. **Given** a first frame whose sibling list has duplicate keys, **When** `init` runs, **Then** a
   `KeyCollision` diagnostic is surfaced on frame 0 and `init` completes without throwing.

---

### Edge Cases

- **Positional shift of a focused, mid-edit field**: must keep identity and the in-progress draft (the
  core defect class 092 wires the host to fix).
- **Unkeyed same-kind siblings**: must each resolve to a **distinct** identity (no shared-id collapse),
  unlike the old `ControlId` hit-test.
- **Replace (kind change at the same key)**: the prior identity's state must be **dropped**, never
  falsely carried onto the replacement.
- **Removed control**: its per-identity state must be **filtered out** (focus clears), not orphaned.
- **Pre-filled field, first keystroke**: must **append** (zero characters lost); a text-area must honor
  MultiLine.
- **Theme change with an otherwise-identical tree**: must repaint **all** fragments (theme is in the
  reuse key), byte-identical to a rebuild under the new theme.
- **Duplicate keys on frame 0**: must surface a `KeyCollision` on frame 0 and stay total.
- **Hit-test outside the root**: must resolve to `None` (a true gap).
- **Multi-frame chain**: identity must carry across every frame and each frame stay byte-identical to a
  full rebuild.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The live Elmish host MUST **read and write** the per-identity state map
  (`StateByIdentity`, keyed by `RetainedId`) on the real render path — not merely carry it — so
  per-control state is resolved and persisted through the stable identity rather than the path-derived
  `ControlId`.
- **FR-002**: Keyboard focus and an in-progress text edit MUST survive an unrelated re-render that
  shifts the control's position, found again under the **same** `RetainedId`, with the in-progress draft
  **continued** (not reset) — proven through the real adapter seam (`resolveFocus` + `routeFocusedText`
  + `RetainedRender.step`), with no hand-seeded state.
- **FR-003**: A rebuild-every-frame baseline (re-running `init` each frame, no carried identity) MUST
  **lose** the focus/draft state across the same shift — the wired step path is what preserves it.
- **FR-004**: The retained hit-test MUST resolve a point to the **deepest** retained node whose cached
  box contains it, returning a **distinct** `RetainedId` per node — including for **unkeyed same-kind
  siblings** and keyed-container-wrapped fields — and MUST return `None` for a point outside the root
  (no shared-id collapse, unlike the `ControlId` hit-test path).
- **FR-005**: A pre-filled field's **first keystroke** MUST **append** to the existing value (zero
  characters lost), and a text-area MUST honor **MultiLine** mode (fixing the carried defects where an
  empty seed wiped a pre-filled field and a text-area was hard-coded single-line).
- **FR-006**: A control wired with **more than one** change handler MUST dispatch **every** matched
  handler on a single change. (A binary correctness guarantee verified directly by the multi-handler
  case in `Feature092RetainedRenderTests`; this requirement has no separate Success Criterion.)
- **FR-007**: A `Replace` (kind change at the same key) MUST **drop** the prior identity's state (no
  false carry), and a **removed** control's per-identity state MUST be **filtered out** (focus clears).
- **FR-008**: The active **theme** MUST be part of the fragment reuse key: a theme change between frames
  MUST invalidate all cached fragments and repaint, producing a frame **byte-identical to a full rebuild
  under the new theme** (and differing from a rebuild under the old theme); an unchanged tree under an
  **unchanged** theme MUST reuse everything (zero recompute, no spurious repaint).
- **FR-009**: Under a sibling-shifting layout change, the work-reduction accounting MUST split honestly:
  `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount`, and that total MUST be strictly less
  than `BaselineNodeCount` (a full rebuild).
- **FR-010**: `init` MUST paint the first frame **exactly once** — returning the painted `Render` the
  adapter reuses (no second full `Control.renderTree`) — and MUST surface any first-frame duplicate-key
  `KeyCollision` as a diagnostic on **frame 0**, staying total (never throws).
- **FR-011**: Across a chained multi-frame sequence (init plus ≥3 steps), each frame's wired render MUST
  be **byte-identical** to a full rebuild of that frame, the same node MUST **carry its identity** across
  the chain, and the wired step MUST be **total** and **deterministic** on the live path.
- **FR-012**: The entire surface MUST remain **assembly-internal** — zero public-surface-baseline delta
  — and remain exercised only through the in-assembly tests via `InternalsVisibleTo`. (Verified directly
  by the surface-drift check; this requirement has no separate Success Criterion.)

### Key Entities *(include if feature involves data)*

- **RetainedId**: the stable, path-independent identity conferred on a matched node (feature 091); the
  key 092 wires the live host's per-control state to.
- **StateByIdentity**: the per-identity UI-state map (`Map<RetainedId, RetainedUiState>`) carried in the
  host loop's retained state — focus, in-progress text draft, and per-identity animation/clock state —
  now **read and written** by the live adapter (091 only carried it).
- **RetainedUiState**: the per-control state stored under an identity (focus flag, in-progress text
  draft + line mode, animation clock) that must survive positional shifts.
- **RetainedRender<'msg>**: the per-frame retained root plus the identity counter, `StateByIdentity`, and
  the **`Theme`** the structure was painted under (the theme that participates in the reuse key).
- **RetainedInit<'msg>**: the first-frame result — the seeded retained structure, the painted `Render`
  the adapter reuses, and any frame-0 `Diagnostics`.
- **RetainedRenderStep<'msg>**: the per-frame `step` result — the next `RetainedRender` (`Retained`), the
  rendered frame (`Render`, byte-identical to a full rebuild of the next tree), the frame's
  `Diagnostics`, and the `WorkReduction` (`WorkReductionRecord`) that splits changed-vs-shifted nodes.
- **Retained hit-test**: resolves a point to the stable identity of the deepest retained node whose box
  contains it (per-node identity, no unkeyed-sibling collapse), used by the live `resolveFocus`.
- **Diagnostic**: an observable warning (e.g. `KeyCollision`) surfaced through the control-diagnostic
  channel — now on frame 0 for first-frame malformed input.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Live focus and an in-progress text edit survive a position-shifting re-render through the
  real adapter seam in 100% of cases (the draft continues, e.g. `hix` → `hixy`); the rebuild-every-frame
  baseline loses the state in 100% of the same cases.
- **SC-002**: Keyed, unkeyed, and keyed-container-wrapped fields resolve to **distinct** identities in
  100% of cases, each to its own field; a pre-filled multi-line field loses **zero** characters on its
  first keystroke.
- **SC-003**: Under a sibling-inserted-above layout shift,
  `RecomputedNodeCount = ChangedSubtreeBound + ShiftedNodeCount` and is strictly less than
  `BaselineNodeCount`.
- **SC-004**: Each frame in a chained sequence of ≥4 frames (init + 3 steps) is byte-identical to a full
  rebuild of that frame.
- **SC-005**: The first frame is painted exactly once (paint count = 1, `Render` byte-identical to a full
  rebuild), and a frame-0 duplicate-key `KeyCollision` is surfaced on frame 0 in 100% of cases while
  `init` stays total.
- **SC-006**: A theme change repaints byte-identically to a full rebuild under the new theme and differs
  from a rebuild under the old theme; an unchanged tree under an unchanged theme reuses everything (zero
  recompute) in 100% of cases.
- **SC-007**: The same node carries its identity across every frame of the multi-frame chain in 100% of
  cases (identity continuity).

## Assumptions

- The keyed reconciler (feature 067) and the retained render structure with stable identity and the
  carried `StateByIdentity` map (feature 091) already exist in the imported source; 092 is the
  **backfilled contract** for *wiring the live host to read/write that state* (plus theme-in-reuse-key,
  single first-frame paint, honest work accounting, and the pre-filled/MultiLine defect fixes) — not
  new-from-scratch construction.
- The surface stays **internal**; "users" of these stories are framework internals plus the in-assembly
  tests (per the constitution's vertical-slice rule), not external package consumers. No public API is
  added.
- The per-control state worth preserving across re-renders is focus and the in-progress text draft;
  per-control **animation-clock** survival has its own real host seam in feature 099 (R4) and is proven
  there (`Feature099AnimationClockTests/us2-survival`) — 092 keeps proving focus + in-progress text
  survival and only carries the clock state through the same map.
- Render-output equivalence is judged by **structural scene equality** (plus bounds and node count), the
  authoritative parity proof; `SceneEvidence.renderPng` is a capability-hash, not a pixel encoder, so
  pixel-level / desktop-visibility proofs are out of scope (the readiness evidence explicitly does not
  claim them).
- The existing readiness evidence under `readiness/` (live-survival, focus-resolution, theme-reuse,
  work-reduction, multi-frame, first-frame) corresponds to SC-001 through SC-007 and is the captured
  artifact for those outcomes.
- This is the **C1** conformance backfill in the 2026-06-15 missing-features plan, following the 091
  pattern and the 093/095/096 closes; `/speckit-plan`, `/speckit-tasks`, and `/speckit-implement` reduce
  to a conformance pass (confirm the suites are green, the readiness evidence regenerates, and the
  surface delta is zero), not a build.
