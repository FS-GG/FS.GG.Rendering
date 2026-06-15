# Phase 0 Research: Runtime Visual-State Bridge (Feature 096)

This is a **backfill**: the implementation already ships. "Research" here is the recovery and
recording of the design decisions embodied in the imported code, so the spec/plan/contract describe
what is actually there. Each entry is **Decision / Rationale / Alternatives considered**. There are
no open `NEEDS CLARIFICATION` items — every Technical Context field is resolved from the live source
(`src/Controls/ControlRuntime.fs`/`.fsi`, `Types.fsi`, `Attributes.fs`, `Control.fs`) and the existing
suites (`tests/Controls.Tests/Feature096RuntimeBridgeTests.fs`,
`tests/Elmish.Tests/Feature096LiveBridgeTests.fs`).

## D1 — Two functions: a public pure projection and an internal host bridge

**Decision**: 096 is exactly two functions on `ControlRuntime` — the public, pure
`deriveVisualState : ControlRuntimeModel -> ControlId -> VisualState` (`.fsi:96`, impl `.fs:208`), and
the internal host bridge `applyRuntimeVisualState : ControlRuntimeModel -> Control<'msg> ->
Control<'msg>` (`.fsi:103`). The projection answers "what state is this id in?"; the bridge walks the
lowered tree and writes that answer onto each control's `visualState` carrier.

**Rationale**: Splitting *derivation* from *stamping* keeps the projection a trivially testable pure
value function (queried directly in `Feature096RuntimeBridgeTests`/`Feature096BridgePropertyTests`)
while the bridge does the impure-looking-but-still-pure tree rewrite. `deriveVisualState` is public
because it is the honest unit a host or test reasons about; `applyRuntimeVisualState` is internal
because it is host plumbing reached only via the typed visual-state attribute and `InternalsVisibleTo`.

