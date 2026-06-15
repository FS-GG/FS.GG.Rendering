# Phase 0 — Research: Visual-State Cross-Fade (Feature 103)

This is a **conformance backfill**: the implementation already exists in the imported source, so "research"
here is the recovery and recording of the design decisions the code already embodies. There were **no open
`NEEDS CLARIFICATION`** items in the spec; each decision below is reconstructed from `RetainedRender.fsi`
(the `AnimationClock.From` field + `updateClockForState`/`sampleOnPaint` clauses), the host tick seam, and
the `Feature103CrossFadeTests` suite (which self-writes the readiness evidence).

## Decision 1 — Cross-fade is two opacity-driven layers, not a fade-in-from-transparent

- **Decision**: `sampleOnPaint` composites the active clock's **`From` prior snapshot** fading OUT (`1→0`)
  **under** `ownScene` (this frame's static own paint) fading IN (the clock's opacity tween), both via the
  public feature-073 `Animation.applyAt`. For a region painted in both states the displayed colour is a
  convex combination of the two endpoints — strictly between them.
- **Rationale**: 099's plain fade-in showed only the next colour appearing over emptiness; the prior colour
  was absent, which reads as a flash, not a transition. Two opacity layers (prior under next) is the minimal
  faithful cross-fade and reuses the existing opacity sampler — no new colour-tween primitive.
- **Alternatives considered**: A direct `Color` lerp per node — rejected: `Animation.applyAt` samples
  opacity/transform and never recolors via a `Color` tween, and a per-node colour lerp would require knowing
  every paint's semantic role; the two-snapshot composite works for any own-scene paint uniformly.

## Decision 2 — Capture `From` = the prior state's static own-scene snapshot

- **Decision**: `updateClockForState` captures `From = priorOwn` (the matched prior node's own-scene
  snapshot, verbatim `RenderFragment.OwnScene`) when starting a transition, so the clock carries exactly the
  layer it must fade from.
- **Rationale**: The cross-fade must fade from the *actual* prior appearance, not a reconstructed or themed
  guess. Snapshotting the prior own-scene at transition start captures the truth and keeps `sampleOnPaint`
  pure (it composites two given layers, no re-derivation).
- **Alternatives considered**: Re-deriving the prior paint at sample time — rejected: the prior state's
  inputs may already be gone (the tree now reflects the next state); snapshotting at start is the only
  reliable source.

## Decision 3 — Retarget re-seeds `From` from the previous target's snapshot and resets `Elapsed`

- **Decision**: A mid-flight state change (e.g. Hover→Pressed) re-seeds `From` from the **previous target's**
  own-scene snapshot and resets `Elapsed` to 0, re-aiming `Target` at the new state — it does **not** snap to
  a stale at-rest endpoint.
- **Rationale**: When the user flips state mid-animation, the eye is currently on the previous target's
  appearance; the new segment must fade from there for continuity (INV-5). Resetting `Elapsed` restarts the
  eased segment cleanly.
- **Alternatives considered**: Keeping the original `From` across a retarget — rejected: it would fade from a
  now-irrelevant earlier endpoint, producing a visible jump.

## Decision 4 — The composite is a mid-flight-only overlay; settle and fast paths are untouched

- **Decision**: `sampleOnPaint` is applied **only** to an active (mid-flight) clock. A settled clock
  (`clockActive = false`) or an absent slot paints `ownScene` verbatim; the assemble fast path returns the
  cached subtree scene unchanged.
- **Rationale**: Identity-at-rest (SC-002) and final-frame identity (SC-003) require the at-rest and settled
  frames to be byte-identical to the static render. Gating the composite to active clocks keeps the
  steady-state paths exactly the 091/099 paths — the cross-fade adds nothing at rest or after settle.
- **Alternatives considered**: Always compositing and relying on the sampler being identity at the endpoints
  — rejected: even an identity-opacity overlay emits an animation attribute and would diverge structurally
  from the static render, churning the work metric.

## Decision 5 — A settled return-to-`Normal` clock is dropped (discarding `From`)

