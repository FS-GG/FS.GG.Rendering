# Data Model: Compositor Proof Interpreter

## Host Profile

**Purpose**: Stable identity of the live presentation environment used to decide whether attempts
belong to the same capable host.

**Fields**:

- `ProfileId`: deterministic host identity.
- `Backend`: SkiaViewer/OpenGL backend identifier.
- `Renderer`: renderer, vendor, driver, and version facts when available.
- `PresentMode`: direct swapchain, offscreen readback, simulated, or unsupported.
- `FramebufferSize`: pixel dimensions used by proof.
- `Scale`: effective scale factor.
- `DisplayEnvironment`: X11, Wayland, headless, missing display, CI, or unknown.
- `ProofMethod`: proof algorithm name and version.
- `PackageVersion`: package or harness build identity.

**Validation rules**:

- Accepted proof sets require matching host profile and proof method.
- Missing display, renderer, readback, permission, or context facts produce
  `environment-limited`, not `accepted`.
- Host, scale, framebuffer, backend, package, or proof-method drift rejects cached acceptance.

## Proof Method

**Purpose**: Versioned method used to create and evaluate sentinel and damage-scoped frame
evidence.

**Fields**:

- `MethodId`: stable method name, for example `sentinel-damage-v1`.
- `SentinelFrameRole`: role name for the baseline frame.
- `DamageFrameRole`: role name for the scoped frame.
- `DamageRegion`: expected changed rectangle.
- `UndamagedSamplePlan`: sample locations or identities outside the damage region.
- `AlgorithmVersion`: explicit version string used in matching.

**Validation rules**:

- Attempts using different proof methods cannot belong to the same accepted proof set.
- Method drift records a fallback-gated reason.

## Live Proof Attempt

**Purpose**: One host-backed attempt to prove that a presentation host can update damaged pixels
while preserving undamaged pixels.

**Fields**:

- `AttemptId`: stable current-run identity.
- `HostProfile`: host profile recorded during the run.
- `ProofMethod`: proof method used by the run.
- `StartedAt` and `CompletedAt`: timestamps used for freshness.
- `SentinelFrame`: sentinel frame artifact.
- `DamageFrame`: damage frame artifact.
- `DamagedSamples`: observed samples inside the damage region.
- `UndamagedSamples`: observed samples outside the damage region.
- `ArtifactQuality`: artifact quality decision for all required proof artifacts.
- `Classification`: `accepted`, `failed`, or `environment-limited`.
- `Reason`: reviewer-visible reason for failed or limited verdicts.
- `Diagnostics`: maintainer-oriented details.

**Validation rules**:

- `accepted` requires damaged samples to match the damage draw and undamaged samples to retain
  sentinel identity.
- Missing, stale, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, or
  quality-failed evidence cannot accept an attempt.
- Unsupported hosts record zero accepted partial-redraw artifacts.

**State transitions**:

```text
initialized -> profile-detected -> sentinel-presented -> damage-presented
            -> samples-observed -> quality-evaluated
            -> accepted | failed | environment-limited
```

## Frame Artifact

**Purpose**: Reviewable image, readback, or metadata evidence attached to a proof attempt.

**Fields**:

- `ArtifactId`: stable artifact identity.
- `AttemptId`: owning attempt.
- `Role`: sentinel frame, damage frame, sample readback, proof summary, limitation, or metadata.
- `Path`: repository-relative path when persisted.
- `ContentIdentity`: checksum, hash, or sample identity used for freshness.
- `Width` and `Height`: captured artifact dimensions when applicable.
- `Decodable`: whether content can be inspected.
- `Blank`: whether the artifact is blank.
- `Synthetic`: whether it was produced by a simulation.
- `CreatedAt`: artifact timestamp.
- `Diagnostics`: artifact quality notes.

**Validation rules**:

- Accepted attempts require required artifacts to exist, decode, and be non-blank, non-stale, and
  non-synthetic.
- Limitation artifacts may support `environment-limited` but cannot satisfy acceptance.
- Artifact paths in readiness summaries must stay inside the feature readiness tree.

