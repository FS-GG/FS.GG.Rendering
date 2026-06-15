# Feature Specification: Visual-State Cross-Fade (Feature 103)

**Feature Branch**: `103-visual-state-cross-fade`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C4** in the 2026-06-15 missing-features plan,
continuing the Workstream C pattern that feature 091 established and features 092 / 093 / 095 / 096 / 099 /
097 have followed.

Feature 099 (the "R4" accretion) made the per-identity **animation clock** live: the host tick advances each
clock by an injected per-frame delta, and paint **samples** the active clock onto the identity's own scene.
But 099 scoped itself to the **live opacity-channel clock** — the degenerate `From = []` **plain fade-in**
(the new appearance fading in from transparent). It explicitly carved out the genuine **cross-fade**: a
visual-state change whose paint differs (a token-derived colour swap) still showed only the *next* colour
fading in, with the prior colour simply **absent** mid-flight. That is not how a real transition looks.

Feature 103 (the "R6" accretion) **makes the transition a genuine two-snapshot cross-fade**. The pure
transition trigger (`updateClockForState`) captures the prior state's **static own-scene snapshot** as the
clock's `From`, and the paint composite (`sampleOnPaint`) renders **two opacity-driven layers** via the
public feature-073 `Animation.applyAt`: the prior `From` snapshot fading **OUT** (`1→0`) **under** the next
state's own-scene fading **IN** (the clock's opacity tween). For a region painted in **both** states (e.g. a
Switch track whose fill restyles `Muted→Accent` on hover via `Style.resolve`), the composite displays a
colour **strictly between** the endpoints mid-flight — the prior dimming, the next brightening — instead of
the next colour appearing over emptiness. Because the cross-fade is a **paint-level overlay gated to active
(mid-flight) clocks only**, an at-rest or settled identity is **byte-identical** to the static render: the
settle and fast paths are **untouched**, the final frame equals the snapped static render for every channel,
and the at-rest frame emits no animation attribute and takes the zero-recompute fast path.

The implementation (the cross-fade composite in `RetainedRender.fs`/`.fsi` reached through the
`ControlRuntime` visual-state bridge and the `Controls.Elmish` host tick), the accreted `RetainedRender.fsi`
surface (the `AnimationClock.From` snapshot field, `updateClockForState`, and `sampleOnPaint` — **shared
with 099** but here realizing the two-layer composite), and the executable suite
(`tests/Controls.Tests/Feature103CrossFadeTests.fs`) together with the captured readiness evidence under
`specs/103-visual-state-cross-fade/readiness/` **already exist** in the imported, rebranded source. **No
Spec Kit spec/plan/tasks have ever described this work.** This document backfills the contract so the
capability is governed by `Spec → .fsi → semantic tests → implementation` like any other feature.

The whole surface is **assembly-internal** (it lives in `module internal RetainedRender` and is reached by
the `ControlRuntime` bridge and the `Controls.Elmish` host tick), exactly like the clock (099) and the
identity state (091/092) it builds on. It adds **zero** public-surface-baseline delta. Per the
constitution's vertical-slice rule, the in-assembly Expecto/FsCheck tests (reaching the internals via
`InternalsVisibleTo`) **are** the user-reachable surface for these internal user stories.

**Scope boundary.** Feature 103 owns the **two-snapshot cross-fade composite** — capturing the prior
own-scene as `From` and compositing the prior layer fading out under the next layer fading in. The
neighbouring shared surface is owned by other features: the **live single-channel clock** itself (advance
from the host tick, the plain `From = []` fade-in) is feature **099 (R4)**; the **no-alloc idle**
reference-equal behaviour of `advanceStateClocks` (a settled/empty state map allocates nothing) is feature
**121**. 103 uses the same `RetainedId`-keyed `StateByIdentity` map and the same `updateClockForState` /
`sampleOnPaint` seam, but the capability it proves is the genuine cross-fade.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A visual-state transition genuinely cross-fades its colours (Priority: P1)

