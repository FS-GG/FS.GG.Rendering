# Feature Specification: Frame-Rate Pacing & No-Alloc Idle Tick (Feature 121)

**Feature Branch**: `121-paced-noalloc-tick`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C10+** (the 121 close, grouped with C3 in the plan)
in the 2026-06-15 missing-features plan (Workstream C pattern: 091 / 092 / 093 / 095 / 096 / 099 / 097 / 103 /
110 / 113 / 114 / 116 / 117 / 120).

Feature 121 carries **two** related live-host efficiency guards:

- **US1 — frame-rate-cap pacing.** A pure decision `GlHost.shouldAdvanceFrame lastFrameTime now frameInterval`
  advances (update **and** present) only once `frameInterval` has elapsed, so a consumer `ViewerOptions.FrameRateCap`
  bounds render cadence; a tighter cap yields strictly fewer advances over the same window; a non-positive cap
  is rejected at option validation as a `ProductDefect`.
- **US2 — no-alloc idle tick.** `advanceStateClocks delta state` returns the per-identity state map
  **reference-equal** when no clock is active (an idle live tick allocates **nothing**); active clocks advance
  exactly as `advance` (features 099/103 unchanged).

The implementation (`advanceStateClocks` in `RetainedRender.fs`/`.fsi`; `GlHost.shouldAdvanceFrame` +
`ViewerOptions.FrameRateCap` + the validation in `SkiaViewer`) and the two suites
(`Feature121IdleTickTests` in `Controls.Tests` — the headless no-alloc core; `Feature121LiveHostPacingTests`
in `SkiaViewer.Tests` — the pure pacing decision + the validation seam) **already exist** in the imported
source. **No Spec Kit spec/plan/tasks describe this work**, and 121 imported with **no `readiness/`**. This
document backfills the contract.

`advanceStateClocks` is **assembly-internal**; `shouldAdvanceFrame` is a public `val` and `FrameRateCap` an
additive field on the already-baselined public `ViewerOptions` — both ride already-baselined types (the
SkiaViewer baseline is type-granular), so the backfill adds **zero new public-surface-baseline delta**.

**Scope boundary.** 121 owns the no-alloc idle gating of `advanceStateClocks` and the frame-rate pacing
decision + validation. The live clock **advance/sample** is feature 099; the **cross-fade** is feature 103 —
both unchanged by 121.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Frame-rate cap paces the live host (Priority: P1)

The live host advances (updates + presents) only once the frame interval has elapsed, so a consumer
`FrameRateCap` bounds render cadence. A tighter cap yields strictly fewer advances over the same window. A
non-positive cap is rejected at option validation (a `ProductDefect`); a positive cap clears validation.

**Why this priority**: Bounding render cadence to the consumer's cap is the core pacing guarantee — the MVP for
this story.

**Independent Test**: `shouldAdvanceFrame` is false before the interval, true at/after it (SC-001); over a 1 s
window polled at 1 ms, a 30 fps cap yields strictly fewer advances than 60 fps, each within tolerance
(FR-002/SC-001); `FrameRateCap = 0` / negative is rejected with `Classification = ProductDefect` and a
"frame-rate cap" message (FR-003/SC-005); a positive cap clears option validation (FR-001).

**Acceptance Scenarios**:

1. **Given** a frame interval, **When** `shouldAdvanceFrame` is polled, **Then** it is false before the
   interval and true at/after it (SC-001).
2. **Given** a 30 vs 60 fps cap over the same window, **When** advanced, **Then** the 30 fps cap yields
   strictly fewer advances, each within tolerance (FR-002/SC-001).
3. **Given** a non-positive `FrameRateCap`, **When** validated, **Then** it is rejected as a `ProductDefect`
   ("frame-rate cap"); a positive cap clears validation (FR-001/FR-003/SC-005).

---

### User Story 2 - An idle live tick allocates nothing (Priority: P1)

When no animation clock is active, `advanceStateClocks` returns the per-identity state map **reference-equal**
(no allocation) — an idle live tick makes no garbage. An active clock advances exactly as the per-clock oracle
(`advance`), and the resulting map is a genuinely rebuilt (not reference-equal) value.

**Why this priority**: Co-critical with US1. A live host that allocates on every idle tick churns GC for no
reason; the reference-equal idle path is the no-alloc guarantee.

