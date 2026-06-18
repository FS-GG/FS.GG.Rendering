# Research: No-Clear Damage-Scissored Render Path

## Decision: Gate runtime damage-scoped repaint on Feature 155 accepted proof

**Rationale**: Feature 155 is the accepted current-host correctness baseline. It accepted three
fresh sentinel/damage attempts and same-profile parity for stable host profile `probe-08a47c01`.
Feature 157 changes runtime rendering behavior, so it must not use damage-scoped repaint when the
proof is missing, stale, rejected, synthetic-only, or from another host profile.

**Alternatives considered**:

- Accept any capable OpenGL host. Rejected because the existing correctness claim is profile-bound.
- Reuse Feature 156 timing as the safety gate. Rejected because Feature 156 is `noisy` timing
  evidence and does not replace the Feature 155 proof/parity gate.

## Decision: Implement in the GL `DirectToSwapchain` path

**Rationale**: `src/SkiaViewer/Host/OpenGl.fs` already owns FBO-0 rendering, frame clear, scene
paint, snapshot caching, and buffer swap. The existing `decideScissorRedraw` helper validates basic
proof and damage facts, but `renderFrameDirect` still clears and repaints full frames. The real
Feature 157 behavior belongs where clear, clip, repaint, snapshot, and swap are decided together.

**Alternatives considered**:

- Implement only in the harness. Rejected because the spec requires a real render path, not just
  readiness evidence.
- Implement in `SceneRenderer.paintNode`. Rejected because the painter should remain a shared,
  exhaustive scene walker; frame preservation, clear policy, and scissor decisions are host-level
  concerns.
- Implement first in `OffscreenReadback`. Rejected because Feature 157 targets live
  `DirectToSwapchain`; readback remains an evidence path.

## Decision: Require trusted retained backing before no-clear repaint

**Rationale**: The report explicitly warns that clipping to damage corrupts frames if a fresh or
cleared framebuffer starts each frame. The path is eligible only when the current buffer is known to
preserve the previous frame or the host restores a retained previous-frame backing before the
damage-clipped repaint. The retained backing identity must match host profile, framebuffer size,
run identity, and previous frame identity.

**Alternatives considered**:

- Trust swapchain preservation by default. Rejected because multi-buffer swapchains can rotate an
  undrawn or unrelated buffer.
- Always draw the previous frame image before damage repaint and count that as accepted without
  disclosure. Rejected because it changes the cost model and must be identified as retained-backing
  restoration, not implicit buffer preservation.
- Fall back whenever buffer preservation is uncertain. Accepted as the default fail-closed behavior.

## Decision: Validate damage as a frame-bound union before rendering

**Rationale**: Existing `normalizeScissorRects` clamps and deduplicates rectangles, but Feature 157
also needs reviewer-visible validation categories. Empty damage for a visible change, out-of-bounds
damage, stale damage, duplicate damage, incomplete old/new movement damage, resize damage, and
full-frame invalidation all require explicit fallback or classification.

**Alternatives considered**:

- Treat any non-empty rectangle as eligible. Rejected because incomplete movement or stale damage
  can preserve pixels that should change.
- Use only a single bounding box. Rejected as a first-class data model because the input may be a
  union; the host may still choose a union bounding rect for the native scissor after validation.

## Decision: Compare accepted damage-scoped output against full redraw

**Rationale**: Feature 157 must prove untouched pixels are preserved and damaged pixels are updated.
The most direct reviewer-facing proof is parity against the equivalent full-redraw frame for the
same scene, frame identity, damage definition, host profile, and run identity. Drift outside allowed
damage rejects the attempt and forces future fallback until fresh proof is available.

**Alternatives considered**:

- Trust present-path proof alone. Rejected because Feature 155 proves host preservation semantics,
  not every Feature 157 runtime scenario.
- Compare only hashes. Rejected for acceptance because reviewers need region-specific preserved and
  damaged-pixel evidence; hashes are useful diagnostics but not enough alone.

## Decision: Add `compositor-damage --feature 157` for correctness evidence

**Rationale**: Feature 156 owns `compositor-performance`; Feature 157 needs a command centered on
accepted/fallback damage attempts, retained backing, validation status, parity, and readiness. The
command follows existing feature routing patterns in `tests/Rendering.Harness/Cli.fs` and publishes
under `specs/157-no-clear-damage-scissor/readiness/`.

**Alternatives considered**:

- Extend `compositor-performance --feature 156`. Rejected because timing remains context-only and
  should not become the runtime correctness entry point.
- Use only `compositor-readiness`. Rejected because readiness should assemble and validate evidence,
  while damage attempt collection is a separate action.

## Decision: Keep the shipped performance claim `performance-not-accepted`

**Rationale**: The report requires later gates before a shipped compositor performance claim:
Feature 158 readback separation, Feature 159 layer promotion/content-key work, and Feature 161 host
performance lane ledger. Feature 157 can accept runtime correctness for the measured profile, but
it cannot by itself accept the shipped performance claim.

**Alternatives considered**:

- Accept performance when Feature 157 correctness passes. Rejected because the timing evidence is
  still `noisy` and later performance gates remain open.
- Block correctness on later performance gates. Rejected because this feature can safely accept or
  reject the runtime path independently while keeping performance unaccepted.
