# Phase 1 — Data Model: Live Animation Clock (Feature 099)

The 099-in-scope entities. All are **assembly-internal** (declared in `RetainedRender.fsi` as
`type internal …` / `val internal …`); none changes the public surface baseline (FR-012). Equality is F#
**structural** equality throughout; the sole time coordinate is the **injected** `TimeSpan` delta.

## AnimationClock

The per-identity live clock — the heart of feature 099.

| Field | Type | Meaning |
|---|---|---|
| `Anim` | `FS.GG.UI.Scene.Animation` | The reused feature-073 animation. The **live channel is the OPACITY tween only** (the next layer's fade-in `0→1`). `Animation.applyAt` samples opacity/transform and never recolors. |
| `Elapsed` | `System.TimeSpan` | The accumulated **injected** delta — the **sole** time coordinate (no wall-clock). Advanced by `advance`, clamped to the animation's `Duration`. |
| `Target` | `VisualState` | The visual state this clock animates toward. Used to detect a **retarget** when the stamped state flips mid-flight. |
| `From` | `FS.GG.UI.Scene.Scene list` | The prior state's **static own-scene snapshot** captured at transition start (matches `RenderFragment.OwnScene` verbatim). `[]` ⇒ nothing to fade from ⇒ a plain fade-in (the 099-owned degenerate case; the two-snapshot cross-fade that uses a non-empty `From` is feature 103). |

**Lifecycle / state transitions** (driven by `updateClockForState`, per FR-008):

- **Start** — fresh state change from a settled/absent clock: new clock with `Elapsed = 0`,
  `Target = desired`, `From = priorOwn`.
- **Retarget** — mid-flight state change: re-aim from the **current sampled value** (no snap to start),
  `Target = desired`, `From = priorOwn`.
- **Advance-only** — state unchanged: keep the clock, retain its existing `From`; `advance` accumulates the
  delta.
- **Drop** — settled return to `VisualState.Normal`: slot becomes `None` (byte-identical at rest restored,
  FR-006).
- **At rest** — `None` on the slot ⇒ the identity emits no animation attribute and paints byte-identically to
  the static render.

**Invariants**:
- `0 ≤ Elapsed ≤ Anim.Duration` (clamped; never rewinds — FR-004).
- A non-positive injected delta is a no-op.
- `clockActive` ⇔ `Elapsed < Anim.Duration` (still in flight); a settled clock (`Elapsed = Duration`) is **not
  sampled**.

## RetainedUiState (091 carrier — the slot 099 writes)

The per-control state keyed by the stable `RetainedId` (not the path-derived `ControlId`), so it survives a
positional shift.

| Field | Type | Meaning |
|---|---|---|
| `Animation` | `AnimationClock option` | The per-control clock. **091 carried this slot; nothing wrote it. 099 is what advances and samples it on the live path.** `None` ⇒ at rest. |
| `Text` | `TextInputModel option` | Re-keyed text-input state (feature 091/092). Listed for completeness; unchanged by 099. |

Focus itself stays in the consumer model's `ControlRuntime.FocusedControl`; 091 only remaps the lookup to
`RetainedId`.

## StateByIdentity (the map the clock rides)

`Map<RetainedId, RetainedUiState>`, carried frame-to-frame in the host loop's mutable-ref retained state.

- **Survival** (SC-002): a sibling shift moves a control's position but not its `RetainedId`, so the carried
  clock keeps advancing under the same key.
- **Garbage collection** (SC-005, FR-010): a removed identity is dropped by the **existing** `liveIds` filter
  — its clock leaves with it. No new GC code; matches focus/text GC.
- **Multi-clock independence** (SC-004): each `RetainedId` advances its **own** clock; no cross-talk.

## defaultTransitionDuration

The single pinned framework default transition: **150 ms**, `EaseOut` (`val internal
defaultTransitionDuration: System.TimeSpan`). Used when a transition does not specify its own duration/easing.

## VisualState (existing — the transition axis)

The existing union (`Normal`/`Hover`/`Pressed`/`Focused`/`Selected`/`Loading`/`Validation`/…). 099 reads it as
the clock's `Target`; a return to `Normal` once settled drops the clock. 099 adds no case.

## WorkReduction (existing — the scoped-repaint oracle for US5)

The per-frame work metric established by 091/092. For 099's US5, a steady-state animating frame must report
**recompute count = 0** and **remeasure count = 0** (the static fragments take the `Keep` fast path) while the
frame still reports a change (the active clock was sampled). 099 adds no field; it asserts against the existing
metric.

## Relationships

```text
RetainedId ──key──▶ RetainedUiState ──Animation──▶ AnimationClock { Anim; Elapsed; Target; From }
   │                                                      │
   │                                                      ├─ advance(delta)         : accumulate, clamp, no-rewind
StateByIdentity (Map)                                     ├─ clockActive            : sampling gate
   │  ├─ survives positional shift (same RetainedId)      ├─ updateClockForState    : start / retarget / advance / drop
   │  └─ liveIds filter drops removed identities (GC)     └─ sampleOnPaint          : composite fade-in via Animation.applyAt
   │
   └─ host tick injects per-frame TimeSpan delta ──▶ advance every active clock ──▶ step ──▶ sample on paint
```
