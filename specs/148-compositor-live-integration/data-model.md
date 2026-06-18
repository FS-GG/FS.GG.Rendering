# Data Model: Compositor Live Integration

## Host Profile

**Purpose**: Stable identity of the rendering environment where live preservation, scissored redraw,
snapshot composition, and timing are evaluated.

**Fields**:

- `ProfileId`: deterministic identity over backend and environment facts.
- `Backend`: SkiaViewer/OpenGL backend identifier.
- `Renderer`: renderer, vendor, driver, and version facts when available.
- `PresentMode`: direct window, offscreen/readback, simulated, or unsupported.
- `FramebufferSize`: pixel width/height used by proof and redraw.
- `Scale`: effective scale factor.
- `DisplayEnvironment`: X11, Wayland, headless, missing display, CI, or unknown.
- `ProofAlgorithmVersion`: sentinel/readback algorithm version.
- `PackageVersion`: package or binary version when available.

**Validation rules**:

- Proof evidence is valid only for the matching host profile and algorithm version.
- Missing observation facts can produce `environment-limited` but not `passed`.
- Size, scale, backend, present-mode, renderer, display, or proof-version drift invalidates cached
  proof readiness.

## Live Preservation Proof

**Purpose**: Real host evidence that untouched regions survive between presents while a known
damaged region changes.

**Fields**:

- `ProofId`: deterministic proof identity.
- `HostProfile`: profile under proof.
- `ScenarioId`: stable live proof scenario identifier.
- `SentinelFrameArtifact`: image/readback/metadata identity for the full sentinel frame.
- `DamageFrameArtifact`: image/readback/metadata identity for the scissored second frame.
- `DamageRect`: known rectangle changed by the proof.
- `UntouchedSamples`: sample regions outside the damage rectangle.
- `DamagedSamples`: sample regions inside the damage rectangle.
- `Verdict`: passed, failed, or environment-limited.
- `FailureCause`: cleared pixels, corrupted pixels, stale damaged region, unsupported readback,
  missing display, timeout, host error, or mismatch.
- `CreatedAt`: timestamp for freshness review.
- `Diagnostics`: actionable proof messages.

**Validation rules**:

- `passed` requires every untouched sample to match the sentinel frame and every damaged sample to
  match the second-frame draw.
- `failed` or `environment-limited` disables damage-scoped readiness.
- Missing, stale, synthetic-only, failed, environment-limited, host-mismatched, or version-mismatched
  proof cannot enable partial redraw.

## Proof Acceptance

**Purpose**: Readiness gate that decides whether a proof can unlock damage-scoped redraw for the
active run.

**Fields**:

- `ActiveProfile`: current host profile.
- `CandidateProof`: proof record being evaluated.
- `FreshnessPolicy`: accepted age/run scope for proof reuse.
- `Accepted`: true only when the proof is passed, fresh, and matching.
- `RejectedReason`: missing, stale, failed, environment-limited, host mismatch, algorithm mismatch,
  artifact missing, or synthetic-only.
- `Diagnostics`: reviewer-facing acceptance details.

**Validation rules**:

- `Accepted = true` requires a passed proof and matching active profile.
- Rejection always produces a full-frame fallback reason.
- Acceptance decisions are deterministic for the same proof/profile inputs.

## Damage Plan

**Purpose**: Concrete repaint plan derived from retained damage and movement decisions for one
frame.

**Fields**:

- `FrameId`: deterministic frame sequence id.
- `FrameSize`: pixel width/height.
- `DamageRegions`: clipped axis-aligned rectangles.
- `UnionArea`: integer area of the damage union.
- `FullFrameInvalidation`: true for resize, theme/global change, provider/resource change, unsafe
  preservation, or explicit full redraw.
- `Cause`: localized update, overlap, movement, resize, theme, resource, unsupported, disabled,
  parity failure, or fallback.
- `SourceBoundaries`: retained identities/control ids that caused damage.