When a control changes visual state on the real host and its paint differs in a token-derived colour — for
example a Switch track restyling `Muted→Accent` on hover — the transition shows **both** endpoint colours
mid-flight: the prior colour fading **out** under the next colour fading **in**, so the displayed colour is
**strictly between** the endpoints. It is a true cross-fade, not the next appearance fading in from
transparent over emptiness.

**Why this priority**: This is the headline payoff of 103 over 099 — 099's plain fade-in showed only the
next colour appearing over nothing; the prior colour was absent. Making the transition a genuine two-layer
cross-fade is the MVP slice and the whole point of the feature. This assertion is **red** on the pre-R6
code (the prior colour is absent mid-flight) and **green** after the snapshot composite.

**Independent Test**: Through the real `ControlRuntime.applyRuntimeVisualState` → `RetainedRender.advance`
(Tick) → `RetainedRender.step` seam, hover a Switch whose track fill swaps `Muted→Accent`, sample mid-flight
(75 ms of the 150 ms default), and inspect the `sampleOnPaint` composite: confirm the clock captured
`From` = the prior (Normal) own-scene snapshot, **both** endpoint colours are present (the prior at alpha
`< full` — fading out; the next partially faded in), and the displayed value for every differing channel is
strictly between the endpoints. The pre-R6 fade-in-from-transparent has only the next colour and fails the
prior-colour-present assertion.

**Acceptance Scenarios**:

1. **Given** a Switch whose track fill restyles `Muted→Accent` on hover, **When** it is hovered and the clock
   is sampled mid-flight, **Then** the composite contains **both** the prior (Normal) colour and the next
   (Hover) colour, the prior at alpha `< full` (fading out) and the next partially faded in.
2. **Given** the same mid-flight sample, **When** the displayed colour is computed (next over prior over a
   transparent canvas), **Then** every channel where the endpoints differ is **strictly between** them.
3. **Given** the pre-R6 build (plain fade-in, no prior layer), **When** the same transition is sampled,
   **Then** the prior colour is **absent** mid-flight (the counterfactual that fails the proof).

---

### User Story 2 - At-rest and settled output is byte-identical to the static render (Priority: P1)

An identity with **no active clock** is **byte-identical** to the full static render: it emits no animation
attribute and takes the unchanged zero-recompute fast path. A **settled** transition (the clock past its
duration, inactive) paints its own scene unchanged, so the final frame equals the snapped static render for
**every channel**. The cross-fade is a mid-flight-only overlay — it is invisible at rest and after settle.

**Why this priority**: Co-critical with US1. A cross-fade that left any residue at rest or after settle —
a lingering prior layer, a non-static final frame — would corrupt the steady state and churn the tree. The
settle and fast paths are exactly the 091/099 paths, untouched; proving byte-identity at both ends is what
makes the overlay safe to ship.

**Independent Test**: Step the live retained path on a frame with no active clock and assert the scene equals
`Control.renderTree` byte-for-byte with no animation attribute and zero recompute/remeasure. Start a clock,
advance it past the 150 ms duration in one large injected delta, and assert the settled frame equals the
snapped static Hover render byte-for-byte (every channel).

**Acceptance Scenarios**:

1. **Given** an at-rest frame (Normal everywhere, no clock started), **When** it is stepped, **Then** no
   animation clock exists, the scene equals the static render byte-for-byte, and recompute = remeasure = 0.
2. **Given** a transition advanced past its duration, **When** the settled frame is stepped, **Then** the
   clock is inactive and the frame is byte-identical to the snapped static Hover render for every channel.

---

### User Story 3 - The cross-fade is deterministic under injected deltas (Priority: P2)

Replaying an identical sequence of injected per-frame deltas reproduces an **identical** sampled-frame
sequence (no wall-clock, no randomness). A **non-positive** delta is a no-op (the frame never rewinds), and
a delta past the duration **settles canonically** with no overshoot in any channel.

