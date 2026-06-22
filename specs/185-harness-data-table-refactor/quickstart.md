# Quickstart — Validating the Harness Data-Table Refactor

This is a behavior-preserving refactor. Validation = prove every emitted artifact is **semantically
equivalent** to a pre-refactor baseline, CI-grepped literals are **byte-identical**, and the test
sweep lands at the same red/green set. See `contracts/harness-internal-contracts.md` for the
guarantees and `data-model.md` for the entities.

## Prerequisites

- .NET `net10.0` SDK; repo builds at HEAD.
- Run from repo root: `/home/developer/projects/FS.GG.Rendering`.

## Step 0 — Capture the pre-refactor baseline (Foundational, before any edit)

```bash
# Build the harness and run each feature's readiness so artifacts are fresh
dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release
# Snapshot every emitted artifact for features 148–161 into a baseline corpus
mkdir -p /tmp/185-baseline
cp -r specs/148-* specs/149-* specs/15[2-9]-* specs/16[0-1]-* /tmp/185-baseline/ 2>/dev/null || true
# Record the current red/green test set (SC-004 reference)
dotnet test -c Release > /tmp/185-baseline/test-sweep.txt 2>&1 || true
```

Also record, in the baseline notes, the **fixed CI-grepped path/header literals** that must stay
byte-identical (FR-008) — the directory strings under `readiness/` and the required report headers.

## Step 1 — US1: SSOT proven (directories/headers from descriptors)

```bash
dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release
dotnet test tests/Rendering.Harness.Tests -c Release
```

**Expected**: builds clean; readiness artifacts land at the **same paths** with the **same headers**
(US1-AS1); no standalone `*ReadinessDirectory` `let` remains (`grep -cE 'ReadinessDirectory' …
Compositor.fs` → 0, SC-003); a duplicate-alias or unknown-id is caught at build/first use (FR-011).

## Step 2 — US2: parametric renderer artifact-equivalent

Re-emit artifacts and semantic-diff against the baseline:

```bash
dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release
# Re-run readiness for each feature, then compare parsed structure to /tmp/185-baseline
```

**Expected**: a feature with `{ValidationSummary; Timing; Parity}` emits exactly those three reports,
semantically identical to before (US2-AS1); divergent features render via `Renderers` hooks, not
top-level functions (`grep -cE '^\s*let\s+renderFeature' … Compositor*.fs` → 0, SC-003); the two
feature-number-dispatch renderers now key on `Id` (US2-AS3); no `Compositor*.fs` file > ~1,500 lines
(SC-001).

## Step 3 — US3: CLI contract unchanged

```bash
# For each alias form, confirm identical artifacts + exit code
dotnet run --project tools/Rendering.Harness -c Release -- 156
dotnet run --project tools/Rendering.Harness -c Release -- feature156
dotnet run --project tools/Rendering.Harness -c Release -- 156-same-profile-timing
# Unknown alias must error the same way (non-zero, same message shape)
dotnet run --project tools/Rendering.Harness -c Release -- feature999 ; echo "exit=$?"
```

**Expected**: every alias dispatches through `runReadiness`, same files/exit code (US3-AS1); unknown
alias errors as before (US3-AS2); no per-feature handler functions remain (SC-002/SC-006).

## Step 4 — US4: lane decomposition equivalent

```bash
dotnet test tests/Rendering.Harness.Tests -c Release
```

**Expected**: normal-lane `LaneResult` matches baseline (US4-AS1); a timed-out lane is reported as a
timeout with logic isolated in `TimeoutManager` (US4-AS2).

## Step 5 — Final acceptance

```bash
dotnet test -c Release > /tmp/185-after/test-sweep.txt 2>&1 || true
diff <(grep -E 'Passed!|Failed!' /tmp/185-baseline/test-sweep.txt) \
     <(grep -E 'Passed!|Failed!' /tmp/185-after/test-sweep.txt)
```

**Expected (success criteria)**: same red/green set as baseline, known pre-existing reds unchanged
(SC-004); every artifact semantically equivalent + CI-grepped literals byte-identical (SC-005); CLI
contract unchanged (SC-006); no `tools/Rendering.Harness/` file > ~1,500 lines (SC-001); 0
`renderFeature*` functions + 0 `*ReadinessDirectory` constants (SC-003); adding a sample feature is a
single descriptor row (SC-002 — walk through adding one row and confirm it compiles + gets a CLI
command with no new handler).
