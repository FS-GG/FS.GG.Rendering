# Quickstart — Validate "disclosed test reds cleared"

Runnable validation that proves the four conditions are fixed and the baseline is green and
deterministic. See [contracts/baseline-green.contract.md](./contracts/baseline-green.contract.md) for
the guarantees and [data-model.md](./data-model.md) for the entities. Implementation details belong in
`tasks.md`, not here.

## Prerequisites

- .NET SDK `net10.0` (observed `10.0.301`).
- Local NuGet feed at `~/.local/share/nuget-local/`.
- Run all commands from the repository root.
- GL/display **optional** — without it, GL-sensitive tests must report explicit skips, not failures.

## Step 0 — Establish the confirmed before-state (Foundational early run)

Capture the reds and the real numbers **before** any fix, to confirm/replace the root-cause hypotheses
(plan standing assumption):

```bash
dotnet fsi scripts/baseline-tests.fsx | tee specs/203-fix-disclosed-test-reds/readiness/baseline-before.md
```

Expect (per feature 202 disclosure): `tests/Package.Tests` 8 failures (7× Feature128 + 1× Feature163);
`AntShowcase.Tests`, `ControlsGallery.Tests`, `SecondAntShowcase.Tests` red on pin/count drift;
`tests/SkiaViewer.Tests` intermittent GL failures. Record the **actual catalog count**, the
**ControlsGallery true value**, the **97th control's identity**, and the **exact flaky test names**.

## Step 1 — US1: refresh pins + feed coherently

```bash
# Packs current sources into the local feed and rewrites every sample pin to the source version.
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --mode refresh --pack \
  --sample samples/AntShowcase --sample samples/SampleApps \
  --sample samples/SecondAntShowcase --sample samples/ControlsGallery

# Verify pin coherence (Feature163, extended to all four samples) + per-sample pin checks.
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature163
```

**Expected**: `package-feed status: passed`; zero "pin does not match source-controlled version".

## Step 2 — US2: design-system gate self-provisions from a clean state

```bash
rm -rf specs/128-design-system-template-param/readiness    # simulate fresh checkout (gitignored)
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature128
```

**Expected**: GV-1..GV-7 pass with the report produced by the gate's own setup (verdict-core), ANT
record `overall=PASS`, **not** red-by-default. (Full live proof remains opt-in:
`FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx`.)

## Step 3 — US3: sample suites pass with assertions intact

```bash
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj
dotnet test samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj
dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj
```

**Expected**: 100% pass; counts equal the **true current value**; `Unreferenced` and
`MissingContractOrReason` genuinely empty (97th control placed + classified); no assertion loosened or
deleted (verify the diff still uses `Expect.equal` / `Set.equal`, not `>`/`>=`).

## Step 4 — US4: SkiaViewer determinism (5×)

```bash
for i in 1 2 3 4 5; do
  echo "=== run $i ==="
  dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj \
    | tee specs/203-fix-disclosed-test-reds/readiness/skiaviewer-run-$i.md \
    | grep -E "Passed!|Failed!|skipped|Skipped"
done
```

**Expected**: identical pass set every run; GL-sensitive cases either pass or report an explicit
`SKIPPED(... Constitution VI)`; **0** tests that flip pass↔fail. Note the skip count.

## Step 5 — Whole-baseline green + determinism

```bash
dotnet fsi scripts/baseline-tests.fsx | tee specs/203-fix-disclosed-test-reds/readiness/baseline-after.md
dotnet fsi scripts/baseline-tests.fsx | tee specs/203-fix-disclosed-test-reds/readiness/baseline-after-2.md
diff <(grep -E "PASS|FAIL|SKIP" specs/203-fix-disclosed-test-reds/readiness/baseline-after.md) \
     <(grep -E "PASS|FAIL|SKIP" specs/203-fix-disclosed-test-reds/readiness/baseline-after-2.md)
```

**Expected (success criteria)**:
- **SC-001**: 0 red projects.
- **SC-002**: `tests/Package.Tests` 100% pass (8 disclosed reds gone), no assertion weakened.
- **SC-003**: each previously-red sample suite 100% pass.
- **SC-004**: identical pass set across runs (the `diff` is empty).
- **SC-005**: any residue is an explicit skip with stated count; 0 intermittent reds.
- **SC-006**: the feature-202 pre-existing-red + flaky disclosures no longer apply.
