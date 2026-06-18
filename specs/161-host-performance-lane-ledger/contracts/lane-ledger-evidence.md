# Contract: Lane Ledger Evidence

## Scope

This contract defines reviewer-visible evidence for host performance lanes, host fact completeness,
cross-lane rejection, prior-gate linkage, artifact locations, and lane status.

## Ledger Entry

Each ledger entry records:

- Entry id and timing run id.
- Command and options.
- Lane id and policy id.
- Host profile.
- Display server and display identity.
- Renderer identity and direct rendering status.
- Driver identity.
- Refresh rate or reason unavailable.
- Package version set.
- CPU/GPU load notes.
- Known environment limits.
- Scenario identity.
- Timing policy identity.
- Collection time.
- Prior gate links.
- Inclusion status.
- Primary exclusion reason when not accepted.
- Artifact paths.
- Diagnostics.

Accepted entries must link every field required by the feature spec and data model.

## Inclusion Rules

A ledger entry is accepted as lane-scoped evidence only when all are true:

- Lane id is `host-ledger`.
- Policy id is `host-lane-ledger-v1`.
- Host facts are complete and tied to the timing run identity.
- Package version set matches the evidence being scoped.
- Scenario and timing policy identity match the timing artifacts.
- The lane is not mixed with another display server, renderer, direct-rendering mode, driver,
  package version, host profile, scenario definition, timing policy, or run identity.
- Artifact paths are present, readable, and inside the requested readiness tree.

## Exclusion Rules

An entry is excluded from lane-scoped performance acceptance when any of these are present:

- Missing display, unknown renderer, indirect rendering, software rasterization, virtualized or
  ambiguous presentation, or unsupported host.
- Missing, ambiguous, contradictory, stale, unreadable, duplicated, or outside-readiness host fact
  artifacts.
- Different host profile, display server, renderer identity, direct-rendering mode, driver string,
  package version, run id, scenario definition, or timing policy.
- Missing package version set, load notes, collection time, scenario identity, timing policy
  identity, inclusion status, exclusion reason, or artifact paths.
- Timing remains noisy, non-representative, or blocked by prior P7 gates.

Excluded records must use exactly one primary reason token and may include secondary diagnostics.

## Reason Tokens

- `missing-display`
- `indirect-rendering`
- `software-raster`
- `unknown-renderer`
- `virtualized-presentation`
- `ambiguous-gpu`
- `refresh-rate-unavailable`
- `package-version-mismatch`
- `load-non-representative`
- `host-facts-missing`
- `host-facts-contradictory`
- `cross-lane-evidence`
- `stale-evidence`
- `noisy-timing`
- `prior-gate-blocked`
- `artifact-unreadable`

## Lane Status

Readiness uses these lane status tokens:

- `accepted`
- `rejected`
- `fallback-only`
- `environment-limited`
- `blocked`

`accepted` means host-lane facts are complete and scoped for the claimed lane. It does not by
itself mean the shipped compositor performance claim is accepted.

## Performance Claim Boundary

The ledger must state `performance-not-accepted` unless same-profile timing is not noisy,
Feature 159 reuse/promotion counters are net-positive, Feature 160 throughput is accepted, and
Feature 161 host-lane facts are complete for the claimed lane.