**Alternatives considered**: A single combined "stamp the tree from the model" function — rejected: it
fuses the testable projection into the walk, so the precedence logic could only be exercised through a
tree, not as a value. A public bridge — rejected: it would widen the surface for plumbing no consumer
calls (the authoring path is feature 093's attribute), adding baseline delta for nothing.

## D2 — Closed runtime precedence: the derivable tail, head states never derived

**Decision**: `deriveVisualState` selects under the fixed closed order
`Pressed > Selected > Focused > Hover > Normal` (`.fs:208-222`), a plain `if/elif` cascade:
`PressedControls.Contains id` → `Pressed`; else `Selection.ControlId = id` → `Selected`; else
`FocusedControl = Some id` → `Focused`; else `HoveredControl = Some id` → `Hover`; else `Normal`. This
is the runtime-derivable **tail** of the full visual-state order
`Disabled > Validation > Loading > Pressed > Selected > Focused > Hover > Normal`; the three head
states `Disabled`/`Validation`/`Loading` are **author intent** and are never produced by the
projection.

**Rationale**: A single closed order makes "which state wins when several are live at once" total and
deterministic, and peeling the top state off must reveal exactly the next — the `precedence` test
asserts this peel directly (SC-001). Excluding the head states from derivation is the contract boundary
that lets US3's arbitration work: the runtime can never *manufacture* a `Disabled`/`Validation`/
`Loading`, so those remain purely author-set.

**Alternatives considered**: Deriving the head states from runtime signals (e.g. an in-flight effect ⇒
`Loading`) — rejected as a permanent non-goal: it would couple interaction tracking to author-domain
semantics and let the runtime override author intent. A configurable/per-kind precedence — rejected:
FR-001 requires **no per-kind branching**; one closed order is simpler and provably total.

## D3 — One carrier channel; consumer non-`Normal` out-ranks derived (replace-or-append)

**Decision**: Arbitration flows through the lone `visualState` attribute (`Attributes.fs:72`,
`create "visualState" State (VisualStateValue state)`) that feature 093's `visualStateOf`
(`Control.fsi:100` / `Control.fs:96`) reads. The bridge preserves a consumer-set non-`Normal` state
unchanged and only fills a consumer-`Normal`/absent slot with the derived runtime state, writing the
carrier **replace-or-append** (last writer) so it never accumulates stale state.

**Rationale**: Routing both author intent and derived state through the *same* carrier 093 already
reads means the resolver needs no knowledge of where the state came from — author and runtime are
indistinguishable at paint time, which is exactly right (FR-003). Consumer-wins arbitration is the
guarantee that a disabled button does not "light up" when pressed; replace-or-append keeps the single
channel clean across re-renders.

**Alternatives considered**: A second "runtime" attribute alongside the author one — rejected: it
would force 093's resolver to merge two channels and re-implement precedence at paint time, and it
widens the carrier surface. Always overwriting with the derived state — rejected: it silently destroys
author intent, the exact failure US3 prevents.

## D4 — At rest is the identity guarantee: a derived `Normal` emits nothing

**Decision**: When the derived state is `Normal` (and the consumer set nothing), the bridge adds **no**
attribute — the control is returned verbatim. A `Normal`-and-unset tree is structurally unchanged, its
rendered `Scene` is byte-identical to the un-bridged build, and the live `RetainedRender.step`
recomputes **zero** nodes (FR-005, SC-003).

**Rationale**: This is the safety guarantee that lets the bridge ship on the live render path —
inserting a runtime-state stamp into every frame must not change what an at-rest control paints, must
not allocate or invalidate at rest, and must not regress the retained-render work reduction. Emitting
nothing at `Normal` makes inertness provable by construction rather than by "a `Normal` attribute
happens to resolve to the same paint." It is co-critical with US1: interaction styling is trustworthy
only if the no-interaction case is provably inert.

**Alternatives considered**: Always stamping a `Normal` attribute when nothing else applies — rejected:
even if `Normal` resolves to the same paint, it mutates the record and can perturb the retained diff,
so the byte-identity and zero-recompute guarantees would rest on coincidence rather than an explicit
no-op. The spec's `Normal`-attribute-equals-unset clause (SC-006) still holds for *consumer-set*
`Normal`, but the bridge avoids manufacturing one.

## D5 — Pre-reconcile tree walk in the `ControlId` domain, riding E2 identity

**Decision**: `applyRuntimeVisualState` is a pure recursive walk of the lowered `Control<'msg>` tree,
keying each control by its `ControlId` (= `Key` if present, else `Kind`), applied **before** reconcile.
Because the derived state is stamped onto the control's attributes pre-reconcile, the runtime look
rides feature 092's stable retained identity (E2) through the keyed diff. The feature-112 targeted
variants (`applyRuntimeVisualStateTargeted` `.fsi:116`, `runtimeStampFor` `.fsi:129`, carried as
`RuntimeStampResult<'msg>` `.fsi:76`) re-stamp only the controls whose interaction state actually
changed, reporting `RuntimeStateTouchedNodeCount`.

**Rationale**: Stamping pre-reconcile (not as a post-paint overlay) is what earns E2 "by construction":
a focus indicator attached to a control survives an unrelated re-render that shifts the control's
position, where a position-keyed baseline loses it (SC-007). The targeted variant is what makes a
localized interaction a **bounded** repaint (`RecomputedNodeCount < BaselineNodeCount`, SC-005) instead
of a full-tree restamp.

**Alternatives considered**: Stamping after reconcile / as a render overlay — rejected: it would not
ride the keyed diff, so the look would not survive a sibling shift and would re-derive every frame.
Re-stamping the whole tree on every interaction — rejected: it defeats the bounded-repaint guarantee;
the targeted variant restamps only changed ids.

## D6 — Composes with 093 (paint) and 092 (identity); duplicates neither, adds no draw of its own

**Decision**: 096 governs **state derivation and stamping only**. The *painting* of the derived state
is delegated to feature 093's resolver (E3) reading the same `visualState` carrier; the
*identity-stable, incremental* re-render is feature 092's keyed reconciler / live `RetainedRender` path
(E2). 096 adds no draw styling and duplicates neither 093's resolver nor 095's slot composition.

**Rationale**: Crisp responsibility — 096 is "interaction → state", 093 is "state → paint", 092 is
"identity + incremental diff". Keeping the draw in 093 means a migrated control restyles through the
exact resolver an author-set state would, so author and runtime states paint identically (the whole
point of routing through one carrier, D3).

**Alternatives considered**: 096 painting the runtime states itself — rejected: it would fork the paint
path between author-set and runtime-derived states and duplicate 093. Re-deriving identity in 096 —
rejected: 092 already owns retained identity; 096 only consumes it.

## D7 — Verification technique: structural Scene equality + ≥1000-case property proofs + live retained path

**Decision**: The projection's correctness (precedence/peel, unknown-id ⇒ `Normal`, determinism) is
asserted as `VisualState` value equality (`Feature096RuntimeBridgeTests`). At-rest inertness and the
"responds" proof are judged by **structural `Scene` equality** against the un-bridged build (an inert
build paints identical frames regardless of interaction; the bridged build differs only because
interaction restyles). Purity/determinism/totality and the consumer-vs-derived arbitration are
property-tested at ≥1000 generated `(model, id, consumer-state)` cases via `Gen096`
(`Feature096BridgePropertyTests`). E2 survival and the bounded repaint are proven through the **live**
`RetainedRender` path (`Feature096LiveBridgeTests`), which also regenerates the readiness markdown.

**Rationale**: Structural `Scene` equality is deterministic and headless (no GL context), so it runs in
the default local inner loop; FsCheck at ≥1000 cases is how the spec's "any `(model, id)`" totality
claim (SC-004) is made falsifiable. Driving E2/bounded-repaint through the real retained path (not a
hand-seeded map) is what makes the survival and work-reduction proofs honest — the readiness evidence
records `hand-seeded-state-by-identity=false`.

**Alternatives considered**: Pixel-diff / desktop-visibility proofs — explicitly out of scope and
disclosed in the readiness evidence (`responds-proof.md`: "Structural Scene inequality, not a pixel
encoder"); they need a GL surface and would make the inner loop non-deterministic. A hand-seeded
retained map for E2 — rejected: it would prove the assertion against a fixture rather than the shipping
path.

## Resolved unknowns

All Technical Context fields are resolved from the live source; no `NEEDS CLARIFICATION` remains. Two
disclosed gaps — the forward-looking `Selected` branch (the live host does not yet populate
`Selection`) and the five-kind scope of the *visible* restyle — are recorded as bounded follow-ups
DF-2 and DF-3 in the plan's Complexity Tracking, not open questions. Both are correct as shipped: the
`Selected` branch is total and test-proven, and every non-widened kind is correctly inert.
