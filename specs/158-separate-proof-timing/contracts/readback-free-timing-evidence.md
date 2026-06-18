# Contract: Readback-Free Timing Evidence

## Scope

This contract defines the evidence required to prove that accepted timing samples exclude
validation readback from the measured interval. It covers timing eligibility only; correctness
proof remains owned by proof/readback artifacts.

## Measurement Window

For each timing sample, the measured interval is the declared render/present interval for one path
and one scenario. The interval must not include screenshot capture, pixel readback, proof readback,
probe readback, PNG encoding, proof artifact validation, or any synchronous validation step whose
purpose is correctness proof rather than render/present timing.

Readback may occur before or after the measured interval only when the sample records
`readback-outside-measurement` and the related artifact is classified as proof or probe evidence.

## Required Fields

Each sample artifact must include:

- Sample id and index.
- Scenario id and scenario definition identity.
- Path: `full-redraw` or `damage-scoped`.
- Run id.
- Host profile id.
- Package and harness version.
- Measurement policy.
- Inclusion status: `included`, `excluded`, or `probe`.
- Exclusion reason when not included.
- Duration in milliseconds.
- Raw artifact path.

Each scenario report must include:

- Included sample count per path.
- Excluded sample count per reason.
- Warmup count.
- Required measured repetitions.
- Distribution fields retained from Feature 156.
- Proof/probe artifact links when present.

## Inclusion Rules

A sample is included only when all are true:

- Policy is `readback-free` or `readback-outside-measurement`.
- Host profile, renderer, display environment, package version, scenario definition, and run id
  match the accepted run.
- Duration is finite and non-negative.
- Raw artifact path is present and readable.
- Sample is not from proof or probe mode.

## Exclusion Rules

Evidence is excluded from performance acceptance when any of these are present:

- `probe-readback-included` policy.
- Missing, ambiguous, or unverifiable policy metadata.
- Readback, screenshot capture, or proof validation inside the measured interval.
- Different host profile, display environment, renderer identity, package version, run id, or
  scenario definition.
- Unsupported or unavailable presentation environment.
- Failed proof readback.
- Missing, duplicate, stale, or unreadable artifact paths.

## Verdicts

- `accepted`: Required scenarios have enough included readback-free samples and all exclusions are
  disclosed.
- `rejected`: Contaminated or invalid evidence prevents acceptance.
- `fallback-only`: Proof/probe evidence exists, but no accepted readback-free timing set exists.
- `environment-limited`: Host cannot collect comparable timing or proof/probe evidence.

## Claim Boundary

Feature 158 can accept measurement separation for a measured profile, but the shipped compositor
performance claim remains `performance-not-accepted` until all later report-defined performance
gates are satisfied.