**Why this priority**: Determinism is the constitution's hard live-path constraint (Principle VI) and the
precondition for the byte-identity proofs (US2) and the strictly-between proof (US1). P2 because it protects
the core cross-fade journeys rather than being a separate user-facing one, and it is proven by the same
suite.

**Independent Test**: Run a fixed 7-frame injected-delta sequence twice and assert the two sampled-frame
sequences are identical; run 60 FsCheck cases over random bounded sequences (0–40 ms, ≤ 8 frames, spanning
pre/mid/settled). Confirm a 0 ms re-step mid-flight leaves the frame unchanged (non-positive no-op).

**Acceptance Scenarios**:

1. **Given** a fixed injected-delta sequence, **When** it is replayed twice, **Then** the two sampled-frame
   sequences are identical (and identical across 60 FsCheck random sequences).
2. **Given** a mid-flight frame, **When** a 0 ms delta is applied, **Then** the sampled frame is unchanged
   (a non-positive delta never rewinds).

---

### User Story 4 - Cross-fade edge cases are handled (retarget, held-state, return-to-Normal, no-colour-delta) (Priority: P2)

A mid-flight state change **retargets** continuously: the clock re-seeds `From` from the **previous
target's** own-scene snapshot and resets `Elapsed`, so the new segment fades from where the eye is (no snap
to a stale endpoint). A **held, settled** state stays a `Keep` — a single scoped repaint, not a per-frame
churn, with no spurious clock re-fire. A settled **return to `Normal`** drops the clock, returning the
identity to byte-identical at-rest output. A transition with **no colour delta** introduces no permanent
artifact — no new colour mid-flight, and it settles and returns byte-identical.

**Why this priority**: These are the correctness guards that keep the cross-fade usable under real
interaction (flipping state mid-animation, holding a state, unhovering, animating a control with no visible
delta). P2 because each refines the core journeys (US1/US2) rather than adding a new capability, and all are
proven by the same suite.

**Independent Test**: (retarget) Hover then press mid-flight; assert the clock re-aims to `Pressed`,
`Elapsed` resets to 0, and `From` equals the previous (Hover) own-scene snapshot. (held-state) Settle Hover
then hold two more frames; assert recompute = remeasure = 0 and the clock stays settled. (return-to-Normal)
Settle Hover, unhover; assert a return cross-fade starts active, then once settled the clock is dropped and
the frame returns to byte-identical at-rest. (no-colour-delta) A label (no Normal/Hover colour delta)
introduces no new colour mid-flight and settles + returns byte-identical.

**Acceptance Scenarios**:

1. **Given** a Hover clock mid-flight, **When** the state flips to `Pressed`, **Then** the clock's `Target`
   becomes `Pressed`, `Elapsed` resets to 0, and `From` is re-seeded from the previous (Hover) own-scene
   snapshot (not a stale at-rest endpoint).
2. **Given** a settled, held Hover state, **When** two more frames are stepped, **Then** recompute =
   remeasure = 0, the clock stays settled (no spurious re-fire), and the frame stays byte-identical to the
   static Hover render.
3. **Given** a settled Hover clock, **When** the control is unhovered, **Then** a Hover→Normal return
   cross-fade starts active and, once settled, the clock is dropped and the identity returns to
   byte-identical at-rest output.
4. **Given** a control with no Normal/Hover colour delta, **When** it transitions, **Then** no new colour
   appears mid-flight and the frame settles and returns byte-identical (no permanent artifact).

---

### Edge Cases

- **No colour delta between states**: the cross-fade introduces no new colour mid-flight (the displayed set
  is a subset of the shared colours) and leaves no permanent artifact.
- **Retarget mid-flight**: `From` is re-seeded from the **previous target's** own-scene snapshot and
  `Elapsed` resets — the new segment fades from the current appearance, never snapping to a stale endpoint.
- **Held settled state**: stays a `Keep` (single scoped repaint, not per-frame); the kept clock does not
  spuriously re-fire.
