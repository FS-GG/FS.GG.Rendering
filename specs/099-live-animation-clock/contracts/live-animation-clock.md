# Contract — Live Animation Clock seam (Feature 099)

The **internal** seam the suites pin. This is not a public package API (FR-012: zero public-surface delta);
per the constitution's vertical-slice rule the in-assembly tests (`InternalsVisibleTo`) are the user-reachable
surface. Signatures are reproduced from `src/Controls/RetainedRender.fsi`; behavior clauses are what the
`Feature099AnimationClockTests` / `Feature099AnimationSeamTests` suites assert.

## C1 — `defaultTransitionDuration` (the pinned framework default)

```fsharp
val internal defaultTransitionDuration: System.TimeSpan
```

- MUST equal **150 ms** and pair with **`EaseOut`** easing.
- Used when a transition does not specify its own duration/easing.

*Pins*: FR-002. *Used by*: US1 (the live-seam default-transition trajectory).

## C2 — `advance` (the pure clock core)

```fsharp
val internal advance: delta: System.TimeSpan -> clock: AnimationClock -> AnimationClock
```

- **Total + pure**: no wall-clock, no randomness; the sole time input is `delta`.
- Accumulates `delta` into `Elapsed`, **clamped** to `Anim.Duration` (a very large delta settles at the end,
  **no overshoot**).
- A **non-positive** `delta` is a **no-op** (the clock never rewinds).
- **Deterministic**: an identical `delta` sequence applied to an identical starting clock produces a
  byte-identical result.

*Pins*: FR-003, FR-004, FR-007. *Used by*: US3 (determinism + edges).

## C3 — `clockActive` (the sampling gate)

```fsharp
val internal clockActive: clock: AnimationClock -> bool
```

- `true` while the clock is still in flight (`Elapsed < Anim.Duration`).
- A **settled** clock (`Elapsed = Duration`) returns `false` and MUST NOT be sampled — the identity paints
  byte-identical to the static render. Only active clocks contribute a per-frame change.

*Pins*: FR-006, FR-011. *Used by*: US3 (identity-at-rest), US5 (scoped repaint).

## C4 — `updateClockForState` (the transition trigger)

```fsharp
val internal updateClockForState:
    desired: VisualState
    -> priorOwn: FS.GG.UI.Scene.Scene list
    -> carried: AnimationClock option
    -> AnimationClock option
```

Given the `desired` visual state, the prior state's own-scene snapshot `priorOwn`, and the carried
(already-advanced) clock, decide the frame's clock:

- **START** a fade-in for a fresh state change (from a settled/absent clock): new clock, `From = priorOwn`.
- **RETARGET** from the **current sampled value** for a mid-flight change (no snap to start): `From = priorOwn`.
- **ADVANCE-ONLY** when the state is unchanged: keep the clock, retain its existing `From`.
- **DROP** a settled return-to-`Normal` clock ⇒ `None` (byte-identical at rest restored).

*Pins*: FR-008, FR-006 (drop), FR-005 (retarget continuity). *Used by*: US1, US2, US3.

## C5 — `sampleOnPaint` (composite the active clock onto the scene)

```fsharp
val internal sampleOnPaint:
    clock: AnimationClock
    -> ownScene: FS.GG.UI.Scene.Scene list
    -> FS.GG.UI.Scene.Scene list
```

- Composites an **active** clock onto `ownScene` via the public feature-073 `Animation.applyAt`: `ownScene`
  (this frame's cached static own paint) fades **in** on the opacity channel.
- `From = []` (the 099-owned case) degenerates to the plain fade-in. (The two-snapshot cross-fade where a
  non-empty `From` fades **out** under `ownScene` is feature **103**.)
- Used **only** for active clocks — a settled/absent clock paints `ownScene` unchanged (the settle path is
  untouched, so the final frame is byte-identical to the static render).

*Pins*: FR-001 (animate-not-snap), FR-006 (settle = unchanged). *Used by*: US1, US3.

## C6 — Survival, GC, and scoped repaint (seam-level, via `advance` + `step`)

These are properties of the host tick + `RetainedRender.step` seam over the existing
`RetainedId`-keyed `StateByIdentity` carry (no new function):

- **Survival** (FR-005): an in-flight clock rides the stable `RetainedId`; an unrelated re-render that shifts
  the control's position keeps the identity, so the carried clock continues advancing — its shifted trajectory
  is byte-identical to its unshifted trajectory. No hand-seeded clock; no parallel identity scheme.
- **GC** (FR-010): a removed identity's clock is dropped by the existing `liveIds` filter — present while live,
  absent after removal — matching focus/text GC.
- **Scoped repaint** (FR-009): a structurally-unchanged animating frame takes the `Keep` fast path
  (`WorkReduction` steady-state recompute = 0, remeasure = 0) while the active clock is still sampled, so one
  active animation never invalidates the at-rest fast path for the rest of the tree.

*Pins*: FR-005, FR-009, FR-010. *Used by*: US2, US4, US5.

## Contract invariants (apply to all of the above)

1. **Zero public-surface delta** — every symbol is `internal`; `tests/surface-baselines/FS.GG.UI.Controls.txt`
   is byte-unchanged (FR-012).
2. **Determinism / totality** — `advance`, `updateClockForState`, `sampleOnPaint` never throw and consult no
   wall-clock; identical injected-delta sequences ⇒ byte-identical output (FR-003, FR-011).
3. **Invisible at rest** — an absent or settled clock emits no animation attribute; the wired scene equals the
   full `Control.renderTree` static rebuild byte-for-byte (FR-006, judged by structural scene equality).
4. **Scope** — the live opacity channel only; cross-fade (103) and no-alloc idle (`advanceStateClocks`, 121)
   are out of scope and owned by their own features.
