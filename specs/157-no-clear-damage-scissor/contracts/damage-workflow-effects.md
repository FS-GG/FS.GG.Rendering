# Contract: Damage Workflow Effects

## Model

The workflow model records:

- Run id.
- Active host profile.
- Proof gate status.
- Scenario queue.
- Retained frame state.
- Damage validation result.
- Attempt records.
- Fallback records.
- Published artifacts.
- Final status.
- Diagnostics.

## Messages

- `HostProfileDetected`
- `HostProfileRejected`
- `ProofGateAccepted`
- `ProofGateRejected`
- `RetainedBackingAvailable`
- `RetainedBackingRejected`
- `DamageValidated`
- `DamageRejected`
- `DamageFrameRendered`
- `FullRedrawFallbackRendered`
- `ParityAccepted`
- `ParityRejected`
- `UnsupportedHostRecorded`
- `ArtifactPublished`
- `DiagnosticRecorded`

## Effects

- `DetectHostProfile`
- `LoadAcceptedProofGate`
- `PrepareScenario`
- `ValidateDamage`
- `CaptureRetainedBacking`
- `RenderDamageScopedFrame`
- `RenderFullRedrawFrame`
- `CompareParity`
- `WriteAttemptArtifact`
- `WriteFallbackArtifact`
- `WriteReadinessSummary`

## Edge Interpreter Rules

- Native window, GL, Skia, readback, and filesystem work happens only in the interpreter.
- Pure update functions only transition model state and emit effects.
- Any effect failure returns a message that records a fallback reason.
- Synthetic messages may be used only for rejection tests and must carry `Synthetic` in the test
  name when used.

## Terminal States

- `accepted`: required accepted attempts and scenario coverage are complete.
- `fallback-only`: the real path never became eligible, but full redraw remained safe.
- `rejected`: an attempted damage-scoped frame failed parity or safety validation.
- `environment-limited`: host or presentation environment prevented live validation.

## Observability

Every transition that rejects or falls back must record:

- Reason category.
- Host profile if available.
- Scenario id if available.
- Frame id if available.
- Artifact path if written.
- Human-readable diagnostic.