## Pixel Observation

**Purpose**: Sample-level comparison used to decide whether frame content is correct.

**Fields**:

- `ObservationId`: stable sample identity.
- `RegionId`: damaged or undamaged region identity.
- `Kind`: damaged or undamaged.
- `ExpectedIdentity`: expected sample color, hash, or content token.
- `ActualIdentity`: observed sample color, hash, or content token.
- `Matched`: whether actual equals expected for the region role.
- `Diagnostics`: mismatch detail.

**Validation rules**:

- Damaged observations must show the expected updated content in the damage frame.
- Undamaged observations must retain the sentinel identity in the damage frame.
- Unsupported or missing observations fail the attempt closed.

## Artifact Quality Decision

**Purpose**: Aggregate pass/fail decision over the required artifacts for one attempt.

**Fields**:

- `Present`: all required artifacts exist.
- `Decodable`: all required artifacts can be decoded or parsed.
- `NonBlank`: image/readback artifacts are not blank.
- `Fresh`: artifacts belong to the current attempt and freshness window.
- `Synthetic`: any required artifact is synthetic.
- `Reason`: first blocking quality reason when not accepted.

**Validation rules**:

- Accepted quality requires present, decodable, non-blank, fresh, and non-synthetic artifacts.
- Any failed flag prevents attempt acceptance.

## Proof Set Decision

**Purpose**: Aggregate decision over the required three matching live attempts.

**Fields**:

- `ProofSetId`: deterministic identity for the selected attempts.
- `HostProfile`: shared host profile.
- `ProofMethod`: shared proof method.
- `Attempts`: exactly three attempts selected for the decision.
- `FreshnessWindow`: policy used to decide freshness.
- `Status`: `accepted`, `fallback-gated`, `failed`, or `environment-limited`.
- `Reasons`: blocking or acceptance reasons.
- `AcceptedAt`: timestamp when accepted, when applicable.
- `Diagnostics`: reviewer-visible decision details.

**Validation rules**:

- Accepted proof sets require exactly three accepted attempts.
- All selected attempts must be fresh, host-matching, proof-method-matching, and artifact-quality
  accepted.
- Failed or limited attempts prevent acceptance and record visible reasons.
- Extra attempts may be linked as context but are not silently folded into the accepted set.

**State transitions**:

```text
empty -> collecting -> accepted
                  \-> fallback-gated
                  \-> failed
                  \-> environment-limited
```

## Fallback Status

**Purpose**: Recorded reason partial redraw remains gated or the host remains non-accepting.

**Fields**:

- `StatusId`: stable identity.
- `Scope`: attempt, proof set, readiness, parity, timing, or compatibility.
- `FallbackMode`: full redraw, disabled, blocked, skipped, or lower-tier redraw.
- `Reason`: missing proof, stale proof, failed proof, environment-limited proof, host mismatch,
  proof-method mismatch, invalid artifact, synthetic artifact, missing parity, or timing pending.
- `UserMessage`: consumer-visible explanation.
- `Diagnostics`: maintainer detail.

**Validation rules**:

- Every unsafe or unsupported path records a reason.
- Fallback decisions are not counted as accepted partial-redraw artifacts.

## Readiness Summary

**Purpose**: Single review entry point for live proof readiness.

**Fields**:

- `SummaryId`: feature readiness identity.
- `ProofSetDecision`: proof-set status and selected attempts.
- `Attempts`: linked attempt records.
- `UnsupportedHostEvidence`: unsupported-host run and reason.
- `FallbackStatus`: current fallback decision.
- `CompatibilityNotes`: public or package-visible impact.
- `RemainingGates`: parity and timing gates still required.
- `Status`: accepted proof set, failed proof set, environment-limited, or fallback-gated.

**Validation rules**:

- Summary must state whether partial redraw remains fallback-gated.
- Summary must state that performance claims remain unaccepted until later timing evidence.
- Accepted proof readiness requires an accepted proof set and still names parity and timing as
  separate decisions.
