# Phase 0 Research: Lookless Slot Composition (Feature 095)

This is a **backfill**: the implementation already ships. "Research" here is the recovery and
recording of the design decisions embodied in the imported code, so the spec/plan/contract describe
what is actually there. Each entry is **Decision / Rationale / Alternatives considered**. There are
no open `NEEDS CLARIFICATION` items — every Technical Context field is resolved from the live source
(`src/Controls/Control.fs`, `Types.fsi`, `Widgets/Primitives.fs`, `Widgets/Containers.fs`) and the
existing suite (`tests/Controls.Tests/Feature095SlotCompositionTests.fs`).

## D1 — Slot fills ride the existing `Attr` mechanism under a new category, not a new field

**Decision**: A slot fill is carried as an ordinary `Attr<'msg>` with `Category = Slot` and
`Value = SlotFillsValue of (string * Control<'msg>) list` — an ordered association list from region
name to fill sub-tree. The builder is the internal `ControlInternals.slotFill`
(`Control.fs:108`); readers are `slotFillsOf` (`:113`) and `slotFor` (`:124`).

**Rationale**: Reusing the attribute carrier means slot fills flow through every place attributes
already flow (mapping, reconciliation, lowering) with zero new plumbing, and the "at most one,
last-writer-wins" semantics fall out of the existing `tryKey`/`tryLast` attribute readers for free.

**Alternatives considered**: A dedicated `Slots` field on `Control<'msg>` — rejected: it would touch
every control constructor, widen the public record, and require bespoke map/reconcile handling. A
per-region attribute (`leadingFill`, `trailingFill`, …) — rejected: it scatters the "one carrier,
last-writer-wins" rule across N attributes and complicates ordering.

## D2 — The authoring path is a closed, typed front door; the slot name is internal plumbing

**Decision**: Consumers fill regions only through typed `Props` fields —
`ButtonProps.Leading`/`Trailing` (`Primitives.fs:25-26`, lowered at `:88-92`) and
`PanelProps.Header`/`Footer` (`Containers.fs:27-28`, lowered at `:126-130`). These map the typed
option fields to `("leading"/"trailing"/"header"/"footer", control)` pairs and call the internal
`ControlInternals.slotFill`. There is **no** public free-form `Attr.slot` builder and no
consumer-facing slot-name string.

**Rationale**: A typed front door makes illegal states unrepresentable — a consumer cannot target a
region the kind doesn't declare or misspell a slot name — and keeps the slot-name string an internal
projection (made only at the lowering edge via `slotName`). FR-001/SC-006 require exactly this
closure; the `typedClosure` test list pins it against the public surface.

**Alternatives considered**: A public `Attr.slot "leading" widget` escape hatch — rejected: it
reintroduces stringly-typed authoring and an unbounded region namespace, defeating the scoping and
typing guarantees.

## D3 — Lowering injects fills into ordinary `Children`, ordered `[leading; intrinsic; trailing]`