- **Decision**: Once a Hover→Normal return cross-fade settles, `updateClockForState` **drops** the clock
  (slot → `None`), discarding the `From` snapshot, so the identity returns to byte-identical at-rest output.
- **Rationale**: A lingering settled `Normal` clock (with a captured `From`) would keep the identity
  structurally distinct from the static render and accumulate state. Dropping restores the exact at-rest
  golden (INV-1) and bounds the map — the same drop rule 099 established, here also releasing the snapshot.
- **Alternatives considered**: Keeping a settled clock and relying on `clockActive` alone — rejected: extra
  state the at-rest byte-equality and GC story would have to reason about; dropping is simpler.

## Decision 6 — A held, settled state stays a Keep (single scoped repaint, no re-fire)

- **Decision**: A state held after settle stays a `Keep` — a single scoped repaint at the transition, not a
  per-frame churn — and the kept clock (`Target = Hover ≠ Normal`) is **not** spuriously re-fired.
- **Rationale**: Without this, holding a hovered control would repaint every frame forever. The held clock
  stays settled (inactive); the frame is byte-identical to the static Hover render with recompute =
  remeasure = 0 (INV-6) — the efficiency guard on the cross-fade.
- **Alternatives considered**: Re-firing the clock on every frame the state is held — rejected: a permanent
  per-frame repaint with no visible change.

## Decision 7 — Determinism: injected delta only, non-positive no-op, past-duration settles canonically

- **Decision**: The cross-fade's sole time coordinate is the injected per-frame delta (advanced via the same
  `advance` 099 uses); replaying an identical sequence reproduces identical frames; a non-positive delta is a
  no-op (no rewind); a past-duration delta settles canonically (no overshoot in any channel).
- **Rationale**: Determinism is the constitution's hard live-path constraint (Principle VI) and the
  precondition for the byte-identity proofs (SC-002/SC-003) and the strictly-between proof (SC-001). It makes
  the composite a pure replayable function of the delta sequence.
- **Alternatives considered**: Reading a monotonic clock at sample time — rejected: non-deterministic,
  untestable byte-for-byte.

## Decision 8 — A no-colour-delta transition introduces no permanent artifact

- **Decision**: A control whose Normal and Hover paint share their colours (e.g. a plain label) introduces no
  **new** colour mid-flight (the displayed set is a subset of the shared set) and settles + returns
  byte-identical.
- **Rationale**: The cross-fade must not invent paint. When there is nothing to interpolate, the two layers
  are colour-identical, so the composite adds no new colour and leaves no residue (FR-008). This guards
  against the overlay introducing artifacts on controls with no visible state delta.
- **Alternatives considered**: Skipping the composite when no colour delta is detected — rejected: detecting
  "no colour delta" per node is more complex than letting the colour-identical composite be a no-op by
  construction.

## Decision 9 — Reuse 099's clock seam rather than a parallel cross-fade scheme

- **Decision**: 103 uses the same `RetainedId`-keyed `StateByIdentity` map, the same `AnimationClock`, and the
  same `updateClockForState`/`sampleOnPaint` functions as 099 — 103 is the cross-fade *realization* of the
  same seam (populating `From`, compositing two layers); 099 is the degenerate `From = []` plain fade-in.
- **Rationale**: Survival across positional shifts and GC of removed identities (091/092) come for free from
  the shared map; one seam keeps a single source of truth. The `From = []` degeneracy means 103's composite
  is a strict superset that reduces to 099 when there is nothing to fade from.
- **Alternatives considered**: A separate cross-fade store — rejected: it would duplicate the survival/GC
  machinery and risk divergence, exactly the anti-pattern 099 also avoided.

## Renderer-mode / evidence honesty

All readiness evidence is **self-written by the suite** in `DeterministicRenderOnly` mode and judged by
**structural scene equality** plus descriptive-scene colour/alpha inspection (mid-flight) and work-count
invariants (at-rest/held). `SceneEvidence.renderPng` is a capability-hash, not a pixel encoder, so the
evidence **does not** claim pixel-level or desktop-visibility proof — consistent with the 091/092/099/097
backfills and disclosed in each readiness file.
</content>
