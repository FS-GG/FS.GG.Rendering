# Research: Separate Proof Readback From Timing

## Decision: Use `compositor-performance --feature 158` for readback-free timing

**Rationale**: Feature 156 already established the same-profile full-redraw versus damage-scoped
timing lane. Feature 158 changes the measurement contract, not the scenario domain. A new
`--feature 158` mode keeps the old Feature 156 result available as the noisy baseline while making
readback-free policy, probe exclusion, and contamination rejection explicit.

**Alternatives considered**:

- Mutate Feature 156 output in place. Rejected because reviewers need to compare old noisy timing
  with the new readback-free lane.
- Add a new unrelated benchmark command. Rejected because it would duplicate scenario/profile
  rules and increase the risk of incomparable evidence.

## Decision: Declare policy `readback-free-timing-v1`

**Rationale**: Accepted timing samples need a stable policy id so readiness can reject missing,
ambiguous, or stale metadata. The policy accepts only samples whose validation readback is absent
from the measured interval or happens outside the measured interval with explicit proof/probe
classification.

**Alternatives considered**:

- Reuse Feature 156's `same-profile-live-threshold-v2` as the only policy. Rejected because that
  policy classifies timing distributions, not measurement-window readback.
- Store a free-form note. Rejected because readiness needs machine-checkable inclusion and
  exclusion reasons.

## Decision: Keep proof readback on explicit proof/probe paths

**Rationale**: Correctness proof remains the safety gate. Screenshot/readback artifacts must still
exist for proof and intentionally requested probes, but those samples cannot enter the performance
timing set. This preserves Feature 155/157 correctness evidence while preventing proof cost from
polluting performance measurement.

**Alternatives considered**:

- Remove readback from all Feature 158 runs. Rejected because the feature must not weaken proof.
- Allow readback probes to count if they are fast. Rejected because any readback-included sample
  changes what the timing lane measures.

## Decision: Treat sample inclusion as a first-class readiness decision

**Rationale**: A reviewer must see which timing samples were included, which samples were excluded,
and why. Exclusion reasons need stable tokens for probe runs, readback contamination, missing
policy, unverifiable policy, cross-profile evidence, unsupported host, failed proof readback, and
scenario or package mismatch.

**Alternatives considered**:

- Drop excluded samples from summaries. Rejected because silent omission hides contamination and
  makes old/new timing comparisons hard to audit.
- Record only one aggregate verdict. Rejected because the spec requires sample-level policy,
  inclusion status, exclusion reason, and artifact location.

## Decision: Reuse the Feature 156 representative scenario set

**Rationale**: Feature 158 is meaningful only if reviewers can compare it to the prior timing lane.
The required scenarios remain `timing/localized-update`, `timing/no-change`,
`timing/movement-old-new`, `timing/overlap`, and `timing/edge-clipping`, with the same host profile
and scenario-definition identity rules.

**Alternatives considered**:

- Add new scenarios before separating readback. Rejected because scenario drift would make the
  readback-free result hard to compare to Feature 156.
- Require all P7 stress scenarios. Rejected as Feature 161 host-lane-ledger work, not this slice.

## Decision: Keep the shipped performance claim `performance-not-accepted`

**Rationale**: The report states that later gates are still required before a shipped compositor
performance claim: Feature 159 for net-positive reuse/promotion counters and Feature 161 for the
host performance lane ledger. Feature 158 can accept the measurement contract and contextualize the
previous noisy result, but it cannot claim a shipped speedup alone.

**Alternatives considered**:

- Accept performance when timing is readback-free. Rejected because readback-free measurement is
  necessary but not sufficient.
- Block Feature 158 on later gates. Rejected because measurement separation is independently
  valuable and should be reviewable before the remaining performance gates.