**Decision**: `lowerSlots` (`Control.fs:163`) matches `slotFillsOf`; on `[]` it returns the control
verbatim; otherwise it computes the kind's `(leadingNames, trailingNames)` via `slotRegions`, `pick`s
each present region's fill in declared order, and sets `Children = pick leadingNames @
control.Children @ pick trailingNames`, while filtering the `Slot` carrier out of `Attributes`
(consuming it).

**Rationale**: Lowering fills into the *same* `Children` collection every other child uses is what
earns E1–E4 "by construction" — a slotted control is dispatched, retained, styled, and focus-routed
by the identical machinery as any other child, with no slot-specific code path (FR-004/FR-005). The
fixed `[leading; intrinsic; trailing]` order is the entire visible contract of placement.

**Alternatives considered**: A separate render pass that paints slots outside the child tree —
rejected: it would require duplicating E1–E4 for slotted content and break retained identity. Sorting
regions by name or by fill-list order — rejected: placement must be deterministic and tied to
declared region position, not author input order.

## D4 — Unfilled is the identity fast path (byte-identity), not "fill with empties"

**Decision**: When no `Slot` attribute is present, `lowerSlots` returns the control unchanged
(`| [] -> control`, `Control.fs:165`). No carrier is added, no `Children` are appended, and nothing
is filtered.

**Rationale**: This is the safety guarantee that lets the seam ship on the live path: adding slot
machinery to a control must not change what it paints when nobody fills a slot (FR-003/SC-002). The
identity arm makes that provable by construction rather than by careful empty-handling, and the
frozen `button.{light,dark}.normal.scene.txt` oracle confirms structural scene equality.

**Alternatives considered**: Always running the fill loop with empty `pick` results — rejected: it
still rewrites the record (`{ control with … }`) and re-filters attributes, so the equality would be
*structural* but not *referential identity*, and it leaves the byte-identity guarantee resting on
"empty list concatenation happens to be a no-op" rather than an explicit fast path.

## D5 — Slot exposure is scoped per-kind via `slotRegions`

**Decision**: `slotRegions` (`Control.fs:148`, `private`) declares regions for exactly two kinds:
`"button" -> [Leading], [Trailing]` and `"panel" -> [Header], [Footer]`; every other kind returns
`[], []`. A `SlotName` DU (`Leading`/`Trailing`/`Header`/`Footer`) with `slotName` projecting to the
wire string keeps the partition typed.

**Rationale**: Scoped exposure means a non-opted-in kind (e.g. `CheckBox`) gains no regions and is
provably unaffected (FR-007/SC-007), while the `[], []` default keeps lowering **total** for every
kind — an unknown kind simply picks nothing. The leading/trailing partition encodes the
before/after-intrinsic-content placement directly in the data.

**Alternatives considered**: A global region registry or per-control region declaration on the
record — rejected: heavier than a single `match` and would let regions drift from the kinds that
actually render them.

## D6 — Lookless and single-control: static fills, no templates / selectors / cascade

**Decision**: A fill is a static `Control<'msg>` value. There is no data-bound template, no deferred
expression, no selector matching, no specificity, and no cascade (FR-008). Slot composition is
single-control structural injection only; visual-state *styling* of slotted sub-trees is delegated to
feature 093's `Style.resolve` (E3), which 095 composes with rather than duplicates.

**Rationale**: Keeping fills lookless makes lowering pure/total/deterministic (no evaluation of a
template against data, nothing that can throw) and keeps the feature's responsibility crisp:
structure in, structure out. Styling lives in 093; runtime state derivation in 096.

**Alternatives considered**: Data-bound slot templates (à la a `RenderFragment<T>`) — rejected as a
permanent non-goal: it would couple slots to a data model and reintroduce evaluation-time failure
modes. Selector/specificity/cascade — rejected: that is a styling concern owned by 093, not a
composition concern.

## D7 — Verification technique: structural scene equality + ≥1000-case property proofs

**Decision**: Render-output equivalence (the unfilled-parity case) is judged by **structural scene
equality** against a frozen procedural oracle (`readiness/parity/button.{light,dark}.normal.scene.txt`),
the same technique features 091/093 use. Purity/determinism/totality are property-tested at ≥1000
generated `(kind, fills)` inputs via the `Gen095` generator. E2 survival is proven through the **live**
`RetainedRender` path, not a hand-seeded map.

**Rationale**: Structural scene equality is deterministic and headless (no GL context), so it runs in
the default local inner loop; FsCheck at ≥1000 cases is how the spec's "any `(kind, fills)`" totality
claim (SC-005) is made falsifiable. Driving E2 through the real retained path is what makes the
identity-survival proof honest.

**Alternatives considered**: Pixel-diff / desktop-visibility proofs — explicitly out of scope and
disclosed in the readiness evidence; they need a GL surface and would make the inner loop
non-deterministic. A hand-seeded retained map for E2 — rejected: it would prove the assertion against
a fixture rather than the shipping path.

## Resolved unknowns

All Technical Context fields are resolved from the live source; no `NEEDS CLARIFICATION` remains. The
one disclosed gap — `panel` has declared regions but no frozen parity scene — is recorded as bounded
follow-up DF-2 in the plan's Complexity Tracking, not an open question.
