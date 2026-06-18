# Quickstart: Compositor Proof Acceptance

## Prerequisites

- .NET SDK capable of building the repository target framework `net10.0`.
- Repository restore already completed, or network/package cache available for restore.
- For capable-host proof, parity, and timing: an OpenGL presentation host with usable
  display/readback. X11/Xvfb is the expected local capable-host path for existing harness work.
- For unsupported-host validation: a shell where display variables can be unset.

## Build

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: the solution builds without public-surface, package reference, or warning drift.

## Focused Semantic and Unit Validation

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature154 --no-build
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature154 --no-build
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature154 --no-build
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature154 --no-build
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature154 --no-build
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature154 --no-build
```

Expected outcome:

- Proof-set selection accepts exactly three fresh matching capable-host attempts.
- Synthetic-named rejection tests prove fail-closed behavior without accepting proof, parity, or
  timing evidence.
- Harness formatting tests link proof, parity, timing, fallback, compatibility, package
  validation, and regression validation.
- Public helper and package tests pass when public surface changes are present.

## Unsupported-Host Proof Run

```bash
env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-live-proof --feature 154 \
  --out specs/154-compositor-proof-acceptance/readiness/live-proof/unsupported
```

Expected outcome:

- Command completes in under 2 minutes.
- Result is `environment-limited` or `failed`, not `accepted`.
- Output records a specific host limitation reason.
- Output records zero accepted partial-redraw artifacts.

## Capable-Host Proof Run

Run on a host with usable OpenGL presentation and readback:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-live-proof --feature 154 \
  --attempt-count 3 \
  --out specs/154-compositor-proof-acceptance/readiness/live-proof/attempts
```

Expected outcome on a capable preserving host:

- Three selected attempt summaries are written.
- Each selected attempt records host profile, proof method, sentinel artifact, damage artifact,
  artifact quality, freshness, damaged samples, undamaged samples, and classification.
- Accepted attempts include fresh, decodable, non-blank, non-synthetic sentinel and damage
  artifacts.
- Damaged samples show expected damaged-pixel updates.
- Undamaged samples preserve sentinel identity.
- `proof-set.md` names exactly three selected attempt identities.

Expected outcome on a non-preserving or unreliable host:

- Attempt classification is `failed` or `environment-limited`.
- The proof set is not accepted.
- The summary includes a specific reviewer-visible reason.

## Same-Profile Parity Corpus

After an accepted proof set exists:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-parity --feature 154 \
  --out specs/154-compositor-proof-acceptance/readiness/parity
```

Expected outcome:

- Parity output records the same host profile and proof-set id as the accepted proof set.
- Required scenarios record verdicts for localized update, no-change, movement, overlap, edge
  clipping, resize, full invalidation, invalid damage, unsupported host, and resource failure.
- Accepted parity scenarios match the full-redraw reference.
- Non-accepted required scenarios retain full redraw with visible fallback or limitation reasons.

## Timing Decision

After accepted proof and same-profile parity:

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-timing --feature 154 \
  --tier damage \
  --scenario-count 5 \
  --repetitions 5 \
  --out specs/154-compositor-proof-acceptance/readiness/timing
```

Expected outcome:

- Timing output declares threshold and noise policy.
- Measurements cover at least five representative live scenarios and at least five comparable
  repetitions per scenario.
- Decision is `accepted`, `rejected`, or `inconclusive`.
- Rejected or inconclusive timing records no accepted performance claim.
- Snapshot, reuse, or deterministic evidence is labeled context-only unless same-profile live
  timing satisfies the policy.

## Final Readiness Publication

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-readiness --feature 154 \
  --out specs/154-compositor-proof-acceptance/readiness
```

Expected outcome:

- `validation-summary.md` states proof-set status, selected attempts, accepted host profile,
  parity status, timing status, fallback status, artifact locations, compatibility impact, and
  remaining limitations.
- `proof-set.md`, `compatibility-ledger.md`, `package-validation.md`, and
  `regression-validation.md` are present.
- Partial redraw is accepted only when proof-set acceptance and same-profile parity acceptance are
  both present and current.
- Performance claim status is reported separately as accepted, rejected, or inconclusive.

## Broad Regression Check

```bash
dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected outcome: Feature 153 interpreter behavior, Feature 152/153 readiness vocabulary, layout
acceptance, render-anywhere behavior, text-shaping behavior, overlay behavior, package checks, and
public-surface drift checks remain valid or explicitly limited with compatibility notes.
