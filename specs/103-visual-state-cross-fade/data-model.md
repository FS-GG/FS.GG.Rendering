# Phase 1 — Data Model: Visual-State Cross-Fade (Feature 103)

The 103-in-scope entities. All are **assembly-internal** (declared in `RetainedRender.fsi` as `type internal`
/ `val internal`); none changes the public surface baseline (FR-009). Equality is F# **structural** equality
throughout; the sole time coordinate is the **injected** `TimeSpan` delta. 103 shares these entities with
feature 099 — 099 owns the live single-channel clock, 103 owns the two-snapshot cross-fade realized through
them.

## AnimationClock (shared with 099 — 103 owns `From`)

The per-identity live clock. 099 owns advancing/sampling it; **103 owns its `From` snapshot** — the layer the
cross-fade fades from.

| Field | Type | Meaning |
|---|---|---|
| `Anim` | `FS.GG.UI.Scene.Animation` | The reused feature-073 animation; the live channel is the **opacity tween** (`0→1` for the next layer; the prior layer uses the exact complement). |
| `Elapsed` | `System.TimeSpan` | The accumulated **injected** delta — the sole time coordinate (no wall-clock). A retarget resets it to 0. |
| `Target` | `VisualState` | The state this clock animates toward; a retarget re-aims it. |
| `From` | `FS.GG.UI.Scene.Scene list` | **103's field.** The prior state's **static own-scene snapshot** captured at transition start (verbatim `RenderFragment.OwnScene`) — the layer that fades **OUT** under the next. `[]` ⇒ nothing to fade from ⇒ the 099 plain fade-in. |

**Invariants**:
- A mid-flight composite displays a colour **strictly between** the endpoints for every region painted in
  both states (SC-001/INV-3).
- `clockActive` ⇔ `Elapsed < Anim.Duration`; a settled clock is **not** composited (paints `ownScene`
  verbatim).
- `From = []` reduces `sampleOnPaint` to the 099 plain fade-in (a strict-superset degeneracy).

## updateClockForState (the pure transition trigger — shared contract C2)

`desired: VisualState -> priorOwn: Scene list -> carried: AnimationClock option -> AnimationClock option`.
103's responsibility is the `From` capture/re-seed:

- **Start** (fresh change from settled/absent): new clock, `Elapsed = 0`, `Target = desired`,
  `From = priorOwn`.
- **Retarget** (mid-flight change): re-seed `From` from the **previous target's** own-scene snapshot,
  `Elapsed = 0`, `Target = desired` (INV-5 — no snap to a stale endpoint).
- **Advance-only** (state unchanged): keep the clock, retain its existing `From`.
- **Drop** (settled return to `Normal`): slot → `None`, discarding `From` (INV-1 — byte-identical at rest
  restored).

## sampleOnPaint (the paint composite — shared seam, 103's two-layer realization)

`clock: AnimationClock -> ownScene: Scene list -> Scene list`. For an **active** clock: the `From` prior
snapshot fading OUT (`1→0`) **under** `ownScene` fading IN (the opacity tween), via `Animation.applyAt` —
paint-level only (opacity, never layout). A settled/absent clock paints `ownScene` unchanged (the settle path
is untouched, FR-005). `From = []` ⇒ plain fade-in (099).

## StateByIdentity / RetainedId (the carrier — 091/092)

`Map<RetainedId, RetainedUiState>`, carried frame-to-frame. The clock and its `From` snapshot ride the stable
`RetainedId`, so a mid-flight cross-fade survives a positional shift and a removed identity's clock (with its
snapshot) is GC'd by the existing `liveIds` filter.

## VisualState (existing — the transition axis)

The existing union (`Normal`/`Hover`/`Pressed`/`Focused`/…). 103 reads it as the clock's `Target`; a retarget
re-aims it; a settled return to `Normal` drops the clock. 103 adds no case.

## WorkReduction (existing — the unchanged-fast-path oracle)

The per-frame work metric (091/099). For 103's US2/US4, an at-rest or held-settled frame must report
**recompute = 0** and **remeasure = 0** (the cross-fade is a mid-flight-only overlay; the steady-state paths
are untouched). 103 adds no field; it asserts against the existing metric.

## Relationships

```text
ControlRuntime.applyRuntimeVisualState (stamp desired VisualState)
        │
        ▼
host tick: advance every active clock by the injected delta ──▶ RetainedRender.step
        │                                                              │
        ▼                                                              ▼
updateClockForState(desired, priorOwn, carried)            sampleOnPaint(clock, ownScene)
   start  : From = priorOwn                                    active  : From (1→0) UNDER ownScene (0→1)  ⇒ colour strictly between
   retarget: From = prev target snapshot, Elapsed 0           settled : ownScene verbatim (untouched)   ⇒ byte-identical to static
   advance-only: keep From                                     absent  : ownScene verbatim
   drop   : None (discard From)                                From=[] : plain fade-in (099)
```
</content>
