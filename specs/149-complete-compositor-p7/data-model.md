# Data Model: Complete P7 Compositor

## Host Profile

**Purpose**: Stable identity of the rendering environment where live preservation, damage-scoped
redraw, snapshot composition, and timing are evaluated.

**Fields**:

- `ProfileId`: deterministic identity over backend and environment facts.
- `Backend`: SkiaViewer/OpenGL backend identifier.
- `Renderer`: renderer, vendor, driver, and version facts when available.
- `PresentMode`: direct swapchain, offscreen/readback, simulated, or unsupported.
- `FramebufferSize`: pixel width/height used by proof and redraw.
- `Scale`: effective scale factor.
- `DisplayEnvironment`: X11, Wayland, headless, missing display, CI, or unknown.
- `ProofAlgorithmVersion`: sentinel/readback algorithm version.
- `PackageVersion`: package or binary version when available.

**Validation rules**:

- Proof evidence is valid only for the matching host profile and algorithm version.
- Missing observation facts can produce `environment-limited` but not `accepted`.
- Size, scale, backend, present-mode, renderer, display, package, or proof-version drift rejects
  cached proof readiness.

## Live Compositor Proof

**Purpose**: Real host evidence that untouched pixels remain valid while a known damaged region
changes.

**Fields**:

- `ProofId`: deterministic proof identity.
- `HostProfile`: profile under proof.
- `ScenarioId`: stable live proof scenario identifier.
- `SentinelArtifact`: captured first-frame image, readback, or metadata identity.
- `DamageArtifact`: captured second-frame image, readback, or metadata identity.
- `DamageRegion`: known rectangle changed by the proof.
- `UntouchedObservations`: sample regions outside damage with expected/actual identities.
- `DamagedObservations`: sample regions inside damage with expected/actual identities.
- `Verdict`: accepted, failed, or environment-limited.
- `FailureCause`: cleared pixels, stale damaged region, corrupted untouched pixels, blank artifact,
  unsupported readback, missing display, timeout, permission, host error, or mismatch.
- `CreatedAt`: timestamp for freshness review.
- `Diagnostics`: actionable proof messages.

**Validation rules**:

- `accepted` requires every untouched sample to match the sentinel and every damaged sample to
  match the second-frame draw.
- Failed or environment-limited proof disables partial redraw.
- Missing, stale, blank, synthetic-only, failed, environment-limited, host-mismatched, or
  version-mismatched proof cannot enable partial redraw.

## Damage Region

**Purpose**: Bounded area of a frame that changed and is eligible for scoped redraw.

**Fields**:

- `RegionId`: deterministic region id.
- `FrameId`: frame sequence id.
- `Bounds`: clipped rectangle in framebuffer pixels.
- `Cause`: localized update, overlap, movement old region, movement new region, resize, theme,
  provider, resource, unsupported, disabled, or parity failure.
- `SourceBoundary`: retained boundary/control id that caused the damage when known.

**Validation rules**:

- Bounds are clipped to the frame before use.
- Overlaps count once in union area.
- Placement-only movement contributes old and new covered regions.
- Empty damage is valid only for idle/no-op frames after a valid prior frame.

## Frame Artifact

**Purpose**: Captured output image, readback result, or stable summary used to compare compositor
output to the full-redraw oracle.

**Fields**:

- `ArtifactId`: stable artifact id.
- `ScenarioId`: corpus scenario id.
- `FrameId`: deterministic frame id.
- `Kind`: image, pixel readback, metadata hash, unsupported-host disclosure, or summary.
- `Path`: relative path when persisted.
- `OutputIdentity`: checksum/hash/identity used for comparison.
- `Decodable`: whether image/readback content could be inspected.
- `Diagnostics`: artifact quality and limitation messages.

**Validation rules**:

- Accepted proof and parity claims require non-missing, non-blank artifacts.
- Unsupported-host artifacts can explain limitations but cannot prove accepted partial redraw.
- Artifact paths in readiness summaries must be reviewable and stable.

