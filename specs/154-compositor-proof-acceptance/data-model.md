# Data Model: Compositor Proof Acceptance

## Host Profile

**Purpose**: Stable identity of the presentation environment used to bind proof, parity, and
timing evidence to one capable host.

**Fields**:

- `ProfileId`: deterministic host identity.
- `Backend`: SkiaViewer/OpenGL backend identifier.
- `Renderer`: renderer, vendor, driver, and version facts when available.
- `PresentMode`: direct swapchain, offscreen readback, simulated, or unsupported mode.
- `FramebufferSize`: pixel dimensions used by proof and parity.
- `Scale`: effective scale factor.
- `DisplayEnvironment`: X11, Wayland, headless, missing display, CI, or unknown.
- `ProofAlgorithmVersion`: Feature 153 proof method version.
- `PackageVersion`: package or harness build identity recorded in readiness output.

**Validation rules**:

- Accepted proof, parity, and timing evidence must share the same host profile.
- Missing display, renderer, readback, permission, or context facts produce
  `environment-limited`, not `accepted`.
- Host, scale, framebuffer, backend, package, or proof-method drift rejects cached acceptance.

## Proof Method

**Purpose**: Versioned method used to create and evaluate sentinel and damage-scoped frame
evidence.

**Fields**:

- `MethodId`: stable method name, for example `sentinel-damage-v1`.
- `AlgorithmVersion`: explicit version used for proof-set matching.
- `SentinelFrameRole`: baseline frame role.
- `DamageFrameRole`: scoped frame role.
- `DamageRegion`: expected changed rectangle.
- `UndamagedSamplePlan`: sample locations or identities outside the damage region.

**Validation rules**:

- Attempts using different proof methods cannot belong to the same accepted proof set.
- Method drift records a fallback-gated reason.

## Proof Attempt

**Purpose**: One live run that records sentinel evidence, damage evidence, sample observations,
artifact quality, host profile, proof method, and attempt classification.

**Fields**:

- `AttemptId`: stable current-run identity.
- `Proof`: Feature 153 `PresentProof` record.
- `ProofMethod`: method name and algorithm version.
- `ArtifactQuality`: present, decodable, non-blank, fresh, and synthetic flags.
- `SentinelArtifacts`: paths and identities for baseline evidence.
- `DamageArtifacts`: paths and identities for scoped redraw evidence.
- `DamagedSamples`: observations inside the damage region.
- `UndamagedSamples`: observations outside the damage region.
- `Classification`: `accepted`, `failed`, or `environment-limited`.
- `Reason`: reviewer-visible blocking or acceptance reason.

**Validation rules**:

- `accepted` requires fresh, decodable, non-blank, non-synthetic sentinel and damage evidence.
- `accepted` requires damaged pixels to update and undamaged pixels to preserve sentinel identity.
- Missing, stale, blank, synthetic-only, undecodable, incomplete, host-mismatched,
  proof-method-mismatched, or failed-pixel evidence fails closed.
- Unsupported hosts record zero accepted partial-redraw artifacts.

**State transitions**:

```text
initialized -> profile-detected -> sentinel-presented -> damage-presented
            -> samples-observed -> quality-evaluated
            -> accepted | failed | environment-limited
```

## Accepted Proof Set

**Purpose**: The exact group of three selected accepted attempts required to accept the capable-host
proof gate.

**Fields**:

- `ProofSetId`: deterministic identity for the selected attempts.
- `HostProfile`: shared capable host profile.
- `ProofMethod`: shared proof method.
- `SelectedAttemptIds`: exactly three selected attempt identities.
- `Attempts`: the selected attempt records.
- `FreshnessWindow`: policy used to decide current evidence.
- `Status`: `accepted`, `fallback-gated`, `failed`, or `environment-limited`.
- `Reasons`: acceptance or blocking reasons.
- `AcceptedAt`: timestamp when accepted, when applicable.

**Validation rules**:

- Accepted proof sets require exactly three selected attempts.
- Each selected attempt must be `accepted`, fresh, host-matching, proof-method-matching, and
  artifact-quality accepted.
- Extra attempts may be linked as context but are not silently folded into the accepted set.
- Failed or limited attempts prevent acceptance unless they are explicitly excluded with a visible
  selection rationale and cannot contradict the selected proof profile.

**State transitions**:

```text
empty -> collecting -> accepted
                  \-> fallback-gated
                  \-> failed
                  \-> environment-limited
```

## Damage-Scoped Parity Scenario

**Purpose**: One representative transition that compares damage-scoped redraw with the full-redraw
reference on the accepted host profile.

**Fields**:

- `ScenarioId`: stable path, for example `damage/localized-update`.
- `HostProfile`: host profile used for the run.
- `InitialFrame`: starting frame identity.
- `Transition`: no-change, localized update, movement, overlap, edge clipping, resize, full
  invalidation, invalid damage, unsupported host, or resource failure.
- `DamageRegion`: requested damage rectangle or invalid-damage marker.
- `ReferenceOutput`: full-redraw output identity.
- `ScopedOutput`: damage-scoped output identity.
- `FallbackDecision`: full-redraw fallback when scenario is not accepted.

**Validation rules**:

- Accepted scenarios require the same host profile as the accepted proof set.
- Accepted scenarios require final visible output to match the full-redraw reference.
- Invalid damage, full invalidation, unsupported host, and resource failure may be safe fallback
  scenarios but cannot be counted as accepted scoped redraw parity.

## Damage-Scoped Parity Result

**Purpose**: Scenario-level verdict for the parity corpus.

**Fields**:

