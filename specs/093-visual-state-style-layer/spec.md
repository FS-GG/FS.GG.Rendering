# Feature Specification: Visual-State Style Layer (Feature 093)

**Feature Branch**: `093-visual-state-style-layer`

**Created**: 2026-06-15

**Status**: Final

**Input**: User description: "next item in the implementation plan"

## Context

This specification **backfills the contract** for a capability that already ships in the imported,
rebranded source: the single state→style resolver `Style.resolve` (`src/Controls/Style.fsi` /
`.fs`), the styling vocabulary it folds (`VisualState`, `StyleVariant`, `StyleClass`,
`ResolvedStyle` in `Types.fsi`), and the `Attr.styleClasses` / `Attr.visualState` attributes that
carry styling intent on a control. The code, the public `.fsi` surface, captured readiness
evidence (`specs/093-visual-state-style-layer/readiness/parity/`), and four executable test suites
(`Feature093StyleResolverTests`, `Feature093StylePropertyTests`, `Feature093ParityTests`,
`Feature093RetainedStateTests`) all exist, but **no Spec Kit spec/plan/tasks ever described them**.
This document brings the feature under the project's `Spec → .fsi → semantic tests →
implementation` contract — the same conformance-backfill pattern feature 091 established — and
records the import-before-spec deviation against Constitution Principle I.

The capability itself: before this layer, every migrated control computed its paint and typography
**procedurally and per-kind**, reading `theme.Accent` / `theme.Danger` / `theme.Muted` inline at
each render site. That made styling intent (which semantic variant? which interaction state?)
implicit, scattered, and impossible to validate or re-theme uniformly. Feature 093 introduces **one
pure, total, deterministic resolver** that folds the active **theme**, a per-kind **base style**, a
list of attached **style classes** (typed `StyleVariant` or free-form `Custom`), and the control's
current **visual state** into a single concrete `ResolvedStyle`, under a **fixed precedence**:
`baseStyle  <  each class in attach order  <  current visual state`, last-writer-wins per field.
Colours always originate from the theme's DTCG-generated tokens — never inline literals — so a
theme swap re-paints every control consistently.

Feature 093 is the **prerequisite framing for the design-system arc** (Workstreams F and D in the
missing-features plan): the central style resolver Workstream F enriches, and the slot/visual-state
vocabulary themes plug into, are exactly this layer. It is paired with features 095 (lookless slot
composition) and 096 (runtime visual-state bridge), which are backfilled alongside it.

This feature has a **public surface** (`Style.resolve`, the styling types, the styling attributes)
already present in the committed surface baselines; backfilling the spec adds **zero new
public-surface-baseline delta**. Per the constitution's vertical-slice rule, the in-assembly
Expecto/FsCheck tests (reaching internals via `InternalsVisibleTo("Controls.Tests")` where needed)
plus the public resolver API are the user-reachable surface for these stories.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A control is styled by declaring a semantic variant, not by hand-painting (Priority: P1)

An app author attaches a semantic style class to a control — a typed variant such as `Primary` or
`Danger`, or a free-form `Custom` class — and the control resolves to a concrete, token-derived
paint and typography. The author never reads palette roles or writes colour literals; the single
resolver folds the variant onto the control's base style. Each built-in variant produces a visibly
distinct, theme-appropriate result, and an unrecognised `Custom` class is harmless (it contributes
no delta) rather than an error.

**Why this priority**: Declarative, token-sourced styling is the foundation the whole layer exists
to provide — it is what lets every other story (state response, re-theme parity, survival across
re-renders) be uniform and validatable. It is the MVP slice: a resolver that turns a declared
variant into a `ResolvedStyle` is independently valuable and testable.

**Independent Test**: Resolve a neutral base under each of the six built-in `StyleVariant`s on one
theme and confirm each yields a distinct token-derived `ResolvedStyle`; confirm `Primary` derives
from the accent family and `Danger` from the danger family; confirm a free-form `Custom` class
flows through the same fold and an unknown `Custom` resolves back to the base (identity delta, never
dropped or thrown).

**Acceptance Scenarios**:

1. **Given** a neutral base style and the active theme, **When** each built-in `StyleVariant` is
   resolved, **Then** the six results are pairwise distinguishable and each colour is theme-derived.
2. **Given** a base style, **When** a `Variant Primary` is resolved, **Then** its fill is from the
   accent family; **When** a `Variant Danger` is resolved, **Then** its fill is from the danger family.
3. **Given** a base style, **When** a free-form `Custom "promo"` class is resolved, **Then** it
   flows through the same fold; **When** the `Custom` name is unrecognised, **Then** the result
   equals the base style (identity delta), with no exception and no silent field drop.

---

### User Story 2 - A control's appearance reflects its interaction state, predictably (Priority: P1)

A control's look responds to its current visual state — `Hover`, `Pressed`, `Disabled`, `Focused`,
`Selected`, `Loading`, or a `Validation` state — and when both a class and a state want the same
field, the **state wins** under one fixed, documented precedence (base < classes-in-attach-order <
state, last-writer-wins per field). A later-attached class wins over an earlier one. `Loading`
intentionally inherits the `Normal` paint, preserving parity. The precedence is the same for every
combination, so styling is predictable rather than order-of-discovery dependent.

**Why this priority**: State-responsive paint with a deterministic precedence is the second half of
the core value: without it, interaction feedback is ad-hoc and conflicts between class and state are
undefined. It is co-critical with US1 — together they replace all procedural per-kind styling.

**Independent Test**: Resolve a base under each of the eight `VisualState` cases and confirm the
differentiated states yield distinct styles while `Loading` matches `Normal`; resolve a base with a
class plus an overlapping-field state and confirm the state's field wins while the class's
non-overlapping fields are retained; resolve two classes and confirm the later one wins; property-
test (≥1000 inputs) that the state layer is outermost and the fold is pure and deterministic.

**Acceptance Scenarios**:

1. **Given** a base style, **When** resolved under each of the eight `VisualState` cases, **Then**
   the differentiated states produce distinct token-derived styles and `Loading` equals the
   `Normal` result.
2. **Given** a base with a `Primary` class and a `Disabled` state that both own `Fill`, **When**
   resolved, **Then** the `Disabled` (state) fill wins and the class's non-overlapping fields remain.
3. **Given** a base with two classes attached in order, **When** resolved, **Then** the
   later-attached class wins for any field both set.
4. **Given** any `(theme, base, classes, state)` over ≥1000 generated inputs, **When** resolved
   twice, **Then** the two `ResolvedStyle`s are identical (purity/determinism) and applying the
   classes then the state equals re-resolving the class-folded style under that state with no
   classes (the state layer is provably outermost).

---

### User Story 3 - A state-driven look survives an unrelated re-render (Priority: P2)

The visual state rides on the control's attributes, so a hover/disabled/pressed appearance travels
through the keyed reconciler diff and **survives** an unrelated model update that shifts the
control's position in the tree — proven through the live retained path, not a hand-seeded state map.

**Why this priority**: This is the payoff that ties the style layer to the retained-identity work
(092): interaction feedback doesn't flicker or reset when an unrelated part of the screen updates.
It is P2 because it depends on US1/US2 producing the styled output in the first place.

**Independent Test**: Build a keyed control in a non-`Normal` visual state with a class attached,
render frame 1 via `RetainedRender.init`, then step to a frame 2 that prepends an unrelated sibling
(shifting the keyed control), and confirm the control is found under the same key with its
state-driven paint identical in content (geometry aside) before and after the shift.

**Acceptance Scenarios**:

1. **Given** a keyed control in a `Disabled` state with a `Primary` class, rendered at frame 1,
   **When** frame 2 prepends an unrelated sibling that shifts it, **Then** the control still
   resolves to its `Disabled` look (the muted token, not the `Primary` accent) via the live
   retained path.
2. **Given** the same positional shift, **When** the state-driven paint is compared before and
   after, **Then** its content is identical (the evaluated box aside).

---

### User Story 4 - The migration is additive, scoped, and parity-preserving (Priority: P2)

Replacing procedural styling with the resolver does not change what already-shipped controls paint:
for the migrated kinds (`Button`, `CheckBox`) the default no-class output is structurally
scene-equal to the prior procedural geometry under every theme and state, and a kind that has
**not** been migrated is
unaffected by an attached style class (the migration is additive and scoped). The resolver is
**total** over every input — every `StyleVariant`, any `Custom` string, all eight `VisualState`
cases — and every colour it emits originates from the theme (no inline literals).

**Why this priority**: These are the safety guarantees that let the layer be trusted on the live
path. P2 because they protect US1–US3 rather than delivering a new journey, and they are verified by
frozen-oracle parity tests and high-volume property tests.

**Independent Test**: Compare the migrated `Button`/`CheckBox` no-class render to a frozen inline
reproduction of the pre-refactor geometry for each (kind, theme, state) and assert structural scene
equality; attach a class to an unmigrated kind and assert zero render delta; property-test totality
over generated inputs.

**Acceptance Scenarios**:

1. **Given** a migrated `Button` or `CheckBox` with no class, **When** rendered under light and dark
   themes, **Then** the scene is structurally equal to the frozen procedural baseline.
2. **Given** an unmigrated kind, **When** a style class is attached, **Then** there is no render
   delta (additive, scoped migration).
3. **Given** any `(theme, base, classes, state)`, **When** resolved, **Then** the call returns a
   `ResolvedStyle` without throwing (totality), and `resolve theme base [] Normal = base` exactly.

---

### Edge Cases

- **Unknown `Custom` class**: contributes an identity delta — resolves back to the base, never
  throws, never silently drops a field.
- **Class vs. state conflict on the same field**: the state always wins (fixed precedence), for
  every `(class, state)` pair including `Disabled + Danger`.
- **`Loading` state**: deliberately inherits `Normal`'s paint (parity-preserving), not a distinct look.
- **No class, `Normal` state**: a strict identity — `resolve theme base [] Normal = base` exactly.
- **Multiple classes**: folded left-to-right in attach order; later wins for overlapping fields.
- **Unmigrated control kinds**: attaching a class is a no-op on their render output.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a **closed, typed set** of built-in semantic variants
  (`Primary`, `Danger`, `Ghost`, `Neutral`, `Success`, `Warning`) plus a free-form `Custom` class,
  carried on a control as an ordered `StyleClass list` whose list position **is** the attach order
  folded left-to-right.
- **FR-002**: The resolver's variant layer MUST be a **total match** over the closed variant set —
  every `StyleVariant` resolves to a token-derived delta with no unmatched case.
- **FR-003**: Resolution MUST follow one **fixed precedence**, last-writer-wins per `ResolvedStyle`
  field: `baseStyle < each class in attach order < current visual state`. A visual state's value for
  a field overrides any class's value for that field; a later-attached class overrides an earlier one.
- **FR-004**: The resolver MUST be **total over all eight `VisualState` cases** (`Normal`,
  `Disabled`, `Hover`, `Pressed`, `Focused`, `Selected`, `Loading`, `Validation _`) and over any
  `Custom` string (unknown ⇒ identity delta, never an exception or silent drop).
- **FR-005**: For the default case the resolver MUST be a strict identity:
  `resolve theme baseStyle [] Normal = baseStyle` exactly, and `Loading` MUST inherit `Normal`'s
  paint (parity preservation).
- **FR-006**: The resolver MUST be **pure, total, and deterministic** — no clock, randomness, or
  side effects; identical `(theme, baseStyle, classes, state)` inputs always produce an identical
  `ResolvedStyle`.
- **FR-007**: The migration MUST be **additive and scoped**: the resolver governs **paint and
  typography only** (geometry is computed as before); migrated kinds (`Button`, `CheckBox`) MUST
  paint identically — judged by **structural scene equality** — to their prior procedural output for
  the default no-class case; unmigrated
  kinds MUST be unaffected by attached style classes.
- **FR-008**: Every colour the variant and state layers emit MUST originate from the active
  `Theme`'s DTCG-generated tokens — **no inline colour literals** — so a theme swap re-paints
  consistently.

### Key Entities *(include if feature involves data)*

- **VisualState**: the control's current interaction/render state consumed by the resolver — one of
  `Normal`, `Disabled`, `Hover`, `Pressed`, `Focused`, `Selected`, `Loading`, or `Validation` of a
  `ValidationState`.
- **StyleVariant**: the closed, typed set of built-in semantic variants (`Primary`, `Danger`,
  `Ghost`, `Neutral`, `Success`, `Warning`).
- **StyleClass**: one attached class entry — a typed `Variant` of a `StyleVariant`, or a `Custom`
  consumer-defined class name. A control carries a `StyleClass list` in attach order.
- **ResolvedStyle**: the flat per-control output record — `Foreground`, `Fill`, `Stroke`,
  `StrokeWidth`, `FontFamily`, `FontSize`, `FontWeight`. Geometry is deliberately not part of it.
- **Theme**: the active DTCG-generated token palette/metrics every resolved colour is sourced from.
- **Style.resolve**: the single pure/total/deterministic function
  `theme → baseStyle → classes → state → ResolvedStyle` that folds them under the fixed precedence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Each of the six built-in `StyleVariant`s resolves to a distinct token-derived
  `ResolvedStyle` on one theme (six pairwise-distinguishable results); `Primary` derives from the
  accent family and `Danger` from the danger family; a free-form `Custom` class flows through the
  same fold and an unknown `Custom` resolves exactly to the base — 100% of cases.
- **SC-002**: Each differentiated `VisualState` resolves to a distinct token-derived style while
  `Loading` equals `Normal`; for an overlapping field the state wins over a class and the class's
  non-overlapping fields are retained; a later-attached class wins over an earlier one — 100% of cases.
- **SC-003**: For the migrated kinds (`Button`, `CheckBox`), the resolver-driven default no-class
  paint is structurally scene-equal to the frozen pre-refactor procedural baseline for each
  (kind, theme, state), and is deterministic across repeated calls — zero diff.
- **SC-004**: Across ≥1000 generated `(theme, base, classes, state)` inputs, resolution is pure and
  deterministic (identical inputs → identical output), the visual state is provably the outermost
  layer (state > classes > base), and `resolve theme base [] Normal = base` holds — 100% pass.
- **SC-005**: A control's state-driven appearance survives an unrelated, position-shifting re-render
  via the live retained path (`RetainedRender.init`/`step`): the control is found under its key with
  its state-driven paint identical in content (geometry aside) before and after the shift — 100% of cases.
- **SC-006**: Every colour in a `ResolvedStyle` produced by the variant/state layers traces to a
  theme token (no inline literal); swapping the theme changes the resolved colours accordingly,
  while structure (which fields are set) is unchanged.
- **SC-007**: Attaching a style class to an **unmigrated** kind produces no render-output delta,
  confirming the migration is additive and scoped to the migrated kinds — 100% of cases.

## Assumptions

- The resolver, the styling types (`VisualState`/`StyleVariant`/`StyleClass`/`ResolvedStyle`), and
  the styling attributes (`Attr.styleClasses`/`Attr.visualState`) already exist in the imported,
  rebranded source; this feature is the **backfilled contract** for them, not new construction.
- The resolver governs **paint and typography only**; geometry is computed as today and is out of
  scope (data-model R3). `ResolvedStyle` is deliberately flat so the precedence is last-writer-wins
  per field and the parity proof is a plain structural record comparison.
- Styling is **single-control**: no selector matching, no specificity algebra, and no cross-control
  cascade — these are permanent roadmap non-goals, not deferrals.
- **Parity** is pinned to `Button` and `CheckBox` — the only kinds with a frozen procedural oracle
  under `readiness/parity/`. The resolver is in fact *called* by six migrated kinds in the shipped
  source (`Button`, `CheckBox`, `RadioGroup`, `Slider`, `Switch`, `TextBox`); the other four are
  covered by the totality/purity/determinism proofs, not by a frozen-oracle parity scene. Adding
  their parity scenes (and extending the resolver to further kinds) is bounded follow-up — recorded
  as plan Complexity Tracking **DF-2** — not part of this contract.
- Render-output equivalence is judged by **structural scene equality** (the same oracle technique
  `DesignTokenParityTests` uses); pixel-level/desktop-visibility proofs are out of scope. The
  committed readiness evidence under `readiness/parity/` is the captured artifact for SC-003.
- Visual-state survival across re-renders relies on feature 092's retained identity (the state rides
  control attributes through the keyed reconciler); that machinery is assumed in place and is proven
  here only to the extent that the state-driven look survives.
