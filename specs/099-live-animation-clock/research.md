# Phase 0 — Research: Live Animation Clock (Feature 099)

This is a **conformance backfill**: the implementation already exists in the imported source, so "research"
here is the recovery and recording of the design decisions the code already embodies. There were **no open
`NEEDS CLARIFICATION`** items in the spec; each decision below is reconstructed from `RetainedRender.fsi`,
the host tick seam, and the captured `readiness/` evidence.

## Decision 1 — Generalize the carried 091 clock slot rather than add a parallel scheme

- **Decision**: Make the per-identity `RetainedUiState.Animation` slot (carried but never written since 091)
  the live clock, advanced from the host tick and sampled on paint. No new identity map, no separate clock
  store.
- **Rationale**: 091 already keys the slot by the stable `RetainedId` and 092 already carries/garbage-collects
  it through the `liveIds` filter. Riding that map gives survival across positional shifts (SC-002) and GC of
  removed identities (SC-005) **for free**, with no second source of truth that could drift from focus/text.
- **Alternatives considered**: A standalone `Map<RetainedId, AnimationClock>` parallel to `StateByIdentity` —
  rejected: it would duplicate the survival/GC machinery, risk divergence, and violate the spec's "no parallel
  identity scheme" constraint (FR-005, FR-010).

## Decision 2 — Injected per-frame delta is the sole time coordinate (no wall-clock)

- **Decision**: `advance: delta -> clock -> clock` accumulates an **injected** `TimeSpan` delta into the
  clock's `Elapsed`; the host tick supplies the delta. Nothing in the clock path reads `Date.now`/system time.
- **Rationale**: Determinism is the constitution's hard live-path constraint (Principle VI) and the precondition
  for byte-identical trajectories (SC-002) and the 1000-case FsCheck determinism proof (SC-004). An injected
  delta makes the clock a pure value transition that tests can replay exactly.
- **Alternatives considered**: Reading a monotonic clock inside `advance` — rejected: non-deterministic,
  untestable byte-for-byte, and would make a tick fail on a missing time source.

## Decision 3 — Advance is total: clamp at duration, no-op on non-positive delta

- **Decision**: `advance` **clamps** the accumulated elapsed to the animation's `Duration` (a very large delta
  settles at the end with no overshoot) and treats a **non-positive** delta as a no-op (never rewinds).
- **Rationale**: Totality (Principle VI) — any delta a host could inject yields a valid, monotone-forward
  clock. Clamping makes "settled at End" a stable fixed point, which is what `clockActive`/drop logic keys off.
