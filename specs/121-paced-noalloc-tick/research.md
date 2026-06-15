# Phase 0 — Research: Frame-Rate Pacing & No-Alloc Idle Tick (Feature 121)

Conformance backfill — recovers the design the imported code embodies. No open `NEEDS CLARIFICATION`.
Reconstructed from `RetainedRender.fsi`/`.fs` (`advanceStateClocks`), `OpenGl.fsi` (`shouldAdvanceFrame`),
`SkiaViewer.fs` (validation), and the two suites.

## Decision 1 — Reference-equal idle tick (no allocation)

- **Decision**: `advanceStateClocks delta state` returns the **same** `state` map (reference-equal) when no
  clock is active; only when ≥1 clock is active does it build a new map (advancing each active clock as
  `advance`).
- **Rationale**: A live host ticks every frame; if an idle tick rebuilt the map it would allocate garbage
  every frame for no behavioural change. Returning the input reference-equal makes the idle path
  allocation-free (FR-004/SC-003), proven via `obj.ReferenceEquals`.
- **Alternatives considered**: Always rebuilding (functionally correct) — rejected: needless per-frame GC
  pressure on the hot live loop.

## Decision 2 — A pure frame-pacing decision that gates update AND present

- **Decision**: `shouldAdvanceFrame lastFrameTime now frameInterval` is a pure boolean: advance iff at least
  `frameInterval` has elapsed. It gates **both** `DoUpdate` and `DoRender`, so a tighter `FrameRateCap` yields
  strictly fewer advances over the same window.
- **Rationale**: Extracting the cadence decision as a pure function makes it deterministically testable without
  a live window (the host interprets it at the edge). Gating both update and present is what makes the cap a
  real cadence bound, not just a present-rate throttle (FR-002/SC-001).
- **Alternatives considered**: Throttling only present — rejected: update would still run at full rate
  (wasted work); reading the clock inside the host loop — rejected (untestable).

## Decision 3 — A non-positive FrameRateCap is a ProductDefect, caught at validation

- **Decision**: `ViewerOptions.FrameRateCap` defaults to 60; a non-positive value is rejected at option
  validation (before any GL init) as a `ProductDefect` ("frame-rate cap must be positive"); a positive cap
  clears validation.
- **Rationale**: A zero/negative cap is a programming error that would divide-by-zero or stall the loop;
  failing fast at validation with a classified defect (Principle VI) surfaces it clearly without touching GL
  (so the test is headless) (FR-001/FR-003/SC-005).
- **Alternatives considered**: Clamping silently to a default — rejected: hides a real consumer bug.

## Decision 4 — 099/103 are unchanged; 121 only gates the rebuild

- **Decision**: An active clock advances exactly as `RetainedRender.advance` (the same value the per-clock
  oracle produces); 121 changes only whether the *map* is rebuilt, not how a clock advances or samples (099),
  nor the cross-fade (103).
- **Rationale**: The no-alloc optimization must be behaviour-neutral for active clocks; asserting the advanced
  `Elapsed` equals `advance`'s output keeps 099/103 provably unchanged.
- **Alternatives considered**: Folding clock-advance changes into 121 — rejected: out of scope; would conflate
  features.

## Renderer-mode / evidence honesty

Both proofs are deterministic and headless: the no-alloc core via `obj.ReferenceEquals` (Controls.Tests), and
the pacing decision + validation seam via the pure `shouldAdvanceFrame` and pre-GL validation
(SkiaViewer.Tests, **not** GL-gated — the persistent window is not driven). Readiness (authored in
`/speckit-implement`, since 121 imported without it) discloses these scopes.
</content>
