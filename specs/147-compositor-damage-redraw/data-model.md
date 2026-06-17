# Data Model: Compositor Damage Redraw

## Host Profile

**Purpose**: Stable identity of the environment where present preservation, scissoring, and
snapshot support are evaluated.

**Fields**:

- `ProfileId`: deterministic identity over backend and environment facts.
- `Backend`: OpenGL host/backend identifier.
- `Renderer`: renderer/vendor/version facts when available.
- `PresentMode`: direct swapchain, offscreen/readback, or other active mode.
- `FramebufferSize`: pixel width/height used by the proof.
- `Scale`: effective scale factor when available.
- `DisplayEnvironment`: X11, Wayland, headless, missing display, or unknown.
- `ProofAlgorithmVersion`: version of the sentinel/damage proof sequence.

**Validation rules**:

- Proof evidence is valid only for the matching host profile.
- Missing backend or observation facts can produce `environment-limited` but not `passed`.
- A size, scale, backend, present-mode, or proof-version change invalidates cached proof readiness.

## Present Path Proof

**Purpose**: Capability result that determines whether damage-scissored redraw may preserve
untouched pixels between presents.

**Fields**:

- `HostProfile`: profile under proof.
- `ScenarioId`: stable proof scenario identifier.
- `Verdict`: passed, failed, or environment-limited.
- `ObservedUntouchedRegions`: checked regions outside the damage rectangle.
- `ObservedDamagedRegion`: checked region inside the damage rectangle.
- `FailureCause`: stale pixels, cleared pixels, unsupported observation, missing display, timeout,
  or host error.
- `EvidenceArtifacts`: readback/image/metadata artifact identities.
- `CreatedAt`: timestamp for freshness review.
- `Diagnostics`: actionable proof messages.

**Validation rules**:

- Passed proof requires all untouched regions to match the original sentinel and the damaged region
  to match the second-frame draw.
- Failed or environment-limited proof disables scissored redraw and snapshot readiness.
- Stale, missing, synthetic, host-mismatched, or proof-version-mismatched evidence cannot enable
  scissoring.

## Damage Region Set

**Purpose**: Concrete regions that must be repainted for a frame update.

**Fields**:

- `FrameId`: deterministic frame sequence id.
- `FrameSize`: pixel width/height.
- `Regions`: distinct axis-aligned rectangles after clipping to the frame.
- `UnionArea`: integer area of the union of all regions.
- `FullFrameInvalidation`: true for resize, theme switch, resource/provider change, or unsafe
  preservation.
- `Cause`: localized update, movement, resize, theme, resource, unsupported, or fallback.
- `SourceNodes`: retained identities or control ids that produced the damage.

**Validation rules**:

- Regions must be clipped to the frame and deduplicated before union area is recorded.
- Overlapping regions count once in `UnionArea`.
- `FullFrameInvalidation = true` requires repainting the affected full frame area.
- Empty damage is valid only for idle/no-op frames and cannot hide required repaint work.

## Full-Redraw Oracle

**Purpose**: Reference frame produced without compositor scissoring, promotion reuse, or snapshot
reuse.

**Fields**:

- `ScenarioId`: corpus scenario id.
- `FrameId`: deterministic frame id.
- `FrameIdentity`: hash/checksum of the rendered output or reference package identity.
- `ImageArtifact`: optional image path/identity for visual comparisons.
- `Metrics`: baseline frame metrics and work counts.
- `Diagnostics`: oracle render diagnostics.

**Validation rules**:

- Every accepted compositor frame must compare against the matching oracle frame.
- Oracle evidence cannot be replaced by a lower compositor tier.
- Missing oracle evidence marks the tier not ready.

## Scissored Redraw Frame

**Purpose**: Frame produced by repainting only the damage union after a passed present-path proof.

**Fields**:

- `HostProfile`: active host profile.
- `ProofId`: accepted present proof identity.
- `Damage`: damage region set.
- `ScissorRects`: final scissor rectangles applied by the host.
- `FallbackReason`: none, missing proof, failed proof, full-frame invalidation, unsupported host,
  parity failure, or unsafe damage.
- `OutputIdentity`: rendered frame checksum/artifact identity.
- `ParityVerdict`: passed, failed, skipped, or environment-limited.
- `Diagnostics`: damage/scissor/fallback messages.

**Validation rules**:

- If proof is absent, stale, failed, or host-mismatched, `FallbackReason` must not be none.
- Scissor rectangles must cover the damage union and not rely on unclipped out-of-frame regions.
- Passed parity requires `OutputIdentity` to match the full-redraw oracle.
- A failed parity frame marks damage-scissored redraw not ready.

## Compositor Boundary

**Purpose**: Reusable retained visual region evaluated for promotion, placement movement, replay,
and snapshot reuse.

**Fields**:

- `BoundaryId`: stable retained identity.
- `ContentIdentity`: render-affecting fingerprint of visual content.
- `PlacementIdentity`: rectangle/transform/clip placement state.
- `ObservationWindow`: consecutive frames observed for stability.
- `CandidateSize`: area or node/work estimate.
- `Tier`: none, retained, replay, snapshot, or demoted.
- `LastParityVerdict`: most recent oracle parity result.