- `ResultId`: stable scenario result identity.
- `Scenario`: parity scenario.
- `Verdict`: `accepted`, `fallback`, `failed`, or `environment-limited`.
- `ArtifactPaths`: reference, scoped, diff, and summary artifact paths when available.
- `Reason`: reviewer-visible acceptance, fallback, failure, or limitation reason.
- `Diagnostics`: maintainer detail.

**Validation rules**:

- The readiness gate accepts parity only when every required accepting scenario passes and every
  non-accepted scenario records a safe fallback reason.
- Cross-profile, stale, missing, or undecodable parity evidence cannot unlock partial redraw.

## Timing Policy

**Purpose**: Declared threshold and noise policy for any performance claim.

**Fields**:

- `PolicyId`: stable policy identity.
- `Threshold`: required benefit threshold for accepting a performance claim.
- `NoisePolicy`: variance, warmup, outlier, and confidence rules.
- `ScenarioCount`: required representative scenario count.
- `RepetitionsPerScenario`: required comparable repetitions per scenario.
- `MeasurementSource`: live same-profile timing source.

**Validation rules**:

- Policy must be declared before timing evidence is accepted.
- Accepted performance benefit requires at least five representative scenarios and at least five
  comparable repetitions per scenario.

## Timing Measurement

**Purpose**: One measured timing sample for a live scenario.

**Fields**:

- `MeasurementId`: stable sample identity.
- `HostProfile`: host profile used for measurement.
- `ScenarioId`: measured scenario.
- `Mode`: full redraw or damage-scoped redraw.
- `Repetition`: sample number.
- `Duration`: measured duration.
- `Warmup`: whether the sample is warmup-only.
- `Diagnostics`: process, clock, and environment detail.

**Validation rules**:

- Measurements must be same-profile and comparable across full-redraw and damage-scoped modes.
- Missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial measurements
  cannot accept a performance claim.

## Timing Decision

**Purpose**: Accepted, rejected, or inconclusive decision for a performance claim.

**Fields**:

- `DecisionId`: stable timing decision identity.
- `Policy`: timing policy.
- `Measurements`: included measurements.
- `Status`: `accepted`, `rejected`, or `inconclusive`.
- `AcceptedClaim`: performance claim text when accepted.
- `Reason`: reviewer-visible reason for rejected or inconclusive decisions.
- `ContextOnlyEvidence`: reuse, snapshot, or deterministic evidence that cannot support the claim.

**Validation rules**:

- Accepted timing requires same-profile proof and parity acceptance plus valid measurements.
- Rejected or inconclusive timing records no accepted performance claim.
- Context-only evidence must be labeled and cannot unlock a performance benefit.

## Fallback Decision

**Purpose**: Recorded reason full redraw remains active for a scenario, host, proof set, timing
claim, compatibility issue, or readiness state.

**Fields**:

- `StatusId`: stable fallback identity.
- `Scope`: proof, parity, timing, readiness, compatibility, or package validation.
- `FallbackMode`: full redraw, disabled, blocked, skipped, or lower-tier redraw.
- `Reason`: missing proof, failed proof, stale proof, environment-limited proof, host mismatch,
  proof-method mismatch, missing parity, failed parity, timing rejected, or compatibility block.
- `UserMessage`: consumer-visible explanation.
- `Diagnostics`: maintainer detail.

**Validation rules**:

- Every unsafe, unsupported, rejected, or unproven path records a reason.
- Fallback decisions are not counted as accepted partial-redraw artifacts.

## Compatibility Record

**Purpose**: Public or package-facing impact record for diagnostics, fallback behavior, readiness
status, package surfaces, docs, or release claims.

**Fields**:

- `RecordId`: stable compatibility entry identity.
- `Surface`: public API, diagnostics, readiness, package, docs, or behavior.
- `ChangeKind`: added, changed, removed, unchanged, or intentional limitation.
- `ValidationEvidence`: tests, FSI transcript, package check, or surface baseline path.
- `MigrationNote`: required consumer note when behavior changes.

**Validation rules**:

- Accepted readiness is blocked by undocumented public drift.
- Tier 1 public deltas require `.fsi` updates, semantic tests, surface baselines, compatibility
  notes, and package validation.

## P7 Readiness Summary

**Purpose**: Single review entry point for the final P7 live partial-redraw readiness verdict.

**Fields**:

- `SummaryId`: Feature 154 readiness identity.
- `ProofSetDecision`: proof-set status, selected attempts, host profile, proof method, and
  artifact links.
- `ParityResults`: same-profile parity corpus verdicts.
- `TimingDecision`: accepted, rejected, or inconclusive performance claim decision.
- `FallbackStatus`: current fallback state for partial redraw.
- `AcceptedHostProfile`: host profile accepted for proof and parity, when applicable.
- `CompatibilityRecords`: consumer-visible impact and validation evidence.
- `UnsupportedHostEvidence`: unsupported-host regression run and zero-accepted-artifact statement.
- `RegressionEvidence`: adjacent Feature 153, layout, render-anywhere, text-shaping, overlay,
  package, and public-surface validation.
- `Status`: `accepted`, `failed`, `environment-limited`, or `fallback-gated`.
- `RemainingLimitations`: explicit unresolved host, timing, compatibility, or environment limits.

**Validation rules**:

- Partial redraw is accepted only when proof-set acceptance and same-profile parity acceptance are
  both present and current.
- Performance claim status is reported separately from safety readiness.
- The summary must name selected proof attempts, artifact paths, fallback state, compatibility
  impact, timing status, and remaining limitations.
