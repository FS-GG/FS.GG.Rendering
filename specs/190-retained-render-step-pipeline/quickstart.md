# Quickstart: Validating the `RetainedRender.step` Pipeline Decomposition

Runnable validation that the four-stage decomposition is byte-identical, independently testable, and
within the perf budget. Run from the repo root. GL-dependent suites need an X11 display
(`DISPLAY=:1`). This is a **validation guide**, not implementation — stage bodies and tests are built
in the implementation phase (`/speckit-tasks` → `/speckit-implement`).

## Prerequisites

- .NET SDK with `net10.0`; SkiaSharp/GL available; `DISPLAY=:1` exported for GL suites.
- Local NuGet feed at `~/.local/share/nuget-local/` (pack output location).
- A clean working tree on `190-retained-render-step-pipeline`.

## Step 0 — Capture the baseline BEFORE any production edit (FR-012)

Everything downstream diffs against this. Capture on the **pre-change** `step`:

```bash
# Public-surface snapshot (must stay identical → no version bump, SC-006)
dotnet fsi scripts/refresh-surface-baselines.fsx        # then `git diff` must be empty later
git stash list   # (or copy readiness/surface-baselines/FS.GG.UI.Controls.txt aside)

# Full test-matrix red set (the pre-change baseline, SC-005)
dotnet test FS.GG.Rendering.sln -c Release 2>&1 | tee /tmp/baseline-tests.log

# Per-frame perf/responsiveness lanes (features 160/161/167/173) — record alloc count + frame time
dotnet test tests/Elmish.Tests -c Release --filter "Feature167|Feature160|Feature161|Feature173" \
  2>&1 | tee /tmp/baseline-perf.log

# Scene / golden-hash corpus fingerprints (the byte-identity reference, SC-002)
dotnet test tests/Controls.Tests -c Release --filter "Retained|hashScene|Feature174" \
  2>&1 | tee /tmp/baseline-hashes.log
```

Keep `/tmp/baseline-*.log` and the surface snapshot — they are the gate's reference.

## Step 1 — Compile probe (research R6, before US1 edits)

Prove the stage signatures + the `Internal/CompositorPolicy.fs` relocation compile with **no
back-edge** (FR-009), using stub stage bodies:

```bash
dotnet build src/Controls/Controls.fsproj -c Debug   # must succeed; fixes the stage grouping
```

A back-edge or a >250-line residual stage here triggers the R3(a) type-re-home fallback for that stage.

## Step 2 — US1: stages are a composition, byte-identical (SC-001/SC-002)

```bash
# Per-stage isolation unit tests (FR-003/SC-003)
dotnet test tests/Controls.Tests -c Release --filter "Feature190" 2>&1 | tee /tmp/stage-units.log

# Byte-identity: corpus scenes + hashScene fingerprints vs the Step-0 reference
dotnet test tests/Controls.Tests -c Release --filter "Retained|hashScene|Feature174"
diff <(grep -E 'hash=' /tmp/baseline-hashes.log) <(grep -E 'hash=' /tmp/stage-units.log) || \
  echo "HASH DELTA — must be reviewed + recorded as an approved golden-hash delta (FR-005), never silent"

# Trace parity (FR-008): every retained-step-* span still emitted. The trace-parity test lives in
# Feature190StagePipelineTests.fs (T013); its Expecto label must contain "trace" so this filter selects it.
FS_GG_RENDER_LAG_TRACE=1 dotnet test tests/Controls.Tests -c Release --filter "Feature190.*trace" \
  2>&1 | grep -c 'event=retained-step-'   # count must match the pre-change span set
```

**Expected**: stage unit tests green; zero hash delta (or a reviewed/recorded delta); trace span set
unchanged. See `contracts/stage-contracts.md` C-DIFF…C-TRACE for the per-stage assertions.

## Step 3 — US3: the regression gate catches an injected regression (SC-008)

```bash
# Negative control: perturb step (reorder an accumulation OR drop a damage box), expect RED.
# The gate tests live in Feature190GateTests.fs (T008/T024); the "Feature190Gate" filter selects them.
dotnet test tests/Controls.Tests -c Release --filter "Feature190Gate"   # must FAIL on the perturbation
git checkout -- src/Controls/RetainedRender.fs                          # revert the perturbation
dotnet test tests/Controls.Tests -c Release --filter "Feature190Gate"   # now GREEN on the real code
```

## Step 4 — US3: perf budget within margin (SC-004)

```bash
DISPLAY=:1 dotnet test tests/Elmish.Tests -c Release --filter "Feature167|Feature160|Feature161|Feature173" \
  2>&1 | tee /tmp/post-perf.log
# Compare alloc count + frame time per scenario against /tmp/baseline-perf.log; within the agreed margin → pass.
```

## Step 5 — US2 (conditional): init convergence nets a reduction (SC-007)

```bash
# Cold-start byte-identity: init's RetainedInit (scene/bounds/identities/seeded caches/metrics) vs baseline
dotnet test tests/Controls.Tests -c Release --filter "Retained.*Init|Feature092"
# Line-count check: converged init must DROP net lines vs the parallel copy; else drop US2 (FR-016)
git diff --stat src/Controls/RetainedRender.fs
```

If convergence does not net a real reduction, **drop US2** and record the decision (FR-007/FR-016).

## Step 6 — Surface drift + full matrix (SC-005/SC-006)

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --exit-code readiness/surface-baselines/FS.GG.UI.Controls.txt   # MUST be empty → no bump
dotnet test FS.GG.Rendering.sln -c Release 2>&1 | tee /tmp/post-tests.log
# Red set in /tmp/post-tests.log must equal /tmp/baseline-tests.log (no new failures)
```

## Done when

- [ ] `step` is a 4-stage composition; no stage body > ≈250 lines; no resulting file > ≈1,500 lines (SC-001).
- [ ] Corpus scenes + `hashScene` byte-identical, or 100% of deltas reviewed+recorded (SC-002).
- [ ] Each stage has ≥1 passing isolation unit test (SC-003).
- [ ] Perf lanes within margin (SC-004); red set unchanged (SC-005); surface diff empty / bump if not (SC-006).
- [ ] US2 shipped with a net reduction, or dropped-and-recorded (SC-007).
- [ ] Gate goes RED on an injected regression and GREEN on the real decomposition (SC-008).