## Fallback Decision

**Purpose**: Recorded reason a frame or tier did not use damage-scoped rendering, reuse, or
snapshot composition.

**Fields**:

- `DecisionId`: deterministic id.
- `FrameId`: affected frame when applicable.
- `Tier`: proof, damage, placement, replay, snapshot, timing, or readiness.
- `FallbackMode`: full redraw, lower-tier redraw, skipped, demoted, or disabled.
- `Reason`: missing proof, stale proof, failed proof, host mismatch, full-frame invalidation,
  unsafe damage, unsupported host, disabled mode, parity failure, resource failure, timing
  inconclusive, or compatibility blocked.
- `UserMessage`: consumer-visible explanation.
- `Diagnostics`: maintainer-oriented context.

**Validation rules**:

- Every fallback has a user-readable reason.
- Fallback frames match full-redraw visible output.
- Ready verdicts cannot hide fallback reasons that affect the tier claim.

## Redraw Frame

**Purpose**: One rendered frame produced by full redraw, damage-scoped redraw, lower-tier fallback,
or skipped preservation.

**Fields**:

- `FrameId`: deterministic frame sequence id.
- `Mode`: full-frame, damage-scoped, lower-tier fallback, skipped-zero-damage, or rejected.
- `ProofId`: accepted live proof identity when damage-scoped.
- `DamageRegions`: final damage regions.
- `ScissorRects`: host scissor rectangles applied.
- `NoClear`: true when untouched pixels must be preserved.
- `OracleArtifact`: full-redraw reference artifact.
- `OutputArtifact`: produced frame artifact.
- `ParityVerdict`: passed, failed, skipped, or environment-limited.
- `Fallback`: fallback decision when not damage-scoped.

**Validation rules**:

- Damage-scoped mode requires accepted proof and valid damage.
- Scissor rectangles cover the damage union and never rely on out-of-frame pixels.
- Host scissor/no-clear state resets before full redraw, readback, and the next frame.
- Accepted output matches the full-redraw oracle within accepted tolerance.

## Reuse Decision

**Purpose**: Per-frame explanation of whether content was refreshed, placement was reused, or the
tier was demoted/rejected.

**Fields**:

- `BoundaryId`: retained visual boundary.
- `ContentIdentity`: render-affecting fingerprint.
- `PlacementIdentity`: rectangle, transform, clip, layer, and scale placement.
- `PreviousPlacementIdentity`: prior placement when movement occurs.
- `Decision`: observe, promote, reuse, refresh, demote, reject, or skip.
- `Reason`: stable, moving-only, content-changed, churn, no-benefit, parity-failed, budget,
  unsupported, stale-proof, unsafe, or fallback.
- `ExpectedSavedWork`: saved node count, replay count, paint time, or compose time.
- `MeasuredOverhead`: bookkeeping/resource overhead where available.
- `Diagnostics`: human-readable decision evidence.

**Validation rules**:

- Reuse requires valid content identity and clean parity evidence.
- Content changes force fresh output or demotion.
- Placement-only movement may reuse content only when old and new regions are damaged.
- Demotion is required after sustained churn, parity failure, budget pressure, or non-benefit.

## Snapshot Resource

**Purpose**: Bounded SkiaViewer-owned visual resource for expensive stable content.

**Fields**:

- `ResourceId`: stable resource identity.
- `BoundaryId`: source boundary.
- `ContentIdentity`: content fingerprint that validates freshness.
- `HostProfile`: host profile where the resource is valid.
- `ByteEstimate`: deterministic memory estimate.
- `BudgetId`: associated resource budget.
- `State`: created, reused, refreshed, replaced, evicted, disposed, invalid, unsupported,
  bypassed, or failed.
- `LastUsedFrame`: deterministic recency marker.
- `Artifact`: optional visual/lifecycle artifact.
- `Diagnostics`: lifecycle evidence.

**Validation rules**:

