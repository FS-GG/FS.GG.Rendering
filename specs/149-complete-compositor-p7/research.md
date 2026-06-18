# Research: Complete P7 Compositor

## Decision: Treat Feature 149 as the final P7 completion slice

**Rationale**: Features 147 and 148 already established deterministic proof contracts,
readiness scaffolding, exact corpus names, FSI transcript tests, harness commands, and focused
evidence tests. The open work is native/live renderer completion: live framebuffer
sentinel/readback, no-clear damage-scoped renderer integration, snapshot composition, real timing
probes, Evidence formatters, and final public surface expansion. Reusing the existing shape keeps
the work bounded and prevents drift into P8 intrinsic layout.

**Alternatives considered**:

- Re-plan P7 from scratch: rejected because it would duplicate accepted Feature 147/148 artifacts.
- Fold P8 intrinsic layout into this feature: rejected by FR-002 and because compositor readiness
  must be accepted before the next roadmap item.
- Leave Feature 148 as the final P7 plan: rejected because the roadmap explicitly records open
  native/live work and public surface completion after the Feature 148 merge.

## Decision: Require real host proof before accepting partial redraw

**Rationale**: Damage-scoped redraw is only safe when the active presentation host preserves
untouched pixels between presents or the implementation owns an equivalent retained backing path.
The current live proof is environment-limited. Feature 149 must produce real artifacts from the
SkiaViewer/OpenGL path: a full sentinel frame, a damage-only second frame, readback/sample records,
and a verdict tied to the active host profile and proof algorithm version.

**Alternatives considered**:

- Static allow-list by backend or OS: rejected because preservation can vary by compositor, driver,
  renderer, present mode, scale, and framebuffer setup.
- Simulated proof only: retained for deterministic failure-path tests, rejected for readiness
  acceptance because it bypasses the presenter that will ship the behavior.
- Accept environment-limited proof with a warning: rejected because this would enable partial
  redraw from assumption.

## Decision: Integrate damage-scoped rendering at the SkiaViewer host boundary

**Rationale**: `Controls` owns retained damage, movement, and source-boundary decisions. The live
host owns GL scissor state, framebuffer clearing, readback, and present behavior. The renderer must
consume a retained damage plan, apply no-clear scissor coverage only after accepted proof, and
reset host state before full redraw, readback, or any non-scoped frame. Full redraw remains both
the oracle and the safe fallback.

**Alternatives considered**:

- Recompute damage by diffing final `Scene` values in SkiaViewer: rejected because SkiaViewer does
  not own retained identity, content/placement history, or promotion decisions.
- Clip every dirty node independently without a union/merge policy: rejected because it increases
  host state churn and makes overlap accounting less reviewable.
- Always repaint the full frame while reporting damage metrics: rejected because it would not
  complete the visible P7 behavior.

## Decision: Keep reuse and snapshots behind parity, resource, and benefit gates

**Rationale**: Placement reuse, replay reuse, and snapshot-assisted composition are optimizations.
They can ship only when they match the full-frame oracle and record why content was reused,
refreshed, demoted, or rejected. Snapshot resources are host-owned artifacts that must have bounded
entry/byte budgets, host-profile validity, content freshness, deterministic eviction/disposal, and
fallback before stale output.

**Alternatives considered**:

- Promote every stable boundary: rejected because simple or churning scenes can regress.
- Expose new public Scene snapshot nodes now: rejected because the current need is a host
  optimization, not a portable authoring contract.
- Treat `SKPicture` replay as equivalent to snapshots: rejected because snapshots require resource
  lifecycle, byte budget, composition, and invalidation evidence.

## Decision: Implement real timing in `Rendering.Harness/Perf.fs`

**Rationale**: Counters and deterministic readiness summaries prove intent, not performance.
Feature 149 must run comparable repeated measurements for full redraw, damage-scoped redraw, and
snapshot-assisted redraw, separating warmup and measured frames. Damage compares against full
redraw; placement/replay compare against lower tiers and full redraw where relevant; snapshots
compare against lower reuse tiers and full redraw. Incomplete, noisy, or environment-limited
timing becomes inconclusive or limited, never ready.

**Alternatives considered**:

- Claim performance from work-reduction counters alone: rejected because the claim is about the
  real host path.
- Use one best-case timing scenario: rejected because non-beneficial/simple/churning corpora must
  protect against over-promotion.
- Compare every tier only to full redraw: rejected because a higher tier can regress relative to a
  lower tier while still beating full redraw.

## Decision: Publish final consumer-visible diagnostics

**Rationale**: P7 changes observable behavior and expands package-facing diagnostics. Consumers
need stable records for proof status, fallback status, corpus/parity status, reuse status,
snapshot status, timing status, readiness verdict, and remaining limitations. Maintainers also
need package validation to catch undocumented public-surface drift.

**Alternatives considered**:

- Keep diagnostics in private readiness Markdown only: rejected because FR-016 requires a public
  diagnostic surface.
- Report one aggregate pass/fail: rejected because accepted, limited, rejected, skipped, and
  failed tiers require different consumer and maintainer responses.
- Delay package surface changes until after native work: rejected because the Tier 1 contract must
  be drafted and tested before implementation bodies are accepted.

## Decision: Preserve existing validation discipline

**Rationale**: The repository constitution requires `.fsi` first, semantic tests, real evidence
where possible, synthetic disclosure where not, surface baselines, documentation, package checks,
and pack validation. Feature 149 is explicitly a contracted change, so it must refresh baselines
only for documented compositor deltas and leave P5/P6/P7 compatibility guarantees intact.

**Alternatives considered**:

- Mark skipped native tests as passed in unsupported environments: rejected because it hides the
  difference between an environment limitation and a defect.
- Weaken previous parity assertions to green the compositor: rejected because P7 must preserve
  existing render-anywhere, overlay, text-shaping, disabled-cache, full-redraw, and package
  readiness guarantees.
- Add broad new dependencies for orchestration: rejected because existing SkiaViewer, Controls,
  Testing, and Rendering.Harness boundaries are sufficient.
