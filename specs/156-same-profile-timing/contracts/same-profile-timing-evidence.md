# Contract: Same-Profile Timing Evidence

## Scope

This contract defines the evidence required to compare full-redraw and damage-scoped redraw on one
accepted host profile. It covers timing evidence only; Feature 155 proof and parity remain the
correctness baseline.

## Required Evidence

Accepted Feature 156 timing evidence requires:

- Feature 155 accepted profile `probe-08a47c01`.
- Feature 155 proof set and same-profile parity references.
- Policy id `same-profile-live-threshold-v2`.
- At least five required scenarios:
  - `timing/localized-update`
  - `timing/no-change`
  - `timing/movement-old-new`
  - `timing/overlap`
  - `timing/edge-clipping`
- At least five measured repetitions per path per scenario after warmup.
- Full-redraw and damage-scoped distributions for each scenario.
- p50, p95, p99, sample count, warmup count, noise band, confidence decision, and artifact paths.
- Readback/validation overhead disclosure.

## Policy

For each scenario:

```text
noise-band-ms = max(0.25, full-redraw-p50-ms * 0.05)
```

A scenario is `positive` only when:

- `full-redraw-p50-ms - damage-scoped-p50-ms >= noise-band-ms`
- `full-redraw-p95-ms - damage-scoped-p95-ms >= noise-band-ms`
- `damage-scoped-p99-ms <= full-redraw-p99-ms + noise-band-ms`

Inside-band results are `noisy`. Damage-scoped results that are slower or effectively equivalent
are `non-beneficial`.

## Rejection Rules

Evidence cannot support a positive timing decision when any of these are present:

- Different host profiles.
- Different display environments.
- Different renderer identities.
- Different package versions.
- Different scenario definitions.
- Different run identities.
- Missing or unreadable raw samples.
- Duplicate or stale artifact paths.
- Fewer than five measured repetitions after warmup for either path.
- Fewer than five required scenarios.
- Noisy distributions.
- Damage-scoped path slower than or equivalent to full redraw.
- Unsupported or unavailable presentation environment.
- Proof readback or validation overhead included in the measured path without clear disclosure.

## Verdicts

- `positive`: Complete, same-profile, comparable evidence is positive outside the noise band.
- `noisy`: Complete evidence falls inside the declared noise band.
- `non-beneficial`: Damage-scoped timing is slower or equivalent.
- `incomplete`: Required samples, scenarios, fields, or artifacts are missing.
- `rejected`: Evidence is stale, duplicated, unreadable, mixed, or otherwise invalid.
- `environment-limited`: Host cannot collect comparable timing evidence.
- `limited`: Measurement is present but cannot support a claim because overhead or path scope is
  not isolated enough.

## Claim Boundary

Feature 156 can report a positive timing verdict for the measured profile, but the shipped P7
performance claim remains `performance-not-accepted` until the later report-defined gates pass on
the same host profile.
