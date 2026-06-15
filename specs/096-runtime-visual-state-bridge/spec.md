# Feature Specification: Runtime Visual-State Bridge (Feature 096)

**Feature Branch**: `096-runtime-visual-state-bridge`

**Created**: 2026-06-15

**Status**: Final

**Input**: User description: "next item in the implementation plan"

## Context

This specification **backfills the contract** for a capability that already ships in the imported,
rebranded source: the **runtime visual-state bridge** — the seam that turns *live interaction*
(which control is focused, hovered, pressed, or selected) into the per-control `VisualState` that
feature 093's resolver paints, **with zero consumer code**. A control the app author never styled
restyles on hover / press / focus / selection because the host stamps the derived state onto the
lowered control tree just before reconcile; with no interaction and no consumer-set state, the tree
is returned **byte-identical** to its un-bridged self.

The mechanism is two functions on `ControlRuntime` (`src/Controls/ControlRuntime.fsi` / `.fs`): the
public, pure projection `deriveVisualState : model -> controlId -> VisualState`, and the internal
host bridge `applyRuntimeVisualState : model -> control -> control` that walks the lowered
`Control<'msg>` tree and stamps each control's derived state onto the single `visualState` attribute
carrier (the same carrier feature 093's `visualStateOf` reads and the resolver consumes). The code,
the `.fsi` surface, captured readiness evidence
(`specs/096-runtime-visual-state-bridge/readiness/`), and an executable test suite
(`Feature096RuntimeBridgeTests` / `Feature096BridgePropertyTests`) all exist, but **no Spec Kit
spec/plan/tasks ever described them**. This document brings the feature under the project's
`Spec → .fsi → semantic tests → implementation` contract — the same conformance-backfill pattern
features 091, 093, and 095 established — and records the import-before-spec deviation against
Constitution Principle I.

The capability itself: feature 093 introduced the *resolver* that maps a control's `VisualState`
(Normal / Hover / Pressed / Focused / Selected / Disabled / Loading / Validation) to a concrete
draw style, and feature 095 introduced *structural* slot composition. Neither, on its own, makes a
control **react to the user**: 093 paints whatever `VisualState` a control already carries, but
something must *set* that state from live input. Feature 096 is that something. The
**`ControlRuntime` MVU** already tracks interaction (focus, hover, press, selection, caret, drag)
as it receives input messages; 096 adds the **projection** from that runtime model to a single
`VisualState` per control, and the **bridge** that stamps it onto the tree on the render path. The
projection follows one fixed, closed precedence — `Pressed > Selected > Focused > Hover > Normal` —
the runtime-derivable *tail* of the full visual-state order
`Disabled > Validation > Loading > Pressed > Selected > Focused > Hover > Normal`; the three head
states (`Disabled` / `Validation` / `Loading`) are **author intent**, set by the consumer and never
derived from interaction. The bridge honors that boundary: a consumer-set non-`Normal` state always
wins; only a consumer-`Normal` (or absent) slot is filled by the derived runtime state. The
projection is **pure, total, and deterministic** (no clock, no randomness, never throws, no per-kind
branching), and the bridge is **byte-identical at rest** — a derived `Normal` emits nothing, so a
control nobody is interacting with paints exactly as it did before the bridge existed.

Feature 096 is paired with features 093 (visual-state style layer) and 095 (lookless slot
composition) under Workstream C11 of the missing-features plan — together the visual-state /
composition vocabulary that the design-system and theme work (Workstreams F and D) plug into. The
runtime states 096 derives are consumed by 093's resolver (E3) rather than duplicated, and they ride
the control's attributes through feature 092's retained-identity keyed diff (E2), so a focus
indicator survives a sibling-shifting re-render.

This feature's **only public-surface entry** is `ControlRuntime.deriveVisualState`; the
`ControlRuntime` module (and its `ControlRuntimeModel` / `Msg` / `Effect` types) was already
committed in `tests/surface-baselines/FS.GG.UI.Controls.txt` at import, the bridge
`applyRuntimeVisualState` (and the later feature-112 targeted-stamp variants) are `internal`, and
the authoring path consumers actually use is feature 093's typed visual-state attribute. Backfilling
the spec adds **zero new public-surface-baseline delta**. Consistent with Constitution Principle I.3
(tests assert observable behavior, not internals), the in-assembly Expecto/FsCheck tests reach the
internal bridge only via `InternalsVisibleTo("Controls.Tests")` to drive the host seam and assert on
the **observable resolved paint** (`faithfulContent` / `renderTree.Scene`) and the lowered control's
visible `VisualState`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A control restyles from live interaction with zero consumer code (Priority: P1)

An app author migrates a built-in control and writes **no interaction-styling code** — no hover
handler, no `:focus` rule, no pressed-state branch. When the user hovers, focuses, presses, or
selects that control, it **restyles automatically**: the host derives the control's `VisualState`
from the live `ControlRuntime` interaction model and stamps it onto the control before render, and
feature 093's resolver paints the new state. A pressed primary button shows its pressed fill; a
focused text-box shows its focus-ring border — all from the runtime bridge, not from authored style.

**Why this priority**: This is the whole point of the feature — interaction-driven styling that
"just works" for migrated controls. The projection (`deriveVisualState`) plus the stamp
(`applyRuntimeVisualState`) is the MVP slice every other story builds on, and it is the seam that
makes feature 093's resolver actually react to the user.

**Independent Test**: Build a model in which one control id is simultaneously pressed, selected,
focused, and hovered, and confirm `deriveVisualState` returns the highest-ranked state, peeling each
state off to reveal exactly the next-ranked one (`Pressed > Selected > Focused > Hover > Normal`);
confirm an id named by no interaction state, and an unknown id, both resolve to `Normal`; stamp a
hovered/pressed/selected/focused model onto a control the consumer left **unstyled** and confirm the
lowered control now carries the derived state; confirm the **resolved paint** of a pressed button
and a focused text-box differs from their Normal render (the runtime state actually drove the draw,
not just the attribute); confirm a non-interacted sibling stays `Normal` and is returned unchanged.

**Acceptance Scenarios**:

1. **Given** a model where `"btn"` is simultaneously pressed, selected, focused, and hovered,
   **When** `deriveVisualState` is queried, **Then** it returns `Pressed`; removing the press
   reveals `Selected`, then `Focused`, then `Hover`, then `Normal` (the closed runtime precedence).
2. **Given** an id named by no interaction state (or an unknown id), **When** `deriveVisualState` is
   queried, **Then** it returns `Normal` (never an exception).
3. **Given** a hovered model and a control the consumer left unstyled, **When** the bridge is
   applied, **Then** the lowered control carries `Hover`; under a pressed model it carries `Pressed`;
   under a selection model it carries `Selected`.
4. **Given** a pressed primary button (and a focused text-box), **When** the bridge stamps the
   derived state, **Then** the resolved paint differs from the control's Normal render (the runtime
   state visibly restyled it via feature 093's resolver).
5. **Given** a hovered model, **When** the bridge walks the tree, **Then** a non-interacted sibling
   resolves `Normal` and is returned structurally unchanged (no attribute added).

---

### User Story 2 - An un-interacted control is byte-identical to its un-bridged self (Priority: P1)

A control (or a whole tree) that nobody is interacting with — an empty runtime model, no consumer
visual-state attribute — behaves exactly as it did before the bridge existed: the bridge adds no
attribute anywhere, the rendered `Scene` is byte-identical to the un-bridged build, and on the live
retained path the at-rest frame recomputes **zero** nodes. An inert (un-bridged) build paints
identical frames regardless of interaction; the bridged build differs only *because* interaction
restyles — the bridge is otherwise transparent.

**Why this priority**: This is the safety guarantee that lets the bridge ship on the live render
path — inserting a runtime-state stamp into every frame must not change what an at-rest control
paints, must not allocate or invalidate at rest, and must not regress the retained-render work
reduction. It is co-critical with US1: interaction styling is only trustworthy if the
no-interaction case is provably inert.

**Independent Test**: Apply the bridge with an **empty model** to a multi-control tree and confirm
the tree is structurally unchanged (no attribute added anywhere); render the at-rest bridged tree
and confirm its `Scene` is byte-identical to the un-bridged build; drive the live `RetainedRender`
step with the at-rest bridged tree and confirm `WorkReduction.RecomputedNodeCount` is `0`.

**Acceptance Scenarios**:

1. **Given** an empty runtime model and a multi-control tree, **When** the bridge is applied,
   **Then** the tree is returned structurally unchanged (no `visualState` attribute added anywhere).
2. **Given** the at-rest bridged tree, **When** it is rendered, **Then** its `Scene` is byte-
   identical to the un-bridged build (a derived `Normal` emits nothing).
3. **Given** the at-rest bridged tree, **When** it is fed to the live `RetainedRender.step`,
   **Then** `RecomputedNodeCount` is `0` (an at-rest frame is identity ⇒ zero recompute).

---

### User Story 3 - Author intent out-ranks derived interaction, through one carrier channel (Priority: P2)

An app author who *does* set a control's state — `Disabled`, `Selected`, a `Validation` result —
keeps that state even while the runtime reports the same control hovered, pressed, or focused.
Consumer intent is authority; the derived runtime state only fills a slot the consumer left at
`Normal`. The bridge enforces this through a **single carrier channel**: the lone `visualState`
attribute that 093's resolver reads, replace-or-appended so it never accumulates stale state.

**Why this priority**: Without this arbitration the runtime would silently override author intent —
a disabled button would "light up" when pressed, a validation-failed field would lose its error look
on focus. It is P2 because it refines US1's stamp with the consumer-vs-derived boundary; it is
verified by composing the projection with consumer-set attributes.

**Independent Test**: Give a consumer-`Disabled` control to a model that reports it hovered, pressed,
and focused, and confirm it stays `Disabled`; give a consumer-`Selected` control to a model that
reports it pressed and confirm it stays `Selected`; give a consumer-`Normal` (unset) control to a
model that reports it focused and confirm the derived `Focused` fills the slot; property-test
(≥1000 generated `(model, id, consumer-state)` combos) that a consumer non-`Normal` state is
preserved 100% of the time and a consumer-`Normal` control takes the derived runtime state, that the
result is deterministic, and that the bridge is total (never throws). Confirm the head states
(`Disabled` / `Validation` / `Loading`) are **never** produced by the projection.

**Acceptance Scenarios**:

1. **Given** a consumer-`Disabled` control the runtime reports hovered + pressed + focused, **When**
   the bridge is applied, **Then** the control stays `Disabled` (author intent out-ranks every
   derived state).
2. **Given** a consumer-`Selected` control the runtime reports pressed, **When** the bridge is
   applied, **Then** the control stays `Selected`.
3. **Given** a consumer-`Normal` (unset) control the runtime reports focused, **When** the bridge is
   applied, **Then** the derived `Focused` fills the slot.
4. **Given** ≥1000 generated `(model, id, consumer-state)` combos, **When** the bridge is applied,
   **Then** a consumer non-`Normal` state is preserved 100%, a consumer-`Normal` control takes
   `deriveVisualState model id`, the result is deterministic, and the bridge never throws.

---

### User Story 4 - The runtime look rides retained identity and repaints only what changed (Priority: P2)

Because the derived state is stamped onto the control's attributes **before reconcile**, the runtime
look rides feature 092's stable retained identity (E2) through the keyed diff: a focus indicator
attached to a control survives an unrelated re-render that shifts the control's position (e.g. a
banner inserted above it), where a baseline keyed only by position would lose it. And because the
bridge changes only the controls whose interaction state actually changed, a localized interaction —
a single hover entering one control — produces a **bounded** repaint (fewer than all nodes), not a
full-tree repaint.

