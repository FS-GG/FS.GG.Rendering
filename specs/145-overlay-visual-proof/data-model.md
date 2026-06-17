# Data Model: Overlay Visual Proof

## OverlayVisualProofScenario

Representative overlay flow selected for real visual proof.

**Fields**
- `ScenarioId`: stable identifier, e.g. Feature 144 date-picker reference flow.
- `InputSequence`: product-visible steps that open, interact with, and dismiss the overlay.
- `OpenStateStep`: step expected to show transient content above covered content.
- `ClosedStateStep`: final step expected to show no transient content.
- `ExpectedTopmostHitTarget`: hit target associated with the open surface.
- `ExpectedFocusState`: focus owner or documented no-focus state for each captured step.
- `ExpectedDispatchSummary`: product-visible open, selection, close, and focus requests.

**Validation**
- Scenario id and evidence labels remain stable across repeated capable-host runs.
- The selected scenario must be tied to existing Feature 144 behavioral evidence.
- The scenario must include at least one open state and one final closed state.

## HostCapabilityResult

Outcome of probing whether the current host may claim real visual proof.

**Fields**
- `EffectiveBackend`: display backend such as X11, Wayland, or no display.
- `Display`: display identifier when present.
- `GlRenderer`: reported GL renderer when present.
- `CaptureAvailability`: whether the harness can capture a live/offscreen artifact.
- `Status`: capable, unsupported, or failed.
- `Owner`: owner for limitation or failure follow-up.
- `Cause`: missing display, missing GL renderer, capture failure, or other classified cause.
- `NextProofPath`: concrete command/environment path for closing the caveat.

**Validation**
- Visual proof can pass only when status is capable and required artifacts are accepted.
- Unsupported status must not include a screenshot path or claim `provesScreenshot=true`.
- Failed status must name the blocked stage and diagnostic category.

## VisualProofRun

One validation execution for the selected scenario.

**Fields**
- `RunId`
- `ScenarioId`
- `HostCapability`
- `Status`: passed, failed, or environment-limited.
- `OpenArtifact`
- `ClosedArtifact`
- `Correlation`
- `FailureCategory`: environment, capture, overlay-behavior, evidence-bookkeeping, or none.
- `ReadinessDecision`

**Validation**
- Passed runs require accepted open and closed artifacts plus accepted correlation.
- Environment-limited runs preserve the caveat and contain no accepted real artifact claim.
- Failed runs distinguish capture, overlay behavior, and bookkeeping errors.

## VisualArtifact

Human-inspectable file captured for one scenario state.

**Fields**
- `ArtifactId`
- `Path`: readiness-relative artifact path.
- `State`: open or closed.
- `Width`
- `Height`
- `PixelContentValidation`: non-blank, blank, unreadable, or invalid.
- `CaptureSource`: live viewer window/offscreen host source.
- `RunId`
- `ScenarioId`
- `CreatedAt`

**Validation**
- Path must stay inside the current feature readiness artifact tree.
- File must exist, decode, have positive dimensions, and be non-blank when required.
- Run id and scenario id must match the current validation execution.
- Stale paths from previous runs are rejected.

## OverlayVisualCorrelation

Metadata connecting pixels to deterministic overlay behavior.

**Fields**
- `ScenarioId`
- `InputStep`
- `ExpectedOverlayState`
- `TopmostHitTarget`
- `FocusState`
- `ProductDispatchSummary`
- `ReplayLogReference`
- `BehavioralEvidenceReference`
- `ArtifactPath`

**Validation**
- Open-state artifact must name the expected open surface and topmost hit target.
- Closed-state artifact must show no stale visible surface and no stale hit target.
- Mismatches between artifact state and behavioral evidence fail readiness.

## UnsupportedHostLimitation

Disclosure record when real proof cannot be produced in the current environment.

**Fields**
- `Owner`
- `Cause`
- `HostFacts`
- `NextProofPath`
- `TrustRationale`
- `NotAuthoritativeFor`

**Validation**
- Must state that deterministic behavior evidence is separate from visual proof.
- Must not carry synthetic, placeholder, or deterministic-log-only artifacts as real proof.
- Must keep the Feature 144 caveat open.

## ReadinessCaveatDecision

Final readiness statement for the Feature 144 visual-proof caveat.

**Fields**
- `Caveat`: Feature 144 overlay visual proof.
- `Decision`: closed, environment-gated, or failed.
- `ArtifactPaths`
- `LimitationDetails`
- `NextWorkstreamGuidance`
- `ReviewedAt`

**Validation**
- Closed requires a passed capable-host run and linked artifacts.
- Environment-gated requires unsupported-host limitation details and a next proof path.
- Failed requires a diagnostic category and does not authorize later workstreams to treat the caveat as closed.
