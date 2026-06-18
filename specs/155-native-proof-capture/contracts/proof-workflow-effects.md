# Contract: Proof Workflow Effects

## Scope

This contract defines the MVU/effect boundary for native proof capture. The pure workflow owns
state transitions; the interpreter owns display, graphics, readback, filesystem, timing, and
timeout effects.

## Required Workflow

```text
init
  -> DetectProfile
  -> PresentSentinelFrame
  -> PresentDamageFrame
  -> ObservePixels
  -> EvaluateArtifactQuality
  -> WriteAttemptArtifacts
  -> CompleteAttempt
```

## Pure Transition Rules

- `DetectProfile` success advances to sentinel presentation.
- Sentinel presentation success advances to damage presentation.
- Damage presentation success advances to pixel observation.
- Pixel observation success advances to artifact-quality evaluation.
- Artifact-quality success advances to artifact writing.
- Artifact writing success completes the attempt.
- Any failure message completes the attempt as failed or environment-limited with a visible reason.

## Interpreter Rules

- Native effects execute only at the edge.
- Interpreter failures return messages; they do not throw away workflow diagnostics.
- Timeouts and denied permissions fail closed.
- Artifact writes include enough metadata for reviewers to reproduce the decision.
- Unsupported-host interpretation records zero accepted partial-redraw artifacts.

## Test Expectations

- Transition tests cover every successful phase and every failure phase.
- Interpreter tests on the capable host produce real artifacts.
- Synthetic tests remain rejection-only and carry explicit disclosure.