**Why this priority**: This is the payoff that makes the runtime bridge cohere with retained render —
interaction styling is identity-stable and incrementally cheap, not a per-frame full rebuild. It is
P2 because it depends on US1 producing the stamped state and on the live `RetainedRender` path
already in place (092/091); 096 only consumes them.

**Independent Test**: Focus an editor, derive its `Focused` state via the bridge, insert a banner
above it (shifting siblings), and re-derive through the live retained path — confirm the control
keeps its retained id and stays `Focused` across the shift, where a position-keyed baseline loses
identity; render a 3-control row at rest, then step the live `RetainedRender` with a model hovering
exactly one control, and confirm `RecomputedNodeCount < BaselineNodeCount` (bounded repaint) while
the hovered control is counted as changed work.

**Acceptance Scenarios**:

1. **Given** a focused control whose `Focused` state was derived via the bridge, **When** a sibling
   is inserted above it (shifting positions) and the tree is re-derived via the live retained path,
   **Then** the control keeps its retained id and stays `Focused` (E2), where a position-keyed
   baseline loses identity on the shift.
2. **Given** a 3-control row rendered at rest, **When** the live `RetainedRender.step` runs with a
   model hovering exactly one control, **Then** the repaint is bounded
   (`RecomputedNodeCount < BaselineNodeCount`) and the hovered control is counted as changed work.

