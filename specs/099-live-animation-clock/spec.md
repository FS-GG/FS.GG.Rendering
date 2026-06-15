# Feature Specification: Live Animation Clock (Feature 099)

**Feature Branch**: `099-live-animation-clock`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C3** in the 2026-06-15 missing-features plan,
continuing the Workstream C pattern that feature 091 established and features 092 / 093 / 095 / 096
have followed.

Feature 091 conferred a stable, path-independent identity (`RetainedId`) on every matched node and
carried a per-identity state map (`StateByIdentity`) frame to frame. That map already had a slot for a
per-control **animation clock** (`RetainedUiState.Animation`) — but **091 only *carried* it; nothing on
the live path ever *wrote* it.** Feature 092 then wired the live Elmish host to read and write focus and
in-progress text through that same map, and proved their survival across a positional shift — but it
proved animation survival only with a **hand-seeded** clock fixture, because no real seam advanced one.

Feature 099 (the "R4" accretion) **makes the animation clock live**: the host **tick** advances each
per-identity clock by an **injected** per-frame time delta (never a wall-clock), and paint **samples**
the active clock onto the identity's own scene. A visual-state transition (e.g. a button's hover) now
**animates** over the framework's single pinned default transition (150 ms, `EaseOut`) on the real
`ControlRuntime.applyRuntimeVisualState` → `RetainedRender.advance` (Tick) → `RetainedRender.step` seam,
instead of snapping instantly. Because the clock rides the existing `RetainedId`-keyed `StateByIdentity`
map, an in-flight animation **survives** an unrelated re-render that shifts the control's position and
**completes** — replacing 092's hand-seeded precondition with a real one. At rest (no active clock) the
wired frame is **byte-identical** to the full static rebuild, so the animation seam is invisible until
something is actually animating; and a removed identity's clock is **garbage-collected** by the same
`liveIds` filter that already drops focus and text for removed identities.

The implementation, the accreted `RetainedRender.fsi` surface (`defaultTransitionDuration`, `advance`,
`clockActive`, `updateClockForState`, `sampleOnPaint`, and the `AnimationClock` record), and the
executable suites — `Feature099AnimationClockTests` in `Controls.Tests` (the pure clock core) and
`Feature099AnimationSeamTests` in `Elmish.Tests` (the live seam) — together with the captured readiness
evidence under `specs/099-live-animation-clock/readiness/` **already exist** in the imported, rebranded
source. **No Spec Kit spec/plan/tasks have ever described this work.** This document backfills the
contract so the capability is governed by `Spec → .fsi → semantic tests → implementation` like any other
feature.

The whole surface is **assembly-internal** (it lives in `module internal RetainedRender` and is reached
by the `Controls.Elmish` adapter and `ControlRuntime`), exactly like the reconciler and identity state it
builds on. It adds **zero** public-surface-baseline delta. Per the constitution's vertical-slice rule, the
in-assembly Expecto/FsCheck tests (reaching the internals via `InternalsVisibleTo`) **are** the
user-reachable surface for these internal user stories.

**Scope boundary.** Feature 099 owns the **live opacity-channel clock** — advancing it from the host
tick and sampling its fade-in (`0→1`) on paint. The same `RetainedRender.fsi` carries surface for
neighbouring features that are **out of scope for 099** and owned by their own backfills: the
two-snapshot **cross-fade composite** (the `From` prior-snapshot fading out under the new scene) is
feature **103 (R6)** — 099 uses the degenerate `From = []` plain fade-in; and the **no-alloc idle**
guarantee of `advanceStateClocks` (reference-equal state map when no clock is active) is feature **121**.
099 carries those shapes through the same map but proves only the live single-channel clock here.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A visual-state transition animates (not snaps) on the live host (Priority: P1)

When a control changes visual state on the real host — for example a button entering its hover state —
the change **animates** over the framework default transition rather than jumping to the new appearance
in a single frame. Driven through the real
`ControlRuntime.applyRuntimeVisualState` → `RetainedRender.advance` (Tick) → `RetainedRender.step` seam
with an injected 16 ms per-frame delta, the first sampled frame is **not** the snapped target; at least
one structurally-distinct intermediate frame precedes a frame that is **byte-equal** to the static
snapped target, and the sequence **converges exactly** to that target (no overshoot).

