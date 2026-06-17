# Research: Compositor Damage Redraw

## Decision: Treat present-path proof as a real host-profile capability gate

**Rationale**: Damage-scissored redraw is only correct if untouched pixels remain valid between
presents. The proof must run on the active host profile and record backend, renderer/vendor/version
facts, present mode, framebuffer size/scale, display environment, proof algorithm version, and
timestamp. Damage scissoring is enabled only when the proof passed for the same host profile and the
proof is fresh enough for the current run.

**Alternatives considered**:

- Static backend allow-list: rejected because driver, present mode, scale, and window-system changes
  can alter preservation behavior.
- Synthetic/offscreen-only proof: rejected because it does not prove the live present path.
- Assume preservation from prior Feature 122 idle-present tests: rejected because those tests prove
  buffer fill/idle behavior, not partial redraw preservation of untouched regions.

## Decision: Use a controlled sentinel/damage probe for proof

**Rationale**: A controlled proof can draw a full-frame sentinel pattern, present it, then redraw
only a known damaged region with GL scissor and no full clear. A readback or equivalent host
observation verifies that untouched sentinel regions remain intact while the damaged region changes.
If the host cannot provide an honest observation, the result is `environment-limited`, not passed.

**Alternatives considered**:

- Infer from final app screenshots: rejected because a normal scene cannot isolate whether
  untouched pixels survived or were freshly repainted.
- Compare only frame metadata: rejected because the correctness question is pixel preservation.
- Run proof through the deterministic pure `Perf.runScript` path only: rejected because it bypasses
  the real presenter.

## Decision: Damage-scissored redraw reuses retained damage union data

**Rationale**: `RetainedRender` already records repaint decisions, damage rectangle counts, and
union area through `WorkReductionRecord` and the public `FrameMetrics` surface. The compositor
should use that existing damage source, correcting or extending it only where proof/parity requires,
instead of creating a second damage calculator.

**Alternatives considered**:

- Recompute damage from final Scene diffs in SkiaViewer: rejected because SkiaViewer lacks the
  control identity, layout invalidation, and promotion decision context.
- Paint every changed node independently without unioning: rejected because overlapping damage would
  overcount work and complicate scissor parity.
- Use dirty-node count as the scissor contract: rejected because the host needs concrete frame
  rectangles and a full-frame invalidation signal.

## Decision: Full-redraw oracle remains the acceptance baseline for every tier

**Rationale**: Partial redraw, promotion, replay, placement-only reuse, and snapshots are all
performance optimizations. The visible frame is accepted only when it matches the full-redraw oracle
for the same input sequence. The oracle must remain available even when a tier is disabled,
unsupported, or failing.

**Alternatives considered**:

- Compare against the previous lower compositor tier only: rejected because lower-tier bugs could
  mask higher-tier corruption.
- Accept performance counters without pixel parity: rejected because counters cannot prove stale
  content was not presented.
- Replace the oracle with golden images only: rejected because golden images are useful evidence but
  do not cover all dynamic frame transitions.

## Decision: Keep compositor policy in Controls and host realization in SkiaViewer

**Rationale**: Promotion, demotion, content identity, placement identity, and damage provenance are
control/retained-render decisions. GL scissor, framebuffer preservation, present timing, and
snapshot resources are SkiaViewer host decisions. Keeping this split preserves existing package
boundaries and avoids leaking GL details into dependency-light Controls logic.

**Alternatives considered**:

- Put all compositor logic in SkiaViewer: rejected because it would duplicate retained-render
  identity and damage knowledge.
- Put GL scissor and snapshots in Controls: rejected because Controls must remain independent of
  Skia/OpenGL host resources.
- Add a new dependency package for the first slice: rejected because existing module boundaries are
  sufficient and simpler.

## Decision: Separate content identity from placement identity

**Rationale**: Moving stable content should not force fresh content recording when the render-
affecting content fingerprint is unchanged. Placement changes still affect where damage is applied
and how reused content is composited. The model therefore tracks content fingerprint, placement
rectangle/transform, and render-affecting dependency version separately.

**Alternatives considered**:

- Use one combined key for content and placement: rejected because placement-only movement would
  miss reuse opportunities.
- Ignore placement for promoted content: rejected because stale placement or uncovered old
  locations would corrupt the frame.
- Reuse content across different theme/text/provider inputs: rejected because those are
  render-affecting inputs and must force fresh output.

## Decision: Promotion requires observed stability and measured benefit

**Rationale**: Existing retained fragments, picture-cache keys, replay counters, and work-reduction
metrics give the evidence needed to promote only boundaries that are stable, large enough, and
beneficial. Promotion decisions record observation window, candidate size, content fingerprint,
expected saved work, parity status, overhead, and demotion reason when rejected.

**Alternatives considered**:

- Promote every cacheable boundary: rejected because simple or churning scenes can lose more to
  bookkeeping than they save.
- Promote only by node count: rejected because node count alone misses paint cost, movement,
  resource budget, and parity failures.
- Keep promotion decisions unreported: rejected because readiness reviewers need to distinguish
  safe reuse from over-promotion.

## Decision: Implement snapshot reuse as a bounded SkiaViewer resource tier

**Rationale**: The lower tiers already have retained fragments and `SKPicture` replay. The higher-
cost snapshot tier should live at the SkiaViewer edge as an offscreen visual artifact with an
explicit byte/entry budget, host capability result, lifecycle diagnostics, and demotion behavior.
It is eligible only for expensive stable content with measured net benefit.

**Alternatives considered**:

- Treat existing replay hits as the snapshot tier: rejected because the feature asks for a higher-
  cost visual reuse tier with resource budget evidence.
- Add snapshot nodes to the public Scene contract now: rejected because this is a host optimization,
  not a portable authoring/protocol concept for the first slice.
- Leave snapshots unbounded: rejected because memory growth would be a correctness and readiness
  defect.

## Decision: Performance probes compare each tier against the right baseline

**Rationale**: Damage scissoring compares against full redraw. Promotion/placement reuse compares
against scissored or full redraw as appropriate. Snapshot reuse compares against the lower reuse tier
and full redraw. Each report records benefit on target corpora and overhead on simple/churning
corpora; a tier that loses value is demoted or marked not ready.

**Alternatives considered**:

- Compare all tiers only against full redraw: rejected because a higher tier can be slower than the
  lower tier while still beating full redraw.
- Claim benefit from a single best-case scene: rejected because the specification requires stable,
  moving, expensive, simple, and churning scenarios.
- Accept environment-limited timing as performance proof: rejected because it cannot validate the
  host path where the claim would ship.

## Decision: Readiness is a single reviewable package

**Rationale**: P7 mixes correctness risk, host capability, public diagnostics, performance claims,
and compatibility impact. A single readiness package lets reviewers trace each tier to proof,
parity, performance, fallback, limitations, and release notes within the 10-minute review target.

**Alternatives considered**:

- Scatter evidence across test output only: rejected because reviewers need durable, named
  artifacts and limitations.
- Report only final pass/fail: rejected because skipped tiers, environment limits, demotions, and
  compatibility impact are material.
- Accept stale evidence from another host profile: rejected because host profile is part of the
  proof identity.