---

### Edge Cases

- **Unknown / unreferenced id**: an id named by no interaction state in the model resolves to
  `Normal` — never an exception. The projection is total.
- **Simultaneous states**: a control the model reports pressed *and* selected *and* focused *and*
  hovered resolves to exactly the highest-ranked runtime state (`Pressed`), under the fixed closed
  order; peeling the top state off reveals the next.
- **Head states never derived**: `Disabled`, `Validation`, and `Loading` are author intent only; the
  projection never produces them, even though the bridge preserves them when consumer-set.
- **At rest**: a derived `Normal` emits **nothing** — the control is returned byte-identical, the
  `Scene` is unchanged, and the retained step recomputes zero nodes.
- **Consumer vs. derived**: a consumer-set non-`Normal` attribute always wins; only a consumer-
  `Normal`/absent slot is filled by the derived state. The carrier is single — replace-or-append, no
  stale accumulation.
- **Scoped restyle**: the visible restyle is realized only for the kinds 096 widened
  (`button` / `slider` / `text-box` / `radio-group` / `switch`); an unmigrated kind
  (`progress-bar` / `numeric-input`) is stamped but shows **no render delta**. A `Normal` attribute
  is byte-identical to the unset (at-rest) render for every kind.
- **Moving focus**: when focus moves from `a` to `b`, the indicator moves — `a` returns to `Normal`,
  `b` becomes `Focused` — purely from the changed model, no per-control teardown.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `deriveVisualState` MUST be a **pure, total, deterministic** projection from the live
  `ControlRuntimeModel` and a `ControlId` to a single `VisualState`: no clock, randomness, or side
  effects; identical inputs always yield an identical result; and it MUST **never throw** for any
  model/id (an unknown or unreferenced id resolves to `Normal`). It MUST have **no per-kind
  branching** — a plain ordered cascade over the runtime model.
