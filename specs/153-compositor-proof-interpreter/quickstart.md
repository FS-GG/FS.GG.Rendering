# Quickstart: Compositor Proof Interpreter

## Prerequisites

- .NET SDK capable of building the repository target framework `net10.0`.
- Repository restore already completed, or network/package cache available for restore.
- For capable-host proof: an OpenGL presentation host with usable display/readback. X11/Xvfb is
  the expected local capable-host path for existing harness work.
- For unsupported-host validation: a shell where display variables can be unset.

## Build

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: the solution builds without public-surface or package reference drift.

## Focused Semantic and Unit Validation

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature153 --no-build
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature153 --no-build
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature153 --no-build
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature153 --no-build
```

Expected outcome:

- Proof-attempt state and classifier tests pass.
- Synthetic-named rejection tests prove fail-closed behavior without accepting proof.
- Harness formatting tests link attempts, proof-set status, unsupported-host evidence, fallback
  status, compatibility, package validation, and regression validation.
- Public helper and package tests pass when public surface changes are present.

## Unsupported-Host Proof Run

```bash
env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-live-proof --feature 153 \
  --out specs/153-compositor-proof-interpreter/readiness/live-proof/unsupported
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
  compositor-live-proof --feature 153 \
  --attempt-count 3 \
  --out specs/153-compositor-proof-interpreter/readiness/live-proof/attempts
```

Expected outcome on a capable preserving host:

- Three attempt summaries are written.
- Each attempt records host profile, proof method, sentinel artifact, damage artifact, artifact
  quality, freshness, damaged samples, undamaged samples, and classification.
- Accepted attempts include decodable, non-blank, non-synthetic, fresh sentinel and damage
  artifacts.
- Damaged samples show expected damaged-pixel updates.
- Undamaged samples preserve sentinel identity.

Expected outcome on a non-preserving or unreliable host:

- Attempt classification is `failed` or `environment-limited`.
- The proof set is not accepted.
- The summary includes a specific reviewer-visible reason.

## Proof-Set and Readiness Publication

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-readiness --feature 153 \
  --out specs/153-compositor-proof-interpreter/readiness
```

Expected outcome:

- `proof-set.md` records `accepted`, `fallback-gated`, `failed`, or `environment-limited`.
- Accepted proof sets name exactly three selected matching attempts.
- `validation-summary.md` links attempts, proof-set status, unsupported-host behavior,
  compatibility impact, package/regression evidence, and remaining gates.
- The summary explicitly states that partial redraw remains fallback-gated until same-profile
  parity passes and that performance claims remain unaccepted until later timing evidence passes.

## Broad Regression Check

```bash
dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected outcome: existing deterministic compositor diagnostics, Feature 152 proof-set rules,
layout acceptance, render-anywhere behavior, text-shaping behavior, overlay behavior, package
checks, and public-surface drift checks remain valid or explicitly limited with compatibility
notes.
