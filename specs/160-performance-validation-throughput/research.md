# Research: Performance Validation Throughput

## Decision: Add a focused throughput lane to the existing performance command

**Rationale**: Features 156 and 158 already use `compositor-performance` for same-profile timing
evidence. Feature 160 should extend that command with `--feature 160 --lane focused` and policy
`focused-throughput-v1` rather than introducing a new command family. This keeps scenario routing,
host-profile detection, timing metadata, unsupported-host handling, and readiness rendering aligned
with the existing P7 performance evidence path while making the focused lane explicit.

**Alternatives considered**:

- Add a new `compositor-throughput` command: rejected because it would duplicate timing command
  parsing and make comparison with Feature 158 harder.
- Reuse Feature 158 unchanged: rejected because it does not record iteration bounds, repeated
  throughput status, or full-validation separation.
- Hide the lane behind test filters only: rejected because readiness reviewers need durable command
  and artifact contracts, not just local test output.

## Decision: Preserve Feature 158 scenario and sample policy for accepted iterations

**Rationale**: The feature's speedup comes from avoiding broad release validation during repeated
timing loops, not from silently narrowing the evidence. Accepted Feature 160 iterations therefore
cover the five Feature 158 timing scenarios:

- `timing/localized-update`
- `timing/no-change`
- `timing/movement-old-new`
- `timing/overlap`
- `timing/edge-clipping`

Each accepted iteration keeps warmup `3`, measured repetitions `5` per path per scenario, and
readback-free or readback-outside-measurement policy metadata. This preserves comparability with
prior accepted measurement-separation evidence.

**Alternatives considered**:

- Reduce repetitions to make the lane faster: rejected because lower sample counts would require a
  new comparability explanation and could hide noise.
- Run only one or two timing scenarios per iteration: rejected because missing scenario categories
  must prevent acceptance.
- Include Feature 159 promotion scenarios in every timing iteration: rejected because Feature 159
  reuse/promotion evidence is a preservation gate for readiness, while Feature 160's focused lane
  measures timing throughput.

## Decision: Use a 10 minute per-iteration bound and a 2 minute unsupported-host bound

**Rationale**: The specification's success criteria require three same-profile focused iterations
under 10 minutes and unsupported-host validation under 2 minutes. The bound is declared before
acceptance evidence is collected, recorded in each iteration artifact, and enforced by the harness.
Timeout, cancellation, or partial output is written as excluded evidence with zero accepted samples.

**Alternatives considered**:

- Use a soft target without enforcement: rejected because reviewers could not distinguish accepted
  throughput from merely fast local runs.
- Derive the bound from current broad-suite duration: rejected because Feature 160 needs a stable,
  reviewer-visible acceptance rule independent of broad-suite wall time.
- Stop writing artifacts on timeout: rejected because excluded evidence must explain why an
  iteration was not accepted.

## Decision: Require at least three fresh same-profile iterations

**Rationale**: Three same-profile iterations match the prior P7 pattern for accepted live evidence
and prove repeatability of the focused lane. Accepted iterations must share host profile
`probe-08a47c01` unless a later accepted profile replaces it, run identity, policy id, package and
harness version, scenario definitions, sample policy, and artifact metadata. Cross-profile, stale,
mixed-policy, partial, missing-metadata, unsupported-host, or environment-limited iterations cannot
contribute to accepted throughput.

**Alternatives considered**:

- Accept a single fast iteration: rejected because it would not prove repeated throughput.
- Accept cross-profile iterations with warnings: rejected because P7 timing comparability is
  profile-scoped.
- Accept stale Feature 158 timing artifacts: rejected because Feature 160 is about fresh bounded
  focused iterations.

## Decision: Keep full solution validation outside the focused loop but mandatory for release

**Rationale**: Feature 160 exists to make repeated timing work faster, not to weaken release
readiness. The focused command must not invoke the broad release suite as part of each iteration.
Readiness records full validation separately under `readiness/full-validation/`; missing, failing,
interrupted, stale, or undocumented full validation blocks release-ready status even when focused
throughput is accepted.

**Alternatives considered**:

- Run broad validation after every focused iteration: rejected because it defeats the throughput
  goal.
- Let focused throughput replace broad validation: rejected by the feature requirements and
  constitution evidence expectations.
- Treat broad validation as optional closeout context: rejected because full solution validation
  remains the release gate.

## Decision: Accept throughput separately from the shipped performance claim

**Rationale**: Feature 160 can prove that timing validation is bounded and repeatable, but the
report-defined shipped compositor performance claim also needs non-noisy same-profile timing,
Feature 159 net-positive reuse/promotion evidence, and Feature 161 host-lane scoping. The readiness
summary therefore has independent fields for throughput status, full-validation status, and shipped
performance claim status. Unless every report-defined gate is complete and positive, the claim
stays `performance-not-accepted`.

**Alternatives considered**:

- Accept performance when throughput passes: rejected because throughput is about validation loop
  duration, not a speedup result.
- Block Feature 160 until Feature 161 is complete: rejected because the report names throughput as
  a separate follow-up gate.
- Omit final claim status from throughput readiness: rejected because reviewers need to see the
  boundary between faster validation and product performance acceptance.