- **FR-002**: The projection MUST select the highest-ranked state under the **closed runtime
  precedence** `Pressed > Selected > Focused > Hover > Normal` — the runtime-derivable tail of the
  full visual-state order `Disabled > Validation > Loading > Pressed > Selected > Focused > Hover >
  Normal`. The three head states (`Disabled` / `Validation` / `Loading`) are **author intent only**
  and MUST never be produced by the projection.
- **FR-003**: A **consumer-set non-`Normal` visual state MUST out-rank** every derived runtime state
  and be preserved unchanged; a consumer-`Normal` (or absent) state MUST be filled by the derived
  runtime state. This arbitration MUST flow through a **single carrier channel** — the lone
  `visualState` attribute feature 093's resolver reads — replace-or-appended (last writer) so the
  channel never accumulates stale state.
- **FR-004**: `applyRuntimeVisualState` MUST be the **internal host bridge** — a pure recursive walk
  of the lowered `Control<'msg>` tree in the `ControlId` domain (id = `Key` if present, else `Kind`),
  applied **pre-reconcile**. It MUST stamp each control's resolved state onto the single carrier so
  the look is realized by feature 093's resolver (E3), with **no duplication** of the resolver or of
  slot composition (095).
- **FR-005**: The bridge MUST be **byte-identical at rest** — a derived `Normal` MUST emit nothing,
  so a `Normal`-and-unset control (and tree) is returned verbatim, renders a `Scene` byte-identical
  to the un-bridged build, and on the live `RetainedRender` path recomputes **zero** nodes.
