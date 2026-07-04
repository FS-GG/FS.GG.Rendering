# Feature Specification: FS.GG.UI runtime ergonomics polish

**Feature Branch**: `241-runtime-ergonomics`

**Created**: 2026-07-04

**Status**: Draft

**Input**: Resolves **FS-GG/FS.GG.Rendering#74** — the **P1 Rendering** child of epic
**FS-GG/.github#165** (Space Invaders consumer feedback). Source: feedback §3.4–§3.6, severity
**Polish**. Follows the simulation-runtime thread closed by **Feature 239** (`Geometry` / `Rng` /
`FixedStep` primitives) and **Feature 240** (the `fs-gg-game-core` product skill). Scope is
additive ergonomics only — no behavioral or architectural change to rendering, input dispatch, or
the Elmish loop.

## Context (non-normative)

A full SDD-lifecycle consumer build of the Space Invaders TestSpec shipped ship-ready (37/37), but
lost time to three small, silent frictions on the FS.GG.UI runtime surface. Each is additive and
non-architectural; together they sharpen the edges a game/sim consumer touches first. Two of the
three are primarily **discoverability** gaps — the capability already ships but the consumer can't
find it — which is the same failure mode the whole epic targets.

**§3.4 — input-constructor name collision (hard compile stop).** A consumer that models its own
`Msg.KeyDown`/`Msg.KeyUp` cannot compile: the viewer namespace already exports a `KeyDown`/`KeyUp`
case (on the viewer key/event type), so `Some (KeyDown key)` in a consumer `mapKey` resolves to the
viewer constructor and fails with `type 'KeyId' does not match 'ViewerKey'`. `docs/product.md`
documents collision-prone names for `Text` / `CloseRequested` / `Rect` and even carries an explicit
`[<RequireQualifiedAccess>]` note for `ControlEventOrigin.Text`, but it does **not** list the input
constructors. The consumer only learns of the collision from the compiler.

**§3.5 — bare `[]` no-op command.** A product `update` returns `model, []` and a `subscribe`
returns `[]`. Elmish convention is `Cmd.none` / `Sub.none`; the bare list is correct but reads as
"forgot to fill this in" and is undiscoverable. (The Controls.Elmish adapter already defines an
internal `none`; the gap is a consumer-facing, product-authoring-surface alias.)

**§3.6 — HUD text placed with magic numbers.** The consumer positioned HUD/overlay strings with
hard-coded coordinates because it believed only render-edge text shaping existed. In fact
`FS.GG.UI.Scene.measureText : string -> FontSpec -> TextMetrics` — a **pure, host-independent**
heuristic returning `{ Width; Height; Baseline }` — already ships (Scene surface). The gap is
discoverability: it is not mentioned in `docs/product.md`, in the `fs-gg-scene`/`fs-gg-layout`/
`fs-gg-game-core` product skills, or shown in a self-positioning idiom, and consumers reach for a
`Size` shape rather than the richer `TextMetrics`. This feature surfaces the existing pure helper
(and a self-positioning idiom); it does **not** build a new measurer.

**Verify-before-adding.** Because §3.5 and §3.6 both have an existing partial implementation, this
feature MUST first confirm the current public surface and prefer surfacing/aliasing over
duplication. Net-new API is limited to what is genuinely absent (the §3.4 remedy, and any thin
alias/projection that improves the authoring surface).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consumer names its own KeyDown/KeyUp without a mystery compile error (Priority: P1)

A product author writing input handling models `Msg.KeyDown of KeyId` / `Msg.KeyUp of KeyId` and
maps raw input to it. Today this fails to compile because the viewer constructor of the same name
wins name resolution, with an error that names viewer types the author never referenced. The author
should either find the collision documented alongside the existing `Text`/`CloseRequested`/`Rect`
guidance, or not hit the collision at all because the viewer key type requires qualified access.

**Why this priority**: It is the only item in this feature that is a **hard compile stop** — it
blocks the consumer until reverse-engineered, and the fix (a doc line and/or an attribute) is small
and self-contained.

**Independent Test**: Author a minimal product with a `Msg.KeyDown of KeyId` case and a `mapKey`
that returns it; confirm it compiles, and that `docs/product.md` names `KeyDown`/`KeyUp` in its
collision guidance so the path is discoverable before the compiler is consulted.

