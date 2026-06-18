# Contract: Native Proof Capture

## Scope

This contract defines the observable capable-host proof-capture run for Feature 155. It consumes
the Feature 154 proof-set rules and supplies the real native attempt artifacts that Feature 154 did
not produce.

## Required Behavior

- Detect the current host profile and classify it as capable only when display, renderer, readback,
  permission, and timeout checks pass.
- Execute three selected proof attempts in one current run.
- For each attempt, produce sentinel and damage artifacts, pixel observations, artifact quality,
  host profile, proof method, verdict, and diagnostics.
- Evaluate the selected attempts with the existing exact-three proof-set acceptance rule.
- Write a proof-set summary that names the selected attempts and host profile.

## Accepted Attempt Rules

An accepted attempt requires:

- Fresh current-run sentinel and damage artifacts.
- Decodable and non-blank artifact content.
- Non-synthetic artifact quality.
- Damaged-pixel update inside the damage region.
- Undamaged-pixel preservation outside the damage region.
- Matching host profile and proof method.

## Rejection Rules

The run fails closed for missing display, missing renderer, readback unavailable, permission
denied, timeout, missing artifacts, stale artifacts, blank artifacts, undecodable artifacts,
synthetic-only artifacts, failed pixels, incomplete artifacts, host mismatch, proof-method
mismatch, or artifact-write failure.

## Required Output

The capable-host output under `readiness/live-proof/attempts/` records:

- One directory or summary per selected attempt.
- `sentinel-frame.png` and `damage-frame.png` or equivalent stable artifact names.
- Attempt proof metadata.
- Artifact quality.
- Observations for damaged and undamaged regions.
- Host profile and proof method.
- Reviewer-visible diagnostics.

The aggregate output under `readiness/proof-set.md` records proof-set status, selected attempts,
host profile, proof method, accepted timestamp, and blocking reasons when not accepted.