**Validation rules**:

- Regions are clipped and deduplicated before union area is recorded.
- Overlaps count once in `UnionArea`.
- Placement-only movement includes both old and new covered regions.
- Empty damage is valid only for idle/no-op frames.

## Redraw Operation

**Purpose**: Host operation used to produce a frame through either full redraw or damage-scoped
redraw.

**Fields**:

- `OperationId`: deterministic operation identity.
- `FrameId`: frame being rendered.
- `Mode`: full-frame, damage-scoped, lower-tier fallback, or skipped.
- `ProofId`: accepted live proof identity when damage-scoped.
- `ScissorRects`: final host scissor rectangles.
- `NoClear`: true when untouched regions must be preserved.
- `FallbackReason`: none, missing proof, stale proof, failed proof, host mismatch, full-frame
  invalidation, unsupported host, disabled mode, unsafe damage, parity failure, or resource failure.
- `OutputIdentity`: rendered frame checksum/artifact identity.
- `Diagnostics`: scissor, clear, reset, and fallback details.

**Validation rules**:

- Damage-scoped mode requires accepted proof and no full-frame invalidation.
- Scissor rectangles cover the damage union and never rely on out-of-frame pixels.
- Host scissor/no-clear state resets before full redraw, readback, and the next frame.
- Fallback mode must produce the same visible output as full redraw.

## Full-Frame Oracle

**Purpose**: Reference frame produced without partial redraw, movement reuse, or snapshot reuse.

**Fields**:

- `ScenarioId`: corpus scenario id.
- `FrameId`: deterministic frame id.
- `ImageArtifact`: optional image/readback path.
- `OutputIdentity`: checksum or package identity.
- `Metrics`: baseline frame cost and work counters.
- `Diagnostics`: oracle render details.

**Validation rules**:

- Every accepted optimized frame compares to the matching oracle frame.
- Missing oracle evidence marks the tier not ready.
- A lower compositor tier cannot replace the oracle.

## Reusable Boundary

**Purpose**: Retained visual region evaluated for placement reuse, replay promotion, demotion, and
snapshot eligibility.

**Fields**:

- `BoundaryId`: stable retained identity.
- `ContentIdentity`: render-affecting fingerprint.
- `PlacementIdentity`: rectangle, transform, clip, layer, and scale placement state.
- `PreviousPlacementIdentity`: prior placement state when movement occurs.
- `ObservationWindow`: consecutive frames considered for stability.
- `CandidateCost`: area, node count, replay cost, or timing estimate.
- `CurrentTier`: none, retained, replay, snapshot, or demoted.
- `LastParityVerdict`: passed, failed, skipped, or environment-limited.

**Validation rules**:

- Content changes invalidate reused output and require fresh paint or demotion.
- Placement-only changes may reuse content only when old and new regions are damaged.
- Boundaries with churn, failed parity, or no measured benefit remain unpromoted or demote.

## Reuse Decision

**Purpose**: Recorded decision to observe, promote, reuse, refresh, demote, or reject a boundary.

**Fields**:

- `BoundaryId`: target boundary.
- `Decision`: observe, promote, reuse, refresh, demote, reject, or skip.
- `Reason`: stable, moving-only, content-changed, churn, no-benefit, parity-failed, budget,
  unsupported, stale-proof, or unsafe.
- `TargetTier`: retained, replay, snapshot, lower-tier, or full-frame.
- `ExpectedSavedWork`: saved node count, replay count, paint time, or compose time.
- `MeasuredOverhead`: bookkeeping/resource overhead where available.
- `Diagnostics`: human-readable decision evidence.

**Validation rules**:

- Reuse requires valid content identity and clean parity evidence.
- Demotion is required after sustained churn, parity failure, budget pressure, or non-benefit.
- Decisions are stable for repeated same-seed scenarios.

## Snapshot Resource

**Purpose**: Bounded SkiaViewer-owned visual artifact for expensive stable content.

**Fields**:

- `ResourceId`: stable resource identity.
- `BoundaryId`: source boundary.
- `ContentIdentity`: content fingerprint that validates freshness.
- `ByteEstimate`: deterministic memory estimate.
- `BudgetId`: associated resource budget.
- `State`: available, missing, invalid, refreshed, evicted, disposed, unsupported, or bypassed.
- `LastUsedFrame`: deterministic recency marker.
- `HostProfile`: host profile where the resource is valid.
- `Diagnostics`: lifecycle evidence.

**Validation rules**:

- Resource count and byte estimate stay within budget.
- Content or host mismatch requires refresh, eviction, demotion, or bypass before use.
- Unsupported or invalid resources fall back to lower tiers or full redraw.
- Disposal/eviction is deterministic and recorded.

## Timing Probe

**Purpose**: Comparable measurement run for a compositor tier against full redraw or the next lower
tier.

**Fields**:

- `ProbeId`: stable run id.
- `HostProfile`: measured host profile.
- `Corpus`: scenario ids under test.
- `TierUnderTest`: damage, placement, replay, or snapshot.
- `BaselineTier`: full-frame, damage, placement, replay, or lower-tier fallback.
- `WarmupFrames`: frames excluded from measurement.
- `MeasuredFrames`: frames included in measurement.
- `Metrics`: frame cost, paint duration, compose duration, skipped work, cache hits/misses,
  snapshot memory, and overhead.
- `Thresholds`: required benefit or overhead limits.
- `Verdict`: passed, failed, demoted, rejected, or environment-limited.
- `Diagnostics`: timing and environment messages.

**Validation rules**:

- Benefit claims require target-corpus pass and non-beneficial-corpus overhead within threshold.
- Environment-limited timing cannot mark a tier ready.
- Failed probes demote or mark the tier rejected.

## Readiness Package

**Purpose**: Reviewable evidence bundle that decides which compositor tiers can be claimed.

**Fields**:

- `HostProfiles`: live proof profiles and verdicts.
- `TierVerdicts`: live proof, damage, placement, replay, snapshot, and timing.
- `ParityResults`: oracle comparison summary by corpus and frame.
- `Fallbacks`: unsafe, unsupported, full-frame, lower-tier, disabled, and demoted paths.
- `ReuseResults`: promotion, movement, content-change, and demotion summaries.
- `SnapshotResults`: budget, lifecycle, support, and composition summaries.
- `TimingResults`: probe thresholds, baselines, and deltas.
- `CompatibilityImpact`: public metrics, diagnostics, baselines, release notes, and migration
  guidance.
- `Limitations`: environment-limited, rejected, skipped, or deferred items.

**Validation rules**:

- Every ready tier links to matching proof, parity, fallback, resource, timing, and compatibility
  evidence.
- Failed, skipped, rejected, or environment-limited tiers are visible and cannot count as shipped
  benefits.
- Reviewers can determine tier status and supporting artifacts within 10 minutes.

## State Transitions

```text
Live Proof:
HostProfile
  -> sentinel frame presented
  -> damage-only frame presented
  -> readback/observation
  -> passed | failed | environment-limited
  -> accepted only when fresh and matching active profile

Damage Redraw:
Frame update
  -> DamagePlan
  -> proof acceptance
  -> damage-scoped no-clear redraw | full-frame/lower-tier fallback
  -> oracle comparison
  -> accepted frame | rejected tier

Content/Placement Reuse:
ReusableBoundary observed
  -> content identity stable | content changed
  -> placement unchanged | placement changed
  -> reuse with old+new damage | fresh paint | demote/reject

Snapshot:
Eligible expensive stable boundary
  -> resource lookup/create/refresh
  -> budget check
  -> compose snapshot | evict/dispose | bypass | demote

Timing and Readiness:
Proof + parity + fallback + reuse + snapshot + timing + compatibility
  -> readiness package
  -> ready | limited | rejected | skipped tier verdicts
```