- **Return to `Normal` once settled**: the clock is **dropped** (discarding `From`), restoring byte-identical
  at-rest output.
- **`From = []`**: degenerates to the plain fade-in (the 099-owned single channel) — 103's composite is a
  strict superset that reduces to 099 when there is nothing to fade from.
- **Non-positive / past-duration delta**: no-op (no rewind) / settles canonically with no overshoot.
- **Settled or absent clock**: not composited — paints `ownScene` verbatim, so the settle path is untouched.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A live visual-state transition whose paint differs in a colour MUST be a **genuine
  two-snapshot cross-fade** — the prior state's own-scene snapshot fading **OUT** (`1→0`) **under** the next
  state's own-scene fading **IN** — not a fade-in-from-transparent. For a region painted in both states the
  composite MUST display **both** endpoint colours mid-flight, with the displayed value for every differing
  channel **strictly between** the endpoints.
- **FR-002**: The cross-fade MUST be composited at **paint level only** (opacity, never layout) via the
  public feature-073 `Animation.applyAt`, riding the same `RetainedId`-keyed per-identity clock as 099.
- **FR-003**: `updateClockForState` MUST: **start** a transition capturing `From` = the prior own-scene
  snapshot, **retarget** a mid-flight change by re-seeding `From` from the previous target's snapshot and
  resetting `Elapsed` (no snap to a stale endpoint), **advance-only** when the state is unchanged (retaining
  the existing `From`), and **drop** a settled return-to-`Normal` clock.
- **FR-004**: An identity with **no active clock** MUST be **byte-identical** to the static render, emit no
  animation attribute, and take the unchanged fast path (zero recompute, zero remeasure) — the cross-fade is
  a mid-flight-only overlay.
- **FR-005**: A **settled** clock MUST paint `ownScene` unchanged so the final frame is **byte-identical** to
  the snapped static render for **every channel**; the settle path MUST be untouched.
- **FR-006**: The cross-fade MUST be **deterministic**: the sole time coordinate is the injected per-frame
  delta (no wall-clock, no randomness); an identical delta sequence reproduces an identical sampled-frame
  sequence; a **non-positive** delta is a no-op (no rewind); a **past-duration** delta settles canonically
  (no overshoot in any channel).
- **FR-007**: A **held, settled** state MUST stay a `Keep` — a single scoped repaint, not a per-frame churn —
  with no spurious clock re-fire.
- **FR-008**: A transition with **no colour delta** MUST introduce **no permanent artifact**: no new colour
  appears mid-flight (the displayed colour set is a subset of the shared set) and the frame settles and
  returns byte-identical.
- **FR-009**: The entire surface MUST remain **assembly-internal** — zero public-surface-baseline delta —
  and remain exercised only through the in-assembly tests via `InternalsVisibleTo`. (Verified directly by the
  surface-drift check; this requirement has no separate Success Criterion.)

### Key Entities *(include if feature involves data)*

- **AnimationClock.From**: the `FS.GG.UI.Scene.Scene list` field carrying the prior state's **static
  own-scene snapshot** captured at transition start — the layer that fades **out** under the next. `[]`
  degenerates to the 099 plain fade-in. 099 carried this field (as `[]`); 103 is what **populates** it with
  a real snapshot and composites through it.
- **updateClockForState**: the pure transition trigger (shared contract C2) — start / retarget / advance-only
  / drop. 103's responsibility is capturing/re-seeding `From` so the right prior snapshot cross-fades.
- **sampleOnPaint**: the paint composite — the active clock's `From` prior layer fading out under `ownScene`
  fading in, via `Animation.applyAt`. 103 makes this a genuine two-layer composite; 099 used the
  `From = []` degenerate single layer.
- **StateByIdentity / RetainedId**: the stable-identity-keyed per-identity state map (091/092) the clock and
  its `From` snapshot ride, so a mid-flight cross-fade survives a positional shift and is GC'd with its
  identity.