**Independent Test**: with an empty / settled (inactive) state, `advanceStateClocks` returns a result that is
`obj.ReferenceEquals` to the input (no allocation) (SC-003); with an active clock, the result is **not**
reference-equal (rebuilt) and the clock's `Elapsed` advanced by the delta, equal to `RetainedRender.advance`
on the same clock (099/103 unchanged).

**Acceptance Scenarios**:

1. **Given** a no-active-clock state (empty or settled), **When** `advanceStateClocks delta` is applied,
   **Then** the result is `obj.ReferenceEquals` to the input (no allocation) (SC-003).
2. **Given** an active clock, **When** advanced, **Then** the result is **not** reference-equal (rebuilt) and
   `Elapsed` matches `RetainedRender.advance` on the same clock (099/103 unchanged).

---

### Edge Cases

- **Empty state map**: reference-equal (nothing to advance).
- **Settled (inactive) clock**: reference-equal (no active clock to advance).
- **Active clock**: rebuilt map (not reference-equal); advances exactly as `advance`.
- **Non-positive `FrameRateCap`**: rejected as a `ProductDefect` at validation.
- **Before the frame interval**: `shouldAdvanceFrame` is false (no advance, no present).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A consumer `ViewerOptions.FrameRateCap` MUST bound render cadence (defaulting to 60 when unset);
  a positive cap MUST clear option validation.
- **FR-002**: `shouldAdvanceFrame` MUST be a **pure** decision that gates **both** update and present — advance
  iff at least `frameInterval` has elapsed; a tighter cap MUST yield strictly fewer advances over the same
  window.
- **FR-003**: A non-positive `FrameRateCap` MUST be rejected at option validation as a `ProductDefect`
  ("frame-rate cap must be positive").
- **FR-004**: `advanceStateClocks` MUST return the state map **reference-equal** when no clock is active (an
  idle live tick allocates nothing); active clocks MUST advance exactly as `advance` (099/103 unchanged).
- **FR-005**: The backfill MUST add **zero new public-surface-baseline delta** (`advanceStateClocks` internal;
  `shouldAdvanceFrame` / `FrameRateCap` ride already-baselined public types).

### Key Entities *(include if feature involves data)*

- **advanceStateClocks**: `delta -> Map<RetainedId, RetainedUiState> -> Map<RetainedId, RetainedUiState>` —
  reference-equal when no clock is active; otherwise advances each active clock as `advance` (internal).
- **GlHost.shouldAdvanceFrame**: `lastFrameTime -> now -> frameInterval -> bool` — the pure pacing decision
  (gates update + present) (public val).
- **ViewerOptions.FrameRateCap**: the consumer cap (positive; default 60; non-positive rejected) (public field).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `shouldAdvanceFrame` advances only once the interval has elapsed, and a tighter cap yields
  strictly fewer advances over the same window, 100% of cases.
- **SC-003**: An idle (no-active-clock) `advanceStateClocks` returns a **reference-equal** map (no allocation),
  100% of cases.
- **SC-005**: A non-positive `FrameRateCap` is rejected at validation as a `ProductDefect`, 100% of cases.

## Assumptions

- The per-identity `StateByIdentity` map + the live clock `advance` (099) and cross-fade (103), and the
  SkiaViewer host loop + `ViewerOptions` already exist. 121 is the **backfilled contract** for the no-alloc
  idle gating + the frame-rate pacing decision/validation, not new-from-scratch construction.
- `advanceStateClocks` is **internal**; `shouldAdvanceFrame` is a public `val` and `FrameRateCap` an additive
  field on the already-baselined public `ViewerOptions` (the SkiaViewer baseline is type-granular) ⇒ **zero
  new** public-surface delta.
- 121 imported with executable suites (`Feature121IdleTickTests` in Controls.Tests — the headless no-alloc
  core via `obj.ReferenceEquals`; `Feature121LiveHostPacingTests` in SkiaViewer.Tests — deterministic-headless,
  **not** GL-gated, exercising the pure `shouldAdvanceFrame` + the validation seam) but **no `readiness/`**
  (tests do not self-write); authoring readiness is part of this backfill.
- The live clock advance/sample (099) and cross-fade (103) are **unchanged** by 121.
- This is the **121 close** conformance backfill (grouped with C3); `/speckit-*` reduce to a conformance pass.
</content>
