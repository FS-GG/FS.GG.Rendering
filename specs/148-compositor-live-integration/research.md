# Research: Compositor Live Integration

## Decision: Implement the preservation proof through the real SkiaViewer/OpenGL presenter

**Rationale**: Partial redraw is only correct when the active host preserves untouched framebuffer
regions between presents or when the implementation owns an equivalent retained backing surface.
Feature 147's proof contracts and deterministic readiness formatter are already in place, but the
current evidence is environment-limited because the live sentinel/readback interpreter is missing.
This feature must run the sentinel/damage sequence through the same host profile that would later
render damage-scoped frames.

**Alternatives considered**:

- Static allow-list by backend or OS: rejected because driver, compositor, framebuffer scale,
  present mode, and window-system details can change preservation behavior.
- Deterministic simulated proof only: retained for tests, rejected for readiness because it bypasses
  the presenter that will ship the behavior.
- Treat Feature 147 proof artifacts as sufficient: rejected because those artifacts explicitly
  record that live proof was environment-limited.

## Decision: Keep proof readiness keyed to host profile, proof version, freshness, and artifacts

**Rationale**: The live proof is valid only for the active backend, renderer/vendor/version facts,
present mode, framebuffer size/scale, display environment, and proof algorithm. A proof registry
or evidence loader can reuse prior artifacts only when those facts match and the freshness policy
accepts them. Any mismatch fails closed to full redraw and records a fallback reason.

**Alternatives considered**:

- Cache one global "partial redraw supported" flag: rejected because it hides profile drift.
- Accept stale proof with a warning: rejected because readiness gates must fail closed.
- Require a fresh proof before every frame: rejected because it adds unnecessary cost once a
  matching proof has been accepted for the current run.

## Decision: Integrate damage-scoped redraw at the host rendering boundary with explicit fallback

**Rationale**: `Controls` already computes retained damage and union area, while `SkiaViewer`
owns GL scissor, canvas clear behavior, readback, and present state. The implementation should use
the existing retained damage source and pass a host redraw plan to the rendering edge. The host
must avoid clearing untouched regions during damage-scoped frames, apply scissor rectangles that
cover the damage union, and reset scissor state before full redraw, readback, or the next frame.

**Alternatives considered**:

- Recompute damage by diffing final `Scene` values in `SkiaViewer`: rejected because the viewer
  lacks retained identity, movement, content, and promotion decision context.
- Clip every dirty node independently without a union/merge step: rejected because overlaps would
  overcount work and increase host state churn.
- Always repaint the full frame and report reduced work counters: rejected because it would not
  deliver the visible P7 payoff.

## Decision: Treat full-frame redraw as the oracle and fallback path for every tier

**Rationale**: Damage scissoring, placement reuse, replay promotion, and snapshots are
optimizations. The output is accepted only when it matches the full-frame oracle for the same input
sequence. The same full-redraw path is also the safe fallback for missing proof, failed proof,
host mismatch, full-frame invalidation, unsupported snapshot resources, parity failure, or a
disabled compositor mode.

**Alternatives considered**:

- Compare a higher tier only to the immediately lower compositor tier: rejected because lower-tier
  corruption could mask stale pixels.
- Accept timing wins without parity evidence: rejected because performance counters cannot prove
  the frame is visually correct.
- Use golden images alone: rejected because the feature needs transition-by-transition oracle
  comparison across dynamic scenarios.

## Decision: Separate content identity from placement identity in retained compositor policy

**Rationale**: Moving stable content should reuse the visual content while updating where it is
composited. Content identity must include render-affecting inputs such as scene fingerprint, theme,
text provider, resources, and host-affecting flags. Placement identity must include rectangle,
transform, clip, scale, and layer/portal placement. Placement-only movement damages both old and
new covered regions so stale pixels are not left behind.

**Alternatives considered**:

- One combined content+placement key: rejected because movement-only updates would miss the reuse
  opportunity the feature targets.
- Ignore placement when content is stable: rejected because old exposed regions would remain stale.
- Reuse across theme/provider/resource changes: rejected because those are render-affecting inputs.

