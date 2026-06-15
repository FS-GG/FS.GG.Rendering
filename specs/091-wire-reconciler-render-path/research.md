# Phase 0 Research: Wire the Keyed Reconciler onto the Render Path (Feature 091)

Because 091 is a **backfill**, there were no open `NEEDS CLARIFICATION` markers to resolve by
investigation — the design decisions are already embodied in the imported `Reconcile` /
`RetainedRender` implementation and pinned by `Feature091RetainedRenderTests`. This document
**recovers and ratifies** those decisions in the standard Decision / Rationale / Alternatives form,
so the contract is explicit rather than implicit in the code.

---

## D1 — Stable identity is diff-conferred, not path-derived

- **Decision**: A matched node's stable identity (`RetainedId of uint64`) is conferred by the diff and
  carried across frames, **separate** from the path-derived `ControlId`. Ids are minted from a
  monotonic per-host counter (`RetainedRender.NextId`).
- **Rationale**: The path-derived `ControlId` is unstable across a positional shift — the exact reason
  focus/text/animation state resets today. A diff-conferred id that follows the *match* (not the
  *position*) is what lets per-control state survive an unrelated re-render (FR-001/FR-003).
- **Alternatives considered**: (a) Keep using `ControlId` and accept the reset — rejected, that is the
  defect. (b) Hash node content for identity — rejected, content changes legitimately (an edited
  control would lose identity). (c) Require consumers to supply explicit keys everywhere — rejected as
  a usability tax; keys are used for *matching* but identity is the framework's responsibility.

## D2 — Deterministic minting (counter, not clock/GUID)

- **Decision**: Ids are minted from a per-host integer counter; no wall-clock, no randomness, no GUID.
- **Rationale**: SC-005 demands identical frame sequences mint identical ids so determinism goldens
  reproduce. A counter is the simplest source that satisfies this and keeps `step` pure.
- **Alternatives considered**: GUIDs (non-deterministic, break the determinism property); content
  hashes (collide and change with content). Rejected.

## D3 — Children match by `Key` first, then unkeyed residuals positionally

- **Decision**: `Reconcile.diff` matches children by `Key`, then matches the remaining unkeyed
  children positionally; a `Kind` mismatch on an otherwise-matched pair is a whole-subtree `Replace`.
- **Rationale**: Key-first matching is what makes a positional shift a `ChildMove`/`ChildKeep` rather
  than a `Replace`, preserving identity (US1/US2). The `Kind`-mismatch → `Replace` rule prevents
  **false identity reuse** (FR-002): a control that became a different kind must get a fresh id.
- **Alternatives considered**: Pure positional diff (loses identity on shift — the defect); LCS/edit
  distance over the whole list (more work, no better identity guarantee for the keyed case). Rejected
  for 091's scope.

## D4 — Reuse is keyed on structural equality + an unchanged, unshifted box

- **Decision**: A node at rest reuses its cached `RenderFragment` (own scene, subtree scene, box).
  The subtree scene is reused **verbatim only when** the subtree is unchanged **and** unshifted (its
  evaluated box did not move). Equality is F# **structural** `=`, never object identity.
- **Rationale**: Verbatim subtree reuse is the work reduction (SC-003). Gating on *unshifted* box is
  the correctness guard — a subtree that is structurally identical but relaid out must repaint at its
  new position, or parity (SC-004/FR-006) breaks. Structural equality (FR-005) guarantees identical
  inputs are always treated as unchanged regardless of allocation.
- **Alternatives considered**: Reference-equality reuse (fragile — equal-but-reallocated trees would
  re-paint, breaking the no-op property); reuse ignoring box shift (faster but wrong — breaks parity).
  Rejected.

## D5 — Per-control state is re-keyed to `RetainedId` (carried in 091, written later)

- **Decision**: `RetainedUiState` (animation clock, text model) lives in `RetainedRender.StateByIdentity`
  keyed by `RetainedId`. Focus itself stays in the consumer model's `ControlRuntime.FocusedControl`;
  091 only **remaps the lookup** to `RetainedId`. In 091 the state is **carried** across the diff;
  *writing/advancing* the live clock is feature 099's job.
- **Rationale**: Re-keying to the stable id is the minimal change that makes focus/animation survive a
  shift (FR-003) without taking on the larger job of driving animation, which belongs to a later
  feature. Scoping 091 to "carry, don't write" keeps the slice independently shippable.
- **Alternatives considered**: Make 091 also advance clocks live — rejected as scope creep; the
  survival proof (US2) only needs the carried value to persist and remain advanceable.

## D6 — `step` is a pure transition; the host is the interpreter edge (MVU)

- **Decision**: `RetainedRender.step : Theme → Size → RetainedRender → next → RetainedRenderStep` is a
  pure function returning the next structure + render result + diagnostics + work record. The retained
  structure lives in the host loop's existing mutable-ref state; interpretation (painting, ticking)
  happens at the edge.
- **Rationale**: Satisfies Principle IV — `update` is pure, state is a value, I/O is outside `step`.
  Purity is also what makes the determinism and totality properties testable at ≥1000 cases.
- **Alternatives considered**: Mutating the retained tree in place inside `step` — rejected; it would
  defeat the determinism/totality property tests and the MVU boundary.

## D7 — Malformed input is a diagnostic, never an exception

- **Decision**: Duplicate sibling keys surface a `KeyCollision` `ControlDiagnostic` of severity
  `Warning` through the existing diagnostic channel; `step`/`diff` remain **total** (never throw).
- **Rationale**: Principle VI forbids silent failure and demands safe degradation. A warning keeps the
  frame rendering while making the malformed input observable (FR-009/SC-006). Totality (FR-008) means
  no input pair can crash the render loop.
- **Alternatives considered**: Throw on duplicate keys (violates totality, kills the frame); silently
  pick one (violates observability). Rejected.

## D8 — Parity is proven by structural scene equality, not pixels

- **Decision**: "Byte-identical to a full rebuild" is asserted as structural equality of the
  `ControlRenderResult` (`Scene` + `Bounds` + `NodeCount`) against `Control.renderTree next` — not a
  decoded-PNG pixel diff. The readiness evidence explicitly discloses it does **not** prove scene
  rendering or desktop visibility.
- **Rationale**: The authoritative, deterministic, headless proof of correctness is that the wired
  scene *value* equals the full-rebuild scene *value*. A real pixel PNG would require the windowed
  render-target path, which is a different feature's concern and not needed to prove reuse-correctness.
  Honest disclosure (no overclaim) is a constitution value (Principle V).
- **Alternatives considered**: Pixel-readback parity in CI — rejected for 091: needs a live render
  target, is non-deterministic across drivers, and proves nothing the structural equality doesn't for
  the reuse contract.

---

**Outcome**: No unresolved unknowns. All eight decisions are ratified and already covered by
`Feature091RetainedRenderTests` and the captured `readiness/` artifacts. Phase 1 records the entities
and the internal contract these decisions imply.