- **FR-006**: The visible restyle MUST be **additive and scoped** — realized for the kinds 096 opted
  in (`button` / `slider` / `text-box` / `radio-group` / `switch`); an unmigrated kind
  (`progress-bar` / `numeric-input`) MUST show **no render delta** under a runtime state. For every
  kind, a `Normal` attribute MUST be byte-identical to the unset (at-rest) render.
- **FR-007**: The resolved runtime look MUST ride feature 092's **stable retained identity** (E2),
  consumed not re-derived: a control's derived state (e.g. a focus indicator) MUST survive an
  unrelated, position-shifting re-render via the live retained path, where a position-keyed baseline
  loses identity. A **localized** interaction (a single hover) MUST produce a **bounded** repaint
  (`RecomputedNodeCount < BaselineNodeCount`), with the changed control counted as work.
- **FR-008**: The bridge MUST be **internal plumbing** — `applyRuntimeVisualState` (and the later
  feature-112 targeted-stamp variants `applyRuntimeVisualStateTargeted` / `runtimeStampFor` /
  `RuntimeStampResult`) are `internal`. The **only public-surface entry** is
  `ControlRuntime.deriveVisualState`; because the `ControlRuntime` module was committed at import,
  backfilling this feature adds **zero** new public-surface-baseline delta.

### Key Entities *(include if feature involves data)*

- **`ControlRuntimeModel`**: the aggregate live interaction state the projection reads — focused /
  hovered / pressed controls, text selection, caret, composition, drag. Maintained by the
  `ControlRuntime` MVU as it receives input messages; 096 projects *from* it, does not mutate it.
- **`VisualState`**: the per-control state feature 093's resolver paints — `Normal` / `Hover` /
  `Pressed` / `Focused` / `Selected` / `Disabled` / `Loading` / `Validation`. The closed precedence
  orders them; 096 derives only the runtime-derivable tail.
- **`deriveVisualState`**: the public, pure/total/deterministic projection `model -> controlId ->
  VisualState` under the closed runtime precedence; unknown id ⇒ `Normal`.
- **`applyRuntimeVisualState`**: the internal host bridge — a pure pre-reconcile tree walk (id =
  `Key` ?? `Kind`) that preserves a consumer-set non-`Normal` state and otherwise stamps the derived
  state onto the single `visualState` carrier, emitting nothing at `Normal`.
- **`visualState` attribute (single carrier)**: the lone attribute channel feature 093's
  `visualStateOf` reads and the resolver consumes; the bridge replace-or-appends it (last writer).
- **Stamped `Control<'msg>` tree**: the lowered tree after the bridge — interaction-styled where
  interacted with, byte-identical where at rest — fed to the keyed reconciler so the look rides E2
  retained identity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `deriveVisualState` selects the correct state under the closed runtime precedence for
  100% of `(model, id)` cases (a simultaneously pressed/selected/focused/hovered id ⇒ `Pressed`, and
  each peel reveals the next-ranked state); an unknown or unreferenced id ⇒ `Normal`; identical
  inputs always yield an identical result.
