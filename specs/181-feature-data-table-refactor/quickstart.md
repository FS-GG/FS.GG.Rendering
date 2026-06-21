# Quickstart — Validation Guide (Per-Feature Data-Table Refactor)

**Feature**: 181 | **Date**: 2026-06-21

This is the run/validation guide. It proves the refactor changed **no observable output**. The acceptance
gate is a **regenerate-and-diff** of harness artifacts + command output, plus an unchanged test red/green
set. Contracts: [feature-descriptor](./contracts/feature-descriptor.md),
[generic-renderer](./contracts/generic-renderer.md), [command-table](./contracts/command-table.md).

## Prerequisites

- .NET `net10.0` SDK; build from `FS.GG.Rendering.slnx`.
- Linux desktop with `DISPLAY=:1` (SkiaSharp/GL) for the full test sweep.
- A clean working tree on `181-feature-data-table-refactor`.

## Step 0 — Capture the pre-edit baseline (BEFORE any change)

```bash
mkdir -p specs/181-feature-data-table-refactor/readiness/baseline

# 0a. Full test sweep (records the allowed pre-existing reds as baseline-not-regression)
dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/181-feature-data-table-refactor/readiness/baseline/tests.md

# 0b. Regenerate every feature's readiness artifacts into a throwaway tree and snapshot them,
#     capturing stdout/stderr/exit per command. Run the per-feature command matrix below
#     (compositor-readiness / compositor-performance / compositor-damage … --feature NNN)
#     for each of: 148 149 152 153 154 155 156 157 158 159 160 161
#     redirecting --out to a baseline path and tee-ing stdout+stderr+`echo $?` per command.
```

Archive the generated `specs/###-*/readiness/**` and the per-command `stdout/stderr/exit` capture under
`readiness/baseline/`. This is the byte oracle for every later step.

> Expected baseline (per feature 180 evidence): full sweep is green except the known pre-existing reds
> (`tests/Package.Tests`, `samples/ControlsGallery` stale-feed). Record that set; it is not a regression.

## Step 1 — Build & test after each story

```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release
```

Expected: build green; the **same** red/green set as `readiness/baseline/tests.md` — **no new failures**.

## Step 2 — Byte-stability diff (the acceptance gate)

After each story, regenerate the artifacts into `readiness/post-change/` exactly as in Step 0b, then:

```bash
diff -r specs/181-feature-data-table-refactor/readiness/baseline \
        specs/181-feature-data-table-refactor/readiness/post-change
```

Expected: **empty diff** — every readiness artifact and every command's stdout/stderr/exit code is
byte-identical (FR-003, SC-002, SC-004). A non-empty diff means the collapse changed output → revert that
family to explicit form (FR-007); it is out of scope to change output.

## Step 3 — Per-story acceptance

**US1 — descriptor + generic renderer**
```bash
# renderer-function count drops to the retained-divergent set:
grep -c 'let renderFeature' tools/Rendering.Harness/Compositor.fs   # << pre-refactor count
# constants are derived, not hand-declared:
grep -c 'ReadinessDirectory\|ParityDirectory\|TimingDirectory' tools/Rendering.Harness/Compositor.fs  # sharply down
```
- Add a *hypothetical* 13th descriptor entry locally and confirm it renders all its standard variants
  through the generic path with **zero** new `renderFeature…` functions (SC-001). Revert the probe.
- Byte diff (Step 2) clean.

**US2 — descriptor-keyed CLI table**
- For every feature NNN and every per-feature command, `diff` of captured stdout/stderr/exit is empty
  (SC-002). `isFeature###` predicates and `if/elif` chains are gone from `Cli.fs`.

**US3 — data-driven compatibility tests**
- `tests/Package.Tests/Feature###Compatibility*Tests.fs` files are deleted; one `testList` over the
  catalog remains. Every feature previously covered is still asserted (SC-004); test pass/fail set
  unchanged vs baseline.

## Step 4 — SC-005 net-line measurement (gated, do not skip)

```bash
# Record net source-line delta across touched files vs baseline (per family where collapsed):
git diff --stat main -- tools/Rendering.Harness/ tests/Package.Tests/
```

Expected: net change is **not a regression attributable to abstraction overhead**. If any family's
collapse increased lines, that family must be left explicit and the exclusion recorded (FR-007) — mirroring
the Phase-3 (180) SC-005 finding. Record the per-family decisions in the plan's Implementation Outcome.

## Done when

- [ ] Build green; test red/green set identical to baseline (SC-006).
- [ ] `diff -r` of regenerated `readiness/**` + command stdout/stderr/exit is empty (SC-002/SC-004).
- [ ] One `FeatureDescriptor` catalog is the source of truth for renderer, CLI table, and tests (SC-001).
- [ ] Surviving `renderFeature…` functions = retained-divergent set only (SC-003).
- [ ] Per-feature compatibility test files = 0 (SC-004).
- [ ] Net-line delta measured; no family collapsed at a net line cost; FR-007 retentions recorded (SC-005).
- [ ] No `FS.GG.UI.*` surface baseline changed (FR-008).
