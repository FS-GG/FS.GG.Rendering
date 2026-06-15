# Phase 1 — Data Model: Frame-Rate Pacing & No-Alloc Idle Tick (Feature 121)

The 121-in-scope entities. `advanceStateClocks` is **assembly-internal**; `shouldAdvanceFrame` is a public
`val` and `FrameRateCap` an additive field on the already-baselined public `ViewerOptions`. All pure/total.

## advanceStateClocks (internal)

`delta: System.TimeSpan -> state: Map<RetainedId, RetainedUiState> -> Map<RetainedId, RetainedUiState>`.
Returns the input `state` **reference-equal** when no clock is active (no allocation); when ≥1 clock is active,
builds a new map advancing each active clock exactly as `RetainedRender.advance` (099/103 unchanged).

**Invariants**:
- no active clock ⇒ `obj.ReferenceEquals(result, state)` (SC-003).
- active clock ⇒ result is **not** reference-equal (rebuilt); each clock's `Elapsed` equals `advance`'s output.

## GlHost.shouldAdvanceFrame (public val)

`lastFrameTime: float -> now: float -> frameInterval: float -> bool`. The pure pacing decision: `true` iff at
least `frameInterval` seconds have elapsed since the last advance. Gates **both** update and present, so a
tighter `FrameRateCap` ⇒ strictly fewer advances over the same window.

## ViewerOptions.FrameRateCap (public field, additive)

The consumer's render-cadence cap; defaults to 60 when unset; a positive value clears validation; a
**non-positive** value is rejected at option validation as a `ProductDefect` ("frame-rate cap must be positive").

## Relationships

```text
live host tick (now) ──GlHost.shouldAdvanceFrame(lastFrameTime, now, frameInterval = 1/FrameRateCap)──┐
                                                                                                       ▼
                          true  ─▶ DoUpdate + DoRender (advance)        false ─▶ skip (paced)
   ViewerOptions.FrameRateCap (default 60; non-positive ─▶ ProductDefect at validation, before GL init)

   per-frame: advanceStateClocks(delta, StateByIdentity)
       ├─ no active clock  ─▶ return SAME map (obj.ReferenceEquals; no allocation)   [SC-003]
       └─ active clock(s)  ─▶ rebuilt map; each clock advanced as RetainedRender.advance (099/103 unchanged)
```
</content>
