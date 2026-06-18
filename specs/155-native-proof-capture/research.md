# Research: Native Proof Capture

## Decision: Treat the host as capable when display, renderer, readback permission, and timeout checks pass

**Rationale**: The current workspace reports a reachable X11 display, direct OpenGL rendering, an
AMD Mesa renderer, Present/DRI3, and a stable refresh source. The Feature 154 environment-limited
artifact was produced by intentionally unsetting display variables for unsupported-host validation.

**Alternatives considered**:

- Continue recording environment-limited output from deterministic harness commands. Rejected
  because it cannot finish P7 on a capable host.
- Require an external host. Rejected because this host already satisfies the relevant capability
  facts and should be the acceptance target.

## Decision: Reuse Feature 154 acceptance rules unchanged

**Rationale**: Feature 154 already defines exact-three selected attempts, freshness, host/profile
matching, artifact quality, same-profile parity, and separate timing decisions. Changing those
rules would create a second P7 acceptance vocabulary.

**Alternatives considered**:

- Add a separate Feature 155 proof status. Rejected because it would confuse readiness consumers.
- Accept one high-quality proof attempt. Rejected because the report and Feature 154 require three
  fresh matching attempts.

## Decision: Keep native capture behind an MVU/effect interpreter

**Rationale**: Native capture is stateful I/O: display probing, presentation, pixel observation,
artifact writing, and timeout/failure handling. The constitution requires pure transition state and
edge interpretation for this shape of workflow.

**Alternatives considered**:

- Put capture directly inside the CLI. Rejected because it hides failure modes and makes transition
  testing weak.
- Use only synthetic tests. Rejected because synthetic evidence cannot satisfy P7 acceptance.

## Decision: Separate correctness readiness from performance claims

**Rationale**: P7 can accept live partial-redraw correctness for one host profile when proof and
same-profile parity pass. A performance claim still requires comparable timing evidence and may be
accepted, rejected, or explicitly unclaimed.

**Alternatives considered**:

- Block P7 correctness on a positive timing benefit. Rejected because correctness readiness and
  performance claims are separate gates in Feature 154.
- Accept performance from proof artifacts. Rejected because proof artifacts are not timing evidence.

## Decision: Preserve unsupported-host validation as a separate regression package

**Rationale**: Accepted capable-host evidence must not weaken fail-closed behavior for missing
display, missing renderer, denied readback, timeouts, or synthetic-only evidence.

**Alternatives considered**:

- Replace unsupported-host output with the accepted proof set. Rejected because it would hide
  environment-specific limitations.