- **SC-002**: A focused control gains the `Focused` indicator with **no consumer focus attribute**,
  and moving focus moves the indicator (the previously-focused control returns to `Normal`) — 100%
  of cases; a focused text-box's resolved paint differs from its unfocused render (the indicator is
  visible).
- **SC-003**: A `Normal`-and-unset tree is returned unchanged, its rendered `Scene` is byte-identical
  to the un-bridged build, and the live `RetainedRender.step` recomputes **0** nodes at rest — zero
  diff.
- **SC-004**: Across ≥1000 generated `(model, id, consumer-state)` combos, the bridge is **total**
  (never throws) and **deterministic**, and the closed order holds — a consumer non-`Normal` state
  is preserved 100% and a consumer-`Normal` control takes `deriveVisualState model id` — 100% pass.
- **SC-005**: A localized interaction (a single hover entering one control) repaints **fewer than
  all** nodes via the live retained path (`RecomputedNodeCount < BaselineNodeCount`) and the hovered
  control is counted as changed work.
- **SC-006**: A migrated kind (`button` / `slider` / `text-box` / `radio-group` / `switch`) visibly
  restyles under a runtime state (resolved paint differs from `Normal`); an unmigrated kind
  (`progress-bar` / `numeric-input`) shows **no render delta**; for every kind a `Normal` attribute
  is byte-identical to the unset render.
- **SC-007**: The derived runtime look survives a sibling-shifting re-render via the live retained
  path — the focus state is `Focused` both before and after the shift and the retained id is stable —
  where a position-keyed baseline loses identity on the shift.
- **SC-008**: The bridge adds **zero** new public-surface-baseline delta — `deriveVisualState` is the
  lone public entry (the `ControlRuntime` module was committed at import) and the bridge functions
  are `internal`; the surface-drift check passes unchanged.

## Assumptions

- The runtime bridge (`ControlRuntime.deriveVisualState` / `applyRuntimeVisualState`, and the
  feature-112 targeted-stamp variants) already exists in the imported, rebranded source; this feature
  is the **backfilled contract** for it, not new construction.
- Feature 096 governs **state derivation and stamping only** — it projects live interaction to a
  `VisualState` and writes the single carrier attribute. The *painting* of that state is delegated to
  feature 093's resolver (E3); 096 adds no draw styling of its own and duplicates neither 093's
  resolver nor 095's slot composition.
- The `ControlRuntimeModel` is maintained by the `ControlRuntime` MVU (focus / hover / press /
  selection tracking) and is assumed in place; 096 reads it and does not change its update logic. The
  live host (`ControlsElmish`) populates focus / hover / press but not the text-range `Selection`, so
  the `Selected`-derivation branch is forward-looking on the real render path today (kept so a future
  host derives `Selected` without a code change).
- The head visual states (`Disabled` / `Validation` / `Loading`) are **author intent**, set by the
  consumer through feature 093's attribute, and are **permanently** outside the projection's range —
  not a deferral.
- The visible restyle is **scoped** to the kinds 096 widened (`button` / `slider` / `text-box` /
  `radio-group` / `switch`). Widening further kinds (e.g. `progress-bar`, `numeric-input`) is bounded
  follow-up, not part of this contract; those kinds are correctly inert under the bridge today.
- Retained-identity survival relies on feature 092's retained-identity machinery (the derived state
  rides the control's attributes through the keyed reconciler); that machinery is assumed in place and
  is proven here only to the extent that a runtime-styled control survives a position-shifting
  re-render (`readiness/focus-survives-reshuffle.md`).
- Render-output equivalence and the "responds" proof are judged by **structural `Scene` equality**
  (the same evidence technique features 091/093/095 use; an inert build paints identical frames
  regardless of interaction, the bridged build differs only because interaction restyles) — pixel-
  level / desktop-visibility proofs are out of scope. The committed readiness evidence under
  `readiness/` is the captured artifact for the responds and focus-survival proofs.
- The feature's only public-surface entry is `deriveVisualState` on the already-committed
  `ControlRuntime` module; backfilling the spec adds **zero** new public-surface-baseline delta.