**Why this priority**: This is the headline payoff of 099 over 091/092 — the clock slot existed and was
carried, but nothing advanced or sampled it, so every visual-state change snapped. Making transitions
actually animate on the real seam is the MVP slice and the whole point of the feature.

**Independent Test**: On the live seam, hover a button (R1-migrated, animating the opacity channel) with
the framework default transition (150 ms, `EaseOut`), inject 16 ms deltas, and capture the sampled frame
sequence; confirm the first frame does **not** equal the snapped target, ≥1 intermediate frame
(structurally distinct from the target) precedes a frame byte-equal to the static snapped target, and the
final frame equals the target exactly. A build **without** the seam paints the snapped target on frame 0
(no intermediate) and fails the intermediate-frame assertion.

**Acceptance Scenarios**:

1. **Given** a button on the live seam whose hover transition uses the framework default (150 ms,
   `EaseOut`), **When** it enters hover and the host advances the clock by injected 16 ms deltas, **Then**
   the first sampled frame is **not** the snapped hover target, at least one intermediate frame precedes a
   frame byte-equal to the static snapped target, and the sequence converges to the target exactly.
2. **Given** the same transition in a build with **no** animation seam, **When** the state changes,
   **Then** the snapped target is painted on frame 0 with no intermediate frame (the counterfactual that
   fails the proof).

---

### User Story 2 - An in-flight animation survives an unrelated re-render and completes (Priority: P1)

An animation that is mid-flight when an **unrelated** part of the screen re-renders — shifting the
animating control's position — keeps animating to completion. The clock rides the stable
`RetainedId`-keyed `StateByIdentity` map (091/092), so the sibling shift moves the control's position but
not its identity; the carried clock keeps advancing from where it was, on its **same** trajectory, with
**no** hand-seeded clock and no parallel identity scheme.

**Why this priority**: This is the real-seam replacement for feature 092's hand-seeded `startedClock()`
precondition. Survival across a positional shift is co-critical with US1: an animation that resets every
time something unrelated re-renders is not usable. Proving it through the actual carry is the second
half of the MVP.

**Independent Test**: Through the real `advance` (Tick) + `step` seam over the existing
`RetainedId`-keyed `StateByIdentity` carry, hover a button, tick it mid-flight (16 ms injected deltas, so
2 frames = 32 ms elapsed), insert a banner above it (a sibling shift), then tick once more across the
shift and continue to completion; confirm the identity is stable across the shift, the elapsed time
**continued** across the straddling tick (32 ms before the shift → 48 ms after — not reset to 0), and the
shifted trajectory is byte-identical to the unshifted trajectory.

**Acceptance Scenarios**:

1. **Given** a button hovered and its clock advanced mid-flight to 32 ms (two 16 ms ticks) on the live
   seam, **When** a banner is inserted above it (shifting its position) and the clock is ticked once more
   across the shift, **Then** the button's `RetainedId` is unchanged and the carried clock keeps
   advancing — elapsed continues (32 ms → 48 ms), not reset to 0.
2. **Given** the same sequence continued to completion, **When** the trajectory after the shift is
   compared to the unshifted trajectory, **Then** they are byte-identical (the shift changed position,
   not the animation), with no hand-seeded clock.

---

### User Story 3 - The pure clock core is deterministic, and an at-rest identity is invisible (Priority: P2)

The clock advance core is **pure and total**: advancing the same starting clock by the same sequence of
injected deltas always produces byte-identical output (no wall-clock, no randomness), and each identity
advances **its own** clock independently. At rest — when no clock is active — the wired frame is
**byte-identical to the full static rebuild**: an at-rest identity emits no animation attribute, so the
animation seam contributes **zero** recompute and **zero** remeasure, and a settled clock that has
returned to `Normal` is **dropped** so the byte-identical-at-rest state is restored.

**Why this priority**: Determinism is the constitution's hard constraint on the live path (Principle VI),
and identity-at-rest is the correctness guard that the animation seam is *invisible* until something
animates — a regression here would silently churn the whole tree. Both are P2: they protect the core
animation journey (US1/US2) rather than being a separate user-facing journey, and both are proven by the
same pure-core suite.