- Entry count and byte estimate stay within budget.
- Content or host mismatch requires refresh, eviction, demotion, or bypass before use.
- Invalid, lost, or unsupported resources fall back to lower tiers or full redraw.
- Snapshot-assisted output participates in oracle parity before readiness claims.

## Timing Probe

**Purpose**: Repeated measurement set comparing full redraw, damage-scoped redraw, and
snapshot-assisted redraw.

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
- `Verdict`: passed, failed, demoted, rejected, environment-limited, or inconclusive.
- `Diagnostics`: timing and environment messages.

**Validation rules**:

- Benefit claims require comparable repeated measurements.
- Beneficial-corpus pass and non-beneficial-corpus overhead checks are both required.
- Environment-limited, incomplete, or inconclusive timing cannot mark a tier ready.
- Failed probes demote or reject the responsible tier.

## Compositor Readiness Report

**Purpose**: Maintainer-facing summary that states whether P7 is accepted, environment-limited, or
failed.

**Fields**:

- `ReportId`: stable report id.
- `HostProfiles`: proof profiles and verdicts.
- `TierVerdicts`: proof, damage, placement, replay, snapshot, timing, and public diagnostics.
- `ParityResults`: oracle comparison summary by corpus and frame.
- `Fallbacks`: unsafe, unsupported, full-frame, lower-tier, disabled, and demoted paths.
- `ReuseResults`: promotion, movement, content-change, and demotion summaries.
- `SnapshotResults`: budget, lifecycle, support, and composition summaries.
- `TimingResults`: probe thresholds, baselines, and deltas.
- `CompatibilityImpact`: public metrics, diagnostics, baselines, release notes, and migration
  guidance.
- `Limitations`: environment-limited, rejected, skipped, inconclusive, or deferred items.

**Validation rules**:

- Every accepted claim links to supporting proof, parity, fallback, resource, timing, and
  compatibility evidence.
- Failed, skipped, rejected, inconclusive, or environment-limited tiers are visible and cannot
  count as accepted benefits.
- A maintainer can determine P7 status and supporting artifact paths from one summary.

## Public Diagnostic Surface

**Purpose**: Consumer-visible package contract for compositor proof, parity, reuse, snapshot,
timing, fallback, and readiness state.

**Fields**:

- `ProofStatus`: accepted, failed, environment-limited, missing, stale, or host-mismatched.
- `DamageParityStatus`: passed, failed, skipped, environment-limited, or not-run.
- `ReuseStatus`: ready, demoted, rejected, skipped, or limited.
- `SnapshotStatus`: ready, demoted, rejected, skipped, unsupported, or limited.
- `TimingStatus`: passed, failed, inconclusive, environment-limited, or not-run.
- `FallbackStatus`: none, full-redraw, lower-tier, disabled, demoted, or blocked.
- `ReadinessVerdict`: accepted, environment-limited, failed, or incomplete.
- `Limitations`: consumer-facing limitations and next evidence path.

**Validation rules**:

- The surface is declared in `.fsi` before implementation.
- Semantic tests exercise expected consumer calls through the package surface.
- Package validation rejects undocumented public-surface drift.

## State Transitions

```text
Live Proof:
HostProfile
  -> sentinel frame presented
  -> damage-only frame presented
  -> readback/observation
  -> accepted | failed | environment-limited
  -> accepted only when fresh and matching active profile

Damage Redraw:
Frame update
  -> DamageRegions
  -> proof acceptance
  -> damage-scoped no-clear redraw | full-frame/lower-tier fallback
  -> oracle comparison
  -> accepted frame | rejected tier

Reuse:
Reusable boundary observed
  -> content stable | content changed
  -> placement unchanged | placement changed
  -> reuse with old+new damage | fresh paint | demote | reject

Snapshot:
Eligible expensive stable boundary
  -> resource lookup/create/refresh
  -> budget check
  -> compose snapshot | evict/dispose | bypass | demote | fallback

Timing and Readiness:
Proof + parity + fallback + reuse + snapshot + timing + compatibility
  -> readiness report
  -> accepted | environment-limited | failed | incomplete
```
