# Quickstart: No-Clear Damage-Scissored Render Path

## Prerequisites

- .NET SDK for `net10.0`.
- Restored repository dependencies.
- A capable X11/OpenGL presentation environment for accepted live attempts.
- Feature 155 accepted proof artifacts available under `specs/155-native-proof-capture/readiness/`.

Unsupported or unavailable presentation environments are valid validation inputs, but they must
produce `environment-limited` evidence with zero accepted partial-redraw artifacts.

## 1. Build the focused projects

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: build succeeds. If public `.fsi` surface changes, surface baselines and package
compatibility tests must be updated before readiness is accepted.

## 2. Run focused SkiaViewer tests

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature157 --no-restore
```

Expected outcome: tests cover eligibility, retained backing, damage validation, fallback reasons,
resource failure, parity mismatch, and no-clear/scissor decision behavior.

## 3. Run focused harness tests

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature157 --no-restore
```

Expected outcome: tests cover `compositor-damage --feature 157`, scenario inventory, accepted
attempt rendering, fallback rendering, unsupported-host output, and readiness summary generation.

## 4. Collect capable-host damage evidence

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-damage --feature 157 \
  --attempt-count 3 \
  --out specs/157-no-clear-damage-scissor/readiness/damage
```

Expected outcome on the accepted host profile:

- At least three fresh accepted attempts.
- At least five representative scenarios.
- Preserved-pixel evidence for untouched regions.
- Damaged-pixel evidence for damage regions.
- Parity evidence against full redraw.
- `damage/summary.md` and `damage/summary.json` written.

If the host profile does not match Feature 155, the command must fallback or record
`environment-limited` with zero accepted partial-redraw artifacts.

## 5. Collect unsupported-host evidence

Run the damage command with display variables unset or in the repository's unsupported-host harness
mode once implementation defines that mode.

Expected outcome:

- Status is `environment-limited`.
- Full redraw remains the safe fallback.
- Accepted partial-redraw artifact count is zero.
- Output is written under `readiness/damage/unsupported/`.

## 6. Assemble readiness

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- \
  compositor-readiness --feature 157 \
  --out specs/157-no-clear-damage-scissor/readiness
```

Expected outcome:

- `validation-summary.md` links accepted attempts, fallbacks, unsupported-host evidence, parity,
  compatibility, package validation, regression validation, and final claim status.
- Final status is one of `accepted`, `fallback-only`, `rejected`, or `environment-limited`.
- Shipped performance claim remains `performance-not-accepted` unless later report-defined gates
  are also complete.

## 7. Run package and regression checks

```bash
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature157 --no-restore
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature157 --no-restore
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature155|Feature156|Feature157" --no-restore
dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected outcome: focused Feature 155, Feature 156, Feature 157, package, and broad regression
checks pass or record explicit environment/tooling limitations in readiness.