- **VisualState**: the state a clock animates toward (`Normal`/`Hover`/`Pressed`/…); a retarget re-aims
  `Target`; a settled return to `Normal` drops the clock.
- **WorkReduction (recompute/remeasure)**: the per-frame work metric (091/099) used to prove the at-rest and
  held-state fast paths are unchanged (counts = 0).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Mid-flight, the displayed colour of a region painted in both states is **strictly between** the
  endpoints (both prior and next present; prior alpha `< full`), in 100% of cases; the pre-R6 fade-in shows
  only the next colour and fails the proof. *(INV-3)*
- **SC-002**: An at-rest (no active clock) frame is **byte-identical** to the static render with no animation
  attribute and zero recompute/remeasure, in 100% of cases. *(INV-1)*
- **SC-003**: A settled transition is **byte-identical** to the snapped static render for **every channel**,
  in 100% of cases. *(INV-2)*
- **SC-004**: An identical injected-delta sequence reproduces an identical sampled-frame sequence (fixed
  7-frame replay + **60** FsCheck random sequences); a non-positive delta never rewinds. *(INV-4)*
- **SC-006**: The edge behaviours hold in 100% of cases — retarget re-seeds `From` from the previous
  target's snapshot and resets `Elapsed` *(INV-5)*; a held settled state stays a `Keep` with recompute =
  remeasure = 0 *(INV-6)*; a settled return-to-`Normal` clock is dropped; a no-colour-delta transition
  leaves no permanent artifact.

## Assumptions

- The keyed reconciler (feature 067), the retained render structure with stable `RetainedId`, the carried
  `StateByIdentity` map with its `RetainedUiState.Animation` slot (091), the live read/write wiring and host
  tick/step seam (092), and the **live animation clock** that advances and samples the slot (099) already
  exist in the imported source. 103 is the **backfilled contract** for *making the sampled transition a
  genuine two-snapshot cross-fade* (capture the prior own-scene as `From`, composite prior-out-under-next),
  not new-from-scratch construction.
- The surface stays **internal**; "users" of these stories are framework internals plus the in-assembly
  tests (per the constitution's vertical-slice rule), not external package consumers. No public API is added;
  the public feature-073 `Animation.applyAt` is the existing primitive `sampleOnPaint` composites through.
- Feature 103 owns the **two-snapshot cross-fade composite**. The **live single-channel clock** (advance +
  plain `From = []` fade-in) is feature **099 (R4)**, and the **no-alloc idle** reference-equal behaviour of
  `advanceStateClocks` is feature **121** — both share the same `RetainedRender.fsi` seam but are out of
  scope for 103 and proven by their own features.
- Render-output equivalence is judged by **structural scene equality** (the authoritative parity proof, the
  `DeterministicRenderOnly` renderer mode), and mid-flight colour by the descriptive scene's paint colours +
  alphas; `SceneEvidence.renderPng` is a capability-hash, not a pixel encoder, so pixel-level /
  desktop-visibility proofs are out of scope (the readiness evidence explicitly does not claim them).
- The existing readiness evidence under `readiness/` (`mid-flight-interpolation`, `at-rest-byte-identity`,
  `final-frame-identity`, `determinism`) corresponds to SC-001/SC-002/SC-003/SC-004 and is **self-written by
  the suite** on each run; the SC-006 edge behaviours are proven by the suite's edge tests.
- SC-005 is intentionally unused — the SC numbering (001/002/003/004/006) mirrors the SC labels already
  embedded in `Feature103CrossFadeTests` and its readiness files.
- This is the **C4** conformance backfill in the 2026-06-15 missing-features plan, following the 091 pattern
  and the 092 / 093 / 095 / 096 / 099 / 097 closes; `/speckit-plan`, `/speckit-tasks`, and
  `/speckit-implement` reduce to a conformance pass (confirm the suite is green, the readiness regenerates,
  and the surface delta is zero), not a build.
</content>
