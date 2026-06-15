# Feature Specification: Lookless Slot Composition (Feature 095)

**Feature Branch**: `095-lookless-slot-composition`

**Created**: 2026-06-15

**Status**: Final

**Input**: User description: "next item in the implementation plan"

## Context

This specification **backfills the contract** for a capability that already ships in the imported,
rebranded source: **lookless slot composition** — the ability to inject a consumer-authored
sub-tree into a *named region* of a built-in control (a `Button`'s leading/trailing area, a
`Panel`'s header/footer) without forking the control. The mechanism is the internal slot seam
`ControlInternals.slotFill` / `slotFillsOf` / `slotFor` / `lowerSlots` (`src/Controls/Control.fsi`
/ `.fs`), the `AttrCategory.Slot` carrier and `SlotFillsValue` `AttrValue` case (`Types.fsi`), and
the **typed front-door** props that are the only sanctioned authoring path —
`ButtonProps.Leading` / `Trailing` (`Widgets/Primitives.fsi`) and `PanelProps.Header` / `Footer`
(`Widgets/Containers.fsi`). The code, the `.fsi` surface, captured readiness evidence
(`specs/095-lookless-slot-composition/readiness/parity/`), and an executable test suite
(`Feature095SlotCompositionTests`) all exist, but **no Spec Kit spec/plan/tasks ever described
them**. This document brings the feature under the project's `Spec → .fsi → semantic tests →
implementation` contract — the same conformance-backfill pattern features 091 and 093 established —
and records the import-before-spec deviation against Constitution Principle I.

The capability itself: before this layer, a control's structure (where its content, icon,
header, or affordances sit) was fixed by the kind. To put an icon before a button's label, or a
title row above a panel's content, a consumer would have to fork the control or hand-assemble the
geometry. **"Lookless slot composition"** lets a consumer instead *fill a declared region* with a
static `Control<'msg>` sub-tree. "Lookless" means a slot fill is a **plain static value**, not a
data-bound template or deferred expression; "slot" is a **named region** the control declares;
"composition" is that the fill is injected — *lowered* — into the control's ordinary `Children`,
ordered by region position (leading regions, then intrinsic children, then trailing regions).
Because the fill becomes a normal child, it **inherits every prior reconciler feature by
construction**: flat per-`ControlId` message dispatch (E1), retained identity across re-renders
(E2), the visual-state style resolver (E3 / feature 093), and focus/tab routing (E4). The lowering
is **pure, total, and deterministic**, and with no slot filled the control is returned
**byte-identical** to its pre-slot self.

Feature 095 is paired with features 093 (visual-state style layer) and 096 (runtime visual-state
bridge) under Workstream C11 of the missing-features plan — together the visual-state/composition
vocabulary that the design-system and theme work (Workstreams F and D) plug into. The slot fills it
introduces compose *with* 093's resolver rather than duplicating it.

This feature's **only public-surface delta** is the `SlotFillsValue` case on the already-public
`AttrValue<'msg>` type (committed in `tests/surface-baselines/FS.GG.UI.Controls.txt` at import);
the slot builders/readers are `internal`, and the authoring path is the typed `Props` fields.
Backfilling the spec adds **zero new public-surface-baseline delta**. Following the
**typed-front-door vertical slice** the 091/093 backfills established — and consistent with
Constitution Principle I.3 (tests assert observable behavior, not internals) — the in-assembly
Expecto/FsCheck tests reach the internal slot seam only via `InternalsVisibleTo("Controls.Tests")`
to drive the **public typed front door** (`ButtonProps.Leading`/`Trailing`,
`PanelProps.Header`/`Footer`) and assert on the resulting lowered `Children`; the typed props are
the user-reachable surface for these stories.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A consumer fills a control's named region with their own sub-tree (Priority: P1)

An app author reshapes a built-in control by filling one of its **declared, typed regions** — a
`Button`'s `Leading` or `Trailing`, a `Panel`'s `Header` or `Footer` — with their own
`Widget<'msg>`. They do this through the control's typed props front door, never by naming a
free-form slot string. The fill is *lowered* into the control's children at the correct position
(leading regions before the intrinsic content, trailing regions after; a panel header before the
body, a footer after), and the slot carrier attribute is consumed so it leaves no trace beyond the
placement. Lowering is pure, total, and deterministic, so the same authored tree always produces
the same intermediate representation and never throws for any kind/fill combination.

**Why this priority**: Region-targeted composition is the whole point of the feature — it is what
lets a consumer add an icon, badge, or header without forking the control. A lowering that injects
a typed fill into the right child position is independently valuable and testable, and it is the
MVP slice every other story builds on.

**Independent Test**: Through the typed front door, fill `Button.Leading` and confirm the fill
appears in the lowered control's `Children` with the slot carrier consumed; fill both `Leading`
and `Trailing` and confirm two distinct, correctly-ordered regions (no swap, no collision); fill
`Panel.Header` and `Footer` and confirm `[header; body; footer]` ordering; confirm `slotFor`
resolves a present region and distinguishes **absent** (None ⇒ default chrome) from **present-but-
empty** (Some); property-test (≥1000 generated kind/fill inputs) that lowering is pure,
deterministic, and total (never throws). Confirm that when a control carries more than one
`Slot`-category attribute, only the **last** is honored (last-writer-wins). Confirm there is **no**
public free-form `Attr.slot` / slot-name escape hatch — the typed props are the only authoring path.

**Acceptance Scenarios**:

1. **Given** a `Button` authored with a `Leading` fill, **When** it is lowered, **Then** the fill
   appears in the control's `Children` and the slot carrier attribute is consumed (no residue).
2. **Given** a `Button` with both `Leading` and `Trailing` filled, **When** lowered, **Then** the
   two fills land in two distinct regions in `[leading; intrinsic; trailing]` order with no swap or
   collision.
3. **Given** a `Panel` with `Header` and `Footer` filled, **When** lowered, **Then** the children
   are ordered `[header; body; footer]`.
4. **Given** a control's attributes, **When** a region name is queried, **Then** `slotFor` returns
   `Some` for a present region (including a present-but-empty fill) and `None` for an absent one
   (absent ≠ empty).
5. **Given** any `(kind, fills)` over ≥1000 generated inputs, **When** lowered twice, **Then** the
   two intermediate representations are identical (purity/determinism) and lowering never throws
   (totality).
6. **Given** the public surface, **When** searched for a slot authoring path, **Then** there is no
   public free-form slot builder or slot-name string — only the typed `Props` fields.
7. **Given** a control carrying more than one `Slot`-category attribute, **When** its fills are
   resolved, **Then** exactly one is honored — the **last writer wins** (the last `slot`
   attribute's fills are the active set); earlier `Slot`-category attributes are ignored (FR-002).

---

### User Story 2 - An unfilled control is byte-identical to its pre-slot self (Priority: P1)

A control that exposes slots but has **none filled** behaves exactly as it did before the feature
existed: it carries no slot attribute, gains no peripheral children, and renders structurally
scene-equal to the frozen pre-slot procedural baseline under every theme. Exposing slots is
**scoped** — only the kinds that opted in (`Button`, `Panel`) carry the region machinery; a kind
that did not opt in (e.g. `CheckBox`) is entirely unaffected.

**Why this priority**: This is the safety guarantee that lets the feature ship on the live path —
adding a slot seam to a control must not change what the control paints when nobody uses it. It is
co-critical with US1: composition is only trustworthy if the no-composition case is provably inert.

**Independent Test**: Author an unfilled `Button` and confirm it carries no slot attribute and no
extra children; render it under light and dark themes and assert structural scene equality to the
frozen pre-slot baseline (`readiness/parity/button.{light,dark}.normal.scene.txt`); lower an
unfilled `Panel` and confirm it is identical to the legacy no-slot panel; attach nothing to a
non-slotted kind (`CheckBox`) and confirm it gains no slots.

**Acceptance Scenarios**:

1. **Given** a `Button` with no slot filled, **When** authored and lowered, **Then** it carries no
   slot attribute and no peripheral children.
2. **Given** an unfilled `Button`, **When** rendered under light and dark themes, **Then** the
   scene is structurally equal to the frozen pre-slot procedural baseline.
3. **Given** an unfilled `Panel`, **When** lowered, **Then** the result is identical to the legacy
   no-slot panel.
4. **Given** a non-slotted kind (`CheckBox`), **When** authored normally, **Then** it exposes no
   slots and is unchanged (scoped exposure).

---

### User Story 3 - Slotted content inherits dispatch, style, focus, and identity for free (Priority: P2)

Because a slot fill is lowered into the control's ordinary `Children`, the injected sub-tree
participates in every prior reconciler feature **by construction**, with no slot-specific
special-casing: a binding inside a slot dispatches through the flat per-`ControlId` mechanism (E1);
a style class on slotted content resolves through the feature-093 visual-state resolver (E3); a
focusable slotted control appears in the tab order (E4); and a keyed slotted control keeps its
retained identity (E2) across an unrelated re-render that shifts its host's position.

**Why this priority**: This is the payoff that makes slot composition cohere with the rest of the
framework — slotted content is not a second-class citizen. It is P2 because it depends on US1
producing the lowered children in the first place, and it is verified by composing the existing
E1/E2/E3/E4 mechanisms over slotted trees.

**Independent Test**: Put a message-dispatching binding inside a slot and confirm it dispatches
(E1); put a `Danger`-classed control in a slot and confirm it resolves to a distinct style via the
E3 resolver; put a focusable control in a slot and confirm it appears in `Focus.order` (E4); render
a keyed slotted control at frame 1, step to a frame 2 that inserts a sibling above its host, and
confirm via the live `RetainedRender` path that the control keeps its `RetainedId` and the stepped
scene equals a full rebuild (E2).

**Acceptance Scenarios**:

1. **Given** a binding inside a slot fill, **When** the slotted control is rendered, **Then** the
   binding dispatches through the flat per-`ControlId` mechanism (E1).
2. **Given** a style class on slotted content, **When** resolved, **Then** it renders distinctly
   via the feature-093 visual-state resolver (E3).
3. **Given** a focusable slotted control, **When** the focus order is computed, **Then** the
   slotted control appears in the tab order (E4).
4. **Given** a keyed slotted control rendered at frame 1, **When** frame 2 inserts a sibling above
   its host and re-renders via the live retained path, **Then** the slotted control keeps its
   `RetainedId` and the stepped scene equals a full rebuild (E2).

---

### Edge Cases

- **Unfilled slot**: the control is returned **byte-identical** to its pre-slot self — no slot
  attribute, no extra children, structurally scene-equal to the frozen baseline.
- **Absent vs. empty region**: `slotFor` distinguishes a region **absent** from the fill list
  (`None` ⇒ default chrome) from a region **present but filled with an empty sub-tree** (`Some`).
- **Multiple Slot-category attributes on one control**: at most one is honored —
  **last-writer-wins** (the last `slot` attribute's fills are the active set).
- **Region ordering**: fills are injected `[leading regions; intrinsic children; trailing
  regions]`; a `Button`'s `Leading` precedes its label and `Trailing` follows it; a `Panel`'s
  `Header` precedes the body and `Footer` follows it — never swapped.
- **Any `(kind, fills)` combination**: lowering is **total** — it never throws, for any control
  kind and any fill list.
- **Non-slotted kinds**: a kind that did not opt into slots exposes none and is unaffected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Slot authoring MUST go through a **closed, typed front door** only — the typed
  `Props` slot fields (`ButtonProps.Leading` / `Trailing`, `PanelProps.Header` / `Footer`). There
  MUST be **no public free-form slot builder** and no consumer-facing slot-name string; the slot
  name is internal plumbing. Only the internal `ControlInternals.slotFill` builder produces a
  `Slot`-category attribute.
- **FR-002**: Slot fills MUST ride the existing `Attr` mechanism under `AttrCategory.Slot` as an
  **ordered association list** from region name to fill sub-tree (`SlotFillsValue of (string *
  Control<'msg>) list`). A control carries **at most one** `Slot`-category attribute;
  **last-writer-wins**. A region **absent** from the list is unfilled (default chrome); a region
  **present** is filled (even when the fill sub-tree is empty) — absent ≠ empty.
- **FR-003**: With **no slot attribute** present, lowering MUST return the control **verbatim /
  byte-identical** — the unfilled case is structurally scene-equal to the prior pre-slot output.
- **FR-004**: Lowering MUST inject the fills into the control's ordinary `Children`, **ordered by
  region position** (leading regions, then intrinsic children, then trailing regions), and MUST
  **consume** the slot carrier attribute. Because fills become normal children, they MUST inherit
  flat per-`ControlId` dispatch (E1), the visual-state style resolver (E3), and focus routing (E4)
  **by construction**, with no slot-specific special-casing.
- **FR-005**: Slotted content MUST inherit **retained identity** (E2): a keyed slotted control
  keeps its `RetainedId` across an unrelated re-render that shifts its host's position, proven via
  the live `RetainedRender` path (not a hand-seeded map).
- **FR-006**: The lowering `lowerSlots` MUST be **pure, total, and deterministic** — no clock,
  randomness, or side effects; identical `(kind, fills)` inputs always lower to an identical
  intermediate representation; and it MUST **never throw** for any `(kind, fills)` combination.
- **FR-007**: Slot exposure MUST be **additive and scoped** — only the kinds that opt in (`Button`,
  `Panel`) carry slot regions; a non-slotted kind (e.g. `CheckBox`) MUST be unaffected and expose
  no slots.
- **FR-008**: A slot fill MUST be a **static `Control<'msg>` value (lookless)** — not a data-bound
  template or deferred expression. There is **no selector matching, no specificity, and no cascade**
  — slot composition is single-control structural injection only.

### Key Entities *(include if feature involves data)*

- **Slot (region)**: a named region a control declares where a consumer may inject a sub-tree. The
  name is internal plumbing, projected to a string only at the lowering edge; consumers address it
  via typed `Props` fields, never a free-form string.
- **SlotFills (`SlotFillsValue`)**: the ordered association list from region name to the consumer's
  fill `Control<'msg>`, carried under `AttrCategory.Slot`. At most one per control,
  last-writer-wins.
- **Typed slot props**: `ButtonProps.Leading` / `Trailing` and `PanelProps.Header` / `Footer` —
  the sanctioned, type-safe authoring path that builds the slot carrier internally.
- **`slotFillsOf` / `slotFor`**: readers — the full ordered fill list for a control, and the fill
  for one named region (or `None` when absent; `Some` when present, including present-but-empty).
- **`lowerSlots`**: the pure/total/deterministic lowering that injects fills into `Children` by
  region position and consumes the carrier; identity (byte-identical) when no slot is filled.
- **Control `Children`**: the ordinary child collection slot fills are lowered into, so they
  inherit E1–E4 by construction.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Filling a typed region injects the fill into the lowered control's `Children` at the
  correct position and consumes the carrier — `Leading`/`Trailing` land in two distinct ordered
  regions; a `Panel`'s children order `[header; body; footer]`; `slotFor` resolves a present region
  and distinguishes absent from empty — 100% of cases.
- **SC-002**: An unfilled slotted control carries no slot attribute and no peripheral children, and
  its render is **structurally scene-equal** to the frozen pre-slot procedural baseline under both
  light and dark themes — zero diff.
- **SC-003**: Slotted content composes with the prior reconciler features: a binding inside a slot
  dispatches (E1), a style class on slotted content resolves through the feature-093 resolver (E3),
  and a focusable slotted control appears in the tab order (E4) — 100% of cases.
- **SC-004**: A keyed slotted control keeps its `RetainedId` across an unrelated, position-shifting
  re-render via the live `RetainedRender` path, and the stepped scene equals a full rebuild — 100%
  of cases.
- **SC-005**: Across ≥1000 generated `(kind, fills)` inputs, lowering is pure and deterministic
  (identical inputs → identical intermediate representation) and total (never throws); with no slot
  attribute, lowering is the identity (byte-identical) — 100% pass.
- **SC-006**: The slot carrier is internal plumbing — there is no public free-form slot builder,
  `Attr.slot`, or consumer slot-name string; the typed `Props` fields are the only authoring path —
  verified against the public surface.
- **SC-007**: Slot exposure is scoped — only `Button` and `Panel` carry slot regions, and a
  non-slotted kind (`CheckBox`) gains none and is unchanged — 100% of cases.

## Assumptions

- The slot seam (`ControlInternals.slotFill` / `slotFillsOf` / `slotFor` / `lowerSlots`), the
  `AttrCategory.Slot` carrier + `SlotFillsValue` case, and the typed slot props
  (`ButtonProps.Leading`/`Trailing`, `PanelProps.Header`/`Footer`) already exist in the imported,
  rebranded source; this feature is the **backfilled contract** for them, not new construction.
- Slot composition governs **structure only** — it injects sub-trees into `Children`. Visual-state
  styling of those sub-trees is delegated to feature 093's resolver (E3), and runtime state
  derivation to feature 096; 095 adds no styling of its own.
- Slots are **lookless and single-control**: static `Control<'msg>` fills, no data-bound templates,
  no selector/specificity/cascade. These are permanent roadmap non-goals, not deferrals.
- Slot exposure is **pinned to `Button` and `Panel`** — the only kinds that opt in and the only
  kinds with a frozen pre-slot oracle under `readiness/parity/`. Extending slots to further kinds
  (and capturing their parity scenes) is bounded follow-up, not part of this contract.
- Render-output equivalence is judged by **structural scene equality** (the same oracle technique
  features 091/093 use); pixel-level / desktop-visibility proofs are out of scope. The committed
  readiness evidence under `readiness/parity/` is the captured artifact for SC-002.
- Slotted-content retained-identity survival relies on feature 092's retained identity machinery
  (fills ride `Children` through the keyed reconciler); that machinery is assumed in place and is
  proven here only to the extent that a slotted control survives a position-shifting re-render.
- The feature's only public-surface entry is `SlotFillsValue` on the already-public
  `AttrValue<'msg>` type, committed at import; backfilling the spec adds **zero** new
  public-surface-baseline delta.