- **Alternatives considered**: Allowing overshoot and sampling beyond `Duration` — rejected: the easing sampler
  is only defined on `[0, Duration]`, and overshoot would produce a frame that never byte-matches the snap
  target (breaking SC-001's exact convergence).

## Decision 4 — `clockActive` gates sampling; a settled/absent clock is invisible

- **Decision**: Only a clock that is still in flight (`clockActive = true`) is sampled. A settled clock, and an
  absent (`None`) slot, are not sampled — the identity paints byte-identical to the static render.
- **Rationale**: Identity-at-rest (SC-003) requires that an at-rest identity emits **no** animation attribute,
  so the wired scene equals the full `Control.renderTree` rebuild byte-for-byte. Gating on `clockActive` is
  what makes "only active clocks contribute a per-frame change" true.
- **Alternatives considered**: Always sampling and relying on the sampler being identity at `Elapsed = Duration`
  — rejected: even an identity transform/opacity emits an animation attribute, which would diverge structurally
  from the static render and churn the work metric.

## Decision 5 — Drop a settled return-to-`Normal` clock so at-rest is byte-identical again

- **Decision**: `updateClockForState` **drops** the clock (sets the slot to `None`) once a transition back to
  `VisualState.Normal` has settled.
- **Rationale**: Without dropping it, a settled `Normal` clock would linger in `StateByIdentity` and (a) keep
  the identity structurally distinct from the static render and (b) accumulate state. Dropping restores the
  exact pre-R4 golden at rest (SC-003) and bounds the map.
- **Alternatives considered**: Keeping a settled clock and relying on `clockActive` alone — rejected: even an
  unsampled settled clock is extra state that the identity-at-rest byte-equality and the GC story would have to
  reason about; dropping is simpler and is what the readiness `us3-identity-at-rest` evidence asserts.

## Decision 6 — `updateClockForState` retargets mid-flight from the current sampled value

- **Decision**: The transition trigger distinguishes four cases: **start** a fade-in for a fresh state change
  (from a settled/absent clock), **retarget from the current sampled value** for a mid-flight change (no snap
  to start), **advance-only** when the state is unchanged, and **drop** a settled return-to-`Normal`. On a
  fresh transition or a mid-flight retarget the new clock's `From` is the prior own-scene snapshot; an
  advance-only/kept clock retains its existing `From`.
- **Rationale**: Retargeting from the current value avoids a visible "snap to start" jolt when the user flips
  state mid-animation (the `edge-retarget-mid-flight` readiness case). Keeping `From` accurate is what lets
  `sampleOnPaint` cross-fade from the right prior snapshot.
- **Alternatives considered**: Always restarting the clock on any state change — rejected: produces the
  snap-to-start jolt the readiness evidence explicitly rules out.

## Decision 7 — 099 owns the live OPACITY channel only; cross-fade is 103, no-alloc idle is 121

- **Decision**: 099 samples the active clock's **fade-in** (`0→1`) via the public feature-073
  `Animation.applyAt` opacity tween, using the degenerate `From = []` plain fade-in. The two-snapshot
  **cross-fade composite** (the `From` prior snapshot fading out under the new scene) is feature **103 (R6)**;
  the **no-alloc idle** reference-equal behavior of `advanceStateClocks` is feature **121**.
- **Rationale**: `Animation.applyAt` samples opacity/transform and never recolors via a `Color` tween, so a
  standalone color cross-fade was never an option; the two-snapshot composite is the correct mechanism and it
  is a distinct, separately-evidenced feature. Scoping 099 to the single channel keeps this backfill honest
  and matches how 092 scoped out its later-feature accretions.
- **Alternatives considered**: Folding 103's cross-fade and 121's no-alloc proof into 099 — rejected: they have
  their own readiness and owning features; bundling them would overstate 099's scope.

## Decision 8 — Single pinned framework default transition (150 ms, `EaseOut`)

- **Decision**: `defaultTransitionDuration` is exactly **150 ms** with `EaseOut`, used when a transition does
  not specify its own duration/easing.
- **Rationale**: One pinned default keeps transitions consistent and makes the live-seam test deterministic
  (16 ms injected deltas over 150 ms ⇒ ~10 intermediate frames before the snap target, the
  `us1-animates-vs-snaps` evidence). A single constant is the simplest contract.
- **Alternatives considered**: A configurable/global animation speed setting — rejected: out of scope, adds
  surface, and would make the default-transition test non-deterministic without an injected override.

## Decision 9 — Test split: pure clock core vs live seam (mirror 092)

- **Decision**: Pure-core determinism, edges, and identity-at-rest live in `Feature099AnimationClockTests`
  (`Controls.Tests`); animate-not-snap, survival, GC, and scoped-repaint live in `Feature099AnimationSeamTests`
  (`Elmish.Tests`), driven through the real `ControlRuntime.applyRuntimeVisualState` + `advance` (Tick) +
  `step` seam.
- **Rationale**: The headline guarantees (US1/US2) are properties of the **host seam**, not of the clock core
  in isolation — exactly the reason 092's live-survival proof lives in `Elmish.Tests`. Pure-core properties
  belong with the core. This split keeps each suite's "user-reachable surface" honest.
- **Alternatives considered**: One combined suite — rejected: it would conflate the pure-core contract with the
  seam contract and lose the clean "no hand-seeded clock" framing of the seam tests.

## Renderer-mode / evidence honesty

All readiness evidence is captured in `DeterministicRenderOnly` mode and judged by **structural scene
equality** (plus bounds and node count) and clock-trajectory byte-equality. `SceneEvidence.renderPng` is a
capability-hash, not a pixel encoder, so the evidence **does not** claim pixel-level or desktop-visibility
proof — consistent with the 091/092 backfills and disclosed in each readiness file.
