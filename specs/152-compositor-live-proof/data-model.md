# Data Model: Compositor Live Proof Acceptance

## Host Profile

**Purpose**: Stable identity of the live presentation environment used for proof, parity, and
timing acceptance.

**Fields**:

- `ProfileId`: deterministic identity over backend and environment facts.
- `Backend`: SkiaViewer/OpenGL backend identifier.
- `Renderer`: renderer, vendor, driver, and version facts when available.
- `PresentMode`: direct swapchain, offscreen/readback, simulated, or unsupported.
- `FramebufferSize`: pixel dimensions used by proof and corpus runs.
- `Scale`: effective scale factor.
- `DisplayEnvironment`: X11, Wayland, headless, missing display, CI, or unknown.
- `ProofMethod`: proof algorithm name and version.
- `PackageVersion`: package or harness build identity.

**Validation rules**:

- Proof, parity, and timing evidence can form an accepted decision only when their host profile
  and proof method match.
- Missing display, renderer, readback, or permission facts produce `environment-limited`, not
  `accepted`.
- Host, scale, framebuffer, backend, package, or proof-method drift rejects cached acceptance.

## Live Proof Attempt

**Purpose**: One live proof run used to determine whether a host preserves undamaged pixels across
a damage-scoped frame.

**Fields**:

- `AttemptId`: stable run identity.
- `HostProfile`: host profile recorded during the run.
- `StartedAt` and `CompletedAt`: timestamps used for freshness.
- `ProofMethod`: proof algorithm used by the run.
- `SentinelArtifact`: first full-frame sentinel artifact identity.
- `DamageArtifact`: second damage-scoped artifact identity.
- `DamageRegion`: known changed rectangle.
- `DamagedSamples`: observed samples inside the damage region.
- `UndamagedSamples`: observed samples outside the damage region.
- `ArtifactQuality`: present, decodable, non-blank, non-stale, and non-synthetic flags.
- `Verdict`: `accepted`, `failed`, or `environment-limited`.
- `Reason`: reviewer-visible reason for failed or limited verdicts.
- `Diagnostics`: maintainer-oriented proof details.

**Validation rules**:

- `accepted` requires damaged samples to match the damage draw and undamaged samples to retain
  sentinel identity.
- Missing, blank, stale, synthetic-only, failed, environment-limited, host-mismatched, or
  proof-method-mismatched attempts cannot enter an accepted proof set.
- Unsupported hosts record zero accepted partial-redraw artifacts.

**State transitions**:

```text
started -> profile-detected -> sentinel-presented -> damage-presented -> samples-observed
        -> accepted | failed | environment-limited
```

## Accepted Proof Set

**Purpose**: The group of live proof attempts that unlocks live partial-redraw acceptance for a
host profile.

**Fields**:

- `ProofSetId`: deterministic identity for the accepted run set.
- `HostProfile`: shared host profile.
- `ProofMethod`: shared proof method.
- `Attempts`: accepted proof attempts in the set.
- `FreshnessWindow`: policy used to decide whether attempts are fresh enough.
- `ArtifactSummary`: quality and path summary for all proof artifacts.
- `AcceptedAt`: timestamp when the set became accepted.
- `Diagnostics`: reviewer-visible acceptance details.

**Validation rules**:

- Requires at least 3 accepted attempts.
- All attempts must be fresh, matching, non-blank, non-stale, non-synthetic, and from the same
  host profile and proof method.
- Any failed, limited, missing, stale, host-mismatched, or method-mismatched attempt keeps the
  proof set unaccepted and records why.

**State transitions**:

```text
empty -> collecting -> accepted
                  \-> rejected
                  \-> environment-limited
```

## Proof Artifact

**Purpose**: Reviewable image, readback, or summary evidence attached to a proof attempt.

**Fields**:

- `ArtifactId`: stable artifact identity.
- `AttemptId`: owning proof attempt.
- `Kind`: sentinel frame, damage frame, sample readback, proof summary, limitation, or metadata.
- `Path`: repository-relative path when persisted.
- `ContentIdentity`: checksum, hash, or sample identity used for freshness.
- `Decodable`: whether content can be inspected.
- `Blank`: whether the artifact is blank.
- `Synthetic`: whether it was produced by a simulation.
- `Diagnostics`: artifact quality notes.

**Validation rules**:

- Accepted proof requires required artifacts to exist, decode, and be non-blank and non-synthetic.
- Optional limitation artifacts may support `environment-limited` but cannot satisfy acceptance.
- Artifact paths in readiness summaries must be stable and reviewable.

## Live Corpus Scenario

**Purpose**: Representative frame sequence used to compare damage-scoped live output with full
redraw.

**Fields**:

- `ScenarioId`: stable scenario name.
- `Frames`: ordered frame transitions.
- `Category`: localized update, no-change, movement, resize, full invalidation, invalid damage,
  unsupported host, or resource failure.
- `RequiredProofSet`: accepted proof set required for damage-scoped execution.
- `ExpectedFallback`: expected fallback reason when scoped redraw is unsafe.
- `OracleMode`: full-redraw reference mode.

**Validation rules**:

- Accepted parity scenarios require an accepted proof set for the same host profile.
- Resize, full invalidation, invalid damage, host-profile drift, and resource failure must select
  full redraw or another safe fallback with a reason.
- Unsupported-host scenarios remain non-accepting evidence.

## Damage-Scoped Parity Result

**Purpose**: Result of comparing live damage-scoped output to the full-redraw oracle.

**Fields**:

- `ResultId`: stable result identity.
- `ScenarioId`: live corpus scenario.
- `FrameId`: frame under comparison.
- `HostProfile`: host profile used for the run.
- `ProofSetId`: accepted proof set identity when scoped redraw is attempted.
- `DamageRegions`: final clipped damage rectangles.
- `Fallback`: fallback decision when scoped redraw does not run.
- `OracleArtifact`: full-redraw artifact identity.
- `ScopedArtifact`: damage-scoped artifact identity.
- `ParityVerdict`: `passed`, `failed`, `skipped`, or `environment-limited`.
- `Diagnostics`: comparison details.

**Validation rules**:

- Accepted scoped results require matching host profile and proof set.
- `passed` requires final visible output to match the full-redraw oracle.
- Failed, skipped, or environment-limited parity cannot count as accepted partial-redraw evidence.

## Fallback Decision

**Purpose**: Recorded reason the system kept or returned to full redraw instead of accepting
damage-scoped rendering.

**Fields**:

- `DecisionId`: deterministic identity.
- `FrameId`: affected frame when applicable.
- `Tier`: proof, parity, timing, readiness, damage, snapshot, or public diagnostics.
- `FallbackMode`: full redraw, lower-tier redraw, disabled, demoted, skipped, or blocked.
- `Reason`: missing proof, stale proof, failed proof, environment-limited proof, host mismatch,
  proof-method mismatch, invalid damage, full invalidation, unsupported host, resource failure,
  parity failure, timing inconclusive, non-beneficial, or compatibility blocked.
- `UserMessage`: consumer-visible explanation.
- `Diagnostics`: maintainer detail.

**Validation rules**:

- Every unsafe or unsupported path records a reason.
- Fallback output must match full redraw when it produces a frame.
- Fallback decisions are not counted as accepted partial-redraw artifacts.

## Timing Evidence

**Purpose**: Repeated same-profile measurement used to accept, reject, or mark inconclusive a
performance claim.

**Fields**:

- `TimingId`: stable measurement identity.
- `HostProfile`: measured host profile.
- `ProofSetId`: accepted proof set identity for scoped measurements.
- `ScenarioIds`: representative live scenarios.
- `Repetitions`: comparable repetitions per scenario.
- `WarmupFrames`: frames excluded from measurement.
- `Baseline`: full redraw metrics.
- `DamageScoped`: damage-scoped metrics.
- `Thresholds`: benefit and noise thresholds.
- `Verdict`: `accepted-benefit`, `rejected`, `inconclusive`, or `environment-limited`.
- `Reason`: noisy, incomplete, non-beneficial, host-limited, missing proof, or accepted benefit.
- `Diagnostics`: measurement detail.

**Validation rules**:

- A performance benefit requires at least 5 representative live scenarios with at least 5
  comparable repetitions per scenario.
- Timing must match the accepted proof/parity host profile.
- Incomplete, noisy, environment-limited, or non-beneficial measurements reject or mark the claim
  inconclusive.

## Performance Claim Decision

**Purpose**: Reviewer-facing decision on whether Feature 152 accepts a compositor performance
claim.

**Fields**:

- `DecisionId`: stable decision identity.
- `TimingEvidence`: timing records used by the decision.
- `ProofSetId`: accepted proof set identity.
- `ParityResults`: parity evidence linked to timing scenarios.
- `Verdict`: `accepted`, `rejected`, or `inconclusive`.
- `ClaimText`: public claim text when accepted, or no-claim text otherwise.
- `Limitations`: remaining timing caveats.

**Validation rules**:

- Accepted claims require accepted proof, passed parity, and accepted timing benefit for the same
  host profile.
- Snapshot, reuse, or deterministic counters can support context but cannot replace live timing.
- Rejected or inconclusive decisions must be visible in readiness output.

## P7 Readiness Summary

**Purpose**: Single review entry point that states P7 live partial-redraw status.

**Fields**:

- `SummaryId`: stable summary identity.
- `Status`: `accepted`, `environment-limited`, `failed`, or `fallback-gated`.
- `HostProfiles`: proof, parity, and timing host profiles.
- `ProofSet`: accepted, rejected, or limited proof-set status.
- `ParityResults`: live corpus parity summary.
- `TimingDecision`: performance claim decision.
- `Fallbacks`: fallback reasons by scenario and tier.
- `CompatibilityImpact`: compatibility ledger reference.
- `EvidenceLinks`: proof, parity, timing, compatibility, and regression artifacts.
- `Limitations`: remaining blocked or rejected claims.

**Validation rules**:

- `accepted` requires accepted proof set, passed same-profile parity, compatibility validation, and
  no blocking fallback for accepted scenarios.
- Performance status is independent: P7 can accept correctness while rejecting a performance
  claim if timing is non-beneficial or inconclusive.
- Environment-limited, failed, and fallback-gated statuses must name the blocking evidence.

## Compatibility Ledger

**Purpose**: Record of public or package-facing compositor readiness changes caused by Feature
152.

**Fields**:

- `LedgerId`: stable ledger identity.
- `PublicApiChanges`: `.fsi` and surface baseline changes.
- `DiagnosticChanges`: public status, fallback, timing, or readiness vocabulary changes.
- `BehaviorChanges`: compositor mode, fallback, or performance-claim behavior changes.
- `MigrationGuidance`: consumer action required, if any.
- `ValidationEvidence`: package, FSI, surface, and docs validation results.
- `UnchangedContracts`: Feature 149, Feature 151, and adjacent guarantees preserved.

**Validation rules**:

- Public drift without a ledger entry blocks accepted readiness.
- Ledger must distinguish intentional deltas from no-change validation.
- Package and public contract validation must pass with zero undocumented compositor drift.