**Independent Test**: (determinism) Drive `RetainedRender.advance` with a fixed 12-frame injected-delta
sequence twice and assert the two runs are byte-identical (both settle at 150 ms); run 1000 FsCheck cases
over random delta sequences. (identity-at-rest) Step the live retained path on a frame with no active
clock and assert the produced scene equals the full `Control.renderTree` static rebuild byte-for-byte,
with at-rest recompute and remeasure counts both 0, and that a settled return-to-`Normal` clock has been
dropped from the state.

**Acceptance Scenarios**:

1. **Given** a starting clock and a fixed sequence of injected deltas, **When** `advance` replays the
   sequence twice, **Then** both runs produce byte-identical output (both elapsed = 150 ms).
2. **Given** an identity with no active clock, **When** the live retained path renders the frame, **Then**
   the scene is byte-identical to the full static rebuild and the animation seam adds zero recompute and
   zero remeasure.
3. **Given** a clock that has settled back to `Normal`, **When** the next frame is stepped, **Then** the
   settled clock is dropped from `StateByIdentity` and the identity is byte-identical at rest.
4. **Given** multiple identities each with their own clock, **When** a delta is applied, **Then** each
   `RetainedId` advances its **own** clock independently (no cross-talk).

---

### User Story 4 - A removed identity's animation clock is garbage-collected (Priority: P3)

When a control that owns an active animation clock is removed from the tree, its clock leaves with it —
the next frame's `StateByIdentity` carries **no** clock for the removed identity. This rides the same
`liveIds` filter that already drops focus and text state for removed identities; there is no new GC code
and no parallel animation-state scheme, so no dangling animation state can accumulate.

**Why this priority**: This is a hygiene/leak guard rather than a core journey — P3. It matters because an
unbounded, never-collected clock map would be a slow leak on a long-running host, but it is a refinement
of the survival machinery (US2) rather than a new capability.

**Independent Test**: Hover a button so its clock is active, confirm a clock is present for its identity
while it is live, re-render with the button removed, and confirm the next frame's `StateByIdentity`
carries no clock for that identity.

**Acceptance Scenarios**:

1. **Given** a button with an active animation clock, **When** the next render removes the button,
   **Then** the next frame's `StateByIdentity` has no clock for the removed identity (present while live,
   absent after removal) — matching the existing focus/text GC behavior.

---

### User Story 5 - One active animation does not force a whole-tree repaint (Priority: P2)

While a single control is animating, the rest of the tree stays on its fast path. A structurally-unchanged
animating frame takes the `Keep` fast path — **zero** re-measure and **zero** re-paint of the cached static
fragments — while still **sampling** the active clock, so the animation produces a per-frame change scoped
to the animating subtree. One active animation never invalidates the at-rest fast path for the rest of the
tree.

**Why this priority**: This is the efficiency guard on the animation seam — without it, a single hover
animation could relayout/repaint the entire screen every frame. P2 because it protects the work-reduction
story (the same story 091/092 established) under animation rather than delivering the core animate-vs-snap
journey.

**Independent Test**: On a steady-state animating frame, inspect the `RetainedRender.step` `WorkReduction`
metric and confirm the steady-state recompute count is 0 and the steady-state remeasure count is 0, while
the frame still reports a change (the clock was sampled) — animation is a paint-level overlay applied to
cached static fragments at scene assembly.

**Acceptance Scenarios**:

1. **Given** a frame with exactly one active animation and no structural change, **When** it is stepped,
   **Then** the `WorkReduction` steady-state recompute count is 0 and remeasure count is 0, yet the frame
   reports a change because the active clock was sampled.

---

### Edge Cases

- **Non-positive injected delta**: `advance` is a **no-op** — the clock never rewinds.
- **Very large injected delta**: `advance` **clamps** to the animation's duration; the sample settles at
  the end with **no overshoot**.
- **Retarget mid-flight** (the visual state changes while a clock is still in flight): the clock **re-aims
  from its current sampled value** — it does **not** snap back to the start.
- **Return to `Normal` once settled**: the clock is **dropped** to `None`, restoring the byte-identical
  at-rest state.
- **Multiple concurrent clocks**: each `RetainedId` advances its **own** clock independently (no
  cross-talk).
- **No prior snapshot to cross-fade from** (`From = []`): `sampleOnPaint` degenerates to the plain
  fade-in (the 099-owned single channel); the two-snapshot cross-fade composite is feature 103's concern.
