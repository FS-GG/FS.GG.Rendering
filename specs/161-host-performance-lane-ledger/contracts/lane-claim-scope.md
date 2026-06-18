# Contract: Lane Claim Scope

## Scope

This contract defines how readiness states the host lane covered by any compositor performance
claim and which lanes are not covered.

## Claim Scope Fields

The readiness summary records:

- Performance claim status.
- Accepted lane id when available.
- Host profile id.
- Display server.
- Display identity.
- Renderer identity.
- Direct rendering status.
- Driver identity.
- Refresh behavior.
- Package version set.
- Prior gate status.
- Non-generalized lanes.
- Remaining blockers.
- Supporting ledger entries.

## Required Non-Generalization Statement

When the current X11 direct OpenGL AMD/Mesa lane is accepted, the summary must state that this
evidence does not generalize to:

- Wayland.
- Indirect GL.
- Missing display.
- Software rasterization.
- Virtualized or ambiguous presentation.
- Unknown renderer or driver facts.
- Any lane with a different package version set, scenario definition, timing policy, or host
  profile.

Other lanes may be listed as future or contextual lanes only when they have separate ledger
entries.

## Claim Acceptance Rules

`performance-accepted` is allowed only when all are true:

- Same-profile timing is not noisy.
- Feature 159 reuse and promotion counters are net-positive.
- Feature 160 throughput is accepted.
- Feature 161 lane facts are complete and scoped for the claimed lane.
- Full validation, compatibility, package, and regression evidence are current and passing.

Otherwise the claim status is `performance-not-accepted`.

## Blocker Reporting

When the claim is not accepted, readiness must list all applicable blockers:

- Noisy timing.
- Missing or incomplete lane facts.
- Cross-lane evidence.
- Missing or non-positive Feature 159 reuse/promotion counters.
- Missing or rejected Feature 160 throughput.
- Unsupported or environment-limited host.
- Missing, failing, interrupted, stale, or undocumented full validation.
- Undocumented compatibility or package drift.

## Acceptance Tests

- Complete lane facts with noisy timing produce `performance-not-accepted` and list noisy timing as
  a blocker.
- Complete lane facts with missing Feature 160 throughput produce `performance-not-accepted` and
  list throughput as a blocker.
- Cross-lane evidence never produces an accepted claim scope.
- Accepted current-lane evidence includes the non-generalization statement.