## Decision: Use conservative promotion and deterministic demotion

**Rationale**: Promotion only helps when a boundary is stable, expensive enough, parity-clean, and
measurably beneficial. The existing retained renderer, picture-cache counters, and Feature 147
policy helpers provide the starting point. Promotion decisions must record observation window,
expected saved work, measured overhead, parity result, target tier, and demotion/rejection reason.
Churning, simple, parity-failing, or non-beneficial boundaries demote or remain unpromoted.

**Alternatives considered**:

- Promote every cache boundary: rejected because over-promotion is pure cost on simple/churning
  scenes.
- Promote by area or node count alone: rejected because paint cost, movement, resource pressure,
  and parity outcomes matter.
- Hide demotion as an internal optimization: rejected because readiness reviewers need to
  distinguish safe reuse from rejected tiers.

## Decision: Keep snapshots as bounded SkiaViewer resources

**Rationale**: Snapshot reuse is a host-owned visual artifact tier above retained replay. It should
be represented as a SkiaViewer resource lease/pool keyed by content identity, with explicit byte
and entry budgets, deterministic refresh/evict/dispose behavior, unsupported-host fallback, and
readiness evidence. This avoids turning snapshots into public Scene authoring concepts before
there is a proven need.

**Alternatives considered**:

- Add public Scene snapshot nodes now: rejected because this slice needs a host optimization, not a
  portable authoring contract change.
- Treat existing `SKPicture` replay hits as snapshots: rejected because the feature requires
  bounded lifecycle, composition, resource pressure, and visual artifact evidence.
- Leave snapshot memory unbounded: rejected because resource growth would be a correctness and
  readiness defect.

## Decision: Expand the corpus around transitions, not only final states

**Rationale**: The dangerous compositor failures occur between frames: stale proof, old exposed
movement regions, scissor state leakage, full-frame invalidation after scoped frames, resource
invalidations, and demotion after churn. The corpus must cover transitions and record stable
scenario ids so repeated same-seed runs produce comparable evidence.

**Alternatives considered**:

- Reuse only Feature 147 deterministic summaries: rejected because they do not exercise live
  presenter preservation or actual damage-scoped frame integration.
- Test only localized updates: rejected because movement, resize, theme, resource, snapshot, and
  non-beneficial cases are part of the success criteria.
- Depend on manual visual inspection: rejected because readiness requires durable artifact links.

## Decision: Timing probes compare each tier to the right baseline on real host runs

**Rationale**: Damage scissoring compares against full redraw. Placement/promotion reuse compares
against the lower redraw tier and full redraw where relevant. Snapshot reuse compares against the
lower reuse tier and full redraw. Probes must separate warmup, measurement, environment facts,
thresholds, and corpus categories. Environment-limited timing is useful disclosure but cannot mark
a tier ready.

**Alternatives considered**:

- Compare every tier only to full redraw: rejected because a higher tier can regress relative to a
  lower tier while still beating full redraw.
- Claim wins from a single best-case scenario: rejected because the spec requires beneficial and
  non-beneficial corpora.
- Use deterministic model counters as timing proof: rejected because counters do not measure the
  real host path where the performance claim ships.

## Decision: Publish one readiness package with tier verdicts and compatibility impact

**Rationale**: Release reviewers need to trace proof, parity, fallback, reuse, snapshot lifecycle,
timing, diagnostics, public surface changes, and limitations without reconstructing raw logs.
Feature 147 already established the readiness-package shape; this feature extends it with live
proof and timing evidence and keeps ready, limited, rejected, and skipped tier verdicts explicit.

**Alternatives considered**:

- Scatter proof/timing outputs under raw test logs only: rejected because the 10-minute review
  target requires durable summaries and artifact links.
- Report a single aggregate pass/fail: rejected because limited, rejected, demoted, and skipped
  tiers are materially different.
- Count environment-limited evidence as "ready with caveat": rejected because that would overclaim
  the compositor benefit.