- **Settled / absent clock**: not sampled — `clockActive` is false, so the identity paints byte-identical
  to the static render and contributes no per-frame change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A visual-state transition on the live host MUST **animate** over the framework default
  transition rather than snapping in a single frame, driven through the real
  `ControlRuntime.applyRuntimeVisualState` → `RetainedRender.advance` (Tick) → `RetainedRender.step` seam:
  the first sampled frame MUST NOT be the snapped target, at least one structurally-distinct intermediate
  frame MUST precede a frame byte-equal to the static snapped target, and the sequence MUST converge to
  that target **exactly** (no overshoot).
- **FR-002**: There MUST be exactly **one** pinned framework default transition — **150 ms**, `EaseOut` —
  used when a transition does not specify its own duration/easing.
- **FR-003**: The animation clock MUST be advanced by an **injected** per-frame time delta only — never a
  wall-clock or system time source — and the advance core MUST be **total** and **pure**.
- **FR-004**: `advance` MUST **clamp** to the animation's duration (a very large delta settles at the end
  with no overshoot) and MUST treat a **non-positive** delta as a no-op (never rewinds).
- **FR-005**: An in-flight animation clock MUST ride the per-identity `StateByIdentity` map keyed by the
  stable `RetainedId` (091/092), so it **survives** an unrelated re-render that shifts the control's
  position: the clock MUST **continue** advancing (not reset) to completion under the same identity, its
  shifted trajectory MUST equal its unshifted trajectory, and there MUST be **no** hand-seeded clock and
  **no** parallel identity scheme.
- **FR-006**: An identity with **no active clock** MUST emit no animation attribute and MUST paint
  **byte-identical** to the full static rebuild — contributing **zero** recompute and **zero** remeasure;
  a clock that has **settled back to `Normal`** MUST be **dropped** so the byte-identical-at-rest state is
  restored.
- **FR-007**: The pure clock core MUST be **deterministic**: an identical injected-delta sequence MUST
  produce byte-identical output, and each `RetainedId` MUST advance **its own** clock independently (no
  cross-talk).
- **FR-008**: The transition trigger (`updateClockForState`) MUST: **start** a fade-in for a fresh state
  change (from a settled/absent clock), **retarget from the current sampled value** for a mid-flight change
  (no snap to start), **advance-only** when the state is unchanged, and **drop** a settled
  return-to-`Normal` clock. On a fresh transition or a mid-flight retarget the new clock's `From` MUST be
  the prior own-scene snapshot it cross-fades from; an advance-only/kept clock MUST retain its existing
  `From`.
- **FR-009**: An active animation MUST stay **scoped**: a structurally-unchanged animating frame MUST take
  the `Keep` fast path (zero re-measure, zero re-paint of cached static fragments) while still **sampling**
  the active clock, so one active animation never invalidates the at-rest fast path for the rest of the
  tree.
- **FR-010**: A **removed** identity's animation clock MUST be **garbage-collected** by the existing
  `liveIds` filter (it leaves with its identity), matching the focus/text GC behavior — no dangling
  animation state, no new GC code path.
- **FR-011**: Only an **active** clock (per `clockActive`) MUST be sampled; a settled or absent clock MUST
  NOT be sampled, so only active clocks contribute a per-frame change. The wired `advance`,
  `updateClockForState`, and `sampleOnPaint` MUST be **total** and **deterministic** on the live path.
- **FR-012**: The entire surface MUST remain **assembly-internal** — zero public-surface-baseline delta —
  and remain exercised only through the in-assembly tests via `InternalsVisibleTo`. (Verified directly by
  the surface-drift check; this requirement has no separate Success Criterion.)

### Key Entities *(include if feature involves data)*

- **AnimationClock**: the per-identity live clock. Carries `Anim` (the reused feature-073 `Animation`
  whose live channel is the **opacity tween** `0→1`), `Elapsed` (the accumulated injected-delta `TimeSpan`
  — the clock's sole time coordinate; never a wall-clock), `Target` (the `VisualState` it animates toward,
  used to detect a retarget), and `From` (the prior state's own-scene snapshot it cross-fades from — `[]`
  for the 099 plain fade-in). Advanced by an injected delta, sampled on paint.
