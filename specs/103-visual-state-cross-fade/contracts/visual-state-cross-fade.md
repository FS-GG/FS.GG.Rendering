# Contract — Visual-State Cross-Fade seam (Feature 103)

The **internal** seam the suite pins. This is not a public package API (FR-009: zero public-surface delta);
per the constitution's vertical-slice rule the in-assembly tests (`InternalsVisibleTo`) are the user-reachable
surface. Signatures are reproduced from `src/Controls/RetainedRender.fsi`; behaviour clauses are what
`Feature103CrossFadeTests` asserts. The seam is **shared with feature 099** — 099 owns the live single-channel
clock (plain `From = []` fade-in); 103 owns the two-snapshot cross-fade (`From` populated, two-layer
composite).

## C1 — `AnimationClock.From` (the prior-snapshot layer — 103's field)

```fsharp
// field on the internal AnimationClock record:
From: FS.GG.UI.Scene.Scene list
```

- The prior state's **static own-scene snapshot** captured at transition start (verbatim
  `RenderFragment.OwnScene`) — the layer that fades **OUT** under the next.
- `[]` ⇒ nothing to fade from ⇒ the 099 plain fade-in (degenerate case).

*Pins*: FR-001, FR-003. *Used by*: US1 (the cross-from layer), US4 (retarget re-seed).

## C2 — `updateClockForState` (the pure transition trigger — shared)

```fsharp
val internal updateClockForState:
    desired: VisualState -> priorOwn: FS.GG.UI.Scene.Scene list -> carried: AnimationClock option -> AnimationClock option
```

- **Start** a fresh transition: `Elapsed = 0`, `Target = desired`, `From = priorOwn`.
- **Retarget** a mid-flight change: re-seed `From` from the **previous target's** own-scene snapshot, reset
  `Elapsed`, re-aim `Target` (no snap to a stale endpoint).
- **Advance-only** when the state is unchanged: keep the clock, retain its existing `From`.
- **Drop** a settled return-to-`Normal` clock: result `None`, discarding `From`.
- **Total + pure**: no wall-clock, no randomness.

*Pins*: FR-003. *Used by*: US1 (start captures `From`), US4 (retarget INV-5, return-to-Normal drop).

## C3 — `sampleOnPaint` (the two-layer cross-fade composite — shared seam)

```fsharp
val internal sampleOnPaint:
    clock: AnimationClock -> ownScene: FS.GG.UI.Scene.Scene list -> FS.GG.UI.Scene.Scene list
```

- For an **active** clock: composite the `From` prior snapshot fading **OUT** (`1→0`) **under** `ownScene`
  fading **IN** (the clock's opacity tween), via the public `Animation.applyAt` — **paint-level only**
  (opacity, never layout).
- A region painted in both states displays a colour **strictly between** the endpoints mid-flight (both
  present; prior alpha `< full`).
- A **settled or absent** clock paints `ownScene` **unchanged** (the settle path is untouched; the final
  frame stays byte-identical, FR-005).
- `From = []` degenerates to the plain fade-in (099).

*Pins*: FR-001, FR-002, FR-005. *Used by*: US1 (strictly-between), US2 (settled = `ownScene` verbatim).

## C4 — `advance` (the clock advance — shared with 099)

```fsharp
val internal advance: delta: System.TimeSpan -> clock: AnimationClock -> AnimationClock
```

- Accumulates the **injected** `delta` into `Elapsed`, clamped to `Anim.Duration`; a **non-positive** delta
  is a no-op (no rewind); a past-duration delta settles canonically (no overshoot). Deterministic.

*Pins*: FR-006. *Used by*: US3 (determinism, non-positive no-op, past-duration settle).

## C5 — `clockActive` (the composite gate — shared with 099)

```fsharp
val internal clockActive: clock: AnimationClock -> bool
```

- `true` while `Elapsed < Anim.Duration`. The gate that makes the cross-fade a **mid-flight-only overlay**:
  only an active clock is composited; a settled clock paints `ownScene` verbatim.

*Pins*: FR-004, FR-005, FR-007. *Used by*: US2 (at-rest/settled), US4 (held-state, return-to-Normal).

## Surface-drift

- **Zero public-surface-baseline delta** (FR-009): the whole seam is `internal` (absent from
  `FS.GG.UI.Controls.txt` / `FS.GG.UI.Controls.Elmish.txt`). The surface-drift check must pass byte-unchanged.
</content>
