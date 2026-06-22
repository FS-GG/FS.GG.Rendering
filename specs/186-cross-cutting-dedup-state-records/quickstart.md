# Quickstart — Validating the Cross-Cutting Dedup + State Records

This is a **byte-identical-by-construction** Pattern-C refactor. Validation = prove rendered frames,
per-frame metrics, and emitted artifacts match a pre-refactor baseline (byte-identical, or
semantically equivalent only where prior wording legitimately differed), the test sweep lands at the
**same** red/green set, and the **public surface is unchanged**. See
`contracts/internal-contracts.md` for the invariants and `data-model.md` for the entities.

## Prerequisites

- .NET `net10.0` SDK; repo builds at HEAD; X11 display available for GL (`DISPLAY=:1`).
- Run from repo root: `/home/developer/projects/FS.GG.Rendering`.
- Test command (default local tier): `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release`.

## Step 0 — Capture the pre-refactor baseline (Foundational, before any production edit)

```bash
mkdir -p /tmp/186-baseline
# 0a. Record the red/green test set across the 4 affected projects (SC-004 reference)
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release > /tmp/186-baseline/test-sweep.txt 2>&1 || true
# 0b. Snapshot the public surface (must stay byte-identical — SC-006)
cp -r readiness/surface-baselines /tmp/186-baseline/surface-baselines
git stash list > /dev/null; git diff --stat -- '**/*.fsi' > /tmp/186-baseline/fsi-clean.txt   # expect empty
# 0c. Snapshot emitted readiness/inspection/evidence artifacts for the affected features
#     (visual readiness, visual inspection summary, retained inspection summary, readiness metrics)
cp -r specs/16[45]-* specs/170-* /tmp/186-baseline/ 2>/dev/null || true
```

Also record the rendered-frame / per-frame-metrics reference the Controls/Elmish suites assert
against (these suites already encode the byte-level expectation; the baseline test-sweep captures
their pass/fail). The byte-identity gate is: **these same suites stay green with unchanged
expectations** after each story.

## Step 1 — US1: FrameMetrics built once (P1)

```bash
DISPLAY=:1 dotnet test tests/Elmish.Tests tests/Controls.Tests -c Release
# Confirm exactly one full 32-field construction remains:
grep -nE "ProductModelChanged\s*=" src/Controls.Elmish/ControlsElmish.fs   # the field appears in 1 full builder
```

**Expected**: builds clean; the 32-field record is spelled at **1** builder site, the 2 former full
sites (`1423–1460`, `1957–1990`) delegate to it (C-METRICS-ONE-SITE); metrics tests pass with
byte-identical emitted metrics (US1-AS2); `ControlsElmish.fsi` unchanged (`git diff` empty).

## Step 2 — US2: explicit named frame state (P2)

```bash
DISPLAY=:1 dotnet test tests/Controls.Tests tests/Elmish.Tests -c Release
# 0 loose accumulator/carrier mutables remain in the migrated regions (SC-002):
sed -n '1455,1900p' src/Controls/RetainedRender.fs | grep -c 'let mutable'      # → 0 for migrated accumulators
sed -n '1835,1900p' src/Controls.Elmish/ControlsElmish.fs | grep -c 'let mutable' # → 0 for migrated carriers
```

**Expected**: `step`'s 19 accumulators live in one `FrameState` with the **same update order**
(C-STEP-STATE); `init` seeds onto the same record with byte-identical cold-start (US2-AS2);
`runScriptCore`'s 7 metric carriers live in `FrameScriptState` (C-SCRIPT-STATE); retained-render +
metrics suites pass with byte-identical frames + metrics (US2-AS4); both `.fsi` files unchanged.

## Step 3 — US3: inspection validation written once (P3)

```bash
DISPLAY=:1 dotnet test tests/Testing.Tests -c Release
```

**Expected**: one shared `internal` validation routine backs both `validateCheck` functions
(C-VALIDATION-ONE-DEF); Feature165 (visual) + Feature170 (retained) suites pass with identical
red/green; a `Warning`-severity retained finding is handled exactly as before and derives
`ReviewRequired`, while the visual path still rejects/omits `Warning` (US3-AS2); public
`validateCheck` signatures unchanged (only a new `internal` helper added to `TestingVisual.fsi`).

## Step 4 — US4: one managed-section updater (P4)

```bash
DISPLAY=:1 dotnet test tests/Testing.Tests -c Release
# Re-emit the affected summary artifacts and byte-compare against baseline:
diff -r /tmp/186-baseline/170-* specs/170-* 2>&1 | head    # expect no diff where logic was identical
```

**Expected**: one shared `internal` `ManagedSection` helper backs all three `updateManagedSection`
writers (C-SECTION-ONE-DEF); `(0,0)`→append, `(1,1)`→replace, and the **fail-loud** branch on
duplicate/imbalanced markers all behave as before (US4-AS2/3/4); re-emitted summary artifacts
byte-identical.

## Step 5 — Final acceptance

```bash
mkdir -p /tmp/186-after
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release > /tmp/186-after/test-sweep.txt 2>&1 || true
# Same red/green set as baseline (SC-004):
diff <(grep -E 'Passed!|Failed!|error' /tmp/186-baseline/test-sweep.txt) \
     <(grep -E 'Passed!|Failed!|error' /tmp/186-after/test-sweep.txt)
# Public surface byte-identical (SC-006):
git diff --stat -- '**/*.fsi'                                   # only NEW internal lines in TestingVisual.fsi, if any
diff -r /tmp/186-baseline/surface-baselines readiness/surface-baselines   # expect no diff
# No new project/dependency (FR-010):
git diff -- '**/*.fsproj' 'FS.GG.Rendering.slnx'                # no new ProjectReference/PackageReference
```

**Expected (success criteria)**: same red/green set as baseline, no assertion weakened (SC-004);
rendered frames + per-frame metrics byte-identical, artifacts byte/semantic-equivalent (SC-005);
metrics built at 1 site (SC-001), 0 loose migrated mutables (SC-002), validation + section
algorithms each defined once (SC-003); public surface baseline diff empty + no version bump (SC-006);
adding one metric field is a single-builder edit (SC-007 — walk through, no commit required).
