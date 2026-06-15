# Feature Specification: Retained Pointer Routing → Authored Control ID (Feature 110)

**Feature Branch**: `110-retained-authored-routing`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C5** in the 2026-06-15 missing-features plan,
continuing the Workstream C pattern (091 / 092 / 093 / 095 / 096 / 099 / 097 / 103).

Feature 092 made pointer hit-testing route from the **retained** frame (`retainedHitTest`: a point → a
stable `RetainedId`). Feature 098 established the **authored-control-id** scheme (a binding lives on the
nearest ancestor that is *keyed* or whose canonical id is in `BoundIds`, climbed by `Control.nearestAuthored`
over a freshly-rendered tree). But dispatching a pointer event still ran a **full `host.View` +
`Control.renderTree`** to rebuild the tree and re-climb to the authored binding.

Feature 110 **routes the whole pointer interaction from the retained frame with no re-render**:
`authoredControlIds` reproduces, *from retained identity*, exactly the keyed-OR-in-`BoundIds` climb that the
full-render oracle performs — yielding a `Map<RetainedId, ControlId>` from a hit identity to the authored
binding it should fire. `routeRetainedInteraction` / `routeRetainedPointer` then dispatch the **same**
message list the oracle would, while running **no** view and **no** full render (`ViewCalled = false`,
`FullRenderCount = 0`). The preserved full-render path survives only as a **parity oracle** and a **counted
escape hatch**: when a bindable hit cannot be resolved from the retained frame, routing falls back to a full
render and increments `FullRenderFallbackCount` by exactly one — `0` for every normal scripted scenario.

The implementation (`authoredControlIds` in `RetainedRender.fs`/`.fsi`; `routeRetainedInteraction` /
`routeRetainedPointer` in `ControlsElmish.fs`/`.fsi`), the accreted surface (the internal routing functions
plus the public `FrameMetrics.FullRenderFallbackCount` field), and the three suites
(`Feature110RetainedRoutingTests`, `Feature110RetainedRoutingParityTests`, `Feature110FallbackTests`, all in
`Elmish.Tests`) **already exist** in the imported source. **No Spec Kit spec/plan/tasks describe this work**,
and 110 imported with **no `readiness/` evidence**. This document backfills the contract.

The routing surface is **assembly-internal** (reached via `InternalsVisibleTo`); the one public touch is the
additive `FrameMetrics.FullRenderFallbackCount` field on the already-baselined public `FrameMetrics` type, so
the backfill adds **zero new public-surface-baseline delta** (the baseline is type-granular). Per the
constitution's vertical-slice rule the in-assembly tests are the user-reachable surface — and routing *is*
the production pointer path.

**Scope boundary.** 110 owns retained pointer **routing** (`authoredControlIds` + `routeRetained*` + the
counted fallback). It **reuses** `retainedHitTest` (092, point → `RetainedId`), the keyed-OR-in-`BoundIds`
authored-id scheme and `MapPointer` oracle (098), and the move-coalescing infrastructure (`PointerMovesProcessed`).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pointer events route from the retained frame with zero full renders (Priority: P1)

A pointer move or click is routed entirely from the retained frame: no `host.View` is run, no
`Control.renderTree` rebuild happens, and move bursts still coalesce. The user sees identical behaviour at a
fraction of the per-event cost.

**Why this priority**: This is the headline payoff — eliminating a full render per pointer event is the whole
point of 110. The MVP slice.

**Independent Test**: Drive scripted moves/clicks (bound and unbound) through the retained route and assert
`FullRenderCount = 0`, `ViewCalled = false`, `FullRenderFallbackCount = 0`; a burst of N moves processes ≤ 1
move with zero routing renders; an interleaved move/discrete script drops none.

**Acceptance Scenarios**:

1. **Given** a routed move, **When** it is processed, **Then** `FullRenderCount = 0`, `ViewCalled = false`,
   `FullRenderFallbackCount = 0` (SC-001/SC-005).
2. **Given** a routed click on a bound control, **When** processed, **Then** its binding is dispatched from
   the retained frame with no routing render (SC-002).
3. **Given** a burst of N move samples in one frame, **When** processed, **Then** ≤ 1 move is processed,
   zero routing renders, no fallback (SC-009); a drag/freehand path keeps fidelity (FR-012).

---

### User Story 2 - Retained routing is dispatch-identical to the full-render oracle (Priority: P1)

The message list the retained route dispatches is **identical** to what the preserved full-render oracle
would dispatch — for keyed controls, unkeyed same-kind siblings (each fires its own distinct binding), a
composite whose binding is authored *above* the hit node, nested containers, the `MapPointer` fallback, and
the resulting focus identity.

**Why this priority**: Co-critical with US1. Routing that is faster but dispatches a *different* message is a
regression. Parity is what makes the fast path safe.

**Independent Test**: For each scenario, dispatch through both the retained route and the oracle and assert
identical message lists (and identical focused identity for the focus clause).

**Acceptance Scenarios**:

1. **Given** keyed controls / nested containers, **When** clicked, **Then** the retained route dispatches the
   identical message list as the oracle (SC-003).
2. **Given** unkeyed same-kind siblings, **When** the second is clicked, **Then** it fires its own distinct
   binding (SC-004/FR-005).
3. **Given** a composite, **When** an inner node is clicked, **Then** the binding authored *above* it is
   dispatched (SC-003/FR-003); an unbound control routes via `MapPointer` identically (FR-006); a focusable
   click resolves the same focused identity (FR-006 focus clause).

---

### User Story 3 - The full-render fallback is a counted escape hatch (Priority: P2)