**Acceptance Scenarios**:

1. **Given** a product `Msg` with its own `KeyDown of KeyId` case, **When** the author writes a
   `mapKey` returning `Some (KeyDown k)` following the documented guidance, **Then** the product
   compiles without a `does not match 'ViewerKey'` error.
2. **Given** a product author reading `docs/product.md` before writing input code, **When** they
   scan the collision guidance, **Then** `KeyDown` and `KeyUp` appear in the same collision list as
   `Text` / `CloseRequested` / `Rect`, with the qualification (or attribute) needed to disambiguate.

---

### User Story 2 - Consumer positions HUD text from measured metrics, not magic numbers (Priority: P2)

A product author placing a score/lives HUD string wants to size and align it from the string's
measured extent rather than guessing pixel coordinates. The pure `measureText` heuristic already
exists but is invisible to the author. After this feature, the product skills and `docs/product.md`
point the author at the pure measure helper and show a self-positioning idiom (e.g. right-align a
HUD label within the reserved HUD band), so no magic-number coordinates are needed.

**Why this priority**: Removes a recurring real-authoring friction (magic-number placement) at low
cost because the capability already ships — the work is surfacing plus, if genuinely useful, a thin
`Size`-shaped projection over the existing `TextMetrics`.

**Independent Test**: Follow the surfaced guidance to place a HUD label whose position is computed
from `measureText`; confirm the label's bounds are derived from measured width/height (no literal
coordinate) and that the guidance is reachable from at least one product skill.

**Acceptance Scenarios**:

1. **Given** a product author reading the scene/game-core product skill, **When** they look for how
   to place HUD text, **Then** they find the pure `measureText` helper named with a worked
   self-positioning example.
2. **Given** a HUD label sized from `measureText`, **When** the label is rendered at the reserved
   HUD region's size, **Then** its box is at least as wide as the drawn glyphs (no mid-string clip),
   consistent with the helper's conservative calibration.

---

### User Story 3 - Consumer writes Cmd.none / Sub.none instead of a bare [] (Priority: P3)

A product author whose `update` performs no command writes `model, Cmd.none`, and a `subscribe` with
no subscriptions returns `Sub.none`, matching Elmish convention and reading as a deliberate no-op
rather than an unfinished stub.

**Why this priority**: Pure readability/discoverability with no behavioral effect; lowest risk and
lowest urgency of the three, but cheap to deliver alongside the others.

**Independent Test**: Author a product `update`/`subscribe` returning `Cmd.none`/`Sub.none`; confirm
it compiles and is behaviorally identical to returning `[]`, and that the alias is named in product
guidance.

**Acceptance Scenarios**:

1. **Given** a product `update` that issues no command, **When** the author returns `model, Cmd.none`,
   **Then** it compiles and behaves identically to `model, []`.
2. **Given** a `subscribe` with no subscriptions, **When** the author returns `Sub.none`, **Then**
   it compiles and behaves identically to `[]`.

---

### Edge Cases

- A consumer that opens the viewer namespace *after* its own `Msg` — does the documented guidance
  hold regardless of `open` order (the remedy must not depend on open order)?
- `measureText` on an empty string, or on a glyph the bundled default family lacks — the helper stays
  pure and returns a conservative, non-negative extent (no throw).
- Does `Cmd.none`/`Sub.none` remain a compile-time alias with zero runtime cost and no change to
  command/subscription semantics?
- Introducing `[<RequireQualifiedAccess>]` on the viewer key type (if chosen over the doc-only
  remedy) MUST NOT break existing viewer/host call sites or samples that reference the cases
  unqualified.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `docs/product.md` MUST list `KeyDown` and `KeyUp` in its collision guidance alongside
  the existing `Text` / `CloseRequested` / `Rect` entries, stating the disambiguation a consumer
  needs so a product `Msg.KeyDown`/`Msg.KeyUp` compiles regardless of `open` order.
- **FR-002**: The remedy for the input-constructor collision MUST let a consumer model its own
  `KeyDown`/`KeyUp` cases and compile without a `does not match 'ViewerKey'` error. If the chosen
  remedy is `[<RequireQualifiedAccess>]` on the viewer key type, it MUST NOT regress existing
  viewer, host, or sample call sites.