- **RetainedUiState.Animation**: the `AnimationClock option` slot in the per-identity state. 091 carried
  it frame to frame; 099 is what actually **writes** it (advance + sample) on the live path.
- **StateByIdentity / RetainedId**: the stable-identity-keyed per-identity state map (091/092) the clock
  rides, so the clock survives positional shifts and is garbage-collected with its identity.
- **defaultTransitionDuration**: the single pinned framework default transition — exactly **150 ms**,
  `EaseOut`.
- **VisualState**: the visual state a clock animates toward (`Normal`/`Hover`/`Pressed`/`Focused`/… ); a
  return to `Normal` once settled drops the clock.
- **WorkReduction (steady-state)**: the per-frame work metric (091/092) used to prove an active animation
  stays scoped — steady-state recompute and remeasure counts are 0 while the frame still changes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the live seam, a visual-state transition produces **≥1 intermediate frame** (structurally
  distinct from the target) before a frame byte-equal to the static snapped target, converging to the
  target exactly (no overshoot), in 100% of cases; a no-seam build snaps on frame 0 (no intermediate) and
  fails the proof.
- **SC-002**: An in-flight clock survives a position-shifting re-render in 100% of cases — its identity is
  stable across the shift, its elapsed time **continues** (e.g. 32 ms → 48 ms, not reset), and its shifted
  trajectory is byte-identical to its unshifted trajectory — with no hand-seeded clock.
- **SC-003**: An at-rest (no active clock) frame is **byte-identical** to the full static rebuild with
  **zero** recompute and **zero** remeasure in 100% of cases; a settled return-to-`Normal` clock is dropped.
- **SC-004**: An identical injected-delta sequence produces byte-identical clock output across **1000**
  FsCheck cases, and each identity's clock advances independently; the advance core consults **no**
  wall-clock.
- **SC-005**: A removed identity's clock is **absent** from the next frame's `StateByIdentity` in 100% of
  cases (present while live, gone after removal).
- **SC-006**: While exactly one animation is active, the static remainder of the tree takes the `Keep`
  fast path — steady-state recompute count = 0 and remeasure count = 0 — while the frame still reports a
  change (the clock was sampled), in 100% of cases.

## Assumptions

- The keyed reconciler (feature 067), the retained render structure with stable `RetainedId` identity, and
  the carried `StateByIdentity` map with its `RetainedUiState.Animation` slot (feature 091), plus the live
  read/write wiring and the host tick/step seam (feature 092), already exist in the imported source. 099 is
  the **backfilled contract** for *making the carried animation clock live* (advance from the host tick,
  sample on paint), not new-from-scratch construction.
- The surface stays **internal**; "users" of these stories are framework internals plus the in-assembly
  tests (per the constitution's vertical-slice rule), not external package consumers. No public API is
  added; the public feature-073 `Animation.applyAt` is the existing primitive `sampleOnPaint` composites
  through.
- Feature 099 owns the **live opacity-channel clock** (fade-in `0→1`) only. The two-snapshot **cross-fade
  composite** that fades the `From` prior snapshot out under the new scene is feature **103 (R6)**, and the
  **no-alloc idle** reference-equal behavior of `advanceStateClocks` is feature **121** — both share
  surface in the same `RetainedRender.fsi` but are out of scope for 099 and proven by their own backfills.
  099 uses the degenerate `From = []` plain fade-in.
- Render-output equivalence is judged by **structural scene equality** (the authoritative parity proof, the
  `DeterministicRenderOnly` renderer mode); `SceneEvidence.renderPng` is a capability-hash, not a pixel
  encoder, so pixel-level / desktop-visibility proofs are out of scope (the readiness evidence explicitly
  does not claim them).
- The existing readiness evidence under `readiness/` (`us1-animates-vs-snaps`, `us2-survival`,
  `us3-determinism`, `us3-identity-at-rest`, `us4-gc`, `scoped-repaint`) corresponds to SC-001 through
  SC-006 and is the captured artifact for those outcomes.
- This is the **C3** conformance backfill in the 2026-06-15 missing-features plan, following the 091
  pattern and the 092 / 093 / 095 / 096 closes; `/speckit-plan`, `/speckit-tasks`, and `/speckit-implement`
  reduce to a conformance pass (confirm the suites are green, the readiness evidence regenerates, and the
  surface delta is zero), not a build.