**Validation rules**:

- Content changes force a fresh visual result and invalidate reuse.
- Placement-only changes may reuse content but must damage old and new covered regions.
- Boundaries with unstable content, failed parity, or insufficient size/benefit are not promoted.

## Promotion Decision

**Purpose**: Recorded decision to promote, keep, demote, or reject a compositor boundary.

**Fields**:

- `BoundaryId`: target boundary.
- `Decision`: promote, keep, demote, reject, or observe.
- `Reason`: stable, moving-only, content-changed, churn, budget, no-benefit, parity-failed,
  unsupported, or unsafe.
- `ObservedStabilityFrames`: frames satisfying stability criteria.
- `ExpectedSavedWork`: saved node count, paint time, or replay/snapshot work estimate.
- `MeasuredOverhead`: bookkeeping or resource overhead where available.
- `Tier`: target or current tier.
- `Diagnostics`: human-readable decision evidence.

**Validation rules**:

- Promotion requires observed stability, positive expected benefit, and clean parity.
- Demotion is required when churn, failed parity, budget pressure, or sustained no-benefit is
  observed.
- Decisions must be deterministic for same-seed scenarios.

## Snapshot Resource

**Purpose**: Higher-cost SkiaViewer-owned visual artifact used only when probe evidence shows net
benefit.

**Fields**:

- `ResourceId`: stable compositor resource id.
- `BoundaryId`: promoted boundary source.
- `ContentIdentity`: content fingerprint used to validate freshness.
- `PlacementIdentity`: placement state for composition.
- `ByteEstimate`: deterministic resource byte estimate.
- `BudgetId`: associated resource budget.
- `State`: available, missing, invalid, evicted, refreshed, unsupported, or disposed.
- `LastUsedFrame`: deterministic recency marker.
- `Diagnostics`: resource lifecycle messages.

**Validation rules**:

- Resource count and byte estimate must stay within configured budget.
- Content identity mismatch requires refresh or demotion before use.
- Unsupported or invalid resources fall back to lower tiers or full redraw without accepted stale
  output.
- Disposal/eviction must be deterministic and observable.

## Performance Probe

**Purpose**: Measurement run comparing compositor tiers against full redraw or the next lower tier.

**Fields**:

- `ProbeId`: stable run id.
- `Corpus`: scenario ids under test.
- `TierUnderTest`: damage, promotion, placement, replay, or snapshot.
- `BaselineTier`: full redraw or lower compositor tier.
- `Metrics`: frame cost, paint duration, compose duration, skipped work, cache hits/misses, memory
  estimate, and overhead.
- `Thresholds`: required benefit or overhead limits.
- `Verdict`: passed, failed, demoted, or environment-limited.
- `Diagnostics`: timing/probe messages.

**Validation rules**:

- Benefit claims require target-corpus pass and non-beneficial-corpus overhead within threshold.
- Environment-limited probes cannot claim a tier as ready.
- A failed probe demotes or marks the tier not ready.

## Compositor Readiness Package

**Purpose**: Reviewable evidence bundle for release decisions.

**Fields**:

- `HostProfiles`: proof profiles and verdicts.
- `TierVerdicts`: present proof, damage, promotion, placement, replay, snapshot.
- `ParityResults`: oracle comparison summary by corpus and frame.
- `PerformanceResults`: probe summaries and thresholds.
- `Fallbacks`: skipped, failed, unsupported, or demoted paths.
- `DiagnosticsSummary`: proof, damage, promotion, snapshot, and performance diagnostics.
- `CompatibilityImpact`: public metrics, diagnostics, baseline updates, release notes, and
  migration guidance.
- `Limitations`: environment-limited or deferred items.

**Validation rules**:

- Every ready tier links to a passed proof/parity/performance chain.
- Failed, skipped, or environment-limited tiers are visible and cannot count as shipped benefits.
- Reviewers must be able to determine ready/limited/rejected tiers within 10 minutes.

## State Transitions

```text
Present Proof:
HostProfile
  -> sentinel/damage proof run
  -> passed | failed | environment-limited
  -> scissoring allowed only for passed matching profile

Damage Redraw:
Frame update
  -> DamageRegionSet
  -> full-frame invalidation | scissor redraw candidate
  -> full-redraw oracle comparison
  -> accepted frame | fallback | tier not ready

Promotion:
CompositorBoundary observed over frames
  -> PromotionDecision observe | promote | reject
  -> reuse content at placement | fresh paint
  -> keep | demote based on parity/benefit/churn

Snapshot:
Promoted expensive stable boundary
  -> snapshot probe
  -> resource allocated/refreshed within budget
  -> reuse | evict | demote | fallback

Readiness:
Proof + parity + promotion + snapshot + performance + compatibility evidence
  -> CompositorReadinessPackage
  -> ready tier | limited tier | rejected tier
```