Every normal scripted scenario reports `FullRenderFallbackCount = 0`. A bindable hit that genuinely cannot be
resolved from the retained frame falls back to a full render — incrementing the count by **exactly one** —
and that fallback dispatches the same message the oracle would. The fallback is an escape hatch, never the
normal path.

**Why this priority**: P2 — the honesty/observability guard on the fast path. It proves routing is not
silently falling back (which would erase the win) and that the rare fallback is still correct.

**Independent Test**: Assert every frame of every normal scenario reports `FullRenderFallbackCount = 0`; a
deliberately-unroutable case increments it by exactly one and matches the oracle; a resolvable hit takes no
fallback.

**Acceptance Scenarios**:

1. **Given** any normal scripted pointer scenario, **When** run, **Then** `FullRenderFallbackCount = 0` for
   every frame (SC-005).
2. **Given** a deliberately-unroutable bindable hit, **When** routed, **Then** the count increments by
   exactly one and the fallback dispatch matches the oracle (SC-006); a resolvable hit takes no fallback.

---

### Edge Cases

- **Unkeyed same-kind siblings**: distinguishable only through retained identity — each fires its own binding.
- **Composite (binding authored above the hit)**: routes to the ancestor's binding via the retained climb.
- **Unbound control**: routes through `MapPointer` identically to the oracle (no binding fired).
- **Move burst**: coalesces to ≤ 1 processed move, zero routing renders.
- **Unresolvable bindable hit**: the only case that falls back — counted, exactly one, oracle-matched.
- **A node with no authored ancestor**: no entry in the map (no binding fired).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A single already-resolved `PointerInteraction` MUST be resolvable from the retained frame
  (`routeRetainedInteraction`).
- **FR-002**: Routing MUST perform **no** `host.View` and **no** `Control.renderTree` rebuild.
- **FR-003**: `authoredControlIds` MUST reproduce, from retained identity, the keyed-OR-in-`BoundIds` climb
  the full-render oracle performs — covering a composite whose binding is authored above the hit node. Pure,
  total, deterministic.
- **FR-004**: Pointer input MUST route from the retained frame; the routing frame MUST perform zero full
  renders.
- **FR-005**: Unkeyed same-kind siblings MUST be distinguishable through retained identity, each firing its
  own distinct binding.
- **FR-006**: The retained route MUST be **dispatch-identical** to the full-render oracle — including the
  `MapPointer` fallback path and the resulting focused identity.
- **FR-007**: The preserved full-render path MUST survive only as a parity oracle and a counted escape hatch.
- **FR-008**: A pure routing frame MUST NOT run the view (`ViewCalled = false`); raw pointer samples
  (including queued moves) are accepted.
- **FR-009**: `FullRenderFallbackCount` MUST increment by **exactly one** on an unresolvable bindable hit.
- **FR-012**: Move coalescing / drag-freehand path fidelity MUST be preserved through the retained route.
- **FR-013**: The backfill MUST add **zero new public-surface-baseline delta**; the routing functions stay
  `internal`, and `FullRenderFallbackCount` is an additive field on the already-baselined public `FrameMetrics`.

### Key Entities *(include if feature involves data)*

- **authoredControlIds**: `Set<ControlId> -> RetainedRender -> Map<RetainedId, ControlId>` — the retained-id →
  authored-binding lookup (internal). Reproduces `nearestAuthored`'s climb from retained identity.
- **routeRetainedInteraction / routeRetainedPointer**: the internal routing entry points; return a fallback
  count (`1` on an unresolvable bindable hit, else `0`).
- **FullRenderFallbackCount**: the public `FrameMetrics` field — how many times routing fell back to a full
  render this frame; `0` on every normal path (SC-005).
- **The oracle**: `host.View` + `Control.renderTree` + `Control.nearestAuthored` / `MapPointer` — preserved
  as the parity reference and counted escape hatch.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Routing a move performs **zero** full renders (and `ViewCalled = false`), 100% of cases.
- **SC-002**: An unbound/bound click is a pure routing frame (zero full renders), with the binding resolved
  from the retained frame, 100% of cases.
- **SC-003**: The retained route dispatches a message list **identical** to the oracle, 100% of cases.
- **SC-004**: Unkeyed-sibling selection is correct — each sibling fires its own binding, 100% of cases.
- **SC-005**: Every normal scripted pointer scenario reports `FullRenderFallbackCount = 0` for every frame.
- **SC-006**: A constructed unroutable case increments `FullRenderFallbackCount` by **exactly one** and the
  fallback matches the oracle.
- **SC-009**: A move burst coalesces to ≤ 1 processed move with zero routing renders and no fallback.

## Assumptions

- `retainedHitTest` (092, point → `RetainedId`), the keyed-OR-in-`BoundIds` authored-id scheme + `MapPointer`
  oracle (098), and the move-coalescing infrastructure already exist. 110 is the **backfilled contract** for
  routing the interaction from the retained frame, not new-from-scratch construction.
- The routing surface is **internal**; "users" are framework internals plus the in-assembly tests — and
  routing *is* the production pointer path. The only public touch is the additive
  `FrameMetrics.FullRenderFallbackCount` on an already-baselined type ⇒ **zero new** public-surface delta.
- 110 imported with executable suites (in `Elmish.Tests`, headless) but **no `readiness/` evidence**;
  authoring readiness is part of this backfill. No FsCheck (all deterministic example-based tests).
- This is the **C5** conformance backfill; `/speckit-plan`, `/speckit-tasks`, `/speckit-implement` reduce to a
  conformance pass (suites green, readiness authored, zero surface delta), not a build.
</content>