- **FR-003**: A consumer-facing, product-authoring-surface `Cmd.none` and `Sub.none` MUST be
  available and behaviorally identical to returning `[]`, before any net-new alias is added the
  feature MUST confirm whether an existing surface already exposes one.
- **FR-004**: The pure host-independent text-measure helper (`FS.GG.UI.Scene.measureText`) MUST be
  surfaced to consumers — named in `docs/product.md` and in at least one product skill
  (`fs-gg-scene`, `fs-gg-layout`, or `fs-gg-game-core`) — with a worked self-positioning example
  that places HUD/overlay text from measured metrics rather than literal coordinates.
- **FR-005**: The feature MUST NOT duplicate an existing measurer: if a `Size`-shaped projection over
  `TextMetrics` is added for authoring convenience, it MUST be a thin pure derivation of the existing
  helper's output, not a second measurement path.
- **FR-006**: All changes MUST be additive and behavior-preserving for existing consumers — no
  change to rendering output, input dispatch order, or the Elmish update/subscription contract.
- **FR-007**: If any surfaced helper (`measureText`, `Cmd.none`/`Sub.none`) is part of the bundled
  authoritative framework contract surface (`template/base/docs/api-surface/**`), that surface MUST
  reflect it so the guidance's contract pointer does not dangle.
- **FR-008**: Any product skill touched MUST remain consistent with the skill-union machinery
  (ADR-0017 / Feature 238) — its `materializes-when` / `supplied-by` stay coherent and the
  skill-manifest gate stays green.

### Key Entities

- **Viewer key/event type**: the viewer-namespace type whose `KeyDown`/`KeyUp` cases collide with a
  consumer's own message cases; the subject of the §3.4 remedy.
- **`measureText` heuristic**: existing pure `string -> FontSpec -> TextMetrics` helper on the Scene
  surface; `TextMetrics = { Width; Height; Baseline }`. Subject of §3.6 surfacing.
- **`Cmd.none` / `Sub.none`**: the no-op command/subscription the product-authoring surface should
  name; subject of §3.5.
- **`docs/product.md` + product skills**: the consumer-facing guidance surfaces that must name the
  above so the capability is discoverable before the compiler is.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A consumer can model and compile its own `Msg.KeyDown`/`Msg.KeyUp` following the
  documented guidance in a single pass, with **zero** occurrences of the `does not match 'ViewerKey'`
  compile error.
- **SC-002**: A product author can locate the pure text-measure helper from the product guidance
  without reading framework source — the helper is named in `docs/product.md` and ≥1 product skill,
  with a runnable self-positioning example.
- **SC-003**: A HUD label positioned from `measureText` needs **0** magic-number coordinates for its
  placement box, and its box is never narrower than the drawn glyphs.
- **SC-004**: `Cmd.none`/`Sub.none` are available at the product-authoring surface and pass a test
  proving behavioral identity with `[]`.
- **SC-005**: The full existing test suite and the skill-manifest / api-surface gates remain green;
  no existing consumer, sample, or viewer call site regresses.

## Assumptions

- The §3.6 remedy is **surfacing** an existing pure helper plus an optional thin `Size` projection —
  not authoring a new measurer. (Confirmed: `FS.GG.UI.Scene.measureText` already exists as a pure
  heuristic returning `TextMetrics`.)
- The §3.5 remedy prefers reusing/aliasing an existing no-op over introducing a parallel one; the
  exact host module for the consumer-facing `Cmd.none`/`Sub.none` is resolved during planning.
- For §3.4, either remedy (doc line, or `[<RequireQualifiedAccess>]` on the viewer key type) is
  acceptable; the choice is made in planning against the regression constraint in FR-002. The
  doc-only remedy is the lower-risk default; the attribute is preferred only if it does not disturb
  existing call sites.
- HUD/overlay placement targets the reserved HUD region already established by the layout guidance
  (default output 640×480); this feature does not change layout region computation.
- No cross-repo contract changes are expected; this is a repo-local P1 Rendering polish item. Should
  a surfaced helper turn out to touch a versioned contract, it is escalated per cross-repo protocol.
